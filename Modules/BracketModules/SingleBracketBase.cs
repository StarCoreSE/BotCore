namespace BotCore.Modules.BracketModules
{
    internal class SingleBracketBase
    {
        public SingleBracketBase()
        {
        }

        public Match[][] Matches = Array.Empty<Match[]>();
        public int ThisRoundId = 0, ThisMatchId = 0;
        public bool IsFinished => ThisRoundId + 1 >= Matches.Length && ThisMatchId + 1 >= Matches[ThisRoundId].Length && GetCurrentMatch().Winner != null;

        public virtual Match? GetNextTeamMatch(Team team)
        {
            for (int round = ThisRoundId; round < Matches.Length; round++)
            {
                for (int match = ThisMatchId; match < Matches[round].Length; match++)
                {
                    if (Matches[round][match].Competitors.Contains(team))
                        return Matches[round][match];
                }
            }

            return null;
        }

        public virtual Match GetCurrentMatch()
        {
            return Matches[ThisRoundId][ThisMatchId];
        }

        public virtual Match GetNextMatch()
        {
            int nextMatchId = ThisMatchId, nextRoundId = ThisRoundId;
            if (ThisMatchId < Matches[ThisRoundId].Length - 1)
                ThisMatchId++;
            else if (ThisRoundId < Matches.Length - 1)
            {
                ThisRoundId++;
                ThisMatchId = 0;
            }

            return Matches[ThisRoundId][ThisMatchId];
        }

        public virtual void StartMatch(int roundId, int matchId)
        {
            ThisRoundId = roundId;
            ThisMatchId = matchId;
        }

        /// <summary>
        /// Ends the specified match.
        /// </summary>
        /// <param name="winner">The winner of the match. Null if a draw.</param>
        /// <param name="roundId">Leave at -1 for the current match.</param>
        /// <param name="matchId">Leave at -1 for the current match.</param>
        public virtual void EndMatch(Team winner, int roundId = -1, int matchId = -1)
        {
            var currentMatch = Matches[roundId == -1 ? ThisRoundId : roundId][matchId == -1 ? ThisMatchId : matchId];
            currentMatch.Winner = winner;

            if (ThisRoundId + 1 < Matches.Length)
            {
                int nextRound = ThisRoundId + 1;
                int id = (int) Math.Floor(ThisMatchId / 2d);
                var nextMatch = Matches[nextRound][id];
                Console.WriteLine($" Next: [({ThisRoundId}, {ThisMatchId})-({nextRound}, {id})]");
                if (nextMatch == null)
                {
                    nextMatch = new Match
                    {
                        Id = id,
                        Server = Program.Config.Servers[id % Program.Config.Servers.Length],
                        Competitors =
                        [
                            currentMatch.Winner
                        ]
                    };
                }
                else
                {
                    nextMatch.Competitors =
                    [
                        nextMatch.Competitors[0],
                        currentMatch.Winner
                    ];
                }
                Matches[nextRound][id] = nextMatch;
            }
        }

        /// <summary>
        /// Returns true if the tournament is over.
        /// </summary>
        /// <returns></returns>
        internal virtual bool IncrementMatch()
        {
            if (ThisMatchId < Matches[ThisRoundId].Length - 1)
                ThisMatchId++;
            else if (ThisRoundId < Matches.Length - 1)
            {
                ThisRoundId++;
                ThisMatchId = 0;
            }
            else
                return true;

            return false;
        }
    }
}
