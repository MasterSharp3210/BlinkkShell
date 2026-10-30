use std::io::{self, BufRead, BufReader, IsTerminal, Write};
use std::net::TcpStream;
use std::time::Instant;

use simpled_rlang::prelude::*;

fn main() {
    if let Err(error) = run() {
        eprintln!("{}", format!("Sender error: {error}").red().bold());
        wait_for_exit_if_interactive();
    }
}

fn run() -> io::Result<()> {

    println!("{}", "=== BlinkkShell Client v1.0.5 ===".cyan().bold());

    print!(
        "{} ",
        "Enter server address (e.g., 192.168.1.50:8020; use 127.0.0.1 only on the server computer):"
            .yellow()
    );
    io::stdout().flush()?;
    
    let address = read();
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

        let command = read();
        let command = command.trim();

        if command.is_empty() {
            continue;
        }

        let is_ping = command.eq_ignore_ascii_case("ping");
        let ping_start = is_ping.then(Instant::now);

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

        if let Some(start) = ping_start {
            println!(
                "Ping: {:.2} ms",
                start.elapsed().as_secs_f64() * 1_000.0
            );
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
    
    let password = read();
    let mut password = password.trim_end_matches(&['\r', '\n'][..]).to_string();
    
    if password.is_empty() {
        return Err(io::Error::new(
            io::ErrorKind::UnexpectedEof,
            "password input ended",
        ));
    }
    Ok(password)
}

fn wait_for_exit_if_interactive() {
    if io::stdin().is_terminal() {
        println!("Press ENTER to close.");
        let _ = read();
    }
}