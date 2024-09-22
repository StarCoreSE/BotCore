using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Modules.BracketModules
{
    internal class SingleEliminationModule : BracketModuleBase
    {
        private SingleBracketBase singleBracket;

        public SingleEliminationModule(Tournament tournament) : base(tournament)
        {
            singleBracket = new SingleBracketBase(this);
        }

        public override void GenerateBracket(IEnumerable<Team> teams, bool randomSeed)
        {
            
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
            return singleBracket.IncrementMatch();
        }
    }
}
