from binaryninja import (
    BinaryView,
    Component,
    Type,
    PointerType,
    TypeParser,
    Platform,
    Endianness,
    ArrayType,
    BackgroundTaskThread,
    demangle_gnu3,
    get_qualified_name,
    SegmentFlag,
    SectionSemantics,
)
from binaryninja.log import log_error

# try:
# from typing import TYPE_CHECKING
# if TYPE_CHECKING:
# from ..shared_base import BaseStatusHandler, BaseDisassemblerInterface, ScriptContext
# import json
# import os
# import sys
# from datetime import datetime
# from typing import Literal
# bv: BinaryView = None # type: ignore
# except:
# pass

CURRENT_PATH = os.path.dirname(os.path.realpath(__file__))


class BinaryNinjaDisassemblerInterface(BaseDisassemblerInterface):
    supports_fake_string_segment: bool = True

    _status: BaseStatusHandler

    _view: BinaryView
    _undo_id: str
    _components: dict[str, Component]
    _type_cache: dict[str, Type]
    _function_type_cache: dict[str, Type]

    _address_size: int
    _endianness: Literal["little", "big"]

    TYPE_PARSER_OPTIONS = ["--target=x86_64-pc-linux", "-x", "c++", "-D_BINARYNINJA_=1"]

    def __init__(self, status: BaseStatusHandler):
        self._status = status

    def _get_or_create_type(self, type: str) -> Type:
        if type.startswith("struct "):
            type = type[len("struct ") :]
        elif type.startswith("class "):
            type = type[len("class ") :]

        if type in self._type_cache:
            return self._type_cache[type]

        if type.endswith("*"):
            base_type = self._get_or_create_type(type[:-1].strip())

            parsed = PointerType.create(self._view.arch, base_type)  # type: ignore
        else:
            parsed = self._view.get_type_by_name(type)
            if parsed is None:
                parsed, errors = self._view.parse_type_string(type)

        self._type_cache[type] = parsed
        return parsed

    def _parse_type_source(self, types: str, filename: str | None = None):
        parsed_types, errors = TypeParser.default.parse_types_from_source(
            types,
            filename if filename else "types.hpp",
            self._view.platform
            if self._view.platform is not None
            else Platform["windows-x86_64"],
            self._view,
            self.TYPE_PARSER_OPTIONS,
        )

        if parsed_types is None:
            log_error("Failed to import types.")
            log_error(errors)
            return None

        return parsed_types

    def get_script_directory(self) -> str:
        return CURRENT_PATH

    def on_start(self):
        self._view = bv  # type: ignore
        self._undo_id = self._view.begin_undo_actions()
        self._view.set_analysis_hold(True)
        self._components = {}
        self._type_cache = {}
        self._function_type_cache = {}

        self._address_size = self._view.address_size
        self._endianness = (
            "little" if self._view.endianness == Endianness.LittleEndian else "big"
        )

        self._status.update_step("Parsing header")

        with open(os.path.join(self.get_script_directory(), "il2cpp.h"), "r") as f:
            parsed_types = self._parse_type_source(f.read(), "il2cpp.hpp")
            if parsed_types is None:
                return

        self._status.update_step("Importing header types", len(parsed_types.types))

        def import_progress_func(progress: int, total: int):
            self._status.update_progress(1)
            return True

        self._view.define_user_types(
            [(x.name, x.type) for x in parsed_types.types], import_progress_func
        )

    def on_finish(self):
        self._view.commit_undo_actions(self._undo_id)
        self._view.set_analysis_hold(False)
        self._view.update_analysis()

    def define_function(self, address: int, end: int | None = None):
        if self._view.get_function_at(address) is not None:
            return

        self._view.create_user_function(address)

    def define_data_array(self, address: int, type: str, count: int):
        parsed_type = self._get_or_create_type(type)
        array_type = ArrayType.create(parsed_type, count)
        var = self._view.get_data_var_at(address)
        if var is None:
            self._view.define_user_data_var(address, array_type)
        else:
            var.type = array_type

    def set_data_type(self, address: int, type: str):
        var = self._view.get_data_var_at(address)
        dtype = self._get_or_create_type(type)
        if var is None:
            self._view.define_user_data_var(address, dtype)
        else:
            var.type = dtype

    def set_function_type(self, address: int, type: str):
        function = self._view.get_function_at(address)
        if function is None:
            return

        if type in self._function_type_cache:
            function.type = self._function_type_cache[type]  # type: ignore
        else:
            # log_info(f"skipping function type setting for {address}, {type}")
            # pass
            function.type = type.replace("this", "`this`")

    def set_data_comment(self, address: int, cmt: str):
        self._view.set_comment_at(address, cmt)

    def set_function_comment(self, address: int, cmt: str):
        function = self._view.get_function_at(address)
        if function is None:
            return

        function.comment = cmt

    def set_data_name(self, address: int, name: str):
        var = self._view.get_data_var_at(address)
        if var is None:
            return

        if name.startswith("_Z"):
            type, demangled = demangle_gnu3(self._view.arch, name, self._view)
            var.name = get_qualified_name(demangled)
        else:
            var.name = name

    def set_function_name(self, address: int, name: str):
        function = self._view.get_function_at(address)
        if function is None:
            return

        if name.startswith("_Z"):
            type, demangled = demangle_gnu3(self._view.arch, name, self._view)
            function.name = get_qualified_name(demangled)
            # function.type = type - this does not work due to the generated types not being namespaced. :(
        else:
            function.name = name

    def add_cross_reference(self, from_address: int, to_address: int):
        self._view.add_user_data_ref(from_address, to_address)

    def import_c_typedef(self, type_def: str):
        self._view.define_user_type(None, type_def)

    # optional
    def _get_or_create_component(self, name: str):
        if name in self._components:
            return self._components[name]

        current = name
        if current.count("/") != 0:
            split_idx = current.rindex("/")
            parent, child = current[:split_idx], current[split_idx:]
            parent = self._get_or_create_component(name)
            component = self._view.create_component(child, parent)
        else:
            component = self._view.create_component(name)

        self._components[name] = component
        return component

    def add_function_to_group(self, address: int, group: str):
        return
        function = self._view.get_function_at(address)
        if function is None:
            return

        self._get_or_create_component(group).add_function(function)

    def cache_function_types(self, signatures: list[str]):
        function_sigs = set(signatures)
        if len(function_sigs) == 0:
            return

        typestr = ";\n".join(function_sigs).replace("this", "_this") + ";"
        parsed_types = self._parse_type_source(typestr, "cached_types.hpp")
        if parsed_types is None:
            return

        # bv.parse_types_from_source returns a dict in the functions field.
        # TypeParser.parse_types_from_source does not.
        for function_sig, function in zip(function_sigs, parsed_types.functions):
            self._function_type_cache[function_sig] = function.type

    # only required if supports_fake_string_segment == True
    def create_fake_segment(self, name: str, size: int) -> int:
        last_end_addr = self._view.mapped_address_ranges[-1].end
        if last_end_addr % 0x1000 != 0:
            last_end_addr += 0x1000 - (last_end_addr % 0x1000)

        self._view.memory_map.add_memory_region(
            f"mem_{name}",
            last_end_addr,
            bytes(size),
            SegmentFlag.SegmentContainsData | SegmentFlag.SegmentReadable,
        )

        self._view.add_user_section(
            name, last_end_addr, size, SectionSemantics.ReadOnlyDataSectionSemantics
        )

        return last_end_addr

    def write_string(self, address: int, value: str) -> int:
        encoded = value.encode() + b"\x00"
        self._view.write(address, encoded)
        return len(encoded)

    def write_address(self, address: int, value: int):
        self._view.write(address, value.to_bytes(self._address_size, self._endianness))


class BinaryNinjaStatusHandler(BaseStatusHandler):
    def __init__(self, thread: BackgroundTaskThread):
        self.step = "Initializing"
        self.max_items = 0
        self.current_items = 0
        self.start_time = datetime.now()
        self.step_start_time = self.start_time
        self.last_updated_time = datetime.min
        self._thread = thread

    def initialize(self):
        pass

    def update(self):
        if self.was_cancelled():
            raise RuntimeError("Cancelled script.")

        current_time = datetime.now()
        if 0.5 > (current_time - self.last_updated_time).total_seconds():
            return

        self.last_updated_time = current_time

        step_time = current_time - self.step_start_time
        total_time = current_time - self.start_time
        self._thread.progress = f"Processing IL2CPP metadata: {self.step} ({self.current_items}/{self.max_items}), elapsed: {step_time} ({total_time})"

    def update_step(self, step, max_items=0):
        self.step = step
        self.max_items = max_items
        self.current_items = 0
        self.step_start_time = datetime.now()
        self.last_updated_time = datetime.min
        self.update()

    def update_progress(self, new_progress=1):
        self.current_items += new_progress
        self.update()

    def was_cancelled(self):
        return False

    def close(self):
        pass


# Entry point
class Il2CppTask(BackgroundTaskThread):
    def __init__(self):
        BackgroundTaskThread.__init__(self, "Processing IL2CPP metadata...", False)

    def run(self):
        status = BinaryNinjaStatusHandler(self)
        backend = BinaryNinjaDisassemblerInterface(status)
        context = ScriptContext(backend, status)
        context.process()


Il2CppTask().start()
