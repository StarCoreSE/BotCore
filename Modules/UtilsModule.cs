using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace BotCore.Modules
{
    public static class UtilsModule
    {
        public static IMessageChannel? GetChannel(ulong guildId, ulong channelId)
        {
            return Program.Client.GetGuild(guildId)?.GetChannel(channelId) as IMessageChannel;
        }
    }
}
