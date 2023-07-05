using DSharpPlus;
using System;
using System.Threading.Tasks;

namespace Scuffed_Bot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var bot = new Bot();
            bot.RunAsync().GetAwaiter().GetResult();
        }
    }
}
