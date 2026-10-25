use std::io::{self};

pub fn read() -> String {
    let mut input = String::new();
    if io::stdin().read_line(&mut input).is_err() {
        return String::new();
    }
    input
}