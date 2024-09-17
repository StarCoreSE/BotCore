using Discord.Interactions;
using System;
using System.Collections.Generic;
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
                        Name = "timestamp",
                        Type = ApplicationCommandOptionType.Integer,
                        Description = "Timestamp (in unix seconds) at which the event should be started.",
                    },
                    new SlashCommandOptionBuilder
                    {
                        Name = "datetime",
                        Type = ApplicationCommandOptionType.String,
                        Description = "Day and time (in UTC) at which the event should be started.",
                    }
                ]
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
                    Name = "users",
                    Type = ApplicationCommandOptionType.String,
                    Description = "Players on your team.",
                    IsRequired = true,
                },
            ]
            }
        ];

        private static readonly Dictionary<string, Func<SocketSlashCommand, Task>> SlashCommandMethods = new()
        {
            ["sc-ping"] = HandlePing,
            ["sc-create-tt"] = CreateTestTournament,
            ["sc-register"] = RegisterTeam,
        };

        private static async Task SlashCommandHandler(SocketSlashCommand command)
        {
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

            if (!WereCommandsChanged(client))
            {
                Console.WriteLine("Commands already registered.");
                return;
            }

            try
            {
                ApplicationCommandProperties[] allCommands = new ApplicationCommandProperties[SlashCommands.Count];
                for (int i = 0; i < SlashCommands.Count; i++)
                    allCommands[i] = SlashCommands[i].Build();

                await client.Rest.BulkOverwriteGlobalCommands(allCommands);

                Console.WriteLine("Commands successfully registered.");
            }
            catch(HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        private static bool WereCommandsChanged(DiscordSocketClient client)
        {
            var existingCommands = client.Rest.GetGlobalApplicationCommands().Result.ToDictionary(c => c.Name);

            if (SlashCommands.Count != existingCommands.Count)
                return true;

            RestGlobalCommand? existing;
            foreach (var command in SlashCommands)
            {
                if (!existingCommands.TryGetValue(command.Name, out existing))
                    return true;

                if (command.Description != existing.Description)
                    return true;

                if (command.Options?.Count != existing.Options?.Count)
                    return true;
            }

            return false;
        }

        #region Command Methods

        private static async Task HandlePing(SocketSlashCommand command)
        {
            // Now, Let's respond with the embed.
            await command.RespondAsync(text: $"{(command.CreatedAt - DateTimeOffset.Now).TotalMilliseconds}ms", ephemeral: true);
        }

        private static async Task CreateTestTournament(SocketSlashCommand command)
        {
            string name = "";
            long? timestamp = null;
            DateTimeOffset datetime = DateTimeOffset.UnixEpoch;

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
                    case "datetime":
                        if (!DateTimeOffset.TryParse((string) data.Value, out datetime))
                        {
                            await command.RespondAsync(text: $"Please add either a valid datetime or timestamp.", ephemeral: true);
                            return;
                        }
                        break;
                }
            }

            if (timestamp != null)
                datetime = DateTimeOffset.FromUnixTimeSeconds(timestamp.Value);

            // Error checking
            if (datetime == DateTimeOffset.UnixEpoch)
            {
                await command.RespondAsync(text: $"Please add either a valid datetime or timestamp.", ephemeral: true);
                return;
            }
            if (datetime < DateTime.Now)
            {
                await command.RespondAsync(text: $"Cannot create an event in the past! ({datetime:g})", ephemeral: true);
                return;
            }
            if (datetime >= DateTime.Now.AddYears(5))
            {
                await command.RespondAsync(text: $"Cannot create an event more than 5 years in the future! ({datetime:g})", ephemeral: true);
                return;
            }

            var guild = Program.Client.GetGuild(command.GuildId ??
                                                throw new Exception("Command was not executed in a guild!"));

            var newEvent = await guild.CreateEventAsync(name, datetime, GuildScheduledEventType.Voice, channelId: 1277394685802975266, coverImage: new Image("Resources/StarcoreLogo.png"));
            await command.RespondAsync(text: $"Created new event, https://discord.com/events/{newEvent.GuildId}/{newEvent.Id}", ephemeral: false);
        }

        private static async Task RegisterTeam(SocketSlashCommand command)
        {
            string name = "";
            string tag = "";
            string[] users = Array.Empty<string>();
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
                    case "users":
                        users = Regex.Replace((string) option.Value, "[^<\\d@>]", "").Replace(">", ">,").Split(",").Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
                        break;
                }
            }

            await command.RespondAsync(embed: new EmbedBuilder
            {
                Title = $"*Registered [{tag}] {name}*",
                Author = new EmbedAuthorBuilder
                {
                    Name = command.User.GlobalName,
                    IconUrl = command.User.GetAvatarUrl() ?? command.User.GetDefaultAvatarUrl()
                },
                Description = $"\n  {string.Join("\n  ", users)}"
            }.Build(), ephemeral: true);

            TeamsModule.RegisterTeam(name, tag, command.User.Mention, users);
        }

        #endregion
    }
}
