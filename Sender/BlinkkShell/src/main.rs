use std::io::{self, BufRead, BufReader, IsTerminal, Write};
use std::net::TcpStream;

use simpled_rlang::prelude::*;

#[cfg(windows)]
#[link(name = "kernel32")]
unsafe extern "system" {
    fn GetStdHandle(n_std_handle: u32) -> *mut std::ffi::c_void;
    fn GetConsoleMode(handle: *mut std::ffi::c_void, mode: *mut u32) -> i32;
    fn SetConsoleMode(handle: *mut std::ffi::c_void, mode: u32) -> i32;
}

fn enable_terminal_colors() -> io::Result<()> {
    #[cfg(windows)]
    if io::stdout().is_terminal() {
        unsafe {
            let handle = GetStdHandle((-11i32) as u32);
            if handle.is_null() || handle as isize == -1 {
                return Err(io::Error::last_os_error());
            }

            let mut mode = 0;
            if GetConsoleMode(handle, &mut mode) == 0 {
                return Err(io::Error::last_os_error());
            }

            if SetConsoleMode(handle, mode | 0x0004) == 0 {
                return Err(io::Error::last_os_error());
            }
        }
    }

    Ok(())
}

fn main() {
    if let Err(error) = run() {
        eprintln!("{}", format!("Sender error: {error}").red().bold());
        wait_for_exit_if_interactive();
    }
}

fn run() -> io::Result<()> {
    if let Err(error) = enable_terminal_colors() {
        eprintln!("Unable to enable terminal colors: {error}");
    }

    println!("{}", "=== BlinkkShell Client v1.0.5 ===".cyan().bold());

    print!(
        "{} ",
        "Enter server address (e.g., 192.168.1.50:8020; use 127.0.0.1 only on the server computer):"
            .yellow()
    );
    io::stdout().flush()?;
    let mut address = String::new();
    io::stdin().read_line(&mut address)?;
    let address = address.trim();
    if address.is_empty() {
        eprintln!("{}", "A connection address is required.".red());
        wait_for_exit_if_interactive();
        return Ok(());
    }

    let password = match read_password() {
        Ok(password) => password,
        Err(error) => {
            eprintln!("{}", format!("Unable to read the password: {error}").red());
            wait_for_exit_if_interactive();
            return Ok(());
        }
    };

    println!("{}", format!("Connecting to {address}...").blue());
    let mut stream = match TcpStream::connect(address) {
        Ok(stream) => stream,
        Err(error) => {
            eprintln!(
                "{}",
                format!(
                    "Unable to connect to {address}: {error}. Check that the server is running, use its LAN IPv4 address (not 127.0.0.1), and allow inbound TCP port 8020 in Windows Firewall."
                )
                .red()
            );
            wait_for_exit_if_interactive();
            return Ok(());
        }
    };
    let mut reader = BufReader::new(stream.try_clone()?);

    writeln!(stream, "{password}")?;

    let mut auth_response = String::new();
    if reader.read_line(&mut auth_response)? == 0 {
        eprintln!(
            "{}",
            "The server closed the connection during authentication.".red()
        );
        wait_for_exit_if_interactive();
        return Ok(());
    }

    if auth_response.trim() != "AUTH_SUCCESS" {
        eprintln!(
            "{}",
            "Authentication failed! Connection closed.".red().bold()
        );
        wait_for_exit_if_interactive();
        return Ok(());
    }

    println!(
        "{}",
        "Authentication successful! Shell Channel running. Type 'exit' to quit.\n"
            .green()
            .bold()
    );

    loop {
        print!("{} ", "BlinkkShell > ".green().bold());
        io::stdout().flush()?;

        let mut command = String::new();
        if io::stdin().read_line(&mut command)? == 0 {
            break;
        }
        let command = command.trim();

        if command.is_empty() {
            continue;
        }

        writeln!(stream, "{command}")?;

        if command.eq_ignore_ascii_case("exit") {
            println!("{}", "Closing connection...".yellow());
            break;
        }

        loop {
            let mut line = String::new();
            if reader.read_line(&mut line)? == 0 {
                println!("{}", "Server closed the connection.".red());
                wait_for_exit_if_interactive();
                return Ok(());
            }

            if line.trim() == "<END_OF_RESPONSE>" {
                break;
            }

            print!("{line}");
        }
    }

    Ok(())
}

fn read_password() -> io::Result<String> {
    if io::stdin().is_terminal() {
        return rpassword::prompt_password("Enter password: ");
    }

    print!("Enter password: ");
    io::stdout().flush()?;
    let mut password = String::new();
    if io::stdin().read_line(&mut password)? == 0 {
        return Err(io::Error::new(
            io::ErrorKind::UnexpectedEof,
            "password input ended",
        ));
    }
    if password.ends_with('\n') {
        password.pop();
        if password.ends_with('\r') {
            password.pop();
        }
    }
    Ok(password)
}

fn wait_for_exit_if_interactive() {
    if io::stdin().is_terminal() {
        println!("Press ENTER to close.");
        let mut input = String::new();
        let _ = io::stdin().read_line(&mut input);
    }
}
