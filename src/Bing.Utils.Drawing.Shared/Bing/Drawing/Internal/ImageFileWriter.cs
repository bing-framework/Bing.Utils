using System.Threading;

namespace Bing.Drawing.Internal;

/// <summary>
/// 以临时文件方式写入图像文件。
/// </summary>
/// <remarks>
/// 临时文件与目标位于同一目录，提交失败时保留已有目标文件。
/// </remarks>
internal static class ImageFileWriter
{
    /// <summary>
    /// 将图像数据写入目标路径。
    /// </summary>
    /// <param name="path">目标文件路径。</param>
    /// <param name="bytes">待写入的图像数据。</param>
    /// <param name="cancellationToken">用于取消写入的令牌。</param>
    /// <exception cref="ArgumentException">目标路径为空。</exception>
    /// <exception cref="ArgumentNullException">图像数据为 null。</exception>
    internal static void Write(string path, byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("目标路径不能为空。", nameof(path));
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        cancellationToken.ThrowIfCancellationRequested();
        var destination = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, ".bing-image-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                for (var offset = 0; offset < bytes.Length; offset += 81920)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    output.Write(bytes, offset, Math.Min(81920, bytes.Length - offset));
                }
                output.Flush();
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(destination)) File.Replace(temporary, destination, null);
            else File.Move(temporary, destination);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
