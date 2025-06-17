namespace BotCore.Modules.BracketModules
{
    internal class SingleEliminationModule(Tournament tournament) : BracketModuleBase(tournament)
    {
        private SingleBracketBase singleBracket = new SingleBracketBase();

        public override void GenerateBracket(bool randomSeed)
        {
            var sortedTeams = Tournament.TeamsModule.Teams.ToArray();

            //if (randomSeed)
            //    Array.Sort(sortedTeams, (t1, t2) => UtilsModule.Random.Next(-1, 1));
            //else
            //    Array.Sort(sortedTeams, (t1, t2) => GetTeamSeed(t1).CompareTo(GetTeamSeed(t2)));

            int numberRounds = (int) Math.Ceiling(Math.Log2(sortedTeams.Length));
            // If we just did the naive approach, teams would get an uneven number of fights (sometimes skipping all the way to the finals!)
            int secondRoundTeams = (int)Math.Pow(2, numberRounds);
            int teamsFrom2PowN = secondRoundTeams - sortedTeams.Length;
            Console.WriteLine($"Ideal second round matches: {secondRoundTeams/2}\nTeams to remove: {teamsFrom2PowN}");
            singleBracket.Matches = new Match[numberRounds][];

            {
                singleBracket.Matches[0] = new Match[(int)Math.Ceiling(teamsFrom2PowN / 2d)];
                for (int i = 0; i < teamsFrom2PowN; i += 2)
                {
                    int id = i / 2;
                    Match match = new Match
                    {
                        Id = id,
                        Server = Program.Config.Servers[id % Program.Config.Servers.Length],
                        StartTime = Tournament.StartTime + TimeSpan.FromMinutes(30d * id),
                        Competitors = [
                            sortedTeams[i],
                            sortedTeams[i + 1]
                        ]
                    };
                    Console.WriteLine($"FirstRoundMatch: {MatchString(match)}");
                    singleBracket.Matches[0][id] = match;
                }
            }

            {
                singleBracket.Matches[1] = new Match[(int)Math.Ceiling(secondRoundTeams / 2d)];

                
                for (int i = 0; i < singleBracket.Matches[0].Length; i++)
                {
                    var match = singleBracket.Matches[0][i];
                    singleBracket.Matches[1][match.Id] = new Match
                    {
                        Id = match.Id,
                        Server = Program.Config.Servers[match.Id % Program.Config.Servers.Length],
                        StartTime = Tournament.StartTime + TimeSpan.FromMinutes(30d * match.Id),
                        Competitors = [
                            sortedTeams[teamsFrom2PowN + i]
                        ]
                    };
                }

                for (int i = 0; i < singleBracket.Matches[1].Length; i++)
                {
                    //var match = 
                }

                for (int i = teamsFrom2PowN; i < sortedTeams.Length; i += (i % 2 == 1 ? 1 : 2))
                {
                    int id = (i - teamsFrom2PowN) / 2;
                    Match match = new Match
                    {
                        Id = id,
                        Server = Program.Config.Servers[id % Program.Config.Servers.Length],
                        StartTime = Tournament.StartTime + TimeSpan.FromMinutes(30d * id)
                    };

                    if (i % 2 == 1)
                    {
                        match.Competitors =
                        [
                            sortedTeams[i]
                        ];
                        match.Winner = sortedTeams[i];
                    }
                    else
                    {
                        match.Competitors =
                        [
                            sortedTeams[i],
                            sortedTeams[i + 1]
                        ];
                    }

                    Console.WriteLine($"REG START MATCH (0, {id}). COMPETITORS: " + match.Competitors.Length);

                    singleBracket.Matches[1][id] = match;
                }
            }

            for (int i = 2; i < singleBracket.Matches.Length; i++)
            {
                Console.WriteLine($"Following: {i}: {(int) Math.Ceiling(singleBracket.Matches[i-1].Length/2d)} matches.");
                singleBracket.Matches[i] = new Match[(int) Math.Ceiling(singleBracket.Matches[i-1].Length/2d)];
            }

            while (!singleBracket.IsFinished)
            {
                var match = GetCurrentMatch();
                Console.Write($"-   {MatchString(match)}.");
                EndMatch(match.Competitors[UtilsModule.Random.Next(match.Competitors.Length-1)]);
                Console.WriteLine($" Winner: {match.Winner.Tag}");
            }
        }

        public override void EndMatch(Team winner)
        {
            var currentMatch = GetCurrentMatch();
            singleBracket.EndMatch(winner);
            CalculateNewTeamSeeds(currentMatch);

            base.EndMatch(winner);
        }

        public override Match? GetNextTeamMatch(Team team)
        {
            return singleBracket.GetNextTeamMatch(team);
        }

        public override Match GetCurrentMatch()
        {
            return singleBracket.GetCurrentMatch();
        }

        internal override bool IncrementMatch()
        {
            // Increment until winner is not null; i.e. a match is not finished.
            //Console.WriteLine($"({singleBracket.ThisRoundId}, {singleBracket.ThisMatchId})");
            //while (!singleBracket.IncrementMatch())
            //{
            //    Console.WriteLine($"({singleBracket.ThisRoundId}, {singleBracket.ThisMatchId})");
            //    if (GetCurrentMatch().Winner == null)
            //        return false;
            //}
            //return true;
            return singleBracket.IncrementMatch();
        }
    }
}
