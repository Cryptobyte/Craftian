using System;
using System.IO;

namespace Craftian.Minecraft.Versions
{
    public class Version
    {
        public string Number { get; private set; }

        #region Directories

        public string Base => Path.Combine(Directories.Minecraft, Number);
        public string Assets => Path.Combine(Base, "assets");
        public string CrashReports => Path.Combine(Base, "crash-reports");
        public string Libraries => Path.Combine(Base, "libraries");
        public string Logs => Path.Combine(Base, "logs");
        public string ResourcePacks => Path.Combine(Base, "resourcepacks");
        public string Saves => Path.Combine(Base, "saves");
        public string Screenshots => Path.Combine(Base, "screenshots");
        public string Versions => Path.Combine(Base, "versions");

        #endregion

        #region Files

        public string JarFile => Path.Combine(Versions, Number, Number + ".jar");
        public string ConfigFile => Path.Combine(Versions, Number, Number + ".json");

        #endregion

        public VersionConfig Config => new VersionConfig(ConfigFile);

        public Version(string number)
        {
            Number = number;
        }
    }
}
