import { invoke } from "@tauri-apps/api/core";
import { open, save } from "@tauri-apps/plugin-dialog";

export async function getSignalRUrl() {
    const port = await invoke<string>("get_signalr_url");

    if (port === "") {
        throw new Error("No SignalR port specified.");
    }

    if (port.match(/[0-9]*/) === null) {
        throw new Error("Invalid SignalR port specified.");
    }

    return `http://localhost:${port}/il2cpp`;
}
