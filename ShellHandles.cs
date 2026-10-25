using System.Diagnostics;
using System.Security.Principal;
using System.Text;

namespace BlinkkShell
{
    internal static class ShellHandles
    {
        private const string AvailableCommands = "this, whoami, msg <message>, time, exit";

        public static string ProcessCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return "Empty command.";

            string[] parts = commandLine.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string command = parts[0].ToLowerInvariant();
            string args = parts.Length > 1 ? parts[1] : string.Empty;

            return command switch
            {
                "msg" => ShowMessageBox(args),
                "time" => GetLocalTime(),
                "this" => GetShellInfo(),
                "whoami" => GetWhoAmIInfo(),
                _ => $"Unknown command: '{command}'. Available commands: {AvailableCommands}"
            };
        }

        private static string GetLocalTime()
        {
            return $"Local Date and Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        private static string GetShellInfo()
        {
            return $"BlinkkShell v.1.0.0 - {Environment.UserName}@{Environment.MachineName} - Remote Channel PID: {Process.GetCurrentProcess().Id}";
        }

        private static string GetWhoAmIInfo()
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

        private static string ShowMessageBox(string message)
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
    }
}
