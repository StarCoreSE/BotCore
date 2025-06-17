namespace BotCore
{
    public class ConfigWrapper
    {
        public string Token { get; set; }
        public ulong TournamentInfoChannel { get; set; }
        public ulong TournamentVoiceChannel { get; set; }
        public string[] Servers { get; set; }
        public int DefaultELO { get; set; }
    }
}
