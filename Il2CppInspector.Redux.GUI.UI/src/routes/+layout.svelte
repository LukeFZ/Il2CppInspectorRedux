<script lang="ts">
    import Footer from "$lib/components/Footer.svelte";
    import Header from "$lib/components/Header.svelte";
    import Loading from "$lib/components/loading.svelte";
    import { Toaster } from "$lib/components/ui/sonner";
    import "../app.css";
    import { ModeWatcher } from "mode-watcher";
    let { children } = $props();
</script>

<ModeWatcher />
<Toaster richColors />

<Header />
<Footer />

<div
    class="absolute inset-x-0 top-[--header-height] mb-[--footer-height] flex h-[--main-height]"
>
    <Loading>
        {@render children()}
    </Loading>
</div>
