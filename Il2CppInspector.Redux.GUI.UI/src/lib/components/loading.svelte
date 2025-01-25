<script>
    import { signalRState } from "$lib/signalr/api.svelte";
    import { LoaderCircle } from "lucide-svelte";

    let isLoading = $state(false);
    let statusMessage = $state("");
    let { children } = $props();

    /*
    let currentIndex = 0;
    let mockMessages = [
        "Building type model",
        "Generating Assembly-CSharp.dll",
        "Writing Assembly-CSharp.dll",
        "(this is just a test)",
        "aaaaaa",
        "almost there!",
    ];

    setInterval(() => {
        statusMessage = mockMessages[currentIndex++ % mockMessages.length];
    }, 100);
    */

    $effect(() => {
        if (signalRState.api === undefined) return;

        const unregisterLogMessage =
            signalRState.api.client.onLogMessageReceived(async (message) => {
                statusMessage = message;
            });

        const unregisterBeginLoading = signalRState.api.client.onLoadingStarted(
            async () => {
                isLoading = true;
            },
        );

        const unregisterFinishLoading =
            signalRState.api.client.onLoadingFinished(async () => {
                isLoading = false;
                statusMessage = "";
            });

        return () => {
            unregisterFinishLoading();
            unregisterBeginLoading();
            unregisterLogMessage();
        };
    });
</script>

{#if isLoading}
    <div class="flex h-full w-screen flex-col">
        <div class="m-auto flex h-full w-screen flex-col items-center">
            <LoaderCircle class="mt-[25%] h-16 w-16 animate-spin" />
            <p class="leading-7 [&:not(:first-child)]:mt-6">
                {statusMessage}
            </p>
        </div>
    </div>
{:else}
    {@render children()}
{/if}
