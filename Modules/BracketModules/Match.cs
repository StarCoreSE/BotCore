namespace BotCore.Modules.BracketModules
{
    public class Match
    {
        public Team[] Competitors { get; set; }
        public Team? Winner { get; set; } = null;
        public DateTimeOffset StartTime { get; set; }
        public string Server { get; set; }
        public int Id { get; set; }
    }
}
