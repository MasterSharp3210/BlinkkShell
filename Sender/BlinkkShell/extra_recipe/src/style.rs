use crate::Color;
use std::fmt;

#[derive(Debug, Clone)]
pub struct Styled {
    text: String,
    codes: Vec<String>,
}

impl Styled {
    pub fn new(text: impl Into<String>) -> Self {
        Self {
            text: text.into(),
            codes: Vec::new(),
        }
    }

    fn add_code(mut self, code: impl Into<String>) -> Self {
        self.codes.push(code.into());
        self
    }

    pub fn fg(self, color: Color) -> Self {
        self.add_code(color.foreground_code())
    }

    pub fn bg(self, color: Color) -> Self {
        self.add_code(color.background_code())
    }

    pub fn black(self) -> Self {
        self.fg(Color::Black)
    }

    pub fn red(self) -> Self {
        self.fg(Color::Red)
    }

    pub fn green(self) -> Self {
        self.fg(Color::Green)
    }

    pub fn yellow(self) -> Self {
        self.fg(Color::Yellow)
    }

    pub fn blue(self) -> Self {
        self.fg(Color::Blue)
    }

    pub fn magenta(self) -> Self {
        self.fg(Color::Magenta)
    }

    pub fn cyan(self) -> Self {
        self.fg(Color::Cyan)
    }

    pub fn white(self) -> Self {
        self.fg(Color::White)
    }

    pub fn bright_red(self) -> Self {
        self.fg(Color::BrightRed)
    }

    pub fn bright_green(self) -> Self {
        self.fg(Color::BrightGreen)
    }

    pub fn bright_yellow(self) -> Self {
        self.fg(Color::BrightYellow)
    }

    pub fn bright_blue(self) -> Self {
        self.fg(Color::BrightBlue)
    }

    pub fn bold(self) -> Self {
        self.add_code("1")
    }

    pub fn dim(self) -> Self {
        self.add_code("2")
    }

    pub fn italic(self) -> Self {
        self.add_code("3")
    }

    pub fn underline(self) -> Self {
        self.add_code("4")
    }

    pub fn blink(self) -> Self {
        self.add_code("5")
    }

    pub fn reverse(self) -> Self {
        self.add_code("7")
    }

    pub fn hidden(self) -> Self {
        self.add_code("8")
    }

    pub fn strikethrough(self) -> Self {
        self.add_code("9")
    }

    pub fn ansi(self, code: impl Into<String>) -> Self {
        self.add_code(code)
    }

    pub fn render(&self) -> String {
        if self.codes.is_empty() {
            return self.text.clone();
        }

        format!(
            "\x1b[{}m{}\x1b[0m",
            self.codes.join(";"),
            self.text
        )
    }
}

impl fmt::Display for Styled {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        f.write_str(&self.render())
    }
}

pub trait AnsiStyle {
    fn styled(self) -> Styled;

    fn red(self) -> Styled
    where
        Self: Sized,
    {
        self.styled().red()
    }

    fn green(self) -> Styled
    where
        Self: Sized,
    {
        self.styled().green()
    }

    fn yellow(self) -> Styled
    where
        Self: Sized,
    {
        self.styled().yellow()
    }

    fn blue(self) -> Styled
    where
        Self: Sized,
    {
        self.styled().blue()
    }

    fn cyan(self) -> Styled
    where
        Self: Sized,
    {
        self.styled().cyan()
    }

    fn magenta(self) -> Styled
    where
        Self: Sized,
    {
        self.styled().magenta()
    }

    fn bold(self) -> Styled
    where
        Self: Sized,
    {
        self.styled().bold()
    }

    fn underline(self) -> Styled
    where
        Self: Sized,
    {
        self.styled().underline()
    }

    fn italic(self) -> Styled
    where
        Self: Sized,
    {
        self.styled().italic()
    }

    fn dim(self) -> Styled
    where
        Self: Sized,
    {
        self.styled().dim()
    }

    fn strikethrough(self) -> Styled
    where
        Self: Sized,
    {
        self.styled().strikethrough()
    }
}

impl<T> AnsiStyle for T
where
    T: Into<String>,
{
    fn styled(self) -> Styled {
        Styled::new(self)
    }
}
