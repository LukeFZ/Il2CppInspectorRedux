import type { PageLoad } from "./$types";

type FormatData = {
    outputFormats: {
        id: string;
        name: string;
        description?: string;
    }[];
};

export const load: PageLoad<FormatData> = async () => {
    return {
        outputFormats: [
            {
                id: "cs",
                name: "C# prototypes",
                description: "hehe",
            },
            {
                id: "vssolution",
                name: "Visual Studio solution",
                description: "hihi",
            },
            {
                id: "dummydlls",
                name: ".NET dummy assemblies",
            },
            {
                id: "disassemblermetadata",
                name: "Disassembler metadata",
            },
            {
                id: "cppscaffolding",
                name: "C++ scaffolding project",
            },
        ],
    };
};
