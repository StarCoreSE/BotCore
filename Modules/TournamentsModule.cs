using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;
using Discord;

namespace BotCore.Modules
{
    internal class TournamentsModule
    {
        private static readonly string FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BotCore";
        private static readonly string FilePath = FolderPath + @"\data.json";
        private static List<Tournament> _tournaments = [];

        public static void RegisterTournament(Tournament newTournament)
        {
            if (_tournaments.Find(t => t.Name == newTournament.Name) != null)
                return;

            _tournaments.Add(newTournament);
            Task.Run(SaveTournaments);
            newTournament.OnInit(true);
        }

        public static bool CancelTournament(string name)
        {
            Tournament? tournament = _tournaments.Find(t => t.Name == name);
            if (tournament == null)
                return false;

            _tournaments.Remove(tournament);

            var guild = Program.Client.GetGuild(tournament.GuildId);
            guild.GetEvent(tournament.EventId)?.DeleteAsync();
            foreach (var role in tournament.TeamsModule.Teams.Select(t => t.Role))
                guild.GetRole(role).DeleteAsync();

            Task.Run(SaveTournaments);
            return true;
        }

        public static Tournament? GetTournament(ulong guildId, string name)
        {
            return _tournaments.Find(t => t.GuildId == guildId && t.Name == name);
        }

        public static Tournament? GetTournament(ulong guildId, ulong eventId)
        {
            return _tournaments.Find(t => t.GuildId == guildId && t.EventId == eventId);
        }

        public static IEnumerable<Tournament> GetTournaments(ulong guildId)
        {
            return _tournaments.Where(t => t.GuildId == guildId);
        }

        public static async Task LoadExistingTournaments()
        {
            Console.WriteLine("Loading data file from " + FilePath + "...");
            if (!File.Exists(FilePath))
            {
                Console.WriteLine("File does not exist.");
                return;
            }

            _tournaments = JsonSerializer.Deserialize<List<Tournament>>(await File.ReadAllBytesAsync(FilePath)) ?? throw new Exception("Could not read data file!");
            foreach (var tournament in _tournaments)
            {
                tournament.TeamsModule.Tournament = tournament;
                tournament.OnInit(false);
            }
        }

        public static async Task SaveTournaments()
        {
            Console.WriteLine("Writing data file to " + FilePath + @"\data.json" + ".");

            Directory.CreateDirectory(FolderPath);
            File.Delete(FilePath);
            await using FileStream createStream = File.Create(FilePath);
            await JsonSerializer.SerializeAsync(createStream, _tournaments);

            Console.WriteLine("Completed write operation.");
        }
    }

    internal class Tournament
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset SignupDeadline { get; set; }
        public ulong GuildId { get; set; }
        public ulong EventId { get; set; }
        public TeamsModule TeamsModule { get; set; }
        private bool HasSentSignupMessage { get; set; } = false;
        private bool HasSentStartMessage { get; set; } = false;


        public Tournament()
        {
            TeamsModule = new TeamsModule(this);
        }

        public string EventUrl()
        {
            return $"https://discord.com/events/{GuildId}/{EventId}";
        }

        public void OnInit(bool justRegistered)
        {
            if (!HasSentSignupMessage && SignupDeadline != DateTimeOffset.UnixEpoch)
                Observable.Timer(SignupDeadline).Subscribe(DisplaySignup_Timed);
            if (!HasSentStartMessage)
                Observable.Timer(StartTime).Subscribe(DisplayStart_Timed);

            if (!justRegistered)
                return;

            var channel =
                Program.Client.GetGuild(GuildId).GetChannel(Program.Config.TournamentInfoChannel) as IMessageChannel;

            channel?.SendMessageAsync($"# **{Name}**\n`Register your team with /sc-register`\n{Description}\n\n@everyone <@1210205776484769862>\n{EventUrl()}");
        }

        private void DisplaySignup_Timed(long t)
        {
            Console.WriteLine("Signup deadline closed for " + Name);

            var channel =
                Program.Client.GetGuild(GuildId).GetChannel(Program.Config.TournamentInfoChannel) as IMessageChannel;

            channel?.SendMessageAsync($"# **{Name} - SIGNUPS CLOSED.**\n\nRegistered teams:");
            channel?.SendMessageAsync(embeds: TeamsModule.Teams.Select(t => t.GenerateEmbed().Build()).ToArray());
            HasSentSignupMessage = true;

            GenerateBracket();
        }

        private void DisplayStart_Timed(long t)
        {
            Console.WriteLine($"# **{Name} has started!**");
            (Program.Client.GetGuild(GuildId).GetChannel(Program.Config.TournamentInfoChannel) as IMessageChannel)
                ?.SendMessageAsync($"# **{Name} has started!**");
            HasSentStartMessage = true;
        }

        public void EndTournament()
        {
            TournamentsModule.CancelTournament(Name);
            (Program.Client.GetGuild(GuildId).GetChannel(Program.Config.TournamentInfoChannel) as IMessageChannel)
                ?.SendMessageAsync($"{Name} has ended!");
        }

        public void GenerateBracket()
        {

        }
    }
}
