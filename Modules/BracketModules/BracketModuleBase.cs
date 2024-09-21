using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotCore.Modules.BracketModules
{
    /// <summary>
    /// A single bracket.
    /// </summary>
    internal abstract class BracketModuleBase
    {
        private static readonly string FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BotCore";
        private static readonly string FilePath = FolderPath + @"\seedData.json";
        private static Dictionary<string, int> PlayerSeeds = new();
        public static Match[][] Matches = Array.Empty<Match[]>();

        public static async Task LoadData()
        {
            PlayerSeeds = JsonSerializer.Deserialize<Dictionary<string, int>>(await File.ReadAllBytesAsync(FilePath)) ?? throw new Exception("Could not read data file!");
            Console.WriteLine($"Seeds loaded for {PlayerSeeds.Count} players.");
        }

        public static async Task SaveData()
        {
            Directory.CreateDirectory(FolderPath);
            File.Delete(FilePath);
            FileStream createStream = File.Create(FilePath);
            await JsonSerializer.SerializeAsync(createStream, PlayerSeeds);
        }

        public IEnumerable<Team> Teams { get; private set; }

        public virtual void SetTeams(IEnumerable<Team> teams, bool randomSeed)
        {
            Teams = teams;
        }
        public abstract Match GetNextTeamMatch(Team team);
        public abstract Match GetCurrentMatch();
        public abstract void StartMatch(int roundId, int matchId);

        public int GetPlayerSeed(string playerId)
        {
            return PlayerSeeds.GetValueOrDefault(playerId, 0);
        }

        public int GetTeamSeed(Team team)
        {
            return team.Members.Aggregate(0, (accumulated, playerId) => accumulated + GetPlayerSeed(playerId));
        }

        public class Match
        {
            public Team[] Competitors { get; set; }
            public Team Winner { get; set; }
            public DateTimeOffset StartTime { get; set; }
            public string Server { get; set; }
            public int Id { get; set; }
        }
    }
}
