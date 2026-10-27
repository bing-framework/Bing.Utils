using System.IO.Compression;
using System.Text;
using System.Threading;
using Bing.IO;

namespace Bing.Utils.Tests.Bing.IO;

/// <summary>验证有界缓存探测、临时文件生命周期及异步源读取契约。</summary>
[Trait("Bing.IO", "FileContentDetectionBuffered")]
public class FileContentDetectionBufferedTests
{
    private const string ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string RelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string OfficeDocumentRelationship = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

    /// <summary>强制溢写后提供只读、可定位的完整缓存，并在释放时删除临时文件。</summary>
    [Fact]
    public void ProbeBuffered_SpillsToTemporaryStoreAndReturnsReadOnlySeekableContent()
    {
        var content = Encoding.UTF8.GetBytes(new string('x', 256));
        using var fixture = new TemporaryStoreFixture(1024);
        using var source = new InstrumentedNonSeekableStream(content, chunkSize: 32);
        var buffer = FileContentDetector.ProbeBuffered(source, fixture.Store, 1024,
            new FileDetectionOptions { InspectContainers = false }, memoryThresholdBytes: 8);

        Assert.Equal(1, fixture.Store.GetStatistics().FileCount);
        Assert.True(buffer.Content.CanRead);
        Assert.True(buffer.Content.CanSeek);
        Assert.False(buffer.Content.CanWrite);
        Assert.Equal(0, buffer.Content.Position);
        Assert.Throws<NotSupportedException>(() => buffer.Content.WriteByte(1));
        Assert.Equal(content, ReadAll(buffer.Content));
        Assert.Equal(content.Length, buffer.Content.Seek(0, SeekOrigin.End));
        Assert.False(source.IsDisposed);

        buffer.Dispose();

        Assert.False(buffer.Content.CanRead);
        Assert.Equal(0, fixture.Store.GetStatistics().FileCount);
    }

    /// <summary>默认选项对不可定位 DOCX 执行深层识别，且缓存可完整回读。</summary>
    [Fact]
    public void ProbeBuffered_DefaultsToContainerInspectionForNonSeekableDocx()
    {
        var package = CreateDocxPackage();
        using var fixture = new TemporaryStoreFixture(1024 * 1024);
        using var source = new InstrumentedNonSeekableStream(package, chunkSize: 97);

        using (var buffer = FileContentDetector.ProbeBuffered(source, fixture.Store, 1024 * 1024,
                   memoryThresholdBytes: 32))
        {
            Assert.Equal(FileDetectionStatus.Matched, buffer.Result.Status);
            Assert.Equal("docx", buffer.Result.Format.Id);
            Assert.Equal("zip", buffer.Result.ContainerFormat.Id);
            Assert.Equal(FileDetectionEvidence.ContainerStructure, buffer.Result.Evidence);
            Assert.True(buffer.Result.ContainerBytesRead > 0);
            Assert.True(buffer.Content.CanSeek);
            Assert.Equal(0, buffer.Content.Position);
            Assert.Equal(package, ReadAll(buffer.Content));
        }

        Assert.False(source.IsDisposed);
        Assert.Equal(0, fixture.Store.GetStatistics().FileCount);
    }

    /// <summary>从源流当前位置缓存至 EOF，不包含当前位置之前的数据。</summary>
    [Fact]
    public void ProbeBuffered_StartsAtCurrentSourcePosition()
    {
        var content = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50 };
        var expected = content.Skip(2).ToArray();
        using var fixture = new TemporaryStoreFixture(expected.Length);
        using var source = new MemoryStream(content, writable: false);
        source.Position = 2;

        using var buffer = FileContentDetector.ProbeBuffered(source, fixture.Store, expected.Length,
            new FileDetectionOptions { InspectContainers = false });

        Assert.Equal(content.Length, source.Position);
        Assert.Equal(expected, ReadAll(buffer.Content));
    }

    /// <summary>leaveOpen=false 在成功返回前关闭源，但缓存内容由结果对象继续持有。</summary>
    [Fact]
    public void ProbeBuffered_ClosesOwnedSourceAfterSuccessfulCaching()
    {
        var content = Encoding.UTF8.GetBytes(new string('s', 128));
        using var fixture = new TemporaryStoreFixture(512);
        var source = new InstrumentedNonSeekableStream(content, chunkSize: 32);

        using (var buffer = FileContentDetector.ProbeBuffered(source, fixture.Store, 512,
                   new FileDetectionOptions { InspectContainers = false }, memoryThresholdBytes: 8, leaveOpen: false))
        {
            Assert.True(source.IsDisposed);
            Assert.Equal(content, ReadAll(buffer.Content));
        }

        Assert.Equal(0, fixture.Store.GetStatistics().FileCount);
    }

    /// <summary>零字节空输入和恰好达到容量上限的输入均可成功缓存。</summary>
    [Fact]
    public void ProbeBuffered_AllowsEmptyAndExactMaximumLength()
    {
        using var emptyFixture = new TemporaryStoreFixture(0);
        using var emptySource = new InstrumentedNonSeekableStream(Array.Empty<byte>());
        using var empty = FileContentDetector.ProbeBuffered(emptySource, emptyFixture.Store, 0,
            new FileDetectionOptions { InspectContainers = false }, memoryThresholdBytes: 0);
        Assert.Equal(FileDetectionStatus.Empty, empty.Result.Status);
        Assert.Empty(ReadAll(empty.Content));

        var exactContent = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        using var exactFixture = new TemporaryStoreFixture(exactContent.Length);
        using var exactSource = new InstrumentedNonSeekableStream(exactContent, chunkSize: 2);
        using var exact = FileContentDetector.ProbeBuffered(exactSource, exactFixture.Store, exactContent.Length,
            new FileDetectionOptions { InspectContainers = false }, memoryThresholdBytes: 0);
        Assert.Equal(exactContent.Length, exact.Result.BytesRead);
        Assert.Equal(exactContent.Length, exactSource.BytesRead);
        Assert.Equal(exactContent, ReadAll(exact.Content));
    }

    /// <summary>超过容量时只探测一个额外字节，并清理此前已经溢写的缓存。</summary>
    [Fact]
    public void ProbeBuffered_OverMaximumReadsAtMostOneExtraByteAndCleansCache()
    {
        using var fixture = new TemporaryStoreFixture(4);
        using var source = new InstrumentedNonSeekableStream(new byte[20], chunkSize: 2);

        Assert.Throws<InvalidDataException>(() => FileContentDetector.ProbeBuffered(source, fixture.Store, 4,
            new FileDetectionOptions { InspectContainers = false }, memoryThresholdBytes: 0));

        Assert.Equal(5, source.BytesRead);
        Assert.Equal(0, fixture.Store.GetStatistics().FileCount);
        Assert.False(source.IsDisposed);
    }

    /// <summary>选项在读取前快照，后续修改调用方对象不改变本次识别模式。</summary>
    [Fact]
    public void ProbeBuffered_SnapshotsExplicitOptionsBeforeReadingSource()
    {
        var package = CreateDocxPackage();
        var options = new FileDetectionOptions { InspectContainers = false };
        using var fixture = new TemporaryStoreFixture(package.Length);
        using var source = new InstrumentedNonSeekableStream(package, chunkSize: 64,
            afterFirstRead: () => options.InspectContainers = true);

        using var buffer = FileContentDetector.ProbeBuffered(source, fixture.Store, package.Length,
            options, memoryThresholdBytes: 32);

        Assert.True(options.InspectContainers);
        Assert.Equal("zip", buffer.Result.Format.Id);
        Assert.Equal(FileDetectionEvidence.Signature, buffer.Result.Evidence);
        Assert.Equal(0, buffer.Result.ContainerBytesRead);
    }

    /// <summary>发生源读取异常时清理已溢写缓存，并按选项关闭源流。</summary>
    [Fact]
    public void ProbeBuffered_SourceIoFailureCleansCacheAndClosesOwnedSource()
    {
        using var fixture = new TemporaryStoreFixture(1024);
        using var source = new InstrumentedNonSeekableStream(new byte[512], chunkSize: 32, throwOnReadNumber: 2);

        Assert.Throws<IOException>(() => FileContentDetector.ProbeBuffered(source, fixture.Store, 1024,
            new FileDetectionOptions { InspectContainers = false }, memoryThresholdBytes: 8, leaveOpen: false));

        Assert.True(source.IsDisposed);
        Assert.Equal(0, fixture.Store.GetStatistics().FileCount);
    }

    /// <summary>读取期间取消会清理缓存并关闭已接管的源流。</summary>
    [Fact]
    public void ProbeBuffered_CancellationCleansCacheAndClosesOwnedSource()
    {
        using var fixture = new TemporaryStoreFixture(1024);
        using var cancellation = new CancellationTokenSource();
        using var source = new InstrumentedNonSeekableStream(new byte[512], chunkSize: 32,
            cancellationSource: cancellation, cancelOnReadNumber: 2);

        Assert.ThrowsAny<OperationCanceledException>(() => FileContentDetector.ProbeBuffered(source, fixture.Store, 1024,
            new FileDetectionOptions { InspectContainers = false }, memoryThresholdBytes: 8,
            leaveOpen: false, cancellationToken: cancellation.Token));

        Assert.True(source.IsDisposed);
        Assert.Equal(0, fixture.Store.GetStatistics().FileCount);
    }

    /// <summary>预验证失败或初始取消时不接管 leaveOpen=false 的源流。</summary>
    [Fact]
    public void ProbeBuffered_ValidationAndInitialCancellationDoNotTakeSourceOwnership()
    {
        using var fixture = new TemporaryStoreFixture(1024);
        using var source = new InstrumentedNonSeekableStream(new byte[] { 1, 2, 3 });
        Assert.Throws<ArgumentOutOfRangeException>(() => FileContentDetector.ProbeBuffered(source, fixture.Store, -1,
            leaveOpen: false));
        Assert.False(source.IsDisposed);
        Assert.Equal(0, source.BytesRead);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => FileContentDetector.ProbeBuffered(source, fixture.Store, 1024,
            leaveOpen: false, cancellationToken: cancellation.Token));
        Assert.False(source.IsDisposed);
        Assert.Equal(0, source.BytesRead);
    }

    /// <summary>源释放抛错时仍释放已经构造的内容缓存和临时文件租约。</summary>
    [Fact]
    public void ProbeBuffered_SourceDisposeFailureDoesNotLeakCompletedCache()
    {
        using var fixture = new TemporaryStoreFixture(1024);
        var source = new InstrumentedNonSeekableStream(new byte[256], chunkSize: 32, throwOnDispose: true);

        Assert.Throws<IOException>(() => FileContentDetector.ProbeBuffered(source, fixture.Store, 1024,
            new FileDetectionOptions { InspectContainers = false }, memoryThresholdBytes: 8, leaveOpen: false));

        Assert.True(source.IsDisposed);
        Assert.Equal(0, fixture.Store.GetStatistics().FileCount);
    }

    /// <summary>异步入口只对源调用异步读取，并支持不可定位 DOCX 的深层识别与回放。</summary>
    [Fact]
    public async Task ProbeBufferedAsync_UsesOnlyAsyncSourceReadsAndReplaysDeepDetectedDocx()
    {
        var package = CreateDocxPackage();
        using var fixture = new TemporaryStoreFixture(1024 * 1024);
        using var source = new InstrumentedNonSeekableStream(package, chunkSize: 73, rejectSynchronousReads: true);

        await using (var buffer = await FileContentDetector.ProbeBufferedAsync(source, fixture.Store, 1024 * 1024,
                           memoryThresholdBytes: 32))
        {
            Assert.Equal(FileDetectionStatus.Matched, buffer.Result.Status);
            Assert.Equal("docx", buffer.Result.Format.Id);
            Assert.Equal(FileDetectionEvidence.ContainerStructure, buffer.Result.Evidence);
            Assert.Equal(0, buffer.Content.Position);
            Assert.Equal(package, await ReadAllAsync(buffer.Content));
            Assert.True(source.AsyncReadCalls > 0);
            Assert.Equal(0, source.SyncReadCalls);
            Assert.Equal(1, fixture.Store.GetStatistics().FileCount);
        }

        Assert.False(source.IsDisposed);
        Assert.Equal(0, fixture.Store.GetStatistics().FileCount);
    }

    /// <summary>异步读取中取消会异步清理已溢写缓存并释放接管的源。</summary>
    [Fact]
    public async Task ProbeBufferedAsync_CancellationCleansCacheAndClosesOwnedSource()
    {
        using var fixture = new TemporaryStoreFixture(1024);
        using var cancellation = new CancellationTokenSource();
        using var source = new InstrumentedNonSeekableStream(new byte[512], chunkSize: 32,
            cancellationSource: cancellation, cancelOnReadNumber: 2, rejectSynchronousReads: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileContentDetector.ProbeBufferedAsync(source,
            fixture.Store, 1024, new FileDetectionOptions { InspectContainers = false }, memoryThresholdBytes: 8,
            leaveOpen: false, cancellationToken: cancellation.Token));

        Assert.True(source.IsDisposed);
        Assert.Equal(0, fixture.Store.GetStatistics().FileCount);
        Assert.Equal(0, source.SyncReadCalls);
    }

    private static byte[] CreateDocxPackage()
    {
        using var destination = new MemoryStream();
        using (var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml",
                "<Types xmlns=\"" + ContentTypesNamespace + "\"><Override PartName=\"/word/document.xml\" " +
                "ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
            WriteEntry(archive, "_rels/.rels",
                "<Relationships xmlns=\"" + RelationshipsNamespace + "\"><Relationship Id=\"rId1\" Type=\"" +
                OfficeDocumentRelationship + "\" Target=\"word/document.xml\"/></Relationships>");
            WriteEntry(archive, "word/document.xml",
                "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"/>");
        }
        return destination.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var output = entry.Open();
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
        output.Write(bytes, 0, bytes.Length);
    }

    private static byte[] ReadAll(Stream source)
    {
        using var destination = new MemoryStream();
        var buffer = new byte[4096];
        int count;
        while ((count = source.Read(buffer, 0, buffer.Length)) > 0)
            destination.Write(buffer, 0, count);
        return destination.ToArray();
    }

    private static async Task<byte[]> ReadAllAsync(Stream source)
    {
        using var destination = new MemoryStream();
        var buffer = new byte[4096];
        int count;
        while ((count = await source.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None).ConfigureAwait(false)) > 0)
            await destination.WriteAsync(buffer, 0, count, CancellationToken.None).ConfigureAwait(false);
        return destination.ToArray();
    }

    private sealed class TemporaryStoreFixture : IDisposable
    {
        internal string Root { get; }
        internal TemporaryFileStore Store { get; }

        internal TemporaryStoreFixture(long quota)
        {
            Root = Path.Combine(Path.GetTempPath(), "bing-file-content-buffer-" + Guid.NewGuid().ToString("N"));
            Store = new TemporaryFileStore(Root, quota);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        }
    }

    private sealed class InstrumentedNonSeekableStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly int _chunkSize;
        private readonly int _throwOnReadNumber;
        private readonly int _cancelOnReadNumber;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly bool _rejectSynchronousReads;
        private readonly bool _throwOnDispose;
        private Action _afterFirstRead;
        private int _readNumber;

        internal int BytesRead { get; private set; }
        internal int SyncReadCalls { get; private set; }
        internal int AsyncReadCalls { get; private set; }
        internal bool IsDisposed { get; private set; }

        internal InstrumentedNonSeekableStream(byte[] content, int chunkSize = int.MaxValue,
            int throwOnReadNumber = 0, CancellationTokenSource cancellationSource = null,
            int cancelOnReadNumber = 0, Action afterFirstRead = null,
            bool rejectSynchronousReads = false, bool throwOnDispose = false)
        {
            _inner = new MemoryStream(content ?? throw new ArgumentNullException(nameof(content)), writable: false);
            _chunkSize = chunkSize;
            _throwOnReadNumber = throwOnReadNumber;
            _cancellationSource = cancellationSource;
            _cancelOnReadNumber = cancelOnReadNumber;
            _afterFirstRead = afterFirstRead;
            _rejectSynchronousReads = rejectSynchronousReads;
            _throwOnDispose = throwOnDispose;
        }

        public override bool CanRead => !IsDisposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            SyncReadCalls++;
            if (_rejectSynchronousReads) throw new InvalidOperationException("Synchronous source read was used.");
            return ReadCore(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            AsyncReadCalls++;
            _readNumber++;
            ThrowOnConfiguredRead();
            var read = await _inner.ReadAsync(buffer, offset, Math.Min(count, _chunkSize), cancellationToken).ConfigureAwait(false);
            BytesRead += read;
            AfterConfiguredRead();
            return read;
        }

        private int ReadCore(byte[] buffer, int offset, int count)
        {
            _readNumber++;
            ThrowOnConfiguredRead();
            var read = _inner.Read(buffer, offset, Math.Min(count, _chunkSize));
            BytesRead += read;
            AfterConfiguredRead();
            return read;
        }

        private void ThrowOnConfiguredRead()
        {
            if (_throwOnReadNumber > 0 && _readNumber == _throwOnReadNumber)
                throw new IOException("Injected source read failure.");
        }

        private void AfterConfiguredRead()
        {
            if (_readNumber == _cancelOnReadNumber) _cancellationSource.Cancel();
            if (_readNumber == 1)
            {
                var callback = _afterFirstRead;
                _afterFirstRead = null;
                callback?.Invoke();
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                IsDisposed = true;
                _inner.Dispose();
                if (_throwOnDispose) throw new IOException("Injected source dispose failure.");
            }
            base.Dispose(disposing);
        }
    }
}
