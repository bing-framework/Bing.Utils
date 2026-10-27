namespace Bing.IO;

/// <summary>配置文件内容识别的读取与容器解析预算。</summary>
public sealed class FileDetectionOptions
{
    /// <summary>最多检查的前缀字节数，默认 65536，必须大于零；实际还受内置规则长度限制。</summary>
    public int MaxHeaderBytes { get; set; } = 64 * 1024;

    /// <summary>是否检查 ZIP 中的 Office 包结构，默认 false；需要完整字节输入或可定位流。</summary>
    public bool InspectContainers { get; set; }

    /// <summary>容器阶段额外读取或检查的累计字节预算，默认 4 MiB；重复读取同一区域重复计数。</summary>
    public int MaxContainerBytesRead { get; set; } = 4 * 1024 * 1024;

    /// <summary>ZIP 中央目录的最大字节数，默认 1 MiB；在分配目录缓冲之前检查。</summary>
    public int MaxZipDirectoryBytes { get; set; } = 1024 * 1024;

    /// <summary>ZIP 中央目录的最大条目数，默认 4096；在创建条目集合之前检查。</summary>
    public int MaxZipEntries { get; set; } = 4096;

    /// <summary>两个识别用 XML 的累计解压字节预算，默认 1 MiB；不解压 Office 正文。</summary>
    /// <remarks>Deflate 输出恰好耗尽预算且尚未确认结束时也返回 ContainerLimitReached，不额外解压探测。</remarks>
    public int MaxMetadataBytes { get; set; } = 1024 * 1024;

    /// <summary>读取并校验预算；调用开始后不再读取调用方的选项。</summary>
    internal static FileDetectionOptions Snapshot(FileDetectionOptions options)
    {
        var snapshot = options == null ? new FileDetectionOptions() : new FileDetectionOptions
        {
            MaxHeaderBytes = options.MaxHeaderBytes,
            InspectContainers = options.InspectContainers,
            MaxContainerBytesRead = options.MaxContainerBytesRead,
            MaxZipDirectoryBytes = options.MaxZipDirectoryBytes,
            MaxZipEntries = options.MaxZipEntries,
            MaxMetadataBytes = options.MaxMetadataBytes
        };
        Positive(snapshot.MaxHeaderBytes, nameof(MaxHeaderBytes));
        Positive(snapshot.MaxContainerBytesRead, nameof(MaxContainerBytesRead));
        Positive(snapshot.MaxZipDirectoryBytes, nameof(MaxZipDirectoryBytes));
        Positive(snapshot.MaxZipEntries, nameof(MaxZipEntries));
        Positive(snapshot.MaxMetadataBytes, nameof(MaxMetadataBytes));
        return snapshot;
    }

    /// <summary>所有预算均须为正数，不静默放宽配置。</summary>
    private static void Positive(int value, string name)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(name, "检测预算必须大于零。");
    }
}
