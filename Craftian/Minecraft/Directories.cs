using System.IO;
using System.Threading.Tasks;

namespace Craftian.Minecraft
{
    public class Directories
    {
        public static string Base = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        public static string Minecraft = Path.Combine(Base, "Minecraft");
        public static string Profiles = Path.Combine(Minecraft, "Profiles");
        public static string Versions = Path.Combine(Minecraft, "Versions");
        public static string Cache = Path.Combine(Minecraft, "Cache");
        public static string CacheDownloads = Path.Combine(Cache, "Downloads");
        public static string CacheMods = Path.Combine(Cache, "Mods");

        private static async void MakeNew(string directory)
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

            });
        }

        public static async void MakeDefaults()
        {
            await Task.Run(() =>
            {
                MakeNew(Base);
                MakeNew(Profiles);
                MakeNew(Versions);
                MakeNew(Cache);
                MakeNew(CacheDownloads);
                MakeNew(CacheMods);
                MakeNew(Minecraft);
            });
        }
    }
}
