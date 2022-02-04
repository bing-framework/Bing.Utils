using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bing.IO
{
    /// <summary>
    /// 文件操作帮助类 - 读取
    /// </summary>
    public static partial class FileHelper
    {
        /// <summary>
        /// 读取文件到字节流中
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
        /// 读取文件到字节流中
        /// </summary>
        /// <param name="targetFilePath">目标文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async Task<byte[]> ReadAsync(string targetFilePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(targetFilePath))
                return null;
#if NETSTANDARD2_1 || NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0
            await using var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read);
#else
            using var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read);
#endif
            var byteArray = new byte[fs.Length];
            await fs.ReadAsync(byteArray, 0, byteArray.Length, cancellationToken);
            return byteArray;
        }

        /// <summary>
        /// 读取文件到字符串中
        /// </summary>
        /// <param name="targetFilePath">目标文件路径</param>
        public static string ReadToString(string targetFilePath) => ReadToString(targetFilePath, Encoding.UTF8);

        /// <summary>
        /// 读取文件到字符串中
        /// </summary>
        /// <param name="targetFilePath">目标文件路径</param>
        /// <param name="encoding">字符编码</param>
        public static string ReadToString(string targetFilePath, Encoding encoding)
        {
            if (File.Exists(targetFilePath) == false)
                return string.Empty;
            using var reader = new StreamReader(targetFilePath, encoding);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// 读取文件到字符串中
        /// </summary>
        /// <param name="targetFilePath">目标文件路径</param>
        public static Task<string> ReadToStringAsync(string targetFilePath) => ReadToStringAsync(targetFilePath, Encoding.UTF8);

        /// <summary>
        /// 读取文件到字符串中
        /// </summary>
        /// <param name="targetFilePath">目标文件路径</param>
        /// <param name="encoding">字符编码</param>
        public static async Task<string> ReadToStringAsync(string targetFilePath, Encoding encoding)
        {
            if (File.Exists(targetFilePath) == false)
                return string.Empty;
            using var reader = new StreamReader(targetFilePath, encoding);
            return await reader.ReadToEndAsync();
        }
    }
}