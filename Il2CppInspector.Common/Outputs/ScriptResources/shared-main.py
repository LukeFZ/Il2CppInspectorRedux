# Shared interface
def from_hex(addr): return int(addr, 0)

def parse_address(d): return from_hex(d['virtualAddress'])

def define_il_method(jsonDef):
	addr = parse_address(jsonDef)
	set_name(addr, jsonDef['name'])
	set_function_type(addr, jsonDef['signature'])
	set_header_comment(addr, jsonDef['dotNetSignature'])
	add_function_to_group(addr, jsonDef['group'])

def define_il_method_info(jsonDef):
	addr = parse_address(jsonDef)
	set_name(addr, jsonDef['name'])
	set_comment(addr, jsonDef['dotNetSignature'])
	set_type(addr, r'struct MethodInfo *')
	if 'methodAddress' in jsonDef:
		add_xref(from_hex(jsonDef["methodAddress"]), addr)
		

def define_cpp_function(jsonDef):
	addr = parse_address(jsonDef)
	set_name(addr, jsonDef['name'])
	set_function_type(addr, jsonDef['signature'])

def define_string(jsonDef):
	addr = parse_address(jsonDef)
	set_name(addr, jsonDef['name'])
	set_comment(addr, jsonDef['string'])

def define_field(addr, name, type, ilType = None):
	addr = from_hex(addr)
	set_name(addr, name)
	set_type(addr, type)
	if ilType is not None:
		set_comment(addr, ilType)

def define_field_from_json(jsonDef):
	define_field(jsonDef['virtualAddress'], jsonDef['name'], jsonDef['type'], jsonDef['dotNetType'])

def define_array(jsonDef):
	addr = parse_address(jsonDef)
	make_array(addr, int(jsonDef['count']), jsonDef['type'])
	set_name(addr, jsonDef['name'])

def define_field_with_value(jsonDef):
	addr = parse_address(jsonDef)
	set_name(addr, jsonDef['name'])
	set_comment(addr, jsonDef['value'])

# Process JSON
def process_json(jsonData, status):
	# Function boundaries
	functionAddresses = jsonData['functionAddresses']
	functionAddresses.sort()
	count = len(functionAddresses)

	status.update_step('Processing function boundaries', count)
	for i in range(count):
		start = from_hex(functionAddresses[i])
		if start == 0:
			status.update_progress()
			continue

		end = from_hex(functionAddresses[i + 1]) if i + 1 != count else None

		make_function(start, end)
		status.update_progress()

	# Method definitions
	status.update_step('Processing method definitions', len(jsonData['methodDefinitions']))
	for d in jsonData['methodDefinitions']:
		define_il_method(d)
		status.update_progress()
	
	# Constructed generic methods
	status.update_step('Processing constructed generic methods', len(jsonData['constructedGenericMethods']))
	for d in jsonData['constructedGenericMethods']:
		define_il_method(d)
		status.update_progress()

	# Custom attributes generators
	status.update_step('Processing custom attributes generators', len(jsonData['customAttributesGenerators']))
	for d in jsonData['customAttributesGenerators']:
		define_cpp_function(d)
		status.update_progress()
	
	# Method.Invoke thunks
	status.update_step('Processing Method.Invoke thunks', len(jsonData['methodInvokers']))
	for d in jsonData['methodInvokers']:
		define_cpp_function(d)
		status.update_progress()

	# String literals for version >= 19
	if 'virtualAddress' in jsonData['stringLiterals'][0]:
		status.update_step('Processing string literals (V19+)', len(jsonData['stringLiterals']))

		process_string_literals(status, jsonData)

	# String literals for version < 19
	else:
		status.update_step('Processing string literals (pre-V19)')
		litDecl = 'enum StringLiteralIndex {\n'
		for d in jsonData['stringLiterals']:
			litDecl += "  " + d['name'] + ",\n"
		litDecl += '};\n'
		define_code(litDecl)
	
	# Il2CppClass (TypeInfo) pointers
	status.update_step('Processing Il2CppClass (TypeInfo) pointers', len(jsonData['typeInfoPointers']))
	for d in jsonData['typeInfoPointers']:
		define_field_from_json(d)
		status.update_progress()
	
	# Il2CppType (TypeRef) pointers
	status.update_step('Processing Il2CppType (TypeRef) pointers', len(jsonData['typeRefPointers']))
	for d in jsonData['typeRefPointers']:
		define_field(d['virtualAddress'], d['name'], r'struct Il2CppType *', d['dotNetType'])
		status.update_progress()
	
	# MethodInfo pointers
	status.update_step('Processing MethodInfo pointers', len(jsonData['methodInfoPointers']))
	for d in jsonData['methodInfoPointers']:
		define_il_method_info(d)
		status.update_progress()

	# FieldInfo pointers, add the contents as a comment
	status.update_step('Processing FieldInfo pointers', len(jsonData['fields']))
	for d in jsonData['fields']:
		define_field_with_value(d)
		status.update_progress()

	# FieldRva pointers, add the contents as a comment
	status.update_step('Processing FieldRva pointers', len(jsonData['fieldRvas']))
	for d in jsonData['fieldRvas']:
		define_field_with_value(d)
		status.update_progress()

	# IL2CPP type metadata
	status.update_step('Processing IL2CPP type metadata', len(jsonData['typeMetadata']))
	for d in jsonData['typeMetadata']:
		define_field(d['virtualAddress'], d['name'], d['type'])
	
	# IL2CPP function metadata
	status.update_step('Processing IL2CPP function metadata', len(jsonData['functionMetadata']))
	for d in jsonData['functionMetadata']:
		define_cpp_function(d)

	# IL2CPP array metadata
	status.update_step('Processing IL2CPP array metadata', len(jsonData['arrayMetadata']))
	for d in jsonData['arrayMetadata']:
		define_array(d)

	# IL2CPP API functions
	status.update_step('Processing IL2CPP API functions', len(jsonData['apis']))
	for d in jsonData['apis']:
		define_cpp_function(d)

# Entry point
print('Generated script file by Il2CppInspectorRedux - https://github.com/LukeFZ (Original Il2CppInspector by http://www.djkaty.com - https://github.com/djkaty)')
status = StatusHandler()
status.initialize()

try:
	start_time = datetime.datetime.now()

	status.update_step("Running script prologue")
	script_prologue(status)

	with open(os.path.join(get_script_directory(), "%JSON_METADATA_RELATIVE_PATH%"), "r") as jsonFile:
		status.update_step("Loading JSON metadata")
		jsonData = json.load(jsonFile)['addressMap']
		process_json(jsonData, status)

	status.update_step("Running script epilogue")
	script_epilogue(status)

	status.update_step('Script execution complete.')
	print("Took: %s" % (datetime.datetime.now() - start_time))
except RuntimeError: pass
finally: status.close()
