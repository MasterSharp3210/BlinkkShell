# BlinkkShell

BlinkkShell is a small TCP client-server application for sending a limited set of predefined commands from one computer to another. The server is written in C#/.NET and the interactive client is written in Rust.

> BlinkkShell is not a full system shell: the server does **not** execute arbitrary PowerShell or Command Prompt commands. It only accepts the commands listed below.

## Features

- Starts a TCP server on port `8020`.
- Protects access with a password chosen when the server starts.
- Lets a client connect from another device on the same network or from the server computer itself.
- Supports the following commands:
  - `this`: displays the internal shell channel with UUID, PID and gateway info
  - `whoami`: displays the account running the server and, on Windows, whether the process is elevated as an administrator.
  - `msg <text>`: displays a message box on the server computer (Windows only).
  - `time`: displays the server computer's local date and time.
  - `exit`: closes the current client session without stopping the server.
- Handles multiple client connections.

## Requirements

- **.NET SDK 10** to build and run the server.
- **Rust and Cargo** to build and run the client.
- Windows is required for the `msg` command and to report administrator elevation. The server and client can be built on other platforms, but these features are Windows-specific.
- For connections from another device, both computers must be able to communicate over the network, and the server's firewall must allow inbound TCP connections on port `8020`.

## Run in development mode

Open two terminals.

### 1. Start the server

From the project root:

```powershell
dotnet run --project .\BlinkkShell.csproj
```

Enter a password when prompted. The password characters are hidden as you type. The server prints its process ID and available IPv4 addresses; keep this terminal open while using the client.

To connect from the same computer, use `127.0.0.1:8020`. To connect from another device on the same network, use one of the LAN IPv4 addresses printed by the server, followed by `:8020` (for example, `192.168.1.50:8020`).

### 2. Start the client

In the second terminal:

```powershell
Set-Location .\Sender\BlinkkShell
cargo run
```

When prompted, enter the server address and the same password configured on the server. After authentication, the `BlinkkShell >` prompt appears; enter one of the supported commands.

Type `exit` to leave the client session. To stop the server, press **Enter** in the server terminal.

## Build

The following commands create optimized builds for each project:

```powershell
# .NET server (from the project root)
dotnet build .\BlinkkShell.csproj -c Release

# Rust client
Set-Location .\Sender\BlinkkShell
cargo build --release
```

On Windows, the compiled client is at `Sender\BlinkkShell\target\release\BlinkkShell.exe` (the executable name differs on other platforms). The .NET server output is under `bin\Release\net10.0\`, in the runtime/configuration subdirectory selected by .NET.

To publish the server for Windows x64 as a framework-dependent deployment:

```powershell
dotnet publish .\BlinkkShell.csproj -c Release -r win-x64 --self-contained false
```

The published server requires the .NET 10 runtime to be installed on the server computer. The compiled Rust client does not require a separate Rust runtime.

## Windows Firewall rule

If a client on another device cannot connect, allow inbound TCP traffic on port `8020` for a trusted private network. Run the following in PowerShell as an administrator:

```powershell
New-NetFirewallRule -DisplayName "BlinkkShell" -Direction Inbound -Protocol TCP -LocalPort 8020 -Action Allow -Profile Private
```

Avoid creating a broader rule or allowing traffic on public network profiles unless there is a specific need.

## Commands

| Command | Effect |
| --- | --- |
| `whoami` | Returns the user and domain of the server account. On Windows, also reports whether the server is running as an administrator. |
| `ora` | Returns the server's local date and time in `yyyy-MM-dd HH:mm:ss` format. |
| `msg <text>` | Opens a message box with the specified text on the server. Windows only; provide text after the command. |
| `exit` | Closes the current connection. The server keeps running. |

Commands are case-insensitive. `msg` accepts the text following the command name; the text is passed to PowerShell as encoded data, not as a PowerShell command to execute.

## How it works

1. The server prompts for a password and listens on TCP port `8020` on all network interfaces.
2. The client opens a TCP connection to the supplied address and sends the password on a line. The server responds with `AUTH_SUCCESS` or `AUTH_FAILED`.
3. After authentication, the client sends one command per line. The server processes only the documented commands and returns the result, terminated by the `<END_OF_RESPONSE>` marker.
4. `exit` ends the client session. Pressing Enter in the server console stops the listener.

The client hides password input when running in an interactive terminal.

## Project structure

```text
.
├── BlinkkShell.csproj             # .NET server project
├── Program.cs                     # Startup, password prompt, and network addresses
├── RemoteShell.cs                 # TCP listener, authentication, and commands
└── Sender/
    └── BlinkkShell/
        ├── Cargo.toml             # Rust client project
        └── src/main.rs            # Interactive client
```

## Security and limitations

- The connection is not encrypted: passwords, commands, and responses are sent in clear text over the network.
- The password provides basic access control; it is not a replacement for TLS or a robust authentication system.
- Use BlinkkShell only on a trusted private network. **Do not expose port `8020` to the Internet** or forward it through your router.
- The server accepts connections on all network interfaces, so restrict access through your firewall and network configuration.
- `msg` displays a window on the server computer. A successful response means the PowerShell process was started; it does not confirm that anyone read the message.
- `whoami` reports the privileges of the server process. Running the server as an administrator is not required for normal features and increases risk.

## Troubleshooting

- **Unable to connect:** Make sure the server is running, the address is correct, and the client can reach the host over the network. When connecting from another computer, do not use `127.0.0.1`; that address refers to the client computer itself.
- **Connection refused or timed out:** Check the firewall rule for TCP port `8020` and confirm that both devices are on the expected network.
- **Authentication failed:** Enter the exact password configured when the server started.
- **Port already in use:** Stop the other process listening on port `8020` before starting BlinkkShell.
- **`msg` does not display a window:** This command is available only on Windows and displays the message on the server computer, not the client.
- **Unknown command:** Only `whoami`, `ora`, `msg <text>`, and `exit` are supported.
