using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Craftian.Minecraft.Versions
{
    public class VersionLoader
    {
        public async Task<List<Version>> GetInstalled()
        {
            return await Task.Run(() =>
            {
                return Directory.GetDirectories(Directories.Minecraft).Select(vnum => new Version(new DirectoryInfo(vnum).Name)).ToList();
            });
        }
    }
}
