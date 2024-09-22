using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotCore.Modules.BracketModules
{
    /// <summary>
    /// A single bracket. Uses ELO system for seeding.
    /// </summary>
    internal abstract class BracketModuleBase
    {
        #region Static

        private static readonly string FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BotCore";
        private static readonly string FilePath = FolderPath + @"\seedData.json";
        private static Dictionary<string, int> PlayerSeeds = new();

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

        #endregion

        public Tournament Tournament;

        public BracketModuleBase(Tournament tournament)
        {
            Tournament = tournament;
        }

        public abstract void GenerateBracket(IEnumerable<Team> teams, bool randomSeed);
        public abstract Match? GetNextTeamMatch(Team team);
        public abstract Match GetCurrentMatch();
        public virtual void StartMatch(int roundId, int matchId)
        {
            UtilsModule.GetChannel(Tournament.GuildId, Program.Config.TournamentInfoChannel)?.SendMessageAsync($"{string.Join(" vs. ", GetCurrentMatch().Competitors.Select(t => $"<@{t.Role}>"))} is starting!\n*");
        }
        /// <summary>
        /// Ends the specified match.
        /// </summary>
        /// <param name="winner">The winner of the match. Null if a draw.</param>
        public virtual void EndMatch(Team winner)
        {
            UtilsModule.GetChannel(Tournament.GuildId, Program.Config.TournamentInfoChannel)?.SendMessageAsync($"{string.Join(" vs. ", GetCurrentMatch().Competitors.Select(t => $"<@{t.Role}>"))} has finished.\n### *{winner.Name} victory!*");
        }
        /// <summary>
        /// Returns true if the tournament is over.
        /// </summary>
        /// <returns></returns>
        internal abstract bool IncrementMatch();

        #region Seeding

        public int GetPlayerSeed(string playerId)
        {
            return PlayerSeeds.GetValueOrDefault(playerId, 1200);
        }

        public int GetTeamSeed(Team team)
        {
            return team.Members.Aggregate(0, (accumulated, playerId) => accumulated + GetPlayerSeed(playerId)) / team.Members.Length;
        }

        public void SetPlayerSeed(string playerId, int seed)
        {
            PlayerSeeds[playerId] = seed;
            SaveData();
        }

        public void SetTeamSeed(Team team, int seed)
        {
            int oldSeed = GetTeamSeed(team);
            int diff = oldSeed - seed;
            foreach (var playerId in team.Members)
                PlayerSeeds[playerId] = GetPlayerSeed(playerId) + diff;

            SaveData();
        }

        /// <summary>
        /// Calculates the new seeds for a finished match.
        /// </summary>
        /// <param name="match">If the winner of the match is null, it is a draw.</param>
        public virtual void CalculateNewTeamSeeds(Match match)
        {
            Dictionary<Team, int> ratings = new Dictionary<Team, int>();
            foreach (var team in match.Competitors)
                ratings[team] = GetTeamSeed(team);

            foreach (var team in match.Competitors)
            {
                double enemyRating = ratings.Aggregate(0, (accumulated, enemyTeam) => accumulated + (enemyTeam.Key == team ? 0 : enemyTeam.Value));
                double expectedScore = CalculateExpectedScore(ratings[team], enemyRating);
                
                double actualScore = 0;
                if (match.Winner == null)
                    actualScore = 0.5;
                else if (team == match.Winner)
                    actualScore = 1;
                actualScore *= ratings.Count - 1;

                double newRating = GetTeamSeed(team) + CalculateKFactor(ratings[team]) * (actualScore - expectedScore);
                SetTeamSeed(team, (int) Math.Round(newRating));
            }
        }

        private double CalculateExpectedScore(double thisScore, double enemyScore)
        {
            // https://en.wikipedia.org/wiki/Elo_rating_system#Mathematical_details
            return 1 / (1 + Math.Pow(10, (enemyScore - thisScore) / 400d));
        }

        private double CalculateKFactor(double currentRating)
        {
            if (currentRating < 2100)
                return 32;
            if (currentRating < 2400)
                return 24;
            return 16;
        }

        #endregion
    }
}
