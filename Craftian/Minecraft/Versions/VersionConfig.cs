using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Craftian.Minecraft.Versions
{
    public class VersionConfig
    {
        private JObject Config { get; set; }
        private string ConfigFile { get; set; }

        public bool Exists => File.Exists(ConfigFile);

        #region Configuration Strings

        public string MainClass => GetConfigString("mainClass");
        public string Arguments => GetConfigString("minecraftArguments");
        public string MinimumLauncherVersion => GetConfigString("minimumLauncherVersion");
        public string ReleaseTime => GetConfigString("releasetime");
        public string Time => GetConfigString("time");
        public string Type => GetConfigString("type");

        #endregion

        private string GetConfigString(string property)
        {
            try
            {
                return Config[property].ToObject<string>();
            }
            catch
            {
                return null;
            }
        }

        public VersionConfig(string configFile)
        {
            ConfigFile = configFile;
            Config = JObject.Parse(File.ReadAllText(ConfigFile));
        }
    }
}
