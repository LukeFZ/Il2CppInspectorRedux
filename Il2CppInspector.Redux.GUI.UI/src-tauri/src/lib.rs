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
    // nvidia on linux crashes without this set
    #[cfg(target_os = "linux")]
    if std::env::var_os("WEBKIT_DISABLE_DMABUF_RENDERER").is_none() && std::path::Path::new("/proc/driver/nvidia").exists()
    {
        std::env::set_var("WEBKIT_DISABLE_DMABUF_RENDERER", "1");
    }

    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_opener::init())
        .invoke_handler(tauri::generate_handler![get_signalr_url])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
