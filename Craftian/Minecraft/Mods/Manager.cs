using System.Collections.Generic;
using System.Windows.Documents;
using Version = Craftian.Minecraft.Versions.Version;

namespace Craftian.Minecraft.Mods
{
    public class Manager
    {
        public Version Version { get; private set; }
        public List<Mod> InstalledMods { get; private set; }

        public Manager(Version version)
        {
            Version = version;
        }

        public async void Save()
        {

        }

        public async void Load()
        {

        }

        public async void Install(Mod mod)
        {

        }

        public async void Uninstall(Mod mod)
        {

        }
    }
}
