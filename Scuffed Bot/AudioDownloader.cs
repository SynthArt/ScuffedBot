using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Scuffed_Bot
{
    //downloads meta data
    public class AudioDownloader
    {
        public async Task<AudioFile> GetAudioFileData(string path)
        {
            if (path == null) return null;
            Console.WriteLine("Extracting metadata for: " + path);

            Process youtubedlp;
            AudioFile StreamData = new AudioFile();
            try
            {
                ProcessStartInfo youtubedlMetaData = new ProcessStartInfo()
                {
                    FileName = "yt-dlp",
                    Arguments = $"-s -e {path}",
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                youtubedlp = Process.Start(youtubedlMetaData);
                youtubedlp.WaitForExit();

                // read output of the simulation
                string[] output = youtubedlp.StandardOutput.ReadToEnd().Split("\n");

                // set the file name
                StreamData.FileName = path;
                if (output.Length > 0)
                    StreamData.Title = output[0];
            }
            catch
            {
                Console.WriteLine("youtube-dlp.exe failed to extract the file data!!");
                return null;
            }
            await Task.Delay(0);
            return StreamData;
        }
    }
}
