interface StringValue {
    id: string;
    label: string;
}

interface Setting {
    type: "combobox" | "option" | "filepath";
    name: StringValue;
    description?: string;
    condition?: {
        id: string;
        value: string | boolean | string[];
    };
}

interface ComboboxSetting extends Setting {
    type: "combobox";
    default?: string;
    values: StringValue[];
}

interface OptionSetting extends Setting {
    type: "option";
    default?: boolean;
}

interface FilepathSetting extends Setting {
    type: "filepath";
    directoryPath: boolean;
}

type SettingTypes = ComboboxSetting | OptionSetting | FilepathSetting;
