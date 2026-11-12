using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;

namespace BlinkkShell
{
    internal sealed class RemoteShell
    {
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan AuthenticationWindow = TimeSpan.FromMinutes(10);
        private const int MaxConnectionsPerWindow = 20;
        private const int MaxCommandsPerWindow = 120;
        private const int MaxFailedAuthentications = 5;

        private readonly int _port;
        private readonly string _authPassword;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly ConcurrentDictionary<IPAddress, ClientRequestState> _clientStates = new();
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

                IPAddress? clientAddress = GetClientAddress(client);
                if (clientAddress == null || !RegisterRequest(clientAddress, RequestKind.Connection))
                {
                    Console.WriteLine($"[!] Connection rejected | IP: {clientAddress?.ToString() ?? "Unknown"}");
                    client.Dispose();
                    continue;
                }

                _ = HandleClientAsync(client, clientAddress);
            }
        }

        private async Task HandleClientAsync(TcpClient client, IPAddress clientAddress)
        {
            string clientIp = clientAddress.ToString();
            Console.WriteLine($"[+] Client connected | IP: {clientIp}");

            try
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
                            if (!RegisterRequest(clientAddress, RequestKind.FailedAuthentication))
                                Console.WriteLine($"[!] IP blocked after repeated failed authentication | IP: {clientIp}");
                            await writer.WriteLineAsync("AUTH_FAILED");
                            return;
                        }

                        await writer.WriteLineAsync("AUTH_SUCCESS");

                        string? inputLine;
                        while ((inputLine = await reader.ReadLineAsync()) != null)
                        {
                            if (!RegisterRequest(clientAddress, RequestKind.Command))
                            {
                                Console.WriteLine($"[!] IP blocked after exceeding the command limit | IP: {clientIp}");
                                break;
                            }

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
                    catch (IOException)
                    {
                    }
                    catch (SocketException)
                    {
                    }
                }
            }
            finally
            {
                Console.WriteLine($"[-] Client disconnected | IP: {clientIp}");
            }
        }

        public void Stop()
        {
            _shutdown.Cancel();
            _listener?.Stop();
        }

        private static IPAddress? GetClientAddress(TcpClient client)
        {
            IPAddress? address = ((IPEndPoint?)client.Client.RemoteEndPoint)?.Address;
            return address?.IsIPv4MappedToIPv6 == true ? address.MapToIPv4() : address;
        }

        private bool RegisterRequest(IPAddress address, RequestKind kind)
        {
            ClientRequestState state = _clientStates.GetOrAdd(address, static _ => new ClientRequestState());
            DateTimeOffset now = DateTimeOffset.UtcNow;

            lock (state)
            {
                if (state.IsBlocked)
                    return false;

                switch (kind)
                {
                    case RequestKind.Connection:
                        if (!RegisterWithinWindow(state.Connections, now, RateLimitWindow, MaxConnectionsPerWindow))
                            state.IsBlocked = true;
                        break;
                    case RequestKind.Command:
                        if (!RegisterWithinWindow(state.Commands, now, RateLimitWindow, MaxCommandsPerWindow))
                            state.IsBlocked = true;
                        break;
                    case RequestKind.FailedAuthentication:
                        if (!RegisterWithinWindow(state.FailedAuthentications, now, AuthenticationWindow, MaxFailedAuthentications))
                            state.IsBlocked = true;
                        break;
                }

                return !state.IsBlocked;
            }
        }

        private static bool RegisterWithinWindow(Queue<DateTimeOffset> requests, DateTimeOffset now, TimeSpan window, int limit)
        {
            while (requests.Count > 0 && now - requests.Peek() >= window)
                requests.Dequeue();

            if (requests.Count >= limit)
                return false;

            requests.Enqueue(now);
            return true;
        }

        private enum RequestKind
        {
            Connection,
            Command,
            FailedAuthentication
        }

        private sealed class ClientRequestState
        {
            public Queue<DateTimeOffset> Connections { get; } = new();
            public Queue<DateTimeOffset> Commands { get; } = new();
            public Queue<DateTimeOffset> FailedAuthentications { get; } = new();
            public bool IsBlocked { get; set; }
        }
    }
}