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
        public static DiscordSocketClient Client;
        public static Program? I;
        private static ConfigWrapper _config;
        private static InteractionService _interactionService;

        #region Static Methods

        public static async Task Main()
        {
            Client = new DiscordSocketClient();
            Client.Log += Log;

            I = new Program();

            _config = JsonSerializer.Deserialize<ConfigWrapper>(File.ReadAllText("config.json")) ?? throw new NullReferenceException("config.json is null!");

            await Client.LoginAsync(TokenType.Bot, _config.Token);
            await Client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(Timeout.Infinite);
            I = null;
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
            await CommandModule.RegisterCommands(Client);
            await ActivityModule.RegisterActivity(Client);

            Console.WriteLine("Initialized client.");
        }
    }
}
