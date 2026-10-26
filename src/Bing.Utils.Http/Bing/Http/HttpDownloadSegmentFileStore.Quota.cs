namespace Bing.Http;

public sealed partial class HttpDownloadSegmentFileStore
{
    /// <summary>
    /// 显式启用磁盘预算时，旧临时正文清理失败必须阻止继续写入。
    /// </summary>
    internal bool EnforceDiskReservation { get; set; }

    /// <summary>
    /// 统计接管下载前的正文残留，避免旧版本或中断留下的文件被漏算。
    /// </summary>
    internal long GetReservedWorkspaceLength()
    {
        long bytes = 0;
        if (Directory.Exists(_stagingDirectory))
        {
            RejectReservedLink(_stagingDirectory);
            foreach (var file in Directory.EnumerateFileSystemEntries(_stagingDirectory))
                bytes = checked(bytes + ReservedFileLength(file));
        }
        foreach (var file in GetMergeResidues()) bytes = checked(bytes + ReservedFileLength(file));
        return bytes;
    }

    /// <summary>
    /// 保留已验证的分片，严格清理其他正文；失败时不启动后续分片或合并。
    /// </summary>
    internal void PrepareReservedWorkspace(HttpDownloadSegmentsState state)
    {
        var completed = new HashSet<string>(state.Segments.Where(x => x.Completed)
            .Select(x => SegmentPath(x.Range)), StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(_stagingDirectory))
            foreach (var file in Directory.EnumerateFileSystemEntries(_stagingDirectory))
            {
                ReservedFileLength(file);
                if (!completed.Contains(file)) DeleteReservedFile(file);
            }
        foreach (var file in GetMergeResidues()) DeleteReservedFile(file);
    }

    /// <summary>
    /// 只识别本下载命名的合并临时文件，其他同目录文件不属于预算或清理范围。
    /// </summary>
    internal IEnumerable<string> GetMergeResidues()
    {
        var prefix = Path.GetFileName(_targetPath) + ".";
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(_targetPath)))
        {
            var name = Path.GetFileName(file);
            if (name.Length == prefix.Length + 36 && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParseExact(name.Substring(prefix.Length, name.Length - prefix.Length - 4), "N", out _))
                yield return file;
        }
    }

    /// <summary>
    /// 验证预算覆盖的是普通文件，不跟随目录或重解析点。
    /// </summary>
    private static long ReservedFileLength(string file)
    {
        RejectReservedLink(file);
        if (Directory.Exists(file)) throw new IOException("Unexpected directory in the reserved download workspace.");
        return new FileInfo(file).Length;
    }

    /// <summary>
    /// 清理不再需要的正文，拒绝链接并传播失败。
    /// </summary>
    private static void DeleteReservedFile(string file)
    {
        ReservedFileLength(file);
        File.Delete(file);
    }

    /// <summary>
    /// 拒绝预算工作区中的重解析点。
    /// </summary>
    private static void RejectReservedLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Linked download workspaces are not supported with disk reservations.");
    }
}
