using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Newtonsoft.Json;

namespace Scuffed_Bot
{
    public class AudioFile
    {
        private string fileName;
        private string title;

        public AudioFile()
        {
            fileName = "";
            title = "";
        }

        public override string ToString()
        {
            return title;
        }

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        public string Title 
        { 
            get { return title; }
            set { title = value; }
        }
    }
}
