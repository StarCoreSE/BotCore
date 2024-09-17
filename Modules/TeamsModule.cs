using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace BotCore.Modules
{
    internal class TeamsModule
    {
        public static bool RegisterTeam(string teamName, string teamTag, string leader, string[] members)
        {
            Console.WriteLine($"Registering new team.\n  Name: {teamName}\n  Tag: {teamTag}\n  Leader: {leader}\n  Members: {string.Join(", ", members)}");
            return true;
        }

        public static bool UnregisterTeam(string leader)
        {
            return true;
        }
    }
}
