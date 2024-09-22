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
            if (_tournaments.Any(t => t.Name == newTournament.Name))
                return;

            _tournaments.Add(newTournament);
            Task.Run(SaveTournaments);
            CommandModule.UpdateCommandTournamentList(newTournament.GuildId);
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
            CommandModule.UpdateCommandTournamentList(tournament.GuildId);
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

            List<Task> tasks = [];
            foreach (var guild in Program.Client.Guilds)
                tasks.Add(CommandModule.UpdateCommandTournamentList(guild.Id));
            Task.WaitAll(tasks.ToArray());
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
        public bool HasSentSignupMessage { get; set; } = false;
        public bool HasSentStartMessage { get; set; } = false;


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
            if (SignupDeadline == DateTimeOffset.UnixEpoch)
                SignupDeadline = StartTime;

            if (!HasSentSignupMessage && SignupDeadline != DateTimeOffset.UnixEpoch)
                Observable.Timer(SignupDeadline).Subscribe(DisplaySignup_Timed);
            if (!HasSentStartMessage)
                Observable.Timer(StartTime).Subscribe(DisplayStart_Timed);

            if (!justRegistered)
                return;

            UtilsModule.GetChannel(GuildId, Program.Config.TournamentInfoChannel)?.SendMessageAsync($"# **{Name}**\n`Register your team with /sc-register`." + (SignupDeadline == DateTimeOffset.UnixEpoch ? "" : $" Sign-up deadline is <t:{SignupDeadline.ToUnixTimeSeconds()}:f>") + $"\n{Description}\n\n@everyone <@1210205776484769862>\n{EventUrl()}");
        }

        private void DisplaySignup_Timed(long t)
        {
            Console.WriteLine("Signup deadline closed for " + Name);

            var channel =
                UtilsModule.GetChannel(GuildId, Program.Config.TournamentInfoChannel);

            channel?.SendMessageAsync($"# **{Name} - SIGNUPS CLOSED.**\n\nRegistered teams:");
            channel?.SendMessageAsync(embeds: TeamsModule.Teams.Select(t => t.GenerateEmbed().Build()).ToArray());
            HasSentSignupMessage = true;
            TournamentsModule.SaveTournaments();

            GenerateBracket();
        }

        private void DisplayStart_Timed(long t)
        {
            Console.WriteLine($"# **{Name} has started!**");
            UtilsModule.GetChannel(GuildId, Program.Config.TournamentInfoChannel)?.SendMessageAsync($"# **{Name} has started!**");
            HasSentStartMessage = true;
            TournamentsModule.SaveTournaments();
        }

        public void EndTournament()
        {
            TournamentsModule.CancelTournament(Name);
            UtilsModule.GetChannel(GuildId, Program.Config.TournamentInfoChannel)?.SendMessageAsync($"{Name} has ended!");
        }

        public void GenerateBracket()
        {

        }
    }
}
