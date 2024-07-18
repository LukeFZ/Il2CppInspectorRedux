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

cached_genflags = 0
skip_make_function = False
func_dirtree = None
is_32_bit = False
fake_segments_base = None

def script_prologue(status):
	global cached_genflags, skip_make_function, func_dirtree, is_32_bit, fake_segments_base
	# Disable autoanalysis 
	cached_genflags = ida_ida.inf_get_genflags()
	ida_ida.inf_set_genflags(cached_genflags & ~ida_ida.INFFL_AUTO)

	# Unload type libraries we know to cause issues - like the c++ linux one
	PLATFORMS = ["x86", "x64", "arm", "arm64"]
	PROBLEMATIC_TYPELIBS = ["gnulnx"]

	for lib in PROBLEMATIC_TYPELIBS:
		for platform in PLATFORMS:
			ida_typeinf.del_til(f"{lib}_{platform}")

	# Set name mangling to GCC 3.x and display demangled as default
	ida_ida.inf_set_demnames(ida_ida.DEMNAM_GCC3 | ida_ida.DEMNAM_NAME)

	status.update_step('Processing Types')

	if IDACLANG_AVAILABLE:
		header_path = os.path.join(get_script_directory(), "%TYPE_HEADER_RELATIVE_PATH%")
		ida_srclang.set_parser_argv("clang", "-target x86_64-pc-linux -x c++ -D_IDACLANG_=1") # -target required for 8.3+
		ida_srclang.parse_decls_with_parser("clang", None, header_path, True)
	else:
		original_macros = ida_typeinf.get_c_macros()
		ida_typeinf.set_c_macros(original_macros + ";_IDA_=1")
		ida_typeinf.idc_parse_types(os.path.join(get_script_directory(), "%TYPE_HEADER_RELATIVE_PATH%"), ida_typeinf.PT_FILE)
		ida_typeinf.set_c_macros(original_macros)

	# Skip make_function on Windows GameAssembly.dll files due to them predefining all functions through pdata which makes the method very slow
	skip_make_function = ida_segment.get_segm_by_name(".pdata") is not None
	if skip_make_function:
		print(".pdata section found, skipping function boundaries")

	if FOLDERS_AVAILABLE:
		func_dirtree = ida_dirtree.get_std_dirtree(ida_dirtree.DIRTREE_FUNCS)

	is_32_bit = ida_ida.inf_is_32bit_exactly()
		
def script_epilogue(status):
	# Reenable auto-analysis
	global cached_genflags
	ida_ida.inf_set_genflags(cached_genflags)

# Utility methods

def set_name(addr, name):
	ida_name.set_name(addr, name, ida_name.SN_NOWARN | ida_name.SN_NOCHECK | ida_name.SN_FORCE)

def make_function(start, end = None):
	global skip_make_function
	if skip_make_function:
		return

	ida_bytes.del_items(start, ida_bytes.DELIT_SIMPLE, 12) # Undefine x bytes which should hopefully be enough for the first instruction 
	ida_ua.create_insn(start) # Create instruction at start
	if not ida_funcs.add_func(start, end if end is not None else ida_idaapi.BADADDR): # This fails if the function doesn't start with an instruction
		print(f"failed to mark function {hex(start)}-{hex(end) if end is not None else '???'} as function")

TYPE_CACHE = {}

def get_type(typeName):
	if typeName not in TYPE_CACHE:
		info = ida_typeinf.idc_parse_decl(None, typeName, ida_typeinf.PT_RAWARGS)
		if info is None:
			print(f"Failed to create type {typeName}.")
			return None

		TYPE_CACHE[typeName] = info[1:]

	return TYPE_CACHE[typeName]

TINFO_DEFINITE = 0x0001 # These only exist in idc for some reason, so we redefine it here

def set_type(addr, cppType):
	cppType += ';'

	info = get_type(cppType)
	if info is None:
		return

	if ida_typeinf.apply_type(None, info[0], info[1], addr, TINFO_DEFINITE) is None:
		print(f"set_type({hex(addr)}, {cppType}); failed!")

def set_function_type(addr, sig):
	set_type(addr, sig)

def make_array(addr, numItems, cppType):
	set_type(addr, cppType)

	flags = ida_bytes.get_flags(addr)
	if ida_bytes.is_struct(flags):
		opinfo = ida_nalt.opinfo_t()
		ida_bytes.get_opcode(opinfo, addr, 0, flags)
		entrySize = ida_bytes.get_data_elsize(addr, flags, opinfo)
		tid = opinfo.tid
	else:
		entrySize = ida_bytes.get_item_size(addr)
		tid = ida_idaapi.BADADDR

	ida_bytes.create_data(addr, flags, numItems * entrySize, tid)

def define_code(code):
	ida_typeinf.idc_parse_types(code)

def set_comment(addr, comment, repeatable = True):
	ida_bytes.set_cmt(addr, comment, repeatable)

def set_header_comment(addr, comment):
	func = ida_funcs.get_func(addr)
	if func is None:
		return

	ida_funcs.set_func_cmt(func, comment, True)

def get_script_directory():
	return os.path.dirname(os.path.realpath(__file__))

folders = []
def add_function_to_group(addr, group):
	global func_dirtree, folders
	return

	if not FOLDERS_AVAILABLE:
		return

	if group not in folders:
		folders.append(group)
		func_dirtree.mkdir(group)

	name = ida_funcs.get_func_name(addr)
	func_dirtree.rename(name, f"{group}/{name}")

def add_xref(addr, to):
	ida_xref.add_dref(addr, to, ida_xref.XREF_USER | ida_xref.dr_I)

def write_string(addr, string):
	encoded_string = string.encode() + b'\x00'
	string_length = len(encoded_string)
	ida_bytes.put_bytes(addr, encoded_string)
	ida_bytes.create_strlit(addr, string_length, ida_nalt.STRTYPE_C)

def write_address(addr, value):
	global is_32_bit

	if is_32_bit:
		ida_bytes.put_dword(addr, value)
	else:
		ida_bytes.put_qword(addr, value)

def create_fake_segment(name, size):
	global is_32_bit

	start = ida_ida.inf_get_max_ea()
	end = start + size

	ida_segment.add_segm(0, start, end, name, "DATA")
	segment = ida_segment.get_segm_by_name(name)
	segment.bitness = 1 if is_32_bit else 2
	segment.perm = ida_segment.SEGPERM_READ
	segment.update()

	return start

def process_string_literals(status, data):
	total_string_length = 0
	for d in data['stringLiterals']:
		total_string_length += len(d["string"]) + 1
	
	aligned_length = total_string_length + (4096 - (total_string_length % 4096))
	segment_base = create_fake_segment(".fake_strings", aligned_length)

	current_string_address = segment_base
	for d in data['stringLiterals']:
		define_string(d)

		ref_addr = parse_address(d)
		write_string(current_string_address, d["string"])
		write_address(ref_addr, current_string_address)
		set_type(ref_addr, r'const char* const')

		current_string_address += len(d["string"]) + 1
		status.update_progress()

# Status handler

class StatusHandler(BaseStatusHandler):
	def __init__(self):
		self.step = "Initializing"
		self.max_items = 0
		self.current_items = 0
		self.start_time = datetime.datetime.now()
		self.step_start_time = self.start_time
		self.last_updated_time = datetime.datetime.min
	
	def initialize(self):
		ida_kernwin.show_wait_box("Processing")

	def update(self):
		if self.was_cancelled():
			raise RuntimeError("Cancelled script.")

		current_time = datetime.datetime.now()
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
		self.step_start_time = datetime.datetime.now()
		self.last_updated_time = datetime.datetime.min
		self.update()

	def update_progress(self, new_progress = 1):
		self.current_items += new_progress
		self.update()

	def was_cancelled(self):
		return ida_kernwin.user_cancelled()

	def close(self):
		ida_kernwin.hide_wait_box()