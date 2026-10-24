use std::time::Duration;

pub fn sleep(ms: u64) {
    std::thread::sleep(Duration::from_millis(ms));
}
