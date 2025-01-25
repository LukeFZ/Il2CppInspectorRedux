<script lang="ts">
    import { Checkbox } from "../ui/checkbox";
    import { Label } from "../ui/label";

    let {
        selected = $bindable(false),
        disabled = $bindable(false),
        setting,
    }: {
        selected: boolean;
        disabled: boolean;
        setting: OptionSetting;
    } = $props();
</script>

<div class="items-top mt-5 flex space-x-2">
    <Checkbox id={setting.name.id} bind:checked={selected} {disabled} />
    <div class="grid gap-1.5 leading-none">
        <Label
            for={setting.name.id}
            class="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
        >
            {setting.name.label}
        </Label>
    </div>
</div>
