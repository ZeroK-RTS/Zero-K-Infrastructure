using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using SpringDownloader.Properties;

namespace SpringDownloader
{
    class AutoJoinManager
    {
        List<string> channels = new List<string>();
        Dictionary<string, string> passwords = new Dictionary<string, string>();


        public IEnumerable<string> Channels { get { return channels; } }

        public AutoJoinManager()
        {
            Load();
        }

        public void Add(string userName)
        {
            if (!channels.Contains(userName)) channels.Add(userName);
            Save();
        }

        public void AddPassword(string channel, string password)
        {
            channel = channel.Replace("#", String.Empty);
            if (passwords.ContainsKey(channel)) passwords.Remove(channel);
            passwords[channel] = password;
        }

        public string GetPassword(string channel)
        {
            string password;
            passwords.TryGetValue(channel, out password);
            return password;
        }

        public void Remove(string userName)
        {
            channels.RemoveAll(c => userName == c);
            Save();
        }

        void Load()
        {
            if (Program.Conf.AutoJoinChannels != null)
            {
                channels = new List<string>();
                passwords = new Dictionary<string, string>();
                foreach (var line in Program.Conf.AutoJoinChannels)
                {
                    if (line == null) continue;
                    if (line.Contains(" "))
                    {
                        var words = line.Split(' ');
                        channels.Add(words[0]);
                        passwords.Add(words[0], words[1]);
                    }
                    else channels.Add(line);
                }
            }
        }

        void Save()
        {
            var collection = new StringCollection();
            foreach (var channel in channels)
            {
                string password;
                if (passwords.TryGetValue(channel, out password)) collection.Add(channel + " " + password);
                collection.Add(channel);
            }
            Program.Conf.AutoJoinChannels = collection;
            Program.SaveConfig();
        }
    }
}