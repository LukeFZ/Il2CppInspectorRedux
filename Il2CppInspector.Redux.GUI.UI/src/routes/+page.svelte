<script lang="ts">
    import { Button } from "$lib/components/ui/button";
    import { onDestroy, onMount } from "svelte";
    import { signalRState } from "../lib/signalr/api.svelte";
    import { toast } from "svelte-sonner";
    import {
        getCurrentWebview,
        type DragDropEvent,
    } from "@tauri-apps/api/webview";
    import type { UnlistenFn } from "@tauri-apps/api/event";
    import { open } from "@tauri-apps/plugin-dialog";

    async function chooseFile(event: Event) {
        event.preventDefault();

        const selection = await open({
            title: "Select your input files",
            multiple: true,
            directory: false,
            filters: [
                {
                    name: "IL2CPP metadata",
                    extensions: ["dat", "dat.dec"],
                },
                {
                    name: "Android app packages",
                    extensions: ["apk", "xapk", "apkm", "aab"],
                },
                {
                    name: "iOS app packages",
                    extensions: ["ipa"],
                },
                {
                    name: "Archives",
                    extensions: ["zip"],
                },
                {
                    name: "Native libraries",
                    extensions: ["dll", "so", "dylib"],
                },
                {
                    name: "All files",
                    extensions: ["*"],
                },
            ],
        });

        if (selection === null) {
            return;
        }

        await signalRState.api?.server.submitInputFiles(
            Array.isArray(selection) ? selection : [selection],
        );
    }

    async function handleDragDropEvent(event: DragDropEvent) {
        if (event.type === "drop") {
            await signalRState.api?.server.submitInputFiles(event.paths);
        }
    }

    let unlisten: UnlistenFn | undefined;

    onMount(async () => {
        if (!signalRState.apiAvailable) {
            try {
                await signalRState.start();
                await signalRState.api?.server.sendUiLaunched();
            } catch (e) {
                if (e instanceof Error) toast.error(e.message);
            }
        }

        unlisten = await getCurrentWebview().onDragDropEvent((event) => {
            handleDragDropEvent(event.payload);
        });
    });

    onDestroy(() => {
        unlisten?.();
    });
</script>

<div class="flex w-screen flex-col">
    <button
        class="m-auto h-[calc(var(--main-height)-10vh)] w-[75vh] content-center rounded-md border-4 border-dashed text-center hover:cursor-pointer hover:border-dotted"
        onclick={chooseFile}
    >
        <div class="mt-[7.5%]">
            <h3 class="scroll-m-20 text-2xl font-semibold tracking-tight">
                Drag and drop, or select
            </h3>

            <p class="text-m text-muted-foreground">your input files.</p>

            <br />

            <div>
                <div class="mt-10 text-lg font-semibold">
                    Supported file types:
                </div>

                <ul class="my-3 list-outside list-none [&>li]:mt-2">
                    <li>
                        <p class="text-m text-muted-foreground">
                            IL2CPP metadata files (global-metadata.dat)
                        </p>
                    </li>
                    <li>
                        <p class="text-m text-muted-foreground">
                            Android app packages (.apk, .xapk, .aab)
                        </p>
                    </li>
                    <li>
                        <p class="text-m text-muted-foreground">
                            iOS app packages (.ipa, decrypted only)
                        </p>
                    </li>
                    <li>
                        <p class="text-m text-muted-foreground">
                            Archives (.zip)
                        </p>
                    </li>
                </ul>
            </div>
        </div>
    </button>

    <div class="mx-5 mb-3 flex flex-row-reverse justify-between">
        <Button href="/options" variant="secondary">Options</Button>
    </div>
</div>
