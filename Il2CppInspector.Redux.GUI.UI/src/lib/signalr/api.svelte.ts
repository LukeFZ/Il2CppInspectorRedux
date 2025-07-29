import * as signalR from "@microsoft/signalr";
import { SignalRClientApi } from "./client-api";
import { SignalRServerApi } from "./server-api";
import { getSignalRUrl } from "$lib/tauri";

class SignalRApi {
    readonly connection: signalR.HubConnection;
    readonly client: SignalRClientApi;
    readonly server: SignalRServerApi;

    constructor(connection: signalR.HubConnection) {
        this.connection = connection;
        this.client = new SignalRClientApi(connection);
        this.server = new SignalRServerApi(connection);
    }
}

class SignalRState {
    api = $state<SignalRApi>();

    get apiAvailable() {
        return this.api !== undefined;
    }

    async start() {
        const url = await getSignalRUrl();

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(url)
            .withAutomaticReconnect()
            .build();

        const api = new SignalRApi(connection);

        try {
            await connection.start();
        } catch (ex) {
            throw new Error(`Failed to start SignalR connection: ${ex}`);
        }

        this.api = api;
    }

    async stop() {
        try {
            await this.api?.connection?.stop();
        } catch (ex) {
            throw new Error(`Failed to stop SignalR connection: ${ex}`);
        }

        this.api = undefined;
    }
}

export const signalRState = new SignalRState();
