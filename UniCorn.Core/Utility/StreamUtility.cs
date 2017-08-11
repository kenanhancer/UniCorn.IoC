using System.IO;
using System.Threading.Tasks;

namespace UniCorn.Core
{
    public static class StreamUtility
    {
        public static async Task<MemoryStream> StringToStream(this string content)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            await writer.WriteAsync(content);
            writer.Flush();
            return stream;
        }

        public static async Task<bool> FileToStream(string path, Stream destinationStream)
        {
            bool result = false;
            if (File.Exists(path))
                using (FileStream fs = File.OpenRead(path))
                {
                    await fs.CopyToAsync(destinationStream).ConfigureAwait(false);
                    result = true;
                }
            return result;
        }
    }
}