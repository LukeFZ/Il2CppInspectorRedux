import * as signalR from "@microsoft/signalr";

export class SignalRServerApi {
    private connection: signalR.HubConnection;

    constructor(connection: signalR.HubConnection) {
        this.connection = connection;
    }

    async sendUiLaunched() {
        return await this.connection.send("OnUiLaunched");
    }

    async submitInputFiles(inputFiles: string[]) {
        return await this.connection.send("SubmitInputFiles", inputFiles);
    }

    async queueExport(
        exportId: string,
        targetDirectory: string,
        settings: Map<string, string>,
    ) {
        return await this.connection.send(
            "QueueExport",
            exportId,
            targetDirectory,
            Object.fromEntries(settings),
        );
    }

    async startExport() {
        return await this.connection.send("StartExport");
    }

    async getPotentialUnityVersions(): Promise<string[]> {
        return await this.connection.invoke<string[]>(
            "GetPotentialUnityVersions",
        );
    }

    async exportIl2CppFiles(targetDirectory: string) {
        return await this.connection.send("ExportIl2CppFiles", targetDirectory);
    }

    async getInspectorVersion(): Promise<string> {
        return await this.connection.invoke<string>("GetInspectorVersion");
    }
}
