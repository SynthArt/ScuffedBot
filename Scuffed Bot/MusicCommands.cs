using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Scuffed_Bot
{
    public class MusicCommands : BaseCommandModule
    {
        private Queue<string> Playlist = new Queue<string>();
        public AudioService m_Service { private get; set; }
        

        [Command("join")]
        [Description("Joins voice channel")]
        public async Task Join(CommandContext ctx)
        {
            var voice = ctx.Client.GetVoiceNext();
            isVNextEnabled(voice, ctx);
            var target = ctx.Member?.VoiceState?.Channel;
            var guild = ctx.Guild;
            await m_Service.JoinAudioAsync(guild,voice,target);
            await ctx.RespondAsync("Joined");
            /*
            var audioClient = voice.GetConnection(guild);
            isMemberConnected(audioClient, ctx);
            Console.WriteLine("Start flag");
            audioClient = await target.ConnectAsync().ConfigureAwait(false);
            Console.WriteLine("End flag");
            await ctx.RespondAsync("Joined");
            */
        }

        [Command("disconnect")]
        [Description("Makes bot leave current VC")]
        public async Task Disconnect(CommandContext ctx)
        {
            var guild = ctx.Guild;
            await m_Service.LeaveAudioAsync(guild);
        }

        [Command("play")]
        [Description("Plays music in voice channel")]
        public async Task Play(CommandContext ctx, [RemainingText] string url)
        {
            await m_Service.PlaylistAddAsync(url);
            await m_Service.CheckAutoPlayAsync(ctx.Guild, ctx.Channel);
        }

        
        [Command("pause")]
        [Description("pauses the current song")]
        public async Task Pause(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNext();
            isVNextEnabled(vnext, ctx);

            var vnc = vnext.GetConnection(ctx.Guild);
            isMemberConnected(vnc,ctx);

            var txStream = vnc.GetTransmitSink();

            txStream.Pause();
            await ctx.RespondAsync("Music has been paused");
        }

        // helper functions
        private Stream ConvertAudioToPcm(string url)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                UseShellExecute = false,
                Arguments = $"/C yt-dlp --youtube-skip-dash-manifest --verbose --ignore-errors -f 251 -o - {url} | ffmpeg -err_detect ignore_err -i pipe:0 -filter:a loudnorm -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true
            };
            var ffmpeg = Process.Start(psi);
            return ffmpeg.StandardOutput.BaseStream;
        }
        private async void isVNextEnabled(VoiceNextExtension vnext, CommandContext ctx)
        {
            if (vnext == null)
            {
                // not enabled
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }
        }

        private async void isMemberConnected(VoiceNextConnection vnc, CommandContext ctx)
        {
            if (vnc != null)
            {
                // already connected
                await ctx.RespondAsync("Alrdy connected in this guild.");
                return;
            }
        }

        

    }
}
