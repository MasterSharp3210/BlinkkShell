use std::io::{self, IsTerminal};
use std::sync::Once;

static INIT: Once = Once::new();

#[cfg(windows)]
#[link(name = "kernel32")]
unsafe extern "system" {
    fn GetStdHandle(n_std_handle: u32) -> *mut std::ffi::c_void;
    fn GetConsoleMode(handle: *mut std::ffi::c_void, mode: *mut u32) -> i32;
    fn SetConsoleMode(handle: *mut std::ffi::c_void, mode: u32) -> i32;
}

pub fn init_window_colors() {
    INIT.call_once(|| {
        #[cfg(windows)]
        {
            if io::stdout().is_terminal() {
                unsafe {
                    let handle = GetStdHandle((-11i32) as u32);
                    if !handle.is_null() && handle as isize != -1 {
                        let mut mode = 0;
                        if GetConsoleMode(handle, &mut mode) != 0 {
                            let _ = SetConsoleMode(handle, mode | 0x0004);
                        }
                    }
                }
            }
        }
    });
}
