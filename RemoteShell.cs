using System.IO;
using System.Net;
using System.Net.Sockets;
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

                        string response = ShellHandles.ProcessCommand(inputLine);
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

        public void Stop()
        {
            _shutdown.Cancel();
            _listener?.Stop();
        }
    }
}
