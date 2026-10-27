using Bing.Http;
using Bing.Operations;

namespace Bing.Utils.Tests.Bing.Operations;

/// <summary>
/// 验证旧进度对象到统一操作进度的同步适配。
/// </summary>
public class ProgressAdaptersTest
{
    /// <summary>
    /// 字节进度适配保持写入阶段、未知总量和调用顺序。
    /// </summary>
    [Fact]
    public void FromBytesReportsInlineWriteProgress()
    {
        var events = new List<OperationProgress>();
        var progress = ProgressAdapters.FromBytes(new CallbackProgress<OperationProgress>(events.Add));

        progress.Report(4);
        progress.Report(9);

        Assert.Equal(new long[] { 0, 1 }, events.Select(item => item.Sequence));
        Assert.All(events, item =>
        {
            Assert.Equal(OperationStage.Write, item.Stage);
            Assert.Equal(ProgressUnit.Byte, item.Unit);
            Assert.Null(item.Total);
            Assert.False(item.IsFinal);
        });
        Assert.Equal(new long[] { 4, 9 }, events.Select(item => item.Completed));
        Assert.Null(ProgressAdapters.FromBytes(null));
    }

    /// <summary>
    /// HTTP 下载和上传适配保留总量、尝试号及正文完成标记。
    /// </summary>
    [Fact]
    public void HttpAdaptersPreserveMetadataAndMapCompletionToWriteStage()
    {
        var downloadEvents = new List<OperationProgress>();
        ProgressAdapters.FromHttpDownload(new CallbackProgress<OperationProgress>(downloadEvents.Add))
            .Report(new HttpDownloadProgress(8, 10, true));

        var uploadEvents = new List<OperationProgress>();
        var upload = ProgressAdapters.FromHttpUpload(new CallbackProgress<OperationProgress>(uploadEvents.Add));
        upload.Report(new HttpUploadProgress(3, 4, 10, false));
        upload.Report(new HttpUploadProgress(3, 10, 10, true));

        var download = Assert.Single(downloadEvents);
        Assert.Equal(OperationStage.Write, download.Stage);
        Assert.Equal(8, download.Completed);
        Assert.Equal(10, download.Total);
        Assert.Equal(1, download.Attempt);
        Assert.True(download.IsFinal);

        Assert.Equal(new long[] { 0, 1 }, uploadEvents.Select(item => item.Sequence));
        Assert.Equal(new[] { false, true }, uploadEvents.Select(item => item.IsFinal));
        Assert.All(uploadEvents, item => Assert.Equal(OperationStage.Write, item.Stage));
        Assert.Equal(new[] { 3, 3 }, uploadEvents.Select(item => item.Attempt));
        Assert.Equal(new long[] { 4, 10 }, uploadEvents.Select(item => item.Completed));
    }

    private sealed class CallbackProgress<T> : IProgress<T>
    {
        private readonly Action<T> _callback;

        internal CallbackProgress(Action<T> callback) => _callback = callback;

        public void Report(T value) => _callback(value);
    }
}
