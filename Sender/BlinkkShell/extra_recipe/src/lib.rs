mod colors;
mod get_address;
mod simple_read;
mod sleep_thread;
mod style;

pub use colors::Color;
pub use style::{AnsiStyle, Styled};
pub use get_address::{get_address, print_address};

pub fn sleep(ms: u64) {
    sleep_thread::sleep(ms);
}

pub fn read() -> String {
    simple_read::read()
}

pub const fn rgb(r: u8, g: u8, b: u8) -> Color {
    Color::Rgb(r, g, b)
}

pub const fn ansi256(value: u8) -> Color {
    Color::Ansi256(value)
}

pub fn style(text: impl Into<String>) -> Styled {
    Styled::new(text)
}

pub mod prelude {
    pub use crate::{ansi256, get_address, print_address, read, rgb, sleep, style, AnsiStyle, Color, Styled};
}
