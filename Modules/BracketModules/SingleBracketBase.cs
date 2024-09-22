namespace BotCore.Modules.BracketModules
{
    internal class SingleBracketBase
    {
        public SingleBracketBase(BracketModuleBase bracket)
        {
            Bracket = bracket;
        }

        public BracketModuleBase Bracket;

        public Match[][] Matches = Array.Empty<Match[]>();
        public int ThisRoundId = 0, ThisMatchId = 0;

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
            return Matches[ThisMatchId][ThisRoundId];
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
            var match = Matches[roundId == -1 ? ThisRoundId : roundId][matchId == -1 ? ThisMatchId : matchId];
            match.Winner = winner;

            Bracket.CalculateNewTeamSeeds(match);

            UtilsModule.GetChannel(Bracket.Tournament.GuildId, Program.Config.TournamentInfoChannel)?.SendMessageAsync($"{string.Join(" vs. ", match.Competitors.Select(t => $"<@{t.Role}>"))} has finished.\n### *{winner.Name} victory!*");
        }

        /// <summary>
        /// Returns true if the tournament is over.
        /// </summary>
        /// <returns></returns>
        internal virtual bool IncrementMatch()
        {
            if (ThisMatchId < Matches[ThisRoundId].Length - 1)
                ThisRoundId++;
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
