<script lang="ts">
    import Button from "./ui/button/button.svelte";

    import { Moon, Sun } from "lucide-svelte";
    import { toggleMode } from "mode-watcher";
</script>

<div class="absolute inset-x-0 top-0 flex h-[--header-height] flex-row-reverse">
    <div class="mr-3 mt-3">
        <Button onclick={toggleMode} variant="outline" size="icon">
            <Sun
                class="h-[1.2rem] w-[1.2rem] rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0"
            />
            <Moon
                class="absolute h-[1.2rem] w-[1.2rem] rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100"
            />
            <span class="sr-only">Toggle theme</span>
        </Button>
    </div>
</div>
