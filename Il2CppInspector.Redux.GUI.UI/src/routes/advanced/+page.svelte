<script lang="ts">
    import Button from "$lib/components/ui/button/button.svelte";
    import { signalRState } from "$lib/signalr/api.svelte";
    import { open } from "@tauri-apps/plugin-dialog";

    async function exportIl2CppFiles(e: Event) {
        e.preventDefault();

        const exportDirectory = await open({
            title: "Select the output folder",
            directory: true,
            multiple: false,
            recursive: false,
        });

        if (exportDirectory === null) return;

        await signalRState.api?.server.exportIl2CppFiles(exportDirectory);
    }
</script>

<div class="flex w-screen flex-col">
    <div class="mb-10 flex h-full flex-col">
        <h1
            class="ml-10 scroll-m-20 text-4xl font-extrabold tracking-tight lg:text-5xl"
        >
            Advanced
        </h1>
        <div class="mx-5 mt-10 grid h-full grid-cols-2 gap-4 sm:gap-6">
            <Button
                class="sm:p-10"
                variant="outline"
                onclick={exportIl2CppFiles}
            >
                Export IL2CPP files
            </Button>
        </div>
    </div>
    <div class="mx-5 mb-3 flex flex-row-reverse justify-between">
        <Button onclick={() => history.back()} variant="outline">Go back</Button
        >
    </div>
</div>
