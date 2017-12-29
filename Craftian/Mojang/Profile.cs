using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using Craftian.Minecraft;

namespace Craftian.Mojang
{
    public class Profile
    {
        public string Username { get; private set; }
        public string PlayerName { get; private set; }
        public string PlayerUuid { get; private set; }
        public string AccessToken { get; private set; }
        public string Type { get; private set; }

        public async Task<bool> Save()
        {
            return await Task.Run(() =>
            {
                var profile = Path.Combine(Directories.Profiles, PlayerName + ".mcusr");

                if (File.Exists(profile))
                    File.Delete(profile);

                using (var file = File.CreateText(profile))
                {
                    new JsonSerializer().Serialize(file, this);
                }

                return true;

            });
        }

        public static Profile Load(string playerName)
        {
            var profile = Path.Combine(Directories.Profiles, playerName + ".mcusr");

            if (!File.Exists(profile))
                throw new FileNotFoundException();

            using (var file = File.OpenText(profile))
            {
                return (Profile)new JsonSerializer().Deserialize(file, typeof(Profile));
            }
        }

        public async void Remove()
        {
            await Task.Run(() =>
            {
                var profile = Path.Combine(Directories.Profiles, PlayerName + ".mcusr");

                if (File.Exists(profile))
                    File.Delete(profile);
            });
        }

        public Profile(string username, string playerName, string playerUuid, string accessToken, string type)
        {
            Username = username;
            PlayerName = playerName;
            PlayerUuid = playerUuid;
            AccessToken = accessToken;
            Type = type;
        }
    }
}
