using System.Text;

namespace Bing.Helpers;

[Trait("Category", "PerformanceApiReview")]
public class ZipCommitRegressionTest : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "bing-commit-" + Guid.NewGuid());
    public ZipCommitRegressionTest() => Directory.CreateDirectory(_directory);
    public void Dispose() => Directory.Delete(_directory, true);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MoveOrReplace_ConsumesTemporaryFile(bool exists)
    {
        var temp = Path.Combine(_directory, "temp"); var destination = Path.Combine(_directory, "target");
        File.WriteAllText(temp, "new", Encoding.UTF8);
        if (exists) File.WriteAllText(destination, "old", Encoding.UTF8);
        Compression.CommitArchiveFile(temp, destination, true);
        Assert.Equal("new", File.ReadAllText(destination, Encoding.UTF8));
        Assert.False(File.Exists(temp));
    }

    [Fact]
    public void UnsupportedReplace_FallsBack_ButIoFailurePreservesOldTarget()
    {
        var temp = Path.Combine(_directory, "temp"); var destination = Path.Combine(_directory, "target");
        File.WriteAllText(temp, "new", Encoding.UTF8);
        File.WriteAllText(destination, "old", Encoding.UTF8);
        Assert.Throws<IOException>(() => Compression.CommitArchiveFile(temp, destination, true, (_, _) => throw new IOException("replace")));
        Assert.Equal("old", File.ReadAllText(destination, Encoding.UTF8));
        Assert.True(File.Exists(temp));
        Compression.CommitArchiveFile(temp, destination, true, (_, _) => throw new PlatformNotSupportedException());
        Assert.Equal("new", File.ReadAllText(destination, Encoding.UTF8));
    }

    [Fact]
    public void NoOverwrite_DoesNotDeleteTarget()
    {
        var temp = Path.Combine(_directory, "temp"); var destination = Path.Combine(_directory, "target");
        File.WriteAllText(temp, "new", Encoding.UTF8);
        File.WriteAllText(destination, "old", Encoding.UTF8);
        Assert.Throws<IOException>(() => Compression.CommitArchiveFile(temp, destination, false));
        Assert.Equal("old", File.ReadAllText(destination, Encoding.UTF8));
    }
}
