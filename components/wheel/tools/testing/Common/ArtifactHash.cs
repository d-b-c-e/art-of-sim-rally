using System.IO;
using System.Security.Cryptography;
namespace ArtOfSimRally.Testing
{
    internal static class ArtifactHash
    {
        public static string FileHash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var hash = SHA256.Create())
                return System.BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "");
        }
    }
}
