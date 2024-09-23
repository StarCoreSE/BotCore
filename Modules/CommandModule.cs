using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Discord.Net;
using Discord.Rest;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;
using Microsoft.VisualBasic.FileIO;
using System.Xml.Linq;
using BotCore.Modules.BracketModules;
using System.Diagnostics;

namespace BotCore.Modules
{
    public class CommandModule
    {
        private static readonly List<SlashCommandBuilder> SlashCommands =
        [
            new SlashCommandBuilder
            {
                Name = "sc-ping",
                Description = "Ping command to test if the bot is online.",
            },
            new SlashCommandBuilder
            {
                Name = "sc-create-tt",
                Description = "Generate a new Test Tournament.",
                Options =
                [
                    new SlashCommandOptionBuilder
                    {
                        Name = "name",
                        Type = ApplicationCommandOptionType.String,
                        Description = "The name of this event.",
                        IsRequired = true
                    },
                    new SlashCommandOptionBuilder
                    {
                        Name = "description",
                        Type = ApplicationCommandOptionType.String,
                        Description = "The description for this event.",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Name = "timestamp",
                        Type = ApplicationCommandOptionType.Integer,
                        Description = "Timestamp (in unix seconds) at which the event should be started. https://hammertime.cyou/.",
                        IsRequired = true
                    },
                    new SlashCommandOptionBuilder
                    {
                        Name = "deadline",
                        Type = ApplicationCommandOptionType.Integer,
                        Description = "Timestamp (in unix seconds) at which signups close for the event. https://hammertime.cyou/.",
                    },
                ],
            },
            new SlashCommandBuilder
            {
                Name = "sc-cancel-tt",
                Description = "Cancel a Test Tournament.",
                Options =
                [
                    new SlashCommandOptionBuilder
                    {
                        Name = "tournament",
                        Type = ApplicationCommandOptionType.String,
                        Description = "The name of the event.",
                        IsRequired = true
                    },
                ],
            },
            new SlashCommandBuilder
            {
                Name = "sc-list-teams",
                Description = "List all teams currently signed up.",
                Options =
                [
                    new SlashCommandOptionBuilder
                    {
                        Name = "tournament",
                        Type = ApplicationCommandOptionType.String,
                        Description = "The name of the tournament.",
                        IsRequired = true
                    }
                ],
            },
            new SlashCommandBuilder
            {
                Name = "sc-register",
                Description = "Register your team for the next Test Tournament!",
                Options =
                [
                    new SlashCommandOptionBuilder
                    {
                        Name = "tag",
                        Type = ApplicationCommandOptionType.String,
                        Description = "Your faction tag.",
                        IsRequired = true
                    },
                    new SlashCommandOptionBuilder
                    {
                        Name = "name",
                        Type = ApplicationCommandOptionType.String,
                        Description = "Your faction name.",
                        IsRequired = true
                    },
                    new SlashCommandOptionBuilder
                    {
                        Name = "players",
                        Type = ApplicationCommandOptionType.String,
                        Description = "Players on your team.",
                        IsRequired = true,
                    },
                    new SlashCommandOptionBuilder
                    {
                        Name = "tournament",
                        Type = ApplicationCommandOptionType.String,
                        Description = "The tournament you are registering for.",
                        IsRequired = true,
                    }
                ]
            },
            new SlashCommandBuilder
            {
                Name = "sc-unregister",
                Description = "Unregister your team from the next Test Tournament.",
                Options =
                [
                    new SlashCommandOptionBuilder
                    {
                        Name = "tournament",
                        Type = ApplicationCommandOptionType.String,
                        Description = "The tournament you are registering for.",
                        IsRequired = true,
                    }
                ]
            },
            new SlashCommandBuilder
            {
                Name = "sc-list-elos",
                Description = "List the ELO scores of all players.",
            },
            new SlashCommandBuilder
            {
                Name = "sc-get-elo",
                Description = "List the ELO score of a specific player.",
                Options =
                [
                    new SlashCommandOptionBuilder
                    {
                        Name = "player",
                        Type = ApplicationCommandOptionType.String,
                        Description = "Ping the player here.",
                        IsRequired = true,
                    }
                ]
            }
        ];

        private static readonly Dictionary<string, Func<SocketSlashCommand, Task>> SlashCommandMethods = new()
        {
            ["sc-ping"] = HandlePing,
            ["sc-create-tt"] = CreateTestTournament,
            ["sc-cancel-tt"] = CancelTestTournament,
            ["sc-list-teams"] = ListTeams,
            ["sc-register"] = RegisterTeam,
            ["sc-unregister"] = UnregisterTeam,
            ["sc-list-elos"] = ListPlayerElos,
            ["sc-get-elo"] = GetPlayerElo,
        };

        private static async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (Program.Debug && command.GuildId != Program.DebugGuildId)
                return;
            if (!Program.Debug && command.GuildId == Program.DebugGuildId)
                return;

