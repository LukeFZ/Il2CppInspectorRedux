# IDA-specific implementation
import ida_kernwin
import ida_name
import ida_idaapi
import ida_typeinf
import ida_bytes
import ida_nalt
import ida_ida
import ida_ua
import ida_segment
import ida_funcs
import ida_xref

try: # 7.7+
	import ida_srclang
	IDACLANG_AVAILABLE = True
	print("IDACLANG available")
except ImportError:
	IDACLANG_AVAILABLE = False

try:
	import ida_dirtree
	FOLDERS_AVAILABLE = True
	print("folders available")
except ImportError:
	FOLDERS_AVAILABLE = False

#try:
#	from typing import TYPE_CHECKING
#	if TYPE_CHECKING:
#		from ..shared_base import BaseStatusHandler, BaseDisassemblerInterface, ScriptContext
#		import json
#		import os
#		from datetime import datetime
#except:
#	pass

TINFO_DEFINITE = 0x0001 # These only exist in idc for some reason, so we redefine it here
DEFAULT_TIL: "til_t" = None # type: ignore

class IDADisassemblerInterface(BaseDisassemblerInterface):
	supports_fake_string_segment = True

	_status: BaseStatusHandler

	_type_cache: dict
	_folders: list
	
	_function_dirtree: "ida_dirtree.dirtree_t"
	_cached_genflags: int
	_skip_function_creation: bool
	_is_32_bit: bool
	_fake_segments_base: int

	def __init__(self, status: BaseStatusHandler):
		self._status = status
		
		self._type_cache = {}
		self._folders = []

		self._cached_genflags = 0
		self._skip_function_creation = False
		self._is_32_bit = False
		self._fake_segments_base = 0

	def _get_type(self, type: str):
		if type not in self._type_cache:
			info = ida_typeinf.idc_parse_decl(DEFAULT_TIL, type, ida_typeinf.PT_RAWARGS)
			if info is None:
				print(f"Failed to create type {type}.")
				return None

			self._type_cache[type] = info[1:]

		return self._type_cache[type]

	def get_script_directory(self) -> str:
		return os.path.dirname(os.path.realpath(__file__))

	def on_start(self):
		# Disable autoanalysis 
		self._cached_genflags = ida_ida.inf_get_genflags()
		ida_ida.inf_set_genflags(self._cached_genflags & ~ida_ida.INFFL_AUTO)

		# Unload type libraries we know to cause issues - like the c++ linux one
		PLATFORMS = ["x86", "x64", "arm", "arm64"]
		PROBLEMATIC_TYPELIBS = ["gnulnx"]

		for lib in PROBLEMATIC_TYPELIBS:
			for platform in PLATFORMS:
				ida_typeinf.del_til(f"{lib}_{platform}")

		# Set name mangling to GCC 3.x and display demangled as default
		ida_ida.inf_set_demnames(ida_ida.DEMNAM_GCC3 | ida_ida.DEMNAM_NAME)

		self._status.update_step('Processing Types')

		if IDACLANG_AVAILABLE:
			header_path = os.path.join(self.get_script_directory(), "%TYPE_HEADER_RELATIVE_PATH%")
			ida_srclang.set_parser_argv("clang", "-target x86_64-pc-linux -x c++ -D_IDACLANG_=1") # -target required for 8.3+
			ida_srclang.parse_decls_with_parser("clang", None, header_path, True)
		else:
			original_macros = ida_typeinf.get_c_macros()
			ida_typeinf.set_c_macros(original_macros + ";_IDA_=1")
			ida_typeinf.idc_parse_types(os.path.join(self.get_script_directory(), "%TYPE_HEADER_RELATIVE_PATH%"), ida_typeinf.PT_FILE)
			ida_typeinf.set_c_macros(original_macros)

		# Skip make_function on Windows GameAssembly.dll files due to them predefining all functions through pdata which makes the method very slow
		self._skip_function_creation = ida_segment.get_segm_by_name(".pdata") is not None
		if self._skip_function_creation:
			print(".pdata section found, skipping function boundaries")

		if FOLDERS_AVAILABLE:
			self._function_dirtree = ida_dirtree.get_std_dirtree(ida_dirtree.DIRTREE_FUNCS)

		self._is_32_bit = ida_ida.inf_is_32bit_exactly()

	def on_finish(self):
		ida_ida.inf_set_genflags(self._cached_genflags)

	def define_function(self, address: int, end: int | None = None):
		if self._skip_function_creation:
			return

		ida_bytes.del_items(address, ida_bytes.DELIT_SIMPLE, 12) # Undefine x bytes which should hopefully be enough for the first instruction 
		ida_ua.create_insn(address) # Create instruction at start
		if not ida_funcs.add_func(address, end if end is not None else ida_idaapi.BADADDR): # This fails if the function doesn't start with an instruction
			print(f"failed to mark function {hex(address)}-{hex(end) if end is not None else '???'} as function")

	def define_data_array(self, address: int, type: str, count: int):
		self.set_data_type(address, type)

		flags = ida_bytes.get_flags(address)
		if ida_bytes.is_struct(flags):
			opinfo = ida_nalt.opinfo_t()
			ida_bytes.get_opinfo(opinfo, address, 0, flags)
			entrySize = ida_bytes.get_data_elsize(address, flags, opinfo)
			tid = opinfo.tid
		else:
			entrySize = ida_bytes.get_item_size(address)
			tid = ida_idaapi.BADADDR

		ida_bytes.create_data(address, flags, count * entrySize, tid)

	def set_data_type(self, address: int, type: str):
		type += ';'

		info = self._get_type(type)
		if info is None:
			return

		if ida_typeinf.apply_type(DEFAULT_TIL, info[0], info[1], address, TINFO_DEFINITE) is None:
			print(f"set_type({hex(address)}, {type}); failed!")

	def set_function_type(self, address: int, type: str):
		self.set_data_type(address, type)

	def set_data_comment(self, address: int, cmt: str):
		ida_bytes.set_cmt(address, cmt, False)

	def set_function_comment(self, address: int, cmt: str):
		func = ida_funcs.get_func(address)
		if func is None:
			return

		ida_funcs.set_func_cmt(func, cmt, True)

	def set_data_name(self, address: int, name: str):
		ida_name.set_name(address, name, ida_name.SN_NOWARN | ida_name.SN_NOCHECK | ida_name.SN_FORCE)

	def set_function_name(self, address: int, name: str): 
		self.set_data_name(address, name)

	def add_cross_reference(self, from_address: int, to_address: int):
		ida_xref.add_dref(from_address, to_address, ida_xref.XREF_USER | ida_xref.dr_I)

	def import_c_typedef(self, type_def: str):
		ida_typeinf.idc_parse_types(type_def, 0)

	# optional
	def add_function_to_group(self, address: int, group: str):
		if not FOLDERS_AVAILABLE or True: # enable at your own risk - this is slow
				return

		if group not in self._folders:
			self._folders.append(group)
			self._function_dirtree.mkdir(group)

		name = ida_funcs.get_func_name(address)
		self._function_dirtree.rename(name, f"{group}/{name}")

	# only required if supports_fake_string_segment == True
	def create_fake_segment(self, name: str, size: int) -> int: 
		start = ida_ida.inf_get_max_ea()
		end = start + size

		ida_segment.add_segm(0, start, end, name, "DATA")
		segment = ida_segment.get_segm_by_name(name)
		segment.bitness = 1 if self._is_32_bit else 2
		segment.perm = ida_segment.SEGPERM_READ
		segment.update()

		return start

	def write_string(self, address: int, value: str) -> int:
		encoded_string = value.encode() + b'\x00'
		string_length = len(encoded_string)
		ida_bytes.put_bytes(address, encoded_string)
		ida_bytes.create_strlit(address, string_length, ida_nalt.STRTYPE_C)
		return string_length

	def write_address(self, address: int, value: int): 
		if self._is_32_bit:
			ida_bytes.put_dword(address, value)
		else:
			ida_bytes.put_qword(address, value)

