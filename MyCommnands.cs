using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FSocietyBot
{
    public class MyCommands : BaseCommandModule
    {
        [Command("penis")]
        public async Task TestCommand(CommandContext ctx)
        {
            Random rndm = new Random();
            int size = rndm.Next(1, 31);
            if (size == 31)
            {
                await ctx.Channel.SendMessageAsync("() you don't have a dick you have pussy</color>");
                return;
            }
            if (size > 10)
            {
                await ctx.Channel.SendMessageAsync("8" + new string('=', size) + "D " + size + "cm");
                return;
            }
            await ctx.Channel.SendMessageAsync("8" + new string('=', size) + "D micro penis " + size + "cm");
        }
    }
}