using System.Text;
using Discord;
using Discord.WebSocket;
using System.Text.Json;

namespace BotCore
{
    public class Program
    {
        private static DiscordSocketClient _client;
        private static ConfigWrapper _config;

        public static async Task Main()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;

            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            // var token = "token";

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");

            _config = JsonSerializer.Deserialize<ConfigWrapper>(File.ReadAllText("config.json")) ?? throw new NullReferenceException("config.json is null!");

            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
