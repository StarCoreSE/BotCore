using System.Text;
using Discord;
using Discord.WebSocket;
using Discord.Net;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System;
using System.Reflection;
using BotCore.Modules;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BotCore
{
    public class Program
    {
        private static DiscordSocketClient _client;
        public static Program I;
        private static ConfigWrapper _config;
        private static InteractionService _interactionService;

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
            await Task.Delay(Timeout.Infinite);
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
            _client.SlashCommandExecuted += SlashCommandHandler;

            var guildCommand = new SlashCommandBuilder()
                .WithName("list-roles")
                .WithDescription("Lists all roles of a user.")
                .AddOption("user", ApplicationCommandOptionType.User, "The users whos roles you want to be listed", isRequired: true);

            try
            {
                await _client.Rest.CreateGlobalCommand(guildCommand.Build());
            }
            catch(HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }

            Console.WriteLine("Initialized client.");
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Console.WriteLine("WE GOT A COMMAND !!!");
            // Let's add a switch statement for the command name so we can handle multiple commands in one event.
            switch(command.Data.Name)
            {
                case "list-roles":
                    await HandleListRoleCommand(command);
                    break;
            }
        }

        private async Task HandleListRoleCommand(SocketSlashCommand command)
        {
            // We need to extract the user parameter from the command. since we only have one option and it's required, we can just use the first option.
            var guildUser = (SocketGuildUser)command.Data.Options.First().Value;

            // We remove the everyone role and select the mention of each role.
            var roleList = string.Join(",\n", guildUser.Roles.Where(x => !x.IsEveryone).Select(x => x.Mention));

            var embedBuiler = new EmbedBuilder()
                .WithAuthor(guildUser.ToString(), guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl())
                .WithTitle("Roles")
                .WithDescription(roleList)
                .WithColor(Color.Green)
                .WithCurrentTimestamp();

            // Now, Let's respond with the embed.
            await command.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);
        }
    }
}
