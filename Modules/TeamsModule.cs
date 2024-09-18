using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace BotCore.Modules
{
    internal class TeamsModule
    {
        [JsonInclude]
        public List<Team> Teams = [];

        public Tournament Tournament;

        [JsonConstructor]
        private TeamsModule()
        {
        }

        public TeamsModule(Tournament tournament)
        {
            Tournament = tournament;
        }

        public bool RegisterTeam(string teamName, string teamTag, string leader, string[] members, out string reason)
        {
            Team? existingTeam = Teams.Find(t => t.Name == teamName || t.Tag == teamTag || t.Leader == leader);

            if (existingTeam != null)
            {
                if (existingTeam.Leader != leader)
                {
                    reason = $"Cannot register an existing team!\nConflicts with: `[{existingTeam.Tag}] {existingTeam.Name}` (led by {existingTeam.Leader})";
                    return false;
                }

                Teams.Remove(existingTeam);
            }

            if (!members.Contains(leader))
                members = members.Append(leader).ToArray();

            members = members.Where(m => !string.IsNullOrWhiteSpace(m)).ToArray();

            Team newTeam = new Team
            {
                Name = teamName,
                Tag = teamTag,
                Leader = leader,
                Members = members
            };

            Teams.Add(newTeam);

            TournamentsModule.SaveTournaments();
            foreach (var member in members)
            {
                try
                {
                    Console.WriteLine(member);
                    Program.Client.GetUser(ulong.Parse(member.Remove(0, 2).Remove(member.Length-3, 1))).SendMessageAsync(text: $"You have been signed up for `{Tournament.Name}`!", embeds: new []
                    {
                        newTeam.GenerateEmbed().Build(),
                        new EmbedBuilder
                        {
                            Url = Tournament.EventUrl()
                        }.Build()
                    });
                }
                catch (HttpException ex)
                {
                    if (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
                        Console.WriteLine("Could not send message to user " + member);
                    else throw;
                }
            }

            Console.WriteLine($"Registered team:\n  Name: {teamName}\n  Tag: {teamTag}\n  Leader: {leader}\n  Members: {string.Join(", ", members)}");
            reason = "";
            return true;
        }

        public bool UnregisterTeam(string leader, out Team? existingTeam)
        {
            existingTeam = Teams.Find(t => t.Leader == leader);
            if (existingTeam == null)
                return false;

            Teams.Remove(existingTeam);
            TournamentsModule.SaveTournaments();

            return true;
        }
    }

    internal class Team
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Leader { get; set; }
        public string[] Members { get; set; }

        public EmbedBuilder GenerateEmbed(SocketUser? user = null)
        {
            return new EmbedBuilder
            {
                Title = $"*[{Tag}] {Name}*",
                Author = user == null ? null : new EmbedAuthorBuilder
                {
                    Name = user.GlobalName,
                    IconUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl()
                },
                Description = $"Leader: {Leader}\n  {string.Join("\n  ", Members)}"
            };
        }
    }
}
