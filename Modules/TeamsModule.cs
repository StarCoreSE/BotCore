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
            var guild = Program.Client.GetGuild(Tournament.GuildId);
            Team? existingTeam = Teams.Find(t => t.Name == teamName || t.Tag == teamTag || t.Leader == leader);

            if (existingTeam == null && Tournament.SignupDeadline != DateTimeOffset.UnixEpoch && Tournament.SignupDeadline < DateTimeOffset.Now)
            {
                reason = $"Signups are closed as of <t:{Tournament.SignupDeadline.ToUnixTimeSeconds()}:R>.\nContact an admin if you think this is a mistake.";
                return false;
            }

            if (!members.Contains(leader))
                members = members.Append(leader).ToArray();
            members = members.Where(m => !string.IsNullOrWhiteSpace(m)).ToArray();

            Team newTeam = new Team
            {
                Name = teamName,
                Tag = teamTag,
                Leader = leader,
                Members = members,
                Role = existingTeam?.Role ?? guild.CreateRoleAsync(teamName).Result.Id
            };

            if (existingTeam != null)
            {
                if (existingTeam.Leader != leader)
                {
                    reason = $"Cannot register an existing team!\nConflicts with: `[{existingTeam.Tag}] {existingTeam.Name}` (led by {existingTeam.Leader})";
                    return false;
                }

                foreach (var member in existingTeam.Members)
                {
                    if (!members.Contains(member))
                    {
                        var user = Program.Client.GetUser(
                            ulong.Parse(member.Remove(0, 2).Remove(member.Length - 3, 1)));
                        user.SendMessageAsync(text: $"You have been removed from `{Tournament.Name}`.", embed: newTeam.GenerateEmbed().Build());

                        guild.GetUser(user.Id)?.RemoveRoleAsync(newTeam.Role);
                    }
                }

                Teams.Remove(existingTeam);
            }

            Teams.Add(newTeam);

            TournamentsModule.SaveTournaments();
            foreach (var member in members)
            {
                try
                {
                    if (existingTeam?.Members.Contains(member) ?? false)
                        continue;

                    Console.WriteLine("Sending registration DM to " + member);
                    var user = Program.Client.GetUser(
                        ulong.Parse(member.Remove(0, 2).Remove(member.Length - 3, 1)));
                    user.SendMessageAsync(text: $"You have been signed up for `{Tournament.Name}`!\n{Tournament.EventUrl()}", embed: newTeam.GenerateEmbed().Build());
                    guild.GetUser(user.Id)?.AddRoleAsync(newTeam.Role);
                }
                catch (HttpException ex)
                {
                    if (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
                        Console.WriteLine("Could not send registration DM to user " + member);
                    else throw;
                }
            }

            Console.WriteLine($"{(existingTeam == null ? "Registered" : "Updated")} team:\n  Name: {teamName}\n  Tag: {teamTag}\n  Leader: {leader}\n  Members: {string.Join(", ", members)}");
            reason = existingTeam?.Name ?? "";
            return true;
        }

        public bool UnregisterTeam(string leader, out Team? existingTeam)
        {
            existingTeam = Teams.Find(t => t.Leader == leader);
            if (existingTeam == null)
                return false;

            Teams.Remove(existingTeam);
            Program.Client.GetGuild(Tournament.GuildId).GetRole(existingTeam.Role).DeleteAsync();

            TournamentsModule.SaveTournaments();

            return true;
        }
    }

    public class Team
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Leader { get; set; }
        public string[] Members { get; set; }
        public ulong Role { get; set; }

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
