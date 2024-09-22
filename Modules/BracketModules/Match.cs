using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Modules.BracketModules
{
    public class Match
    {
        public Team[] Competitors { get; set; }
        public Team Winner { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public string Server { get; set; }
        public int Id { get; set; }
    }
}
