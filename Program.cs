﻿using Discord;
using Discord.WebSocket;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Reflection;
using BotCore.Modules;
using BotCore.Modules.BracketModules;
using System.Diagnostics;

namespace BotCore
{
    public class Program
    {
        public static DiscordSocketClient Client;
        public static Program? I;
        public static ConfigWrapper Config;

        public static string VersionHeader { get; private set; } = "";

        #if (DEBUG)
            public const bool Debug = true;
        #elif (RELEASE)
            public const bool Debug = false;
        #endif
        public const ulong DebugGuildId = 1277394685333209250;

        #region Static Methods

        public static async Task Main()
        {
            VersionHeader = $"BotCore Server v{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion} - [{(Debug ? "DEBUG" : "RELEASE")}]\n" +
                            $"  by Aristeas";

            Console.WriteLine(VersionHeader);

            Client = new DiscordSocketClient();
            Client.Log += Log;

            I = new Program();

            Config = JsonSerializer.Deserialize<ConfigWrapper>(File.ReadAllText(Debug ? "config_Debug.json" : "config.json")) ?? throw new NullReferenceException("config.json is null!");

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

            //{
            //    var tournament = new Tournament
            //    {
            //        Name = "Test Tournament",
            //        StartTime = DateTimeOffset.Now,
            //
            //    };
            //    tournament.TeamsModule.Teams = new List<Team>
            //    {
            //        new Team
            //        {
            //            Name = "Test 1",
            //            Tag = "TS1",
            //            Role = 1,
            //            Members = [
            //                "Member11",
            //                "Member12"
            //            ]
            //        },
            //        new Team
            //        {
            //            Name = "Test 2",
            //            Tag = "TS2",
            //            Role = 2,
            //            Members = [
            //                "Member21",
            //                "Member22",
            //            ]
            //        },
            //        new Team
            //        {
            //            Name = "Test 3",
            //            Tag = "TS3",
            //            Role = 3,
            //            Members = [
            //                "Member31",
            //                "Member32",
            //            ]
            //        },
            //        new Team
            //        {
            //            Name = "Test 4",
            //            Tag = "TS4",
            //            Role = 4,
            //            Members = [
            //                "Member41",
            //                "Member42",
            //            ]
            //        },
            //        new Team
            //        {
            //            Name = "Test 5",
            //            Tag = "TS5",
            //            Role = 5,
            //            Members = [
            //                "Member51",
            //                "Member52",
            //            ]
            //        },
            //        new Team
            //        {
            //            Name = "Test 6",
            //            Tag = "TS6",
            //            Role = 6,
            //            Members = [
            //                "Member61",
            //                "Member62",
            //            ]
            //        },
            //    };
            //
            //    new SingleEliminationModule(tournament).GenerateBracket(false);
            //}
        }
    }
}
