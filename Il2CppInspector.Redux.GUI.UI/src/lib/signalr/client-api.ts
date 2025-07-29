import { goto } from "$app/navigation";
import * as signalR from "@microsoft/signalr";
import { toast } from "svelte-sonner";

export class SignalRClientApi {
    private connection: signalR.HubConnection;

    constructor(connection: signalR.HubConnection) {
        this.connection = connection;

        this.connection.on("ShowInfoToast", (message: string) => {
            toast.info(message);
        });

        this.connection.on("ShowSuccessToast", (message: string) => {
            toast.success(message);
        });

        this.connection.on("ShowErrorToast", (message: string) => {
            toast.error(message);
        });

        // HACK: This is put here to be persistent, as the normal import screen gets killed once the loading screen begins
        // todo: improve this
        this.connection.on("OnImportCompleted", async () => {
            await goto("/export");
        });
    }

    onLogMessageReceived(
        handler: (message: string) => Promise<void>,
    ): () => void {
        return this.registerHandler("ShowLogMessage", handler);
    }

    onLoadingStarted(handler: () => Promise<void>): () => void {
        return this.registerHandler("BeginLoading", handler);
    }

    onLoadingFinished(handler: () => Promise<void>): () => void {
        return this.registerHandler("FinishLoading", handler);
    }

    onImportCompleted(handler: () => Promise<void>): () => void {
        return this.registerHandler("OnImportCompleted", handler);
    }

    private registerHandler(
        name: string,
        handler: (...args: any[]) => Promise<void>,
    ): () => void {
        this.connection.on(name, handler);
        return () => {
            this.connection.off(name, handler);
        };
    }
}
