using System.IO;
using System.Text;
using System.Threading.Tasks;
using Bing.Helpers;

namespace Bing.IO
{
    /// <summary>
    /// 文件操作帮助类 - 读取
    /// </summary>
    public static partial class FileHelper
    {
        /// <summary>
        /// 将文件读取到字节流中
        /// </summary>
        /// <param name="targetFilePath">目标文件路径</param>
        public static byte[] Read(string targetFilePath)
        {
            if (!File.Exists(targetFilePath))
                return null;

            using var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read);
            var byteArray = new byte[fs.Length];
            fs.Read(byteArray, 0, byteArray.Length);
            return byteArray;
        }

        /// <summary>
        /// 从文件读取到字节流中
        /// </summary>
        /// <param name="targetFilePath">目标文件路径</param>
        public static async Task<byte[]> ReadAsync(string targetFilePath)
        {
            if (!File.Exists(targetFilePath))
                return null;
#if NETSTANDARD2_1 || NETCOREAPP3_0 || NETCOREAPP3_1
            await using var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read);
#else
            using var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read);
#endif
            var byteArray = new byte[fs.Length];
            await fs.ReadAsync(byteArray, 0, byteArray.Length);
            return byteArray;
        }

        /// <summary>
        /// 读取文件所有文本
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static async Task<string> ReadAllTextAsync(string filePath)
        {
            Check.NotNull(filePath, nameof(filePath));
            using var reader = File.OpenText(filePath);
            return await reader.ReadToEndAsync();
        }


        /// <summary>
        /// 读取文件所有字节
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static async Task<byte[]> ReadAllBytesAsync(string filePath)
        {
            Check.NotNull(filePath, nameof(filePath));
            using var stream = File.Open(filePath, FileMode.Open);
            var result = new byte[stream.Length];
            await stream.ReadAsync(result, 0, (int) stream.Length);
            return result;
        }


        ///// <summary>
        ///// 读取文件到字符串
        ///// </summary>
        ///// <param name="filePath">文件的绝对路径</param>
        //public static string Read(string filePath) => Read(filePath, Encoding.UTF8);

        /// <summary>
        /// 读取文件到字符串
        /// </summary>
        /// <param name="filePath">文件的绝对路径</param>
        /// <param name="encoding">字符编码</param>
        public static string Read(string filePath, Encoding encoding)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            if (!File.Exists(filePath))
                return string.Empty;
            using var reader = new StreamReader(filePath, encoding);
            return reader.ReadToEnd();
        }


        /// <summary>
        /// 将文件读取到字节流中
        /// </summary>
        /// <param name="targetFilePath">目标文件路径</param>
        public static byte[] ReadToBytes(string targetFilePath)
        {
            if (!File.Exists(targetFilePath))
                return null;
            using var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read);
            var byteArray = new byte[fs.Length];
            fs.Read(byteArray, 0, byteArray.Length);
            return byteArray;
        }

        /// <summary>
        /// 将文件读取到字节流中
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        public static byte[] ReadToBytes(FileInfo fileInfo)
        {
            if (fileInfo == null)
                return null;
            var fileSize = (int) fileInfo.Length;
            using var reader = new BinaryReader(fileInfo.Open(FileMode.Open));
            return reader.ReadBytes(fileSize);
        }
    }
}