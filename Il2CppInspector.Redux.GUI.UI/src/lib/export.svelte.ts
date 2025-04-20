import { signalRState } from "./signalr/api.svelte";

class ExportState {
    hasExportQueued = false;

    async queueExport(
        formatId: string,
        outputDirectory: string,
        options: Map<string, string>,
    ) {
        await signalRState.api?.server.queueExport(
            formatId,
            outputDirectory,
            options,
        );

        this.hasExportQueued = true;
    }

    async startExport() {
        await signalRState.api?.server.startExport();
        this.hasExportQueued = false;
    }
}

export const exportState = $state<ExportState>(new ExportState());
