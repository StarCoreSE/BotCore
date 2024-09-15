using System.Text;
using Discord;
using Discord.WebSocket;
using Discord.Net;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System;
using Discord.Interactions;

namespace BotCore
{
    public class Program
    {
        private static DiscordSocketClient _client;
        public static Program I;
        private static ConfigWrapper _config;

        #region Static Methods

        public static async Task Main()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;

            I = new Program();

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

        #endregion

        public Program()
        {
            _client.Ready += OnClientReady;
        }

        public async Task OnClientReady()
        {
            // Let's do our global command
            var globalCommand = new SlashCommandBuilder
            {
                Name = "first-global-command",
                Description = "This is my first global slash command"
            };

            await RegisterCommand(globalCommand);

            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.UserCommandExecuted += UserCommandHandler;
            var _interactionService = new InteractionService(_client.Rest);
        }

        public static async Task RegisterCommand(params SlashCommandBuilder[] commands)
        {
            try
            {
                ApplicationCommandProperties[] commandProperties = new ApplicationCommandProperties[commands.Length];
                for (int i = 0; i < commands.Length; i++)
                    commandProperties[i] = commands[i].Build();

                await _client.BulkOverwriteGlobalApplicationCommandsAsync(commandProperties);
                Console.WriteLine($"Registered {commands.Length} commands.");
            }
            catch (HttpException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Console.WriteLine($"{command.User.GlobalName} executed {command.Data.Name}");
            await command.RespondAsync($"You executed {command.Data.Name}");
        }
        
        private async Task UserCommandHandler(SocketUserCommand command)
        {
            Console.WriteLine($"{command.User.GlobalName} executed {command.Data.Name}");
            await command.RespondAsync($"You executed {command.Data.Name}");
        }
    }
}
