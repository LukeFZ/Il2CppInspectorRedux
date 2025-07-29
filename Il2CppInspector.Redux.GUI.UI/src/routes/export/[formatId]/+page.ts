import { signalRState } from "$lib/signalr/api.svelte";
import type { EntryGenerator, PageLoad } from "./$types";

interface FormatConfiguration {
    settings: SettingTypes[];
}

let mockFormatSettings: {
    [key: string]: FormatConfiguration;
} = {
    cs: {
        settings: [
            {
                type: "combobox",
                name: {
                    id: "layout",
                    label: "Layout",
                },
                default: "singlefile",
                values: [
                    {
                        id: "singlefile",
                        label: "Single file",
                    },
                    {
                        id: "namespace",
                        label: "File per namespace",
                    },
                    {
                        id: "assembly",
                        label: "File per assembly",
                    },
                    {
                        id: "class",
                        label: "File per class",
                    },
                    {
                        id: "tree",
                        label: "Tree layout",
                    },
                ],
            },
            {
                type: "option",
                name: {
                    id: "flattenhierarchy",
                    label: "Don't nest folders (flatten hierarchy)",
                },
                default: false,
                condition: {
                    id: "layout",
                    value: ["namespace", "class", "tree"],
                },
            },
            {
                type: "combobox",
                name: {
                    id: "sortingmode",
                    label: "Type sorting",
                },
                default: "alphabetical",
                values: [
                    {
                        id: "alphabetical",
                        label: "Alphabetical",
                    },
                    {
                        id: "typedefinitionindex",
                        label: "Type definition index",
                    },
                ],
                condition: {
                    id: "layout",
                    value: ["singlefile", "namespace", "assembly"],
                },
            },
            {
                type: "option",
                name: {
                    id: "suppressmetadata",
                    label: "Suppress pointer, offset and index metadata comments",
                },
                default: false,
            },
            {
                type: "option",
                name: {
                    id: "mustcompile",
                    label: "Attempt to generate output that compiles",
                },
                default: false,
            },
            {
                type: "option",
                name: {
                    id: "seperateassemblyattributes",
                    label: "Place assembly-level attributes in seperate files",
                },
                default: true,
                condition: {
                    id: "layout",
                    value: ["assembly", "tree"],
                },
            },
        ],
    },
    vssolution: {
        settings: [
            {
                type: "filepath",
                name: {
                    id: "editorpath",
                    label: "Unity editor path",
                },
                directoryPath: true,
            },
            {
                type: "filepath",
                name: {
                    id: "unityassembliespath",
                    label: "Unity script assemblies path",
                },
                directoryPath: true,
            },
        ],
    },
    dummydlls: {
        settings: [
            {
                type: "option",
                name: {
                    id: "suppressmetadata",
                    label: "Suppress output of all metadata attributes",
                },
            },
        ],
    },
    disassemblermetadata: {
        settings: [
            {
                type: "combobox",
                name: {
                    id: "unityversion",
                    label: "Unity version (if known)",
                },
                values: [
                    {
                        id: "nya",
                        label: "nya",
                    },
                ],
            },
            {
                type: "combobox",
                name: {
                    id: "disassembler",
                    label: "Target disassembler",
                },
                values: [
                    {
                        id: "idapro",
                        label: "IDA Pro v7.7+",
                    },
                    {
                        id: "ghidra",
                        label: "Ghidra v11.3+",
                    },
                    {
                        id: "binaryninja",
                        label: "Binary Ninja",
                    },
                    {
                        id: "none",
                        label: "None",
                    },
                ],
                default: "idapro",
            },
        ],
    },
    cppscaffolding: {
        settings: [
            {
                type: "combobox",
                name: {
                    id: "unityversion",
                    label: "Unity version (if known)",
                },
                values: [
                    {
                        id: "nya",
                        label: "nya",
                    },
                ],
            },
            {
                type: "combobox",
                name: {
                    id: "compiler",
                    label: "Target C++ compiler for output",
                },
                values: [
                    {
                        id: "msvc",
                        label: "MSVC",
                    },
                    {
                        id: "gcc",
                        label: "GCC",
                    },
                ],
                default: "msvc",
            },
        ],
    },
};

export const load: PageLoad<FormatConfiguration> = async ({ params }) => {
    const unityVersions =
        (await signalRState.api?.server.getPotentialUnityVersions()) ?? [];

    const unityVersionEntries = unityVersions.map<StringValue>((version) => ({
        id: version,
        label: version,
    }));

    let settings = mockFormatSettings[params.formatId];

    settings.settings.forEach((setting) => {
        if (setting.name.id === "unityversion" && setting.type === "combobox") {
            setting.values = unityVersionEntries;
            setting.default =
                unityVersionEntries.length > 0
                    ? unityVersionEntries[0].id
                    : undefined;
        }
    });

    return settings;
};

export const entries: EntryGenerator = () => {
    return [
        {
            formatId: "cs",
        },
        {
            formatId: "vssolution",
        },
        {
            formatId: "dummydlls",
        },
        {
            formatId: "disassemblermetadata",
        },
        {
            formatId: "cppscaffolding",
        },
    ];
};
