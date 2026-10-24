using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;

namespace BlinkkShell
{
    internal sealed class RemoteShell
    {
        private readonly int _port;
        private readonly string _authPassword;
        private readonly CancellationTokenSource _shutdown = new();
        private TcpListener? _listener;

        public RemoteShell(int port, string authPassword)
        {
            _port = port;
            _authPassword = authPassword;
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"Listening on all network interfaces, port {_port}.");

            while (!_shutdown.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(_shutdown.Token);
                }
                catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
                {
                    break;
                }
                catch (SocketException) when (_shutdown.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (_shutdown.IsCancellationRequested)
                {
                    break;
                }

                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true })
            {
                try
                {
                    string? authLine = await reader.ReadLineAsync();
                    if (authLine == null || !string.Equals(authLine.Trim(), _authPassword, StringComparison.Ordinal))
                    {
                        await writer.WriteLineAsync("AUTH_FAILED");
                        return;
                    }

                    await writer.WriteLineAsync("AUTH_SUCCESS");

                    string? inputLine;
                    while ((inputLine = await reader.ReadLineAsync()) != null)
                    {
                        inputLine = inputLine.Trim();
                        if (string.Equals(inputLine, "exit", StringComparison.OrdinalIgnoreCase))
                        {
                            await writer.WriteLineAsync("Closing connection...");
                            break;
                        }

                        string response = ProcessCommand(inputLine);
                        await writer.WriteLineAsync(response);
                        await writer.WriteLineAsync("<END_OF_RESPONSE>");
                    }
                }
                catch (IOException exception)
                {
                    Console.Error.WriteLine($"Client connection error: {exception.Message}");
                }
                catch (SocketException exception)
                {
                    Console.Error.WriteLine($"Client socket error: {exception.Message}");
                }
            }
        }

        private string ProcessCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return "Empty command.";

            string[] parts = commandLine.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string command = parts[0].ToLowerInvariant();
            string args = parts.Length > 1 ? parts[1] : string.Empty;

            return command switch
            {
                "whoami" => GetWhoAmIInfo(),
                "ora" => $"Local Date and Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "msg" => ShowMessageBox(args),
                _ => $"Unknown command: '{command}'. Available commands: whoami, ora, msg <message>, exit"
            };
        }

        private string GetWhoAmIInfo()
        {
            string username = Environment.UserName;
            string domain = Environment.UserDomainName;
            if (!OperatingSystem.IsWindows())
                return $"Current User: {domain}\\{username}\nAdmin Mode: Unavailable (Windows only)";

            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            string adminStatus = isAdmin ? "YES (Administrator / Elevated)" : "NO (Standard User)";
            return $"Current User: {domain}\\{username}\nAdmin Mode: {adminStatus}";
        }

        private string ShowMessageBox(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Usage: msg <message_text>";
            if (!OperatingSystem.IsWindows())
                return "The message command is only available on Windows.";

            string encodedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));
            string script = "$message = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" +
                encodedMessage +
                "')); Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.MessageBox]::Show($message, 'BlinkkShell Message')";

            try
            {
                using Process? process = Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell",
                    ArgumentList = { "-NoProfile", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(script)) },
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                return process == null ? "Failed to start the message window." : "Message displayed on screen.";
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                Console.Error.WriteLine($"Unable to display the message: {exception.Message}");
                return "Failed to display the message.";
            }
        }

        public void Stop()
        {
            _shutdown.Cancel();
            _listener?.Stop();
        }
    }
}