# Status handler

class IDAStatusHandler(BaseStatusHandler):
	def __init__(self):
		self.step = "Initializing"
		self.max_items = 0
		self.current_items = 0
		self.start_time = datetime.now()
		self.step_start_time = self.start_time
		self.last_updated_time = datetime.min
	
	def initialize(self):
		ida_kernwin.show_wait_box("Processing")

	def update(self):
		if self.was_cancelled():
			raise RuntimeError("Cancelled script.")

		current_time = datetime.now()
		if 0.5 > (current_time - self.last_updated_time).total_seconds():
			return

		self.last_updated_time = current_time

		step_time = current_time - self.step_start_time
		total_time = current_time - self.start_time
		message = f"""
Running IL2CPP script.
Current Step: {self.step}
Progress: {self.current_items}/{self.max_items}
Elapsed: {step_time} ({total_time})
"""

		ida_kernwin.replace_wait_box(message)

	def update_step(self, step, max_items = 0):
		print(step)

		self.step = step
		self.max_items = max_items
		self.current_items = 0
		self.step_start_time = datetime.now()
		self.last_updated_time = datetime.min
		self.update()

	def update_progress(self, new_progress = 1):
		self.current_items += new_progress
		self.update()

	def was_cancelled(self):
		return ida_kernwin.user_cancelled()

	def shutdown(self):
		ida_kernwin.hide_wait_box()

status = IDAStatusHandler()
backend = IDADisassemblerInterface(status)
context = ScriptContext(backend, status)
context.process()