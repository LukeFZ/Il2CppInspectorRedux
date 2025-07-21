# Ghidra-specific implementation
from ghidra.app.cmd.function import ApplyFunctionSignatureCmd
from ghidra.app.util.cparser.C import CParserUtils
from ghidra.program.model.data import ArrayDataType
from ghidra.program.model.symbol import SourceType
from ghidra.program.model.symbol import RefType
from ghidra.app.cmd.label import DemanglerCmd
from ghidra.app.services import DataTypeManagerService
from java.lang import Long

#try:
#	from typing import TYPE_CHECKING
#	if TYPE_CHECKING:
#		from ..shared_base import BaseStatusHandler, BaseDisassemblerInterface, ScriptContext
#		import json
#		import os
#		import sys
#		from datetime import datetime
#except:
#	pass

class GhidraDisassemblerInterface(BaseDisassemblerInterface):
	supports_fake_string_segment = False

	def _to_address(self, value):
		return toAddr(Long(value))

	def get_script_directory(self) -> str: 
		return getSourceFile().getParentFile().toString()

	def on_start(self):
		self.xrefs = currentProgram.getReferenceManager()

		# Check that the user has parsed the C headers first
		if len(getDataTypes('Il2CppObject')) == 0:
			print('STOP! You must import the generated C header file (%TYPE_HEADER_RELATIVE_PATH%) before running this script.')
			print('See https://github.com/djkaty/Il2CppInspector/blob/master/README.md#adding-metadata-to-your-ghidra-workflow for instructions.')
			sys.exit()

		# Ghidra sets the image base for ELF to 0x100000 for some reason
		# https://github.com/NationalSecurityAgency/ghidra/issues/1020
		# Make sure that the base address is 0
		# Without this, Ghidra may not analyze the binary correctly and you will just waste your time
		# If 0 doesn't work for you, replace it with the base address from the output of the CLI or GUI
		if currentProgram.getExecutableFormat().endswith('(ELF)'):
			currentProgram.setImageBase(self._to_address(0), True)
		
		# Don't trigger decompiler
		setAnalysisOption(currentProgram, "Call Convention ID", "false")

	def on_finish(self):
		pass

	def define_function(self, address: int, end: int | None = None):
		address = self._to_address(address)
		# Don't override existing functions
		fn = getFunctionAt(address)
		if fn is None:
			# Create new function if none exists
			createFunction(address, None)

	def define_data_array(self, address: int, type: str, count: int):
		if type.startswith('struct '):
			type = type[7:]
		
		t = getDataTypes(type)[0]
		a = ArrayDataType(t, count, t.getLength())
		address = self._to_address(address)
		removeDataAt(address)
		createData(address, a)

	def set_data_type(self, address: int, type: str):
		if type.startswith('struct '):
			type = type[7:]
		
		try:
			t = getDataTypes(type)[0]
			address = self._to_address(address)
			removeDataAt(address)
			createData(address, t)
		except:
			print("Failed to set type: %s" % type)

	def set_function_type(self, address: int, type: str):
		typeSig = CParserUtils.parseSignature(DataTypeManagerService@None, currentProgram, type)
		ApplyFunctionSignatureCmd(self._to_address(address), typeSig, SourceType.USER_DEFINED, False, True).applyTo(currentProgram)

	def set_data_comment(self, address: int, cmt: str):
		setEOLComment(self._to_address(address), cmt)

	def set_function_comment(self, address: int, cmt: str):
		setPlateComment(self._to_address(address), cmt)

	def set_data_name(self, address: int, name: str):
		address = self._to_address(address)

		if len(name) > 2000:
			print("Name length exceeds 2000 characters, skipping (%s)" % name)
			return

		if not name.startswith("_ZN"):
			createLabel(address, name, True)
			return
		
		cmd = DemanglerCmd(address, name)
		if not cmd.applyTo(currentProgram, monitor):
			print(f"Failed to apply demangled name to {name} at {address} due {cmd.getStatusMsg()}, falling back to mangled")
			createLabel(address, name, True)

	def set_function_name(self, address: int, name: str): 
		return self.set_data_name(address, name)

	def add_cross_reference(self, from_address: int, to_address: int): 
		self.xrefs.addMemoryReference(self._to_address(from_address), self._to_address(to_address), RefType.DATA, SourceType.USER_DEFINED, 0)

	def import_c_typedef(self, type_def: str):
		# Code declarations are not supported in Ghidra
		# This only affects string literals for metadata version < 19
		# TODO: Replace with creating a DataType for enums
		pass

class GhidraStatusHandler(BaseStatusHandler): 
	pass

status = GhidraStatusHandler()
backend = GhidraDisassemblerInterface()
context = ScriptContext(backend, status)
context.process()