using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.VoiceNext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Scuffed_Bot
{
    public class AudioPlayer
    {
        private bool runStatus;
        private Process process;
        private VoiceTransmitSink stream;
        private int BLOCK_SIZE;
        private float volume;
        private bool isPlaying;

        public AudioPlayer()
        {
            runStatus = false;
            process = null;
            stream = null;
            BLOCK_SIZE = 3840;
            volume = 1.0f;
            isPlaying = false;
    }

        private Process CreateNetworkStream(string path)
        {
            try
            {
                /*$"/C yt-dlp --youtube-skip-dash-manifest --verbose --ignore-errors -f 251 -o - {path}"*/
                /*yt-dlp -x m4a -o - {path} | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1*/
                return Process.Start(new ProcessStartInfo {
                    FileName = "cmd.exe",
                    Arguments = $"/C yt-dlp -x m4a -o - \"ytsearch:{path}\" | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
            }
            catch
            {
                Console.WriteLine($"Error while opening network stream: {path}");
                return null;
            }
        }

        private async Task AudioPlaybackAsync(VoiceNextConnection audioClient, AudioFile song)
        {
            runStatus = true;
            process = CreateNetworkStream(song.FileName);
            stream = audioClient.GetTransmitSink();
            isPlaying = true;
            Console.WriteLine("Functioning Flag");
            await Task.Delay(2500); // wait some time to buffer

            while (true)
            {
                // pre conditions
                
                if (process == null || process.HasExited) break;
                if (stream == null) break;
                if (!isPlaying) continue;
                Console.WriteLine("Pre-Condition Flag");
                // read the stream in chunks determined by the m_BLOCK_SIZE variable
                int blockSize = BLOCK_SIZE; // also reffered to as bufferSize
                byte[] buffer = new byte[blockSize];
                int byteCount;
                byteCount = await process.StandardOutput.BaseStream.ReadAsync(buffer, 0, blockSize);
                Console.WriteLine($"Buffer Flag, byteCount: {byteCount}, buffer: {buffer.Length}, blockSize: {blockSize}");
                // if stream can no longer be read we end the loop
                if (byteCount <= 0) break;
                Console.WriteLine("EndOfLoop Flag");
                try
                {
                    await stream.WriteAsync(ScaleVolumeSafeAllocateBuffers(buffer, volume),0,byteCount);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    break;
                }
            }

            // end stream and process if it hasnt already
            if(process != null && !process.HasExited) process.Kill();
            // flush stream for a clean slate
            if(stream != null) stream.FlushAsync().Wait();
            // reset variable states
            process = null;
            stream = null;
            isPlaying = false;
            runStatus = false;
        }
        
        private byte[] ScaleVolumeSafeAllocateBuffers(byte[] audioSamples, float volume)
        {
            Console.WriteLine("Volume Flag");
            if (audioSamples == null) return null;
            if (audioSamples.Length % 2 != 0) return null;
            if (volume < 0.0f || volume > 1.0f) return null;
            var output = new byte[audioSamples.Length];
            try
            {
                if(Math.Abs(volume - 1.0f) < 0.0001f)
                {
                    Buffer.BlockCopy(audioSamples, 0, output, 0, audioSamples.Length);
                    return output;
                }
                // 16-bit precision for the multiplication
                int volumeFixed = (int)Math.Round(volume * 65536d);
                for (int i = 0; i < output.Length; i+=2)
                {
                    int sample = (short)((audioSamples[i+1] << 8) | audioSamples[i]);
                    int processed = (sample * volumeFixed) >> 16;

                    output[i] = (byte)processed;
                    output[i + 1] = (byte)(processed >> 8);
                }
                return output;
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
                return null;
            }
        }
        public async Task Play(VoiceNextConnection audioClient, AudioFile song)
        {
            if (IsRunning()) Stop();
            while (IsRunning())
            {
                await Task.Delay(1000);
            }
            await AudioPlaybackAsync(audioClient, song);
        }

        public void Stop()
        {
            if (process != null) process.Kill(); 
        }

        public bool IsRunning() { return runStatus; }
    }
}
