<script lang="ts">
    import { signalRState } from "$lib/signalr/api.svelte";

    let inspectorVersion = $state<string>();

    $effect(() => {
        if (signalRState.api === undefined) return;

        if (inspectorVersion === undefined) {
            signalRState.api.server.getInspectorVersion().then((version) => {
                inspectorVersion = version;
            });
        }
    });
</script>

<div
    class="absolute inset-x-0 bottom-0 flex h-[--footer-height] flex-row justify-between border-t-2 text-center"
>
    <div class="ml-4 mt-1">
        <p class="text-sm text-muted-foreground">
            Il2CppInspectorRedux - created by djkaty, maintained by LukeFZ
        </p>
    </div>

    {#if inspectorVersion !== undefined}
        <div class="mr-4 mt-1">
            <p class="text-sm text-muted-foreground">
                {inspectorVersion}
            </p>
        </div>
    {/if}
</div>
