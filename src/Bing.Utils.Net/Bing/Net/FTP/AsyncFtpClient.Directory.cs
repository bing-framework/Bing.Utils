using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Bing.Net.FTP;

public sealed partial class AsyncFtpClient
{
    /// <inheritdoc />
    public Task<IReadOnlyList<FtpDirectoryEntry>> GetDirectoryEntriesAsync(
        string remoteDirectory,
        CancellationToken cancellationToken = default)
    {
        ValidatePath(remoteDirectory, nameof(remoteDirectory));
        return ExecuteAsync(async token =>
        {
            var backend = RequireDirectoryBackend();
            var entries = await backend.GetDirectoryEntriesAsync(remoteDirectory, token).ConfigureAwait(false);
            return (IReadOnlyList<FtpDirectoryEntry>)new List<FtpDirectoryEntry>(entries).AsReadOnly();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FtpRemoteChecksum> GetFileChecksumAsync(
        string remotePath,
        FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE,
        CancellationToken cancellationToken = default)
    {
        ValidatePath(remotePath, nameof(remotePath));
        if (!Enum.IsDefined(typeof(FtpHashAlgorithm), algorithm))
            throw new ArgumentOutOfRangeException(nameof(algorithm));
        return ExecuteAsync(token => RequireDirectoryBackend().GetFileChecksumAsync(remotePath, algorithm, token),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<FtpDirectoryTransferResult> UploadDirectoryAsync(
        string localDirectory,
        string remoteDirectory,
        FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error,
        IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidatePath(localDirectory, nameof(localDirectory));
        ValidatePath(remoteDirectory, nameof(remoteDirectory));
        ValidateConflictPolicy(conflictPolicy);
        var localRoot = Path.GetFullPath(localDirectory);
        if (IsExistingReparsePoint(localRoot)) throw new IOException("本地源根目录不能是 reparse point。");
        if (!Directory.Exists(localRoot)) throw new DirectoryNotFoundException($"本地源目录不存在：{localRoot}");

        return ExecuteAsync(token => UploadDirectoryCoreAsync(localRoot, remoteDirectory, conflictPolicy, progress, token),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<FtpDirectoryTransferResult> DownloadDirectoryAsync(
        string localDirectory,
        string remoteDirectory,
        FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error,
        IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidatePath(localDirectory, nameof(localDirectory));
        ValidatePath(remoteDirectory, nameof(remoteDirectory));
        ValidateConflictPolicy(conflictPolicy);
        var localRoot = Path.GetFullPath(localDirectory);
        if (IsExistingReparsePoint(localRoot)) throw new IOException("本地目标根目录不能是 reparse point。");
        if (File.Exists(localRoot)) throw new IOException($"本地目标路径是文件：{localRoot}");

        return ExecuteAsync(token => DownloadDirectoryCoreAsync(localRoot, remoteDirectory, conflictPolicy, progress, token),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> MoveDirectoryAsync(
        string remotePath,
        string destinationPath,
        FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error,
        CancellationToken cancellationToken = default)
    {
        ValidatePath(remotePath, nameof(remotePath));
        ValidatePath(destinationPath, nameof(destinationPath));
        ValidateConflictPolicy(conflictPolicy);
        if (conflictPolicy == FtpConflictPolicy.Resume)
            throw new NotSupportedException("Resume 只适用于文件传输，不能用于目录移动。");

        return ExecuteAsync(async token =>
        {
            var backend = RequireDirectoryBackend();
            var destinationExists = await _backend.DirectoryExistsAsync(destinationPath, token).ConfigureAwait(false) ||
                                    await _backend.FileExistsAsync(destinationPath, token).ConfigureAwait(false);
            if (destinationExists)
            {
                if (conflictPolicy == FtpConflictPolicy.Error)
                    throw new IOException($"远程目标已存在：{destinationPath}");
                if (conflictPolicy == FtpConflictPolicy.Skip) return false;
            }

            var existsMode = conflictPolicy == FtpConflictPolicy.Overwrite
                ? FtpRemoteExists.Overwrite
                : FtpRemoteExists.Skip;
            var moved = await backend.MoveDirectoryAsync(remotePath, destinationPath, existsMode, token)
                .ConfigureAwait(false);
            if (!moved && conflictPolicy == FtpConflictPolicy.Error)
                throw new IOException($"目录未移动；远程目标可能已存在：{destinationPath}");
            return moved;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteDirectoryAsync(string remotePath, CancellationToken cancellationToken = default)
    {
        ValidatePath(remotePath, nameof(remotePath));
        return ExecuteAsync(token => RequireDirectoryBackend().DeleteDirectoryAsync(remotePath, token), cancellationToken);
    }

    private async Task<FtpDirectoryTransferResult> UploadDirectoryCoreAsync(
        string localRoot,
        string remoteRoot,
        FtpConflictPolicy conflictPolicy,
        IProgress<FtpProgress> progress,
        CancellationToken cancellationToken, DirectoryBudget budget = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var backend = RequireDirectoryBackend();
        var result = new DirectoryResultBuilder();
        if (await EnsureRemoteDirectoryAsync(remoteRoot, cancellationToken).ConfigureAwait(false))
        {
            result.CreatedDirectories++;
        }

        var pending = new Stack<DirectoryNode>();
        pending.Push(new DirectoryNode(localRoot, remoteRoot));
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = pending.Pop();
            foreach (var entryPath in Directory.EnumerateFileSystemEntries(current.LocalPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = Path.GetFileName(entryPath);
                ValidatePathSegment(name, entryPath);
                var remotePath = CombineRemotePath(current.RemotePath, name);
                var attributes = File.GetAttributes(entryPath);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    result.SkippedPaths.Add(remotePath);
                    continue;
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    budget?.CheckDepth(checked(current.Depth + 1), remotePath);
                    if (await EnsureRemoteDirectoryAsync(remotePath, cancellationToken).ConfigureAwait(false))
                        result.CreatedDirectories++;
                    pending.Push(new DirectoryNode(entryPath, remotePath, checked(current.Depth + 1)));
                    continue;
                }

                budget?.CountFile(remotePath);
                var exists = await _backend.FileExistsAsync(remotePath, cancellationToken).ConfigureAwait(false);
                if (exists && conflictPolicy == FtpConflictPolicy.Error)
                    throw new IOException($"远程文件已存在：{remotePath}");
                if (exists && conflictPolicy == FtpConflictPolicy.Skip)
                {
                    result.SkippedPaths.Add(remotePath);
                    continue;
                }

                budget?.ReserveFile(new FileInfo(entryPath).Length, remotePath);
                var existsMode = ToRemoteExistsMode(conflictPolicy);
                var status = await backend.UploadFileAsync(entryPath, remotePath, existsMode, progress, cancellationToken)
                    .ConfigureAwait(false);
                RecordTransferStatus(status, conflictPolicy, remotePath, result);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return result.Build();
    }

    private async Task<FtpDirectoryTransferResult> DownloadDirectoryCoreAsync(
        string localRoot,
        string remoteRoot,
        FtpConflictPolicy conflictPolicy,
        IProgress<FtpProgress> progress,
        CancellationToken cancellationToken, DirectoryBudget budget = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var backend = RequireDirectoryBackend();
        var result = new DirectoryResultBuilder();
        if (!Directory.Exists(localRoot))
        {
            Directory.CreateDirectory(localRoot);
            result.CreatedDirectories++;
        }

        var pending = new Stack<DirectoryNode>();
        pending.Push(new DirectoryNode(localRoot, remoteRoot));
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = pending.Pop();
            var entries = await backend.GetDirectoryEntriesAsync(current.RemotePath, cancellationToken).ConfigureAwait(false);
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry.Name == "." || entry.Name == "..")
                {
                    result.SkippedPaths.Add(entry.FullName);
                    continue;
                }

                ValidatePathSegment(entry.Name, entry.FullName);
                var remotePath = CombineRemotePath(current.RemotePath, entry.Name);
                if (entry.Type == FtpDirectoryEntryType.Link || entry.Type == FtpDirectoryEntryType.Unknown)
                {
                    result.SkippedPaths.Add(remotePath);
                    continue;
                }

                var localPath = GetSafeLocalChildPath(localRoot, current.LocalPath, entry.Name);
                if (IsExistingReparsePoint(localPath))
                {
                    result.SkippedPaths.Add(remotePath);
                    continue;
                }

                if (entry.Type == FtpDirectoryEntryType.Directory)
                {
                    budget?.CheckDepth(checked(current.Depth + 1), remotePath);
                    if (File.Exists(localPath))
                    {
                        HandleDirectoryFileCollision(localPath, conflictPolicy, result);
                        continue;
                    }

                    if (Directory.Exists(localPath) && IsReparsePoint(localPath))
                    {
                        result.SkippedPaths.Add(remotePath);
                        continue;
                    }

                    if (!Directory.Exists(localPath))
                    {
                        Directory.CreateDirectory(localPath);
                        result.CreatedDirectories++;
                    }

                    pending.Push(new DirectoryNode(localPath, remotePath, checked(current.Depth + 1)));
                    continue;
                }

                budget?.CountFile(remotePath);
                if (Directory.Exists(localPath))
                {
                    HandleDirectoryFileCollision(localPath, conflictPolicy, result);
                    continue;
                }

                var exists = File.Exists(localPath);
                if (exists && conflictPolicy == FtpConflictPolicy.Error)
                    throw new IOException($"本地文件已存在：{localPath}");
                if (exists && conflictPolicy == FtpConflictPolicy.Skip)
                {
                    result.SkippedPaths.Add(remotePath);
                    continue;
                }

                budget?.ReserveFile(entry.Size, remotePath);
                var parent = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);
                var status = await backend.DownloadFileAsync(localPath, remotePath, ToLocalExistsMode(conflictPolicy),
                    progress, cancellationToken).ConfigureAwait(false);
                RecordTransferStatus(status, conflictPolicy, remotePath, result);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return result.Build();
    }

    private async Task<bool> EnsureRemoteDirectoryAsync(string remotePath, CancellationToken cancellationToken)
    {
        if (await _backend.DirectoryExistsAsync(remotePath, cancellationToken).ConfigureAwait(false)) return false;
        if (await _backend.FileExistsAsync(remotePath, cancellationToken).ConfigureAwait(false))
            throw new IOException($"远程目录路径与现有文件冲突：{remotePath}");
        if (await _backend.CreateDirectoryAsync(remotePath, cancellationToken).ConfigureAwait(false)) return true;
        if (await _backend.DirectoryExistsAsync(remotePath, cancellationToken).ConfigureAwait(false)) return false;
        throw new IOException($"无法创建远程目录：{remotePath}");
    }

    private IAsyncFtpDirectoryBackend RequireDirectoryBackend()
        => _backend as IAsyncFtpDirectoryBackend ??
           throw new NotSupportedException("当前 FTP 后端未实现目录能力。");

    private static void RecordTransferStatus(FtpStatus status, FtpConflictPolicy policy, string path,
        DirectoryResultBuilder result)
    {
        switch (status)
        {
            case FtpStatus.Success:
                result.TransferredPaths.Add(path);
                break;
            case FtpStatus.Skipped:
                if (policy == FtpConflictPolicy.Error)
                    throw new IOException($"文件传输被后端跳过；目标可能已存在：{path}");
                result.SkippedPaths.Add(path);
                break;
            default:
                result.FailedPaths.Add(path);
                break;
        }
    }

    private static void HandleDirectoryFileCollision(string path, FtpConflictPolicy policy,
        DirectoryResultBuilder result)
    {
        if (policy == FtpConflictPolicy.Error) throw new IOException($"文件与目录路径冲突：{path}");
        if (policy == FtpConflictPolicy.Skip)
        {
            result.SkippedPaths.Add(path);
            return;
        }

        throw new IOException($"无法用文件冲突策略覆盖目录：{path}");
    }

    private static string GetSafeLocalChildPath(string root, string parent, string segment)
    {
        var candidate = Path.GetFullPath(Path.Combine(parent, segment));
        var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                         Path.DirectorySeparatorChar;
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!candidate.StartsWith(rootPrefix, comparison))
            throw new InvalidDataException($"远程目录项超出本地目标目录：{segment}");
        return candidate;
    }

    private static void ValidatePathSegment(string segment, string source)
    {
        var normalizedSegment = segment?.TrimEnd(' ', '.');
        if (string.IsNullOrWhiteSpace(segment) || string.IsNullOrEmpty(normalizedSegment) || segment == "." || segment == ".." ||
            normalizedSegment == "." || normalizedSegment == ".." ||
            segment.IndexOf('/') >= 0 || segment.IndexOf('\\') >= 0 || Path.IsPathRooted(segment))
        {
            throw new InvalidDataException($"目录项名称不是安全的单一路径片段：{source}");
        }

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            if (segment.IndexOf(invalidCharacter) >= 0)
                throw new InvalidDataException($"目录项名称包含本地路径不允许的字符：{source}");
        }
    }

    private static string CombineRemotePath(string parent, string segment)
    {
        var trimmed = parent.TrimEnd('/');
        return trimmed.Length == 0 ? "/" + segment : trimmed + "/" + segment;
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static bool IsExistingReparsePoint(string path)
    {
        try
        {
            return IsReparsePoint(path);
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }

    private static FtpRemoteExists ToRemoteExistsMode(FtpConflictPolicy policy)
    {
        switch (policy)
        {
            case FtpConflictPolicy.Overwrite:
                return FtpRemoteExists.Overwrite;
            case FtpConflictPolicy.Resume:
                return FtpRemoteExists.Resume;
            default:
                return FtpRemoteExists.Skip;
        }
    }

    private static FtpLocalExists ToLocalExistsMode(FtpConflictPolicy policy)
    {
        switch (policy)
        {
            case FtpConflictPolicy.Overwrite:
                return FtpLocalExists.Overwrite;
            case FtpConflictPolicy.Resume:
                return FtpLocalExists.Resume;
            default:
                return FtpLocalExists.Skip;
        }
    }

    private static void ValidateConflictPolicy(FtpConflictPolicy policy)
    {
        if (!Enum.IsDefined(typeof(FtpConflictPolicy), policy))
            throw new ArgumentOutOfRangeException(nameof(policy));
    }

    private sealed class DirectoryNode
    {
        internal DirectoryNode(string localPath, string remotePath, int depth = 0)
        {
            LocalPath = localPath;
            RemotePath = remotePath;
            Depth = depth;
        }

        internal string LocalPath { get; }
        internal string RemotePath { get; }
        /// <summary>
        /// 当前目录相对根目录的深度。
        /// </summary>
        internal int Depth { get; }
    }

    private sealed class DirectoryResultBuilder
    {
        internal readonly List<string> TransferredPaths = new List<string>();
        internal readonly List<string> SkippedPaths = new List<string>();
        internal readonly List<string> FailedPaths = new List<string>();
        internal int CreatedDirectories;

        internal FtpDirectoryTransferResult Build() => new(CreatedDirectories, TransferredPaths, SkippedPaths,
            FailedPaths);
    }
}
