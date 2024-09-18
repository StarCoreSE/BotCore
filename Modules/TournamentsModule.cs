using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;

namespace BotCore.Modules
{
    internal class TournamentsModule
    {
        private static readonly string FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BotCore";
        private static readonly string FilePath = FolderPath + @"\data.json";
        private static List<Tournament> _tournaments = [];

        public static void RegisterTournament(Tournament newTournament)
        {
            if (_tournaments.Find(t => t.Name == newTournament.Name) != null)
                return;

            _tournaments.Add(newTournament);
            Task.Run(SaveTournaments);
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

        public static async Task LoadExistingTournaments()
        {
            Console.WriteLine("Loading data file from " + FilePath + "...");
            if (!File.Exists(FilePath))
            {
                Console.WriteLine("File does not exist.");
                return;
            }

            _tournaments = JsonSerializer.Deserialize<List<Tournament>>(await File.ReadAllBytesAsync(FilePath)) ?? throw new Exception("Could not read data file!");
            foreach (var tournament in _tournaments)
            {
                tournament.TeamsModule.Tournament = tournament;
            }
        }

        public static async Task SaveTournaments()
        {
            Console.WriteLine("Writing data file to " + FilePath + @"\data.json" + ".");

            Directory.CreateDirectory(FolderPath);
            File.Delete(FilePath);
            await using FileStream createStream = File.Create(FilePath);
            await JsonSerializer.SerializeAsync(createStream, _tournaments);

            Console.WriteLine("Completed write operation.");
        }
    }

    internal class Tournament
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset SignupDeadline { get; set; }
        public ulong GuildId { get; set; }
        public ulong EventId { get; set; }

        public TeamsModule TeamsModule { get; set; }

        public Tournament()
        {
            TeamsModule = new TeamsModule(this);
        }

        public string EventUrl()
        {
            return $"https://discord.com/events/{GuildId}/{EventId}";
        }
    }
}
