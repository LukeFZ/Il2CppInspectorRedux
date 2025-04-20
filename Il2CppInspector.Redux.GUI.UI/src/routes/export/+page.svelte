<script lang="ts">
    import { goto } from "$app/navigation";
    import Button, {
        buttonVariants,
    } from "$lib/components/ui/button/button.svelte";
    import * as Tooltip from "$lib/components/ui/tooltip";
    import { exportState } from "$lib/export.svelte";
    import { cn } from "$lib/utils";
    import type { PageProps } from "./$types";

    let { data }: PageProps = $props();
</script>

<div class="flex w-screen flex-col">
    <div class="mb-10 flex h-full flex-col">
        <h1
            class="ml-10 scroll-m-20 text-4xl font-extrabold tracking-tight lg:text-5xl"
        >
            Select your export type:
        </h1>
        <div class="mx-5 mt-10 grid h-full grid-cols-2 gap-4 sm:gap-6">
            {#each data.outputFormats as format}
                <Tooltip.Provider>
                    <Tooltip.Root>
                        <Tooltip.Trigger
                            class={cn(
                                buttonVariants({ variant: "outline" }),
                                "sm:p-10",
                            )}
                            onclick={() => goto(`/export/${format.id}`)}
                        >
                            <h3
                                class="scroll-m-20 text-2xl font-semibold tracking-tight"
                            >
                                {format.name}
                            </h3>
                        </Tooltip.Trigger>
                        <Tooltip.Content>
                            <small class="text-sm font-medium leading-none"
                                >{format.description ?? format.name}</small
                            >
                        </Tooltip.Content>
                    </Tooltip.Root>
                </Tooltip.Provider>
            {/each}
        </div>
    </div>
    <div class="mx-5 mb-3 flex flex-row-reverse justify-between">
        <Button href="/" variant="outline">Cancel</Button>
        {#if exportState.hasExportQueued}
            <Button onclick={() => exportState.startExport()} variant="default"
                >Start export</Button
            >
        {/if}
        <Button href="/advanced" variant="ghost">Advanced</Button>
    </div>
</div>
