using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace BlinkkShell
{
    internal static class Program
    {
        private static async Task Main()
        {
            const int port = 8020;
            Console.Write("Enter server password: ");
            string authPassword = ReadPassword();
            if (string.IsNullOrWhiteSpace(authPassword))
            {
                Console.Error.WriteLine("A non-empty server password is required.");
                WaitForExit();
                return;
            }

            Console.WriteLine($"BlinkkShell Server v1.0.5 - {Environment.OSVersion}");
            Console.WriteLine($"Process ID: {Process.GetCurrentProcess().Id}");

            RemoteShell shell = new RemoteShell(port, authPassword);
            Task serverTask = shell.StartAsync();
            Task inputTask = Task.Run(Console.ReadKey);

            IPAddress[] addresses = GetLocalIPv4Addresses();
            if (addresses.Length == 0)
            {
                Console.WriteLine("No active local IPv4 network address was found.");
            }
            else
            {
                Console.WriteLine("Available local IPv4 network addresses:");
                foreach (IPAddress address in addresses)
                    Console.WriteLine($"  {address}:{port}");
            }

            Console.WriteLine("Press ENTER to stop the server.");

            if (await Task.WhenAny(serverTask, inputTask) == inputTask)
            {
                await inputTask;
                shell.Stop();
                await serverTask;
                return;
            }

            try
            {
                await serverTask;
            }
            catch (SocketException exception)
            {
                Console.Error.WriteLine($"Failed to listen on port {port}: {exception.Message}");
                WaitForExit();
            }
            catch (UnauthorizedAccessException exception)
            {
                Console.Error.WriteLine($"Permission denied while starting the server: {exception.Message}");
                WaitForExit();
            }
        }

        private static void WaitForExit()
        {
            Console.WriteLine("Press ENTER to close.");
            Console.ReadLine();
        }

        private static string ReadPassword()
        {
            if (Console.IsInputRedirected)
                return Console.ReadLine() ?? string.Empty;

            StringBuilder password = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return password.ToString();
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                        password.Length--;
                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                    password.Append(key.KeyChar);
            }
        }

        private static IPAddress[] GetLocalIPv4Addresses()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(networkInterface =>
                    networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(networkInterface => networkInterface.GetIPProperties().UnicastAddresses)
                .Select(unicastAddress => unicastAddress.Address)
                .Where(address =>
                    address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address) &&
                    !address.ToString().StartsWith("169.254.", StringComparison.Ordinal))
                .Distinct()
                .ToArray();
        }
    }
}