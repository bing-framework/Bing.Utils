using System.Diagnostics;
using System.Threading;

namespace Bing.IO;

/// <summary>
/// 验证文件写入重载、流式写入及资源归属契约。
/// </summary>
[Trait("Category", "Regression")]
public sealed class FileWriteCapabilityRegressionTest
{
    /// <summary>
    /// 隔离调用可能因旧实现而导致进程栈溢出的保存重载。
    /// </summary>
    private const string ChildDirectoryVariable = "BING_UTILS_FILEWRITE_CHILD_DIRECTORY";

    /// <summary>
    /// 验证全部保存重载在独立测试进程中写入预期内容。
    /// </summary>
    [Fact]
    public void SaveFile_Overloads_WriteExpectedContent_Isolated()
    {
        var childDirectory = Environment.GetEnvironmentVariable(ChildDirectoryVariable);
        if (!string.IsNullOrWhiteSpace(childDirectory))
        {
            WriteWithSaveFileOverloads(childDirectory);
            return;
        }

        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"bing-savefile-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("vstest");
            startInfo.ArgumentList.Add(typeof(FileWriteCapabilityRegressionTest).Assembly.Location);
            startInfo.ArgumentList.Add($"--TestCaseFilter:FullyQualifiedName={typeof(FileWriteCapabilityRegressionTest).FullName}.{nameof(SaveFile_Overloads_WriteExpectedContent_Isolated)}");
            startInfo.EnvironmentVariables[ChildDirectoryVariable] = temporaryDirectory;

            using var process = new Process { StartInfo = startInfo };
            process.Start().ShouldBeTrue();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(60000))
            {
                process.Kill();
                process.WaitForExit();
                throw new TimeoutException("隔离的 SaveFile 回归进程未在 60 秒内结束。");
            }

            var output = outputTask.GetAwaiter().GetResult();
            var error = errorTask.GetAwaiter().GetResult();
            Assert.True(process.ExitCode == 0, $"隔离的 SaveFile 回归进程失败。{Environment.NewLine}{output}{Environment.NewLine}{error}");
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
                Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    /// <summary>
    /// 验证同步字节写入能够创建文件、截断旧内容并保留追加语义。
    /// </summary>
    [Fact]
    public void WriteByteArray_OverwriteAppendAndEmptyContent_PreserveExpectedData()
    {
        var path = CreateTemporaryFilePath();
        try
        {
            FileHelper.Write(new byte[] { 1, 2, 3, 4 }, path, appendMode: false).ShouldBeTrue();
            File.ReadAllBytes(path).ShouldBe(new byte[] { 1, 2, 3, 4 });

            FileHelper.Write(new byte[] { 9, 8 }, path, appendMode: false).ShouldBeTrue();
            File.ReadAllBytes(path).ShouldBe(new byte[] { 9, 8 });

            FileHelper.Write(new byte[] { 7 }, path, appendMode: true).ShouldBeTrue();
            File.ReadAllBytes(path).ShouldBe(new byte[] { 9, 8, 7 });

            FileHelper.Write(Array.Empty<byte>(), path, appendMode: false).ShouldBeTrue();
            File.ReadAllBytes(path).ShouldBeEmpty();

            using var emptyStream = new MemoryStream();
            File.WriteAllBytes(path, new byte[] { 1 });
            FileHelper.Write(path, emptyStream);
            File.ReadAllBytes(path).ShouldBeEmpty();
        }
        finally
        {
            DeleteTemporaryFile(path);
        }
    }

    /// <summary>
    /// 验证同步流写入支持短读和不可定位流，并保留可定位源流的位置约定。
    /// </summary>
    [Fact]
    public void WriteStream_HandlesShortReadsAndPreservesSourceOwnershipAndPosition()
    {
        var path = CreateTemporaryFilePath();
        try
        {
            File.WriteAllBytes(path, new byte[] { 0, 0, 0, 0, 0, 0, 0 });
            using (var nonSeekable = new ShortReadStream(new byte[] { 1, 2, 3, 4, 5, 6 }, 2, seekable: false))
            {
                FileHelper.Write(path, nonSeekable);
                File.ReadAllBytes(path).ShouldBe(new byte[] { 1, 2, 3, 4, 5, 6 });
                nonSeekable.Disposed.ShouldBeFalse();
            }

            using var seekable = new ShortReadStream(new byte[] { 1, 2, 3, 4, 5, 6 }, 2, seekable: true);
            seekable.Position = 4;
            FileHelper.Write(path, seekable);
            File.ReadAllBytes(path).ShouldBe(new byte[] { 1, 2, 3, 4, 5, 6 });
            seekable.Position.ShouldBe(seekable.Length);
            seekable.Disposed.ShouldBeFalse();
        }
        finally
        {
            DeleteTemporaryFile(path);
        }
    }

    /// <summary>
    /// 验证异步流写入支持短读、取消且不会关闭调用方流。
    /// </summary>
    [Fact]
    public async Task WriteStreamAsync_HandlesShortReadsAndCancellationWithoutClosingSource()
    {
        var path = CreateTemporaryFilePath();
        try
        {
            using (var nonSeekable = new ShortReadStream(new byte[] { 1, 2, 3, 4, 5, 6 }, 1, seekable: false))
            {
                await FileHelper.WriteAsync(path, nonSeekable);
                File.ReadAllBytes(path).ShouldBe(new byte[] { 1, 2, 3, 4, 5, 6 });
                nonSeekable.Disposed.ShouldBeFalse();
            }

            using var seekable = new ShortReadStream(new byte[] { 7, 8, 9, 10 }, 1, seekable: true);
            seekable.Position = 3;
            await FileHelper.WriteAsync(path, seekable);
            File.ReadAllBytes(path).ShouldBe(new byte[] { 7, 8, 9, 10 });
            seekable.Position.ShouldBe(seekable.Length);
            seekable.Disposed.ShouldBeFalse();

            using var cancellation = new CancellationTokenSource();
            using var cancellable = new ShortReadStream(new byte[] { 1, 2, 3, 4 }, 1, seekable: false, cancellation.Cancel);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileHelper.WriteAsync(path, cancellable, cancellation.Token));
            cancellable.Disposed.ShouldBeFalse();

            using var emptyStream = new MemoryStream();
            File.WriteAllBytes(path, new byte[] { 1 });
            await FileHelper.WriteAsync(path, emptyStream);
            File.ReadAllBytes(path).ShouldBeEmpty();
        }
        finally
        {
            DeleteTemporaryFile(path);
        }
    }

    /// <summary>
    /// 在隔离进程中验证各公开保存重载的编码及文件模式。
    /// </summary>
    /// <param name="directory">用于保存测试文件的临时目录。</param>
    private static void WriteWithSaveFileOverloads(string directory)
    {
        var defaultPath = Path.Combine(directory, "default.txt");
        FileHelper.SaveFile(defaultPath, "default").ShouldBeTrue();
        File.ReadAllText(defaultPath, Encoding.UTF8).ShouldBe("default");

        var encodingPath = Path.Combine(directory, "encoding.txt");
        FileHelper.SaveFile(encodingPath, "编码", Encoding.Unicode).ShouldBeTrue();
        File.ReadAllText(encodingPath, Encoding.Unicode).ShouldBe("编码");

        var modePath = Path.Combine(directory, "mode.txt");
        File.WriteAllText(modePath, "before-");
        FileHelper.SaveFile(modePath, "append", FileMode.Append).ShouldBeTrue();
        File.ReadAllText(modePath, Encoding.UTF8).ShouldBe("before-append");

        var encodingAndModePath = Path.Combine(directory, "encoding-mode.txt");
        File.WriteAllText(encodingAndModePath, "old tail");
        FileHelper.SaveFile(encodingAndModePath, "short", Encoding.UTF8, FileMode.Create).ShouldBeTrue();
        File.ReadAllText(encodingAndModePath, Encoding.UTF8).ShouldBe("short");
    }

    /// <summary>
    /// 生成唯一的临时文件路径。
    /// </summary>
    /// <returns>临时文件路径。</returns>
    private static string CreateTemporaryFilePath() => Path.Combine(Path.GetTempPath(), $"bing-filewrite-{Guid.NewGuid():N}.bin");

    /// <summary>
    /// 删除测试使用的临时文件。
    /// </summary>
    /// <param name="path">临时文件路径。</param>
    private static void DeleteTemporaryFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    /// <summary>
    /// 只返回有限长度数据并可选择拒绝定位的测试流。
    /// </summary>
    private sealed class ShortReadStream : Stream
    {
        /// <summary>
        /// 保存测试字节及当前位置的内存流。
        /// </summary>
        private readonly MemoryStream _inner;

        /// <summary>
        /// 每次读取允许返回的最大字节数。
        /// </summary>
        private readonly int _maximumReadSize;

        /// <summary>
        /// 指示测试流是否允许定位。
        /// </summary>
        private readonly bool _seekable;

        /// <summary>
        /// 每次读取后执行的可选回调。
        /// </summary>
        private readonly Action _afterRead;

        /// <summary>
        /// 获取测试流是否已释放。
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// 初始化短读测试流。
        /// </summary>
        /// <param name="content">测试流内容。</param>
        /// <param name="maximumReadSize">每次读取允许返回的最大字节数。</param>
        /// <param name="seekable">是否允许定位。</param>
        /// <param name="afterRead">每次读取后执行的可选回调。</param>
        public ShortReadStream(byte[] content, int maximumReadSize, bool seekable, Action afterRead = null)
        {
            _inner = new MemoryStream(content);
            _maximumReadSize = maximumReadSize;
            _seekable = seekable;
            _afterRead = afterRead;
        }

        /// <inheritdoc />
        public override bool CanRead => !Disposed;

        /// <inheritdoc />
        public override bool CanSeek => _seekable;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => _seekable ? _inner.Length : throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => _seekable ? _inner.Position : throw new NotSupportedException();
            set
            {
                if (!_seekable)
                    throw new NotSupportedException();
                _inner.Position = value;
            }
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, Math.Min(count, _maximumReadSize));
            _afterRead?.Invoke();
            return read;
        }

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read(buffer, offset, count));
        }

        /// <inheritdoc />
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = _inner.Read(buffer.Span.Slice(0, Math.Min(buffer.Length, _maximumReadSize)));
            _afterRead?.Invoke();
            return new ValueTask<int>(read);
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => _seekable ? _inner.Seek(offset, origin) : throw new NotSupportedException();

        /// <inheritdoc />
        public override void Flush() { }

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
