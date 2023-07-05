using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.EventArgs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scuffed_Bot
{
    // handles the audio player, downloader, and playlist
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, VoiceNextConnection> ConnectedChannels = new ConcurrentDictionary<ulong, VoiceNextConnection>();
        private readonly ConcurrentQueue<AudioFile> Playlist = new ConcurrentQueue<AudioFile>();
        private readonly AudioDownloader Downloader = new AudioDownloader();
        private readonly AudioPlayer AudioPlayer = new AudioPlayer();

        private int delayActionTime = 10000;
        private bool delayToggle = false;
        private bool autoPlayEnabled = true;
        private bool autoPlayRunning = false;
        private bool autoStopEnabled= false;
        private Timer voiceChannelTimer = null;
        private bool leaveWhenEmpty = true;

        public async Task DelayAction(Action f)
        {
            delayToggle = true; // lock
            f();
            await Task.Delay(delayActionTime);
            delayToggle = false; // unlock
        }

        public async Task JoinAudioAsync(DiscordGuild guild, VoiceNextExtension voice, DiscordChannel target)
        {
            if (guild == null || target == null) { Console.WriteLine("Guild and target channel are null"); return; }

            
            if (ConnectedChannels.TryGetValue(guild.Id, out var connectedAudioClient))
            {
                Console.WriteLine("Client already connected");
                return;
            }

            if (target.Guild.Id != guild.Id) {
                Console.WriteLine("Current voice channel is incorrect");
                return;
            }

            var audioClient = voice.GetConnection(guild);
            if (audioClient != null)
            {
                // already connected
                Console.WriteLine("Alrdy connected in this guild.");
                return;
            }
            audioClient = await voice.ConnectAsync(target).ConfigureAwait(false);
            try
            {   
                if (ConnectedChannels.TryAdd(guild.Id,audioClient))
                {
                    Console.WriteLine("Connected " + guild.Id);
                    if (leaveWhenEmpty)
                        voiceChannelTimer = new Timer(CheckVoiceChannelState, target, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3));
                    return;
                }
                Console.WriteLine(ConnectedChannels.Count);
            }
            catch
            {
                Console.WriteLine("Client failed to connect");
            }
            //await target.ConnectAsync(memberVS).ConfigureAwait(false);
            Console.WriteLine("Unable to join channel");
        }

        public async Task LeaveAudioAsync(DiscordGuild guild)
        {
            if (guild == null) return;
            
            if (AudioPlayer.IsRunning()) AudioPlayer.Stop();
            while (AudioPlayer.IsRunning()) await Task.Delay(1000);

            if (ConnectedChannels.TryRemove(guild.Id, out var audioClient))
            {
                Console.WriteLine("Client disconnected");
                await DelayAction(() => audioClient.Disconnect());  
                return;
            }
            Console.WriteLine("Error " + guild.Id);
            return;
            
        }

        public async void CheckVoiceChannelState(object state) // checks when to leave
        {

            if (!(state is DiscordChannel channel)) return;
            if (!(channel.Type == ChannelType.Voice)) return;
            
            int count = (channel.Users).Count;
            
            if (count < 2)
            {
                await LeaveAudioAsync(channel.Guild);
                if (voiceChannelTimer != null)
                {
                    voiceChannelTimer.Dispose();
                    voiceChannelTimer = null;
                }
            }
            
        }
        
        // helper function for the autoplay function that waits for each track to finish and then pulls from the list
        public async Task AutoPlayAudioAsync(DiscordGuild guild, DiscordChannel channel)
        {
            while (autoPlayRunning = autoPlayEnabled)
            {
                if (AudioPlayer.IsRunning()) await Task.Delay(1000);
                if (Playlist.IsEmpty || !autoPlayEnabled || !autoPlayRunning)
                {
                    break;
                }

                if (ConnectedChannels.TryGetValue((guild.Id), out var audioClient))
                {
                    AudioFile song = PlaylistNext();
                    if (song != null)
                    {
                        await AudioPlayer.Play(audioClient, song);
                    }

                    if (Playlist.IsEmpty || !autoPlayEnabled || !autoPlayRunning)
                    {
                        break;
                    }
                    continue;
                }
                break;
            }
            if (autoStopEnabled) autoPlayEnabled = false;
            autoPlayRunning = false;
        }

        // checks if autoplay is true, but has not started. if not started, start it
        public async Task CheckAutoPlayAsync(DiscordGuild guild, DiscordChannel channel)
        {
            if (autoPlayEnabled && !autoPlayRunning && !AudioPlayer.IsRunning()) {
                await AutoPlayAudioAsync(guild, channel);
            }
        }
        
        // adds a song to the playlist
        public async Task PlaylistAddAsync(string path)
        {
            AudioFile song = await Downloader.GetAudioFileData(path);
            if(song != null)
            {
                Playlist.Enqueue(song);
            }
        }

        // gets the next song in the queue
        private AudioFile PlaylistNext()
        {
            Playlist.TryDequeue(out AudioFile song);
            return song;
        }

        // skips the current song playing if autoplay is on
        public void PlaylistSkip()
        {

        }

        // extracts simple meta data from the path and fills a new AudioFile info abt the audio source
        // if it fails, return null
        private async Task<AudioFile> GetAudioFileAsync(string path)
        {
            try
            {
                AudioFile song = await Downloader.GetAudioFileData(path);
                return song;
            }
            catch
            {
                return null;
            }
        }

        // prints the playlist information into the guild chat
        public void PrintPlaylist()
        {

        }
    }
}
