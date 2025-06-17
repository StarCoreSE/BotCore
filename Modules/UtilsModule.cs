using System.Diagnostics;
using System.Text;
using Discord;

namespace BotCore.Modules
{
    public static class UtilsModule
    {
        public static IMessageChannel? GetChannel(ulong guildId, ulong channelId)
        {
            return Program.Client.GetGuild(guildId)?.GetChannel(channelId) as IMessageChannel;
        }

        public static readonly Random Random = new();

        public static async Task<(int, string)> RunProcess(string path, string? workingDir = null)
        {
            var processInfo = new ProcessStartInfo(path)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDir ?? Path.GetDirectoryName(path),
            };

            var process = Process.Start(processInfo);
            if (process == null)
                throw new ArgumentException();

            StringBuilder output = new();
            process.OutputDataReceived += (_, e) =>
                output.AppendLine(e.Data);
            process.BeginOutputReadLine();

            await process.WaitForExitAsync();
            int exitCode = process.ExitCode;
            process.Close();

            return (exitCode, output.ToString());
        }
    }
}
