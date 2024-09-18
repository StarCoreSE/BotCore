using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Discord;
using Discord.WebSocket;

namespace BotCore.Modules
{
    internal class TeamsModule
    {
        public List<Team> Teams = [];

        public bool RegisterTeam(string teamName, string teamTag, string leader, string[] members, out string reason)
        {
            Team? existingTeam = Teams.Find(t => t.Name == teamName || t.Tag == teamTag);

            if (existingTeam != null)
            {
                if (existingTeam.Leader != leader)
                {
                    reason = $"Cannot register an existing team!\nConflicts with: `[{existingTeam.Tag}] {existingTeam.Name}` (led by {existingTeam.Leader})";
                    return false;
                }

                Teams.Remove(existingTeam);
            }

            Teams.Add(new Team
            {
                Name = teamName,
                Tag = teamTag,
                Leader = leader,
                Members = members
            });

            Console.WriteLine($"Registered team:\n  Name: {teamName}\n  Tag: {teamTag}\n  Leader: {leader}\n  Members: {string.Join(", ", members)}");
            reason = "";
            return true;
        }

        public bool UnregisterTeam(string leader)
        {
            Team? existingTeam = Teams.Find(t => t.Leader == leader);
            if (existingTeam == null)
                return false;

            Teams.Remove(existingTeam);

            return true;
        }
    }

    internal class Team
    {
        public string Name;
        public string Tag;
        public string Leader;
        public string[] Members;

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
                Description = $"\n  {string.Join("\n  ", Members)}"
            };
        }
    }
}
