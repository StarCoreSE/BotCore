using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                Name = "sc-list-roles",
                Description = "Lists all roles of a user",
                Options =
                [
                    new SlashCommandOptionBuilder
                    {
                        Name = "user",
                        Type = ApplicationCommandOptionType.User,
                        Description = "The users whose roles you want to be listed",
                        IsRequired = true
                    }
                ]
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
                        IsRequired = true
                    }
                ]
            }
        ];

        private static readonly Dictionary<string, Func<SocketSlashCommand, Task>> SlashCommandMethods = new()
        {
            ["sc-list-roles"] = HandleListRoleCommand,
            ["sc-create-tt"] = CreateTestTournament,
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
                Console.WriteLine($"- {command.Name} {string.Join(", ", command.Options.Select(o => o.IsRequired ?? false ? o.Name : $"({o.Name})"))}: {command.Description}");

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

                if (command.Options.Count != existing.Options.Count)
                    return true;
            }

            return false;
        }

        #region Command Methods

        private static async Task HandleListRoleCommand(SocketSlashCommand command)
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

        private static async Task CreateTestTournament(SocketSlashCommand command)
        {
            string name = "";
            long timestamp = 0;

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
                }
            }

            // Error checking
            if (DateTimeOffset.FromUnixTimeSeconds(timestamp) < DateTime.Now)
            {
                await command.RespondAsync(text: $"Cannot create an event in the past! ({DateTimeOffset.FromUnixTimeSeconds(timestamp):g})", ephemeral: true);
                return;
            }
            if (DateTimeOffset.FromUnixTimeSeconds(timestamp) >= DateTime.Now.AddYears(5))
            {
                await command.RespondAsync(text: $"Cannot create an event more than 5 years in the future! ({DateTimeOffset.FromUnixTimeSeconds(timestamp):g})", ephemeral: true);
                return;
            }


            var guild = Program.Client.GetGuild(command.GuildId ??
                                                throw new Exception("Command was not executed in a guild!"));

            var newEvent = await guild.CreateEventAsync(name, DateTimeOffset.FromUnixTimeSeconds(timestamp), GuildScheduledEventType.Voice, channelId: 1277394685802975266, coverImage: new Image("Resources/StarcoreLogo.png"));
            await command.RespondAsync(text: $"Created new event, https://discord.com/events/{newEvent.GuildId}/{newEvent.Id}", ephemeral: false);
        }

        #endregion
    }
}
