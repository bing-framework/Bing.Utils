using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FluentFTP;

namespace Bing.Net.FTP;

/// <summary>
/// 标识远程目录项的对象类型。
/// </summary>
public enum FtpDirectoryEntryType
{
    /// <summary>
    /// 普通文件。
    /// </summary>
    File,
    /// <summary>
    /// 目录。
    /// </summary>
    Directory,
    /// <summary>
    /// 符号链接或服务器报告的链接。
    /// </summary>
    Link,
    /// <summary>
    /// 后端无法识别的对象。
    /// </summary>
    Unknown
}

/// <summary>
/// 定义目标文件冲突时采用的处理策略。
/// </summary>
public enum FtpConflictPolicy
{
    /// <summary>
    /// 发现同名文件时抛出异常。
    /// </summary>
    Error,
    /// <summary>
    /// 保留目标并跳过该文件。
    /// </summary>
    Skip,
    /// <summary>
    /// 从头覆盖目标文件。
    /// </summary>
    Overwrite,
    /// <summary>
    /// 从目标现有长度继续传输。
    /// </summary>
    Resume
}

/// <summary>
/// 表示远程 FTP 目录中的一个文件系统项。
/// </summary>
public sealed class FtpDirectoryEntry
{
    /// <summary>
    /// 使用远程目录项元数据创建实例。
    /// </summary>
    /// <param name="name">目录项名称，不包含父目录路径。</param>
    /// <param name="fullName">服务器返回的完整路径。</param>
    /// <param name="type">对象类型。</param>
    /// <param name="size">可用的文件大小；服务器未提供时为 null。</param>
    /// <param name="modifiedTime">可用的服务器修改时间。</param>
    public FtpDirectoryEntry(string name, string fullName, FtpDirectoryEntryType type, long? size = null,
        DateTime? modifiedTime = null)
    {
        Name = name ?? string.Empty;
        FullName = fullName ?? string.Empty;
        Type = type;
        Size = size;
        ModifiedTime = modifiedTime;
    }

    /// <summary>
    /// 获取目录项名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 获取服务器返回的完整路径。
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// 获取目录项类型。
    /// </summary>
    public FtpDirectoryEntryType Type { get; }

    /// <summary>
    /// 获取可用的文件大小；服务器无法提供大小时为 null。
    /// </summary>
    public long? Size { get; }

    /// <summary>
    /// 获取可用的修改时间；服务器无法提供时间时为 null。
    /// </summary>
    public DateTime? ModifiedTime { get; }
}

/// <summary>
/// 表示服务器返回的远程文件校验值。
/// </summary>
public sealed class FtpRemoteChecksum
{
    /// <summary>
    /// 使用服务器返回的算法和值创建校验结果。
    /// </summary>
    /// <param name="algorithm">服务器实际采用的算法。</param>
    /// <param name="value">服务器返回的校验值。</param>
    public FtpRemoteChecksum(FtpHashAlgorithm algorithm, string value)
    {
        Algorithm = algorithm;
        Value = value ?? string.Empty;
    }

    /// <summary>
    /// 获取服务器实际采用的算法。
    /// </summary>
    public FtpHashAlgorithm Algorithm { get; }

    /// <summary>
    /// 获取服务器返回的校验值。
    /// </summary>
    public string Value { get; }
}

/// <summary>
/// 汇总一次目录传输中完成、跳过或失败的文件。
/// </summary>
public sealed class FtpDirectoryTransferResult
{
    internal FtpDirectoryTransferResult(int createdDirectories, IEnumerable<string> transferredPaths,
        IEnumerable<string> skippedPaths, IEnumerable<string> failedPaths)
    {
        CreatedDirectories = createdDirectories;
        TransferredPaths = Freeze(transferredPaths);
        SkippedPaths = Freeze(skippedPaths);
        FailedPaths = Freeze(failedPaths);
    }

    /// <summary>
    /// 获取本地创建或远程创建的目录数。
    /// </summary>
    public int CreatedDirectories { get; }

    /// <summary>
    /// 获取成功传输的文件路径列表。
    /// </summary>
    public IReadOnlyList<string> TransferredPaths { get; }

    /// <summary>
    /// 获取因冲突策略或链接保护而跳过的路径列表。
    /// </summary>
    public IReadOnlyList<string> SkippedPaths { get; }

    /// <summary>
    /// 获取后端返回失败状态的文件路径列表。
    /// </summary>
    public IReadOnlyList<string> FailedPaths { get; }

    private static IReadOnlyList<string> Freeze(IEnumerable<string> values)
    {
        var copy = new List<string>(values);
        return new ReadOnlyCollection<string>(copy);
    }
}
