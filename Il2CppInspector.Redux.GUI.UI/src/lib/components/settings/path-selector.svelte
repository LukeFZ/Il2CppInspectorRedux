<script lang="ts">
    import { open } from "@tauri-apps/plugin-dialog";
    import Button from "../ui/button/button.svelte";

    let {
        selected = $bindable(""),
        disabled = $bindable(false),
        setting,
    }: {
        selected: string;
        disabled: boolean;
        setting: FilepathSetting;
    } = $props();

    async function openFileDialog(e: Event) {
        e.preventDefault();

        const selection = await open({
            directory: setting.directoryPath,
            title: `Select ${setting.name.label}`,
            multiple: false,
        });

        if (selection === null) return;

        selected = selection;
    }
</script>

<div class="mx-[3.3%] flex flex-row-reverse justify-between">
    <Button variant="outline" {disabled} onclick={openFileDialog}>Browse</Button
    >
    <p class="w-fulls mt-2 text-right">
        {selected === "" ? "not selected" : selected}
    </p>
    <p class="mt-2">{setting.name.label}:</p>
</div>
