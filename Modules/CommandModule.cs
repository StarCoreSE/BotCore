using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Modules
{
    public class CommandModule : InteractionModuleBase<InteractionContext>
    {
        [SlashCommand("test", "TEST COMMAND YEAH")]
        public async Task Echo(string input)
        {
            await RespondAsync(input);
        }
    }
}
