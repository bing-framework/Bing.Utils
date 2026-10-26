using System.Text;
using System.Threading;

namespace Bing.IO;

[Trait("Category", "PerformanceApiReview")]
public class FileMemoryReadRegressionTest
{
    [Fact]
    public async Task Cancellation_IsNotConvertedToNull_EvenForMissingFile()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        try
        {
            var missing = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileHelper.ReadToMemoryStreamAsync(path, cts.Token));
            Assert.Equal(cts.Token, missing.CancellationToken);
            File.WriteAllText(path, "content", Encoding.UTF8);
            var existing = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileHelper.ReadToMemoryStreamAsync(path, cts.Token));
            Assert.Equal(cts.Token, existing.CancellationToken);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task SuccessfulReads_PreserveEndPosition_AndMissingReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        try
        {
            Assert.Null(FileHelper.ReadToMemoryStream(path));
            Assert.Null(await FileHelper.ReadToMemoryStreamAsync(path));
            File.WriteAllText(path, "内容", Encoding.UTF8);
            var expected = File.ReadAllBytes(path);
            using var sync = FileHelper.ReadToMemoryStream(path);
            using var asyncResult = await FileHelper.ReadToMemoryStreamAsync(path);
            Assert.Equal(expected, sync.ToArray());
            Assert.Equal(expected, asyncResult.ToArray());
            Assert.Equal(sync.Length, sync.Position);
            Assert.Equal(asyncResult.Length, asyncResult.Position);
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); }
    }
}
