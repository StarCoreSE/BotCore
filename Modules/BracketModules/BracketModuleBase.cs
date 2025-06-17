using System.Text.Json;

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
        public static Dictionary<string, int> PlayerSeeds = new();

        public static async Task LoadData()
        {
            Console.WriteLine($"Loading seed data from {FilePath}...");
            if (File.Exists(FilePath))
                PlayerSeeds =
                    JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllBytes(FilePath)) ??
                    throw new Exception("Could not read data file!");
            else
                await SaveData();
            Console.WriteLine($"Seeds loaded for {PlayerSeeds.Count} players.");
        }

        public static async Task SaveData()
        {
            Console.WriteLine("Saving seed data...");
            Directory.CreateDirectory(FolderPath);
            await File.WriteAllTextAsync(FilePath, string.Empty);
            FileStream createStream = File.OpenWrite(FilePath);
            await JsonSerializer.SerializeAsync(createStream, PlayerSeeds);
            await createStream.FlushAsync();
            createStream.Close();
            Console.WriteLine("Seed data saved.");
        }

        #endregion

        public Tournament Tournament;

        public BracketModuleBase(Tournament tournament)
        {
            Tournament = tournament;
        }

        public abstract void GenerateBracket(bool randomSeed);
        public abstract Match? GetNextTeamMatch(Team team);
        public abstract Match GetCurrentMatch();
        public virtual void StartMatch(int roundId, int matchId)
        {
            UtilsModule.GetChannel(Tournament.GuildId, Program.Config.TournamentInfoChannel)?.SendMessageAsync($"{MatchString(GetCurrentMatch())} is starting!\n*");
        }
        /// <summary>
        /// Ends the specified match.
        /// </summary>
        /// <param name="winner">The winner of the match. Null if a draw.</param>
        public virtual void EndMatch(Team winner)
        {
            UtilsModule.GetChannel(Tournament.GuildId, Program.Config.TournamentInfoChannel)?.SendMessageAsync($"{MatchString(GetCurrentMatch())} has finished.\n### *{winner.Name} victory!*");
            IncrementMatch();
        }
        /// <summary>
        /// Returns true if the tournament is over.
        /// </summary>
        /// <returns></returns>
        internal abstract bool IncrementMatch();

        public virtual string MatchString(Match match)
        {
            return string.Join(" vs. ", match.Competitors.Select(t => $"<@{t.Role}>"));
        }

        #region Seeding

        public static int GetPlayerSeed(string playerId)
        {
            return PlayerSeeds.GetValueOrDefault(playerId, Program.Config.DefaultELO);
        }

        public static int GetTeamSeed(Team team)
        {
            int seed = 0;
            foreach (var member in team.Members)
                seed += GetPlayerSeed(member);
            return seed / team.Members.Length;
        }

        public static void SetPlayerSeed(string playerId, int seed, bool save)
        {
            PlayerSeeds[playerId] = seed;
            if (save)
                Task.WaitAll(SaveData());
        }

        public static void SetTeamSeed(Team team, int seed, bool save = true)
        {
            int oldSeed = GetTeamSeed(team);
            int diff = oldSeed - seed;
            foreach (var playerId in team.Members)
                PlayerSeeds[playerId] = GetPlayerSeed(playerId) + diff;

            if (save)
                Task.WaitAll(SaveData());
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
                double enemyRating = 0;
                foreach (var enemyTeam in ratings)
                    enemyRating += enemyTeam.Key == team ? 0 : enemyTeam.Value;

                double expectedScore = CalculateExpectedScore(ratings[team], enemyRating);
                
                double actualScore = 0;
                if (match.Winner == null)
                    actualScore = 0.5;
                else if (team == match.Winner)
                    actualScore = 1;
                actualScore *= ratings.Count - 1;

                double newRating = GetTeamSeed(team) + CalculateKFactor(ratings[team]) * (actualScore - expectedScore);
                SetTeamSeed(team, (int) Math.Round(newRating), false);
            }

            Task.WaitAll(SaveData());
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
