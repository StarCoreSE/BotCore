using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Modules
{
    internal class TournamentsModule
    {
        private static List<Tournament> _tournaments = [];

        public static void RegisterTournament(Tournament newTournament)
        {
            _tournaments.Add(newTournament);
        }

        public static Tournament? GetTournament(ulong guildId, string name)
        {
            return _tournaments.Find(t => t.GuildId == guildId && t.Name == name);
        }

        public static Tournament? GetTournament(ulong guildId, ulong eventId)
        {
            return _tournaments.Find(t => t.GuildId == guildId && t.EventId == eventId);
        }

        public static IEnumerable<Tournament> GetTournaments(ulong guildId)
        {
            return _tournaments.Where(t => t.GuildId == guildId);
        }
    }

    internal class Tournament
    {
        public string Name;
        public string Description;
        public DateTimeOffset StartTime;
        public DateTimeOffset SignupDeadline;
        public ulong GuildId;
        public ulong EventId;

        public TeamsModule TeamsModule = new();
    }
}