            try
            {
                await (SlashCommandMethods!.GetValueOrDefault(command.Data.Name, null)?.Invoke(command) ??
                       Task.CompletedTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await command.RespondAsync(text: $"An exception was thrown!\n```\n{ex}\n```", ephemeral: true);
            }
        }

        public static async Task RegisterCommands(DiscordSocketClient client)
        {
            client.SlashCommandExecuted += SlashCommandHandler;

            Console.WriteLine("Registered commands:");
            foreach (var command in SlashCommands)
                Console.WriteLine($"- {command.Name} {string.Join(", ", command.Options?.Select(o => o.IsRequired ?? false ? o.Name : $"({o.Name})") ?? Array.Empty<string>())}: {command.Description}");

            try
            {
                ApplicationCommandProperties[] allCommands = new ApplicationCommandProperties[SlashCommands.Count];
                for (int i = 0; i < SlashCommands.Count; i++)
                    allCommands[i] = SlashCommands[i].Build();

                if (Program.Debug)
                {
                    await client.GetGuild(Program.DebugGuildId).BulkOverwriteApplicationCommandAsync(allCommands);
                }
                else
                {
                    Task.WaitAll(client.Guilds.Select(g => g.BulkOverwriteApplicationCommandAsync(allCommands)).ToArray());
                }

                Console.WriteLine("Commands successfully registered.");
            }
            catch(HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        public static async Task UpdateCommandTournamentList(ulong guildId)
        {
            var choices = new List<ApplicationCommandOptionChoiceProperties>();
            foreach (var tournament in TournamentsModule.GetTournaments(guildId))
            {
                choices.Add(new ApplicationCommandOptionChoiceProperties
                {
                    Name = tournament.Name,
                    Value = tournament.Name
                });
            }
            
            foreach (var command in SlashCommands)
            {
                var tournamentOption = command.Options?.Find(o => o.Name == "tournament");
                if (tournamentOption != null)
                    tournamentOption.Choices = choices;
            }

            ApplicationCommandProperties[] allCommands = new ApplicationCommandProperties[SlashCommands.Count];
            for (int i = 0; i < SlashCommands.Count; i++)
                allCommands[i] = SlashCommands[i].Build();

            await Program.Client.GetGuild(guildId).BulkOverwriteApplicationCommandAsync(allCommands);
        }

        #region Command Methods

        private static async Task HandlePing(SocketSlashCommand command)
        {
            // Now, Let's respond with the embed.
            await command.RespondAsync(text: $"{(DateTimeOffset.Now - command.CreatedAt).TotalMilliseconds}ms - [{(Program.Debug ? "DEBUG" : "RELEASE")}]", ephemeral: true);
        }

        private static async Task ListPlayerElos(SocketSlashCommand command)
        {
            StringBuilder builder = new StringBuilder("All Player ELO Scores:\n");
            foreach (var kvp in BracketModuleBase.PlayerSeeds)
                builder.Append($"{kvp.Key}: {kvp.Value}\n");
            await command.RespondAsync(text: builder.ToString(), ephemeral: true);
        }

        private static async Task GetPlayerElo(SocketSlashCommand command)
        {
            await command.RespondAsync(text: $"{(string) command.Data.Options.First()}: {BracketModuleBase.GetPlayerSeed((string) command.Data.Options.First())}", ephemeral: true);
        }

        private static async Task CreateTestTournament(SocketSlashCommand command)
        {
            string name = "";
            string description = "";
            long? timestamp = null;
            long? deadline = null;
            DateTimeOffset startTime = DateTimeOffset.UnixEpoch;
            DateTimeOffset deadlineTime = DateTimeOffset.UnixEpoch;

            foreach (var data in command.Data.Options)
            {
                switch (data.Name)
                {
                    case "name":
                        name = (string) data.Value;
                        break;
                    case "timestamp":
                        timestamp = (long) data.Value;
                        break;
                    case "description":
                        description = (string) data.Value;
                        break;
                    case "deadline":
                        deadline = (long) data.Value;
                        break;
                }
            }

            if (timestamp != null)
                startTime = DateTimeOffset.FromUnixTimeSeconds(timestamp.Value);
            if (deadline != null)
                deadlineTime = DateTimeOffset.FromUnixTimeSeconds(deadline.Value);

            // Error checking
            if (startTime == DateTimeOffset.UnixEpoch)
            {
                await command.RespondAsync(text: $"Please add either a valid datetime or timestamp.", ephemeral: true);
                return;
            }
            if (startTime < DateTime.Now)
            {
                await command.RespondAsync(text: $"Cannot create an event in the past! ({startTime:g})", ephemeral: true);
                return;
            }
            if (startTime >= DateTime.Now.AddYears(5))
            {
                await command.RespondAsync(text: $"Cannot create an event more than 5 years in the future! ({startTime:g})", ephemeral: true);
                return;
            }

            if (deadlineTime != DateTimeOffset.UnixEpoch)
            {
                if (deadlineTime > startTime)
                {
                    await command.RespondAsync(text: $"Signup deadline cannot be after the tournament start time! ({deadlineTime:g})", ephemeral: true);
                    return;
                }
                if (deadlineTime < DateTimeOffset.Now)
                {
                    await command.RespondAsync(text: $"Cannot make a deadline in the past! ({deadlineTime:g})", ephemeral: true);
                    return;
                }
            }

            var guild = Program.Client.GetGuild(command.GuildId ??
                                                throw new Exception("Command was not executed in a guild!"));

            var newEvent = await guild.CreateEventAsync(name, startTime, GuildScheduledEventType.Voice, description: description, channelId: Program.Config.TournamentVoiceChannel, coverImage: new Image("Resources/StarcoreLogo.png"));
            TournamentsModule.RegisterTournament(new Tournament
            {
                Name = name,
                Description = description,
                StartTime = startTime,
                SignupDeadline = deadlineTime,
                GuildId = command.GuildId.Value,
                EventId = newEvent.Id,
            });

            await command.RespondAsync(text: $"Created new event, https://discord.com/events/{newEvent.GuildId}/{newEvent.Id}", ephemeral: false);
        }

        private static async Task CancelTestTournament(SocketSlashCommand command)
        {
            string name = (string) command.Data.Options.First();

            if (!TournamentsModule.CancelTournament(name))
            {
                await command.RespondAsync(text: $"Tournament `{name}` does not exist!", ephemeral: true);
                return;
            }

            await command.RespondAsync(text: $"Cancelled `{name}`.", ephemeral: false);
        }

        private static async Task ListTeams(SocketSlashCommand command)
        {
            Tournament? tournament = TournamentsModule.GetTournament(command.GuildId ?? throw new Exception("Command cannot be run outside of a server!"), (string) command.Data.Options.First().Value);

            if (tournament == null)
            {
                await command.RespondAsync(text: $"Tournament does not exist! Valid options:\n- {string.Join("\n- ", TournamentsModule.GetTournaments(command.GuildId.Value))}", ephemeral: true);
                return;
            }

            await command.RespondAsync(text: $"Teams registered for {tournament.Name}:", ephemeral: true);

            foreach (var team in tournament.TeamsModule.Teams)
                await command.FollowupAsync(embed: team.GenerateEmbed().Build(), ephemeral: true);
        }

        private static async Task RegisterTeam(SocketSlashCommand command)
        {
            string name = "";
            string tag = "";
            string[] users = Array.Empty<string>();
            Tournament? tournament = null;
            foreach (var option in command.Data.Options)
            {
                switch (option.Name)
                {
                    case "name":
                        name = (string) option.Value;
                        break;
                    case "tag":
                        tag = (string) option.Value;
                        break;
                    case "players":
                        users = Regex.Replace((string) option.Value, "[^<\\d@>]", "").Replace(">", ">,").Split(",").Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
                        break;
                    case "tournament":
                        tournament = TournamentsModule.GetTournament(command.GuildId ?? throw new Exception("Command cannot be run outside of a server!"), (string) option.Value);
                        break;
                }
            }

            if (tournament == null)
            {
                await command.RespondAsync(text: $"Failed to register team! Reason:\n> Tournament `{tournament}` does not exist! Valid options:\n- {string.Join("\n- ", TournamentsModule.GetTournaments(command.GuildId.Value))}", ephemeral: true);
                return;
            }

            string failReason;
            if (!tournament.TeamsModule.RegisterTeam(name, tag, command.User.Mention, users, out failReason))
            {
                await command.RespondAsync(text: "Failed to register team! Reason:\n> " + failReason, ephemeral: true);
                return;
            }

            await command.RespondAsync(embed: tournament.TeamsModule.Teams.Find(t => t.Leader == command.User.Mention).GenerateEmbed(command.User).WithTitle($"*{(failReason == "" ? "Registered" : "Updated")} [{tag}] {name}*").Build(), ephemeral: false);
        }

        private static async Task UnregisterTeam(SocketSlashCommand command)
        {
            Tournament? tournament = TournamentsModule.GetTournament(command.GuildId ?? throw new Exception("Command cannot be run outside of a server!"), (string) command.Data.Options.First().Value);

            if (tournament == null || !tournament.TeamsModule.UnregisterTeam(command.User.Mention, out var team))
            {
                await command.RespondAsync(text: "Failed to unregister team - you aren't signed up!", ephemeral: true);
                return;
            }

            await command.RespondAsync(embed: team.GenerateEmbed(command.User).WithTitle($"*Unregistered [{team.Tag}] {team.Name}*").Build(), ephemeral: false);
        }

        #endregion
    }
}
