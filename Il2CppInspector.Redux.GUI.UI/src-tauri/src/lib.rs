// Learn more about Tauri commands at https://tauri.app/develop/calling-rust/
#[tauri::command]
fn get_signalr_url() -> String {
    let args: Vec<String> = std::env::args().collect();
    if args.len() < 2 {
        return String::from("");
    }

    return args[1].clone();
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_opener::init())
        .invoke_handler(tauri::generate_handler![get_signalr_url])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
