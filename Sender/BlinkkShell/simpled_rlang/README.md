# simpled_rlang

`simpled_rlang` is a lightweight Rust library designed to simplify terminal input reading, thread sleeping, and ANSI text styling with minimal boilerplate.

---

## Installation

To use this library in your Rust project, clone or copy the `simpled_rlang` directory into a location adjacent to your project (or within your workspace). 

Then, add the path dependency to your project's `Cargo.toml`:

```toml
[dependencies]
simpled_rlang = { path = "../simpled_rlang" }
```

---

## Usage Guide

By importing the prelude at the top of your file, all core features—reading input, thread sleeping, and ANSI styling—become immediately available.

```rust
use simpled_rlang::prelude::*;

fn main() {
    // 1. ANSI Styling on strings
    println!("{}", "Hello, world!".bold().green());
    println!("{}", "Warning message".yellow().italic());

    // 2. Get Addresses
    let x: i32 = 5;
    let mut address = get_address(&x);
    print_address(&x);

    // 3. Thread Sleep (in milliseconds)
    println!("Waiting for 1 second...");
    sleep(1000);

    // 4. Simple Terminal Input Reading
    println!("{}", "Please enter your name:".cyan());
    let name = read();

    println!("{}", format!("Welcome, {}!", name).bold().magenta());
}
```

---

## Direct Usage (Without Prelude Import)

If you prefer calling functions directly from the crate name without importing traits:

```rust
fn main() {
    // Get Addresses
    let x: i32 = 5;
    let mut address = simpled_rlang::get_address(&x);
    simpled_rlang::print_address(&x);
    
    // Thread Sleep
    simpled_rlang::sleep(500);

    // Terminal Input
    let input = simpled_rlang::read();

    // ANSI Styling via style helper
    println!("{}", simpled_rlang::style("Styled Text").bold().red());
}
```

---

## Issues & Support

If you encounter any bugs, have questions, or want to suggest new features, please open an issue directly on the repository!
