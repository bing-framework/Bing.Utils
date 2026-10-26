namespace Bing.IO;

public static partial class FileHelper
{
    /// <summary>
    /// 严格读取文件，传播路径、I/O 和超限异常。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <param name="maxBytes">可选字节上限，非负且包含边界。</param>
    /// <returns>独立字节数组，空文件返回空数组。</returns>
    public static byte[] ReadBytesStrict(string path, long? maxBytes = null)
    {
        var options = new StreamTransferOptions { MaxBytes = maxBytes }.Snapshot();
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var output = new MemoryStream();
        StreamTransfer.Copy(input, output, options);
        return output.ToArray();
    }

    /// <summary>
    /// 严格异步读取文件，原样传播取消和 I/O 异常。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <param name="maxBytes">可选字节上限，非负且包含边界。</param>
    /// <param name="cancellationToken">开始检查文件前即观察的取消令牌。</param>
    /// <returns>独立字节数组，空文件返回空数组。</returns>
    public static async Task<byte[]> ReadBytesStrictAsync(string path, long? maxBytes = null, CancellationToken cancellationToken = default)
    {
        var options = new StreamTransferOptions { MaxBytes = maxBytes }.Snapshot();
        cancellationToken.ThrowIfCancellationRequested();
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var output = new MemoryStream();
        await StreamTransfer.CopyAsync(input, output, options, cancellationToken: cancellationToken).ConfigureAwait(false);
        return output.ToArray();
    }
}
