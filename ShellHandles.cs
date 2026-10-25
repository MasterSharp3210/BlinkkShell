using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace BlinkkShell
{
    internal static class ShellHandles
    {
        public static string ProcessCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return "Empty command.";

            string[] parts = commandLine.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string command = parts[0].ToLowerInvariant();
            string args = parts.Length > 1 ? parts[1] : string.Empty;

            return command switch
            {
                "help" => GetHelpText(),
                "this" => GetShellInfo(),
                "whoami" => GetWhoAmIInfo(),
                "msg" => ShowMessageBox(args),
                "ping" => "Ping Pong!",
                "shelluptime" => $"Shell Uptime: {DateTime.Now - Process.GetCurrentProcess().StartTime}",
                "sysinfo" => GetSystemInfo(),
                "time" => GetLocalTime(),
                "uptime" => $"System Uptime: {TimeSpan.FromMilliseconds(Environment.TickCount64)}",
                _ => $"Unknown command: '{command}'. Type 'help' to see the available commands."
            };
        }

        private static string GetHelpText()
        {
            return """
                BlinkkShell commands
                ====================
                  help           Show this command list.
                  this           Show BlinkkShell server and process details.
                  sysinfo        Show operating system and runtime details.
                  whoami         Show the server account and administrator status.
                  uptime         Show how long the computer has been running.
                  shelluptime    Show how long the BlinkkShell process has been running.
                  time           Show the server's local date and time.
                  ping           Check that the server responds.
                  msg <text>     Display a message on the server (Windows only).
                  exit           Close this client session.

                Commands are case-insensitive.
                """;
        }

        private static string GetLocalTime()
        {
            return $"Local Date and Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        private static string GetShellInfo()
        {
            Process currentProcess = Process.GetCurrentProcess();
            return $"""
                BlinkkShell Channel
                =================
                  Version     : 1.0.5
                  Computer    : {Environment.MachineName}
                  Account     : {Environment.UserName}
                  Process ID  : {currentProcess.Id}
                """;
        }

        private static string GetSystemInfo()
        {
            return $"""
                System information
                ==================
                  {"Computer",-21}: {Environment.MachineName}
                  {"Operating system",-21}: {RuntimeInformation.OSDescription}
                  {"OS architecture",-21}: {RuntimeInformation.OSArchitecture}
                  {"Process architecture",-21}: {RuntimeInformation.ProcessArchitecture}
                  {"Logical processors",-21}: {Environment.ProcessorCount}
                  {".NET runtime",-21}: {RuntimeInformation.FrameworkDescription}
                """;
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
                "')); Add-Type -AssemblyName System.Windows.Forms; " +
                "$form = New-Object System.Windows.Forms.Form; " +
                "$form.TopMost = $true; " +
                "[System.Windows.Forms.MessageBox]::Show($form, $message, 'BlinkkShell Message', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information); " +
                "$form.Dispose();";

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
                return $"Failed to display the message: {exception.Message}";
            }
        }
    }
}
