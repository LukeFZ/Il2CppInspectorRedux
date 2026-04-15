# Ghidra-specific implementation
from ghidra.app.cmd.function import ApplyFunctionSignatureCmd
from ghidra.app.util.cparser.C import CParserUtils
from ghidra.program.model.data import ArrayDataType
from ghidra.program.model.symbol import SourceType, RefType, SymbolUtilities
from ghidra.app.services import DataTypeManagerService
from ghidra.app.util.demangler import Demangler, DemangledException
from ghidra.app.util.demangler.gnu import (  # type: ignore
    GnuDemangler,
    GnuDemanglerOptions,
    GnuDemanglerFormat,
)
from ghidra.util.classfinder import ClassSearcher
from ghidra.program.model.address import Address
from java.lang import Long

try:
    from typing import TYPE_CHECKING, Union

    if TYPE_CHECKING:
        from ..shared_base import (
            BaseStatusHandler,
            BaseDisassemblerInterface,
            ScriptContext,
        )

        import sys

        from ghidra.ghidra_builtins import (
            currentProgram,
            toAddr,
            getSourceFile,
            getDataTypes,
            setAnalysisOption,
            getFunctionAt,
            createData,
            removeDataAt,
            createFunction,
            setEOLComment,
            setPlateComment,
            createLabel,
            monitor,
        )
except ImportError:
    pass


class GhidraDisassemblerInterface(BaseDisassemblerInterface):
    supports_fake_string_segment = False

    _demangler: Demangler
    _demangler_options: GnuDemanglerOptions

    def __init__(self):
        # Inspector always emits Itanium mangled symbols, but Ghidra does not
        # normally allow demangling them on Windows binaries. Because of this,
        # we need to manually grab the GNU demangler and use it instead of checking
        # all demanglers and canDemangle.

        # We also have to use the "deprecated" demangler binary, as our symbols
        # are quite long and sometimes run into the (scuffed) recursion-limit heuristic
        # causing them to fail on the newer demangler.

        self._demangler = ClassSearcher.getInstances(GnuDemangler)[0]
        self._demangler_options = GnuDemanglerOptions(GnuDemanglerFormat.AUTO, True)
        self._demangler_options.setApplySignature(False)
        self._demangler_options.setApplyCallingConvention(False)
        self._demangler_options.setDoDisassembly(False)
        self._demangler_options.setDemangleOnlyKnownPatterns(False)

    def _to_address(self, value: int) -> Address:
        return toAddr(Long(value))

    def _demangle_symbol(self, symbol: str) -> str | None:
        ctx = self._demangler.createMangledContext(
            symbol,
            self._demangler_options,
            currentProgram,
            Address @ None,  # type: ignore
        )

        try:
            demangled = self._demangler.demangle(ctx)
            if demangled is None:
                return None

            return demangled.getOriginalDemangled()
        except DemangledException as e:
            if e.isInvalidMangledName():
                return None

            print(f"Error while demangling symbol '{symbol}': {e.getMessage()}")

        return None

    def get_script_directory(self) -> str:
        return getSourceFile().getParentFile().toString()

    def on_start(self):
        self.xrefs = currentProgram.getReferenceManager()

        # Check that the user has parsed the C headers first
        if len(getDataTypes("Il2CppObject")) == 0:
            print(
                "STOP! You must import the generated C header file (%TYPE_HEADER_RELATIVE_PATH%) before running this script."
            )
            print(
                "See https://github.com/djkaty/Il2CppInspector/blob/master/README.md#adding-metadata-to-your-ghidra-workflow for instructions."
            )
            sys.exit()

        # Ghidra sets the image base for ELF to 0x100000 for some reason
        # https://github.com/NationalSecurityAgency/ghidra/issues/1020
        # Make sure that the base address is 0
        # Without this, Ghidra may not analyze the binary correctly and you will just waste your time
        # If 0 doesn't work for you, replace it with the base address from the output of the CLI or GUI
        if currentProgram.getExecutableFormat().endswith("(ELF)"):
            currentProgram.setImageBase(self._to_address(0), True)

        # Don't trigger decompiler
        setAnalysisOption(currentProgram, "Call Convention ID", "false")

    def on_finish(self):
        pass

    def define_function(self, address: int, end: Union[int, None] = None):
        addr = self._to_address(address)
        # Don't override existing functions
        fn = getFunctionAt(addr)
        if fn is None:
            # Create new function if none exists
            createFunction(addr, None)

    def define_data_array(self, address: int, type: str, count: int):
        if type.startswith("struct "):
            type = type[7:]

        t = getDataTypes(type)[0]
        a = ArrayDataType(t, count, t.getLength())
        addr = self._to_address(address)
        removeDataAt(addr)
        createData(addr, a)

    def set_data_type(self, address: int, type: str):
        if type.startswith("struct "):
            type = type[7:]

        try:
            t = getDataTypes(type)[0]
            addr = self._to_address(address)
            removeDataAt(addr)
            createData(addr, t)
        except Exception:
            print("Failed to set type: %s" % type)

    def set_function_type(self, address: int, type: str):
        typeSig = CParserUtils.parseSignature(
            DataTypeManagerService @ None,  # type: ignore
            currentProgram,
            type,
        )
        ApplyFunctionSignatureCmd(
            self._to_address(address), typeSig, SourceType.USER_DEFINED, False, True
        ).applyTo(currentProgram, monitor)

    def set_data_comment(self, address: int, cmt: str):
        setEOLComment(self._to_address(address), cmt)

    def set_function_comment(self, address: int, cmt: str):
        setPlateComment(self._to_address(address), cmt)

    def set_data_name(self, address: int, name: str):
        addr = self._to_address(address)

        if not name.startswith("_ZN"):
            createLabel(addr, name, True)
            return

        label = self._demangle_symbol(name)
        if label is None:
            print(
                f"Failed to demangle name {name} at {address}, falling back to mangled"
            )
            label = name

        if len(label) > 2000:
            print(f"Name exceeds 2000 characters, skipping '{label}'")
            return

        createLabel(addr, SymbolUtilities.replaceInvalidChars(label, True), True)

    def set_function_name(self, address: int, name: str):
        return self.set_data_name(address, name)

    def add_cross_reference(self, from_address: int, to_address: int):
        self.xrefs.addMemoryReference(
            self._to_address(from_address),
            self._to_address(to_address),
            RefType.DATA,
            SourceType.USER_DEFINED,
            0,
        )

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
