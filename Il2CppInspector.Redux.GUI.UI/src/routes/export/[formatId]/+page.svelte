<script lang="ts">
    import { page } from "$app/state";
    import Button from "$lib/components/ui/button/button.svelte";

    import type { PageProps } from "./$types";
    import Combobox from "$lib/components/settings/combobox.svelte";
    import Option from "$lib/components/settings/option.svelte";
    import { goto } from "$app/navigation";
    import PathSelector from "$lib/components/settings/path-selector.svelte";
    import { open } from "@tauri-apps/plugin-dialog";
    import { toast } from "svelte-sonner";
    import { onMount } from "svelte";
    import { exportState } from "$lib/export.svelte";

    let formatId = page.params.formatId;
    let { data }: PageProps = $props();

    type ValueType =
        | {
              setting: ComboboxSetting;
              selected: string;
          }
        | {
              setting: OptionSetting;
              selected: boolean;
          }
        | {
              setting: FilepathSetting;
              selected: string;
          };

    let values = $state<ValueType[]>(
        data.settings.map((value) => {
            switch (value.type) {
                case "combobox":
                    return {
                        setting: value,
                        selected: value.default ?? "",
                    };
                case "option":
                    return {
                        setting: value,
                        selected: value.default ?? false,
                    };
                case "filepath":
                    return {
                        setting: value,
                        selected: "",
                    };
            }
        }),
    );

    function getValueEntry(id: string) {
        return values.find((x) => x.setting.name.id === id);
    }

    function getValue(setting: ComboboxSetting): string;
    function getValue(setting: OptionSetting): boolean;
    function getValue(setting: FilepathSetting): string;

    function getValue(setting: SettingTypes): string | boolean {
        return getValueEntry(setting.name.id)!.selected;
    }

    function setValue(
        setting: ComboboxSetting | FilepathSetting,
        value: string,
    ): void;
    function setValue(setting: OptionSetting, value: boolean): void;

    function setValue(setting: SettingTypes, value: string | boolean): void {
        getValueEntry(setting.name.id)!.selected = value;
    }

    function isDisabled(setting: Setting) {
        if (setting.condition !== undefined) {
            const conditionalSetting = getValueEntry(setting.condition.id);
            if (conditionalSetting === undefined) return true;

            switch (conditionalSetting.setting.type) {
                case "combobox":
                    if (Array.isArray(setting.condition.value))
                        return !setting.condition.value.includes(
                            conditionalSetting.selected as string,
                        );

                    return (
                        conditionalSetting.selected === setting.condition.value
                    );
                case "option":
                    return (
                        conditionalSetting.selected === setting.condition.value
                    );
                case "filepath":
                    return false;
            }
        }

        return false;
    }

    async function queueExport(e: Event, shouldStartExport: boolean) {
        e.preventDefault();

        const exportDirectory = await open({
            title: "Select the output folder",
            directory: true,
            multiple: false,
            recursive: false,
        });

        if (exportDirectory === null) return;

        const settings = new Map<string, string>(
            values.map((x) => [x.setting.name.id, x.selected.toString()]),
        );

        await exportState.queueExport(formatId, exportDirectory, settings);

        if (shouldStartExport) {
            await exportState.startExport();
        } else {
            toast.info("Successfully queued export.");
        }

        await goto("/export");
    }

    let isExportAvailable = $derived.by(() => {
        for (var i = 0; i < values.length; i++) {
            if (values[i].selected === "") return false;
        }

        return true;
    });

    onMount(() => {
        // we dismiss all toasts so that they don't block the export/queue buttons
        toast.dismiss();
    });
</script>

<div class="flex w-screen flex-col">
    <div class="mb-10 flex h-full flex-col">
        <h1
            class="ml-10 scroll-m-20 text-4xl font-extrabold tracking-tight lg:text-5xl"
        >
            Adjust your export settings:
        </h1>

        <div class="mx-5 mt-10 h-full *:mt-5">
            {#each data.settings as setting}
                {#if setting.type == "combobox"}
                    <Combobox
                        bind:selected={
                            () => getValue(setting), (v) => setValue(setting, v)
                        }
                        disabled={isDisabled(setting)}
                        {setting}
                    />
                {:else if setting.type == "option"}
                    <Option
                        bind:selected={
                            () => getValue(setting), (v) => setValue(setting, v)
                        }
                        disabled={isDisabled(setting)}
                        {setting}
                    />
                {:else if setting.type == "filepath"}
                    <PathSelector
                        bind:selected={
                            () => getValue(setting), (v) => setValue(setting, v)
                        }
                        disabled={isDisabled(setting)}
                        {setting}
                    />
                {/if}
            {/each}
        </div>
    </div>
    <div class="mx-5 mb-3 flex flex-row-reverse">
        <Button href="/export" variant="outline">Go back</Button>
        <Button
            onclick={(e) => queueExport(e, true)}
            class="mr-5"
            disabled={!isExportAvailable}>Export</Button
        >
        <Button
            onclick={(e) => queueExport(e, false)}
            class="mr-5"
            variant="secondary"
            disabled={!isExportAvailable}>Queue</Button
        >
    </div>
</div>
