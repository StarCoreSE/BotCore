using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Discord.Net;
using Newtonsoft.Json;

namespace BotCore.Modules
{
    public class CommandModule
    {
        private static readonly List<SlashCommandBuilder> SlashCommands =
        [
            new SlashCommandBuilder
            {
                Name = "list-roles",
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
            }
        ];

        private static readonly Dictionary<string, Func<SocketSlashCommand, Task>> SlashCommandMethods = new()
        {
            ["list-roles"] = HandleListRoleCommand
        };

        private static async Task SlashCommandHandler(SocketSlashCommand command)
        {
            await SlashCommandMethods.GetValueOrDefault(command.Data.Name, null)?.Invoke(command);
        }

        public static async Task RegisterCommands(DiscordSocketClient client)
        {
            client.SlashCommandExecuted += SlashCommandHandler;

            var guildCommand = new SlashCommandBuilder()
                .WithName("list-roles")
                .WithDescription("Lists all roles of a user.")
                .AddOption("user", ApplicationCommandOptionType.User, "The users whos roles you want to be listed", isRequired: true);

            try
            {
                ApplicationCommandProperties[] allCommands = new ApplicationCommandProperties[SlashCommands.Count];
                for (int i = 0; i < SlashCommands.Count; i++)
                    allCommands[i] = SlashCommands[i].Build();

                await client.Rest.BulkOverwriteGlobalCommands(allCommands);

                Console.WriteLine("Registered commands:");
                foreach (var command in SlashCommands)
                    Console.WriteLine($"- {command.Name} {string.Join(", ", command.Options.Select(o => o.IsRequired ?? false ? o.Name : $"({o.Name})"))}: {command.Description}");
            }
            catch(HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
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

        #endregion
    }
}
