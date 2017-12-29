using Craftian.Mojang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Craftian.Minecraft.Versions;
using Version = Craftian.Minecraft.Versions.Version;

namespace Craftian.Minecraft
{
    public class Launcher
    {
        private Version Version { get; set; }
        private VersionConfig Config { get; set; }
        private Profile Profile { get; set; }

        private string ParseLaunchArgs()
        {
            var builder = new StringBuilder(Config.Arguments);

            builder.Replace("${auth_player_name}",  Profile.PlayerName);
            builder.Replace("${auth_uuid}",         Profile.PlayerUuid);
            builder.Replace("${auth_access_token}", Profile.AccessToken);
            builder.Replace("${user_type}",         Profile.Type);

            builder.Replace("${version_name}",      Version.Number);
            builder.Replace("${assets_index_name}", Version.Number);
            
            builder.Replace("${version_type}",      Config.Type);

            builder.Replace("${game_directory}",    Directories.Minecraft);
            builder.Replace("${assets_root}",       Version.Assets);
            
            builder.Append(" -XX:+UnlockExperimentalVMOptions");
            builder.Append(" -XX:+UseG1GC");
            builder.Append(string.Format(" -Xmx{0}{1}", 1, "G"));
            builder.Append(string.Format(" -XX:G1NewSizePercent={0}", 20));
            builder.Append(string.Format(" -XX:G1ReservePercent={0}", 20));
            builder.Append(string.Format(" -XX:MaxGCPauseMillis={0}", 50));
            builder.Append(string.Format(" -XX:G1HeapRegionSize={0}{1}", 16, "M"));

            return builder.ToString();
        }

        public void Launch(Profile profile, Version version)
        {
            Version = version;
            Config = version.Config;
            Profile = profile;

            Console.WriteLine(ParseLaunchArgs());
        }
    }
}
