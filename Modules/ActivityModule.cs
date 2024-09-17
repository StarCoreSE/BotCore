using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace BotCore.Modules
{
    internal class ActivityModule
    {
        private static readonly string[] CustomStatusArray =
        [
            "Breaking behind the scenes",
            "Calling Aristeas a nerd",
            "More Stable Than Fusion",
            "Real Gaming\u2122 by Real Gamers\u2122",
            "Pinging Xocliw",
            "Buffing Potatoes",
            "Bothering Muzzled",
            "Waiting for Tiberias to get back",
            "Paying for the right to use WC (in suffering)",
            "Refactoring ShareTrack",
            "Removing gamemodes",
            "Speedrunning a new ship",
            "Upsetting modders",
            "Asking the Omnissiah",
            "Beating CON",
            "Nerfing Fastmovers",
            "On a Biobreak",
            "Complaining at Invalid",
        ];
        private static readonly Random Random = new();

        public static async Task RegisterActivity(DiscordSocketClient client)
        {
            await client.SetActivityAsync(new CustomStatusGame(CustomStatusArray[Random.Next(CustomStatusArray.Length-1)]));
            Console.WriteLine("Set new status.");
        }
    }
}
