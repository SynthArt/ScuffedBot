using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Scuffed_Bot
{
    public class Bot
    {
        public DiscordClient Client { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        
        public async Task RunAsync() {

            var json = string.Empty;

            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Debug,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
                AutoReconnect = true
            };

            var services = new ServiceCollection()
               .AddSingleton<AudioService>()
              .BuildServiceProvider();

            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new String[] { configJson.Prefix },
                EnableMentionPrefix = true,
                EnableDms = false,
                DmHelp = true,
                Services = services
            };
            
            Client = new DiscordClient(config);
            Client.UseVoiceNext();
            Client.Ready += OnClientReady;

            
            Commands = Client.UseCommandsNext(commandsConfig);
            Commands.RegisterCommands<MusicCommands>();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }
    }
}
