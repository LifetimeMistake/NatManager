using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Server.Configuration
{
    public class ConfigurationFile
    {
        private Dictionary<string, string> configEntries;
        public ConfigurationFile(string path)
        {
            configEntries = new Dictionary<string, string>();
            LoadFile(path);
        }

        public ConfigurationFile(Dictionary<string, string> kvp)
        {
            configEntries = kvp ?? throw new ArgumentNullException(nameof(kvp));
        }

        public ConfigurationFile()
        {
            configEntries = new Dictionary<string, string>();
        }

        public void LoadFile(string path)
        {
            Load(File.ReadAllText(path));
        }

        public void Load(string pairs)
        {
            configEntries.Clear();
            foreach(string line in pairs.Split(Environment.NewLine))
            {
                string[] parts = line.Split(" ");
                string key = parts[0];

                if (key == "")
                    continue;

                if (configEntries.ContainsKey(key))
                    continue;

                string value = string.Join(" ", parts.Skip(1));
                configEntries.Add(key, value);
            }
        }

        public void Save(string path)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach(KeyValuePair<string, string> kvp in configEntries)
                stringBuilder.Append(string.Join(" ", kvp.Key, kvp.Value));

            File.WriteAllText(path, stringBuilder.ToString());
        }

        public IEnumerable<KeyValuePair<string, string>> GetConfigValues()
        {
            foreach(KeyValuePair<string, string> kvp in configEntries)
            {
                yield return kvp;
            }
        }

        public string GetConfigValue(string key)
        {
            return configEntries[key];
        }

        public bool HasConfigKey(string key)
        {
            return configEntries.ContainsKey(key);
        }

        public void SetConfigValue(string key, string value)
        {
            if (!configEntries.ContainsKey(key))
                configEntries.Add(key, value);
            else
                configEntries[key] = value;
        }
    }
}
