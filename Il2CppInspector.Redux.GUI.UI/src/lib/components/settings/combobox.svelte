<script lang="ts">
    import * as Select from "../ui/select";

    let {
        selected = $bindable(""),
        disabled = $bindable(false),
        setting,
    }: {
        selected: string;
        disabled: boolean;
        setting: ComboboxSetting;
    } = $props();

    const selectedLabel = $derived(
        setting.values.find((o) => o.id === selected)?.label,
    );
</script>

<Select.Root
    type="single"
    bind:value={selected}
    {disabled}
    name={setting.name.id}
>
    <Select.Trigger class="w-[250px]"
        >{setting.name.label}: {selectedLabel}</Select.Trigger
    >
    <Select.Content>
        {#each setting.values as value}
            <Select.Item value={value.id}>{value.label}</Select.Item>
        {/each}
    </Select.Content>
</Select.Root>
