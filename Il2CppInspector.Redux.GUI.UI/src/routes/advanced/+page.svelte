<script lang="ts">
    import Button from "$lib/components/ui/button/button.svelte";
</script>

<div class="flex w-screen flex-col">
    <div class="mb-10 flex h-full flex-col">
        <h1
            class="ml-10 scroll-m-20 text-4xl font-extrabold tracking-tight lg:text-5xl"
        >
            Advanced information
        </h1>
    </div>
    <div class="mx-5 mb-3 flex flex-row-reverse justify-between">
        <Button onclick={() => history.back()} variant="outline">Go back</Button
        >
    </div>
</div>
