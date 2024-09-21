using System.Text;
using Discord;
using Discord.WebSocket;
using Discord.Net;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System;
using System.Reflection;
using BotCore.Modules;
using BotCore.Modules.BracketModules;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BotCore
{
    public class Program
    {
        public static DiscordSocketClient Client;
        public static Program? I;
        public static ConfigWrapper Config;
        private static InteractionService _interactionService;

        #region Static Methods

        public static async Task Main()
        {
            Client = new DiscordSocketClient();
            Client.Log += Log;

            I = new Program();

            Config = JsonSerializer.Deserialize<ConfigWrapper>(File.ReadAllText("config.json")) ?? throw new NullReferenceException("config.json is null!");

            await Client.LoginAsync(TokenType.Bot, Config.Token);
            await Client.StartAsync();

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
            Client.Ready += OnClientReady;
        }

        public async Task OnClientReady()
        {
            Task.WaitAll(
                CommandModule.RegisterCommands(Client),
                ActivityModule.RegisterActivity(Client),
                TournamentsModule.LoadExistingTournaments(),
                BracketModuleBase.LoadData()
                );

            Console.WriteLine("Initialized client.");
        }
    }
}
