using System.Text.RegularExpressions;
using System.Threading;

namespace Bing.Text.RegularExpressions;

[Collection("RegexPool")]
[Trait("Category", "PerformanceApiReview")]
public class RegexPoolInitializationTest : IDisposable
{
    public RegexPoolInitializationTest() => RegexPool.Clear();
    public void Dispose() => RegexPool.Clear();

    [Fact]
    public async Task ConcurrentColdAccess_ConstructsOnce_AndCountsSuccessfulRequests()
    {
        using var start = new Barrier(16);
        var calls = 0;
        var tasks = Enumerable.Range(0, 16).Select(_ => Task.Factory.StartNew(() =>
        {
            Assert.True(start.SignalAndWait(TimeSpan.FromSeconds(10)));
            return RegexPool.GetOrCreateForTesting("cold", () => { Interlocked.Increment(ref calls); return new Regex("cold"); });
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
        var results = await Task.WhenAll(tasks);
        Assert.Equal(1, calls);
        Assert.All(results, value => Assert.Same(results[0], value));
        Assert.Equal(1, RegexPool.MissCount);
        Assert.Equal(15, RegexPool.HitCount);
    }

    [Fact]
    public void FailedInitialization_IsNotCachedOrCounted_AndCanRetry()
    {
        Assert.Throws<FormatException>(() => RegexPool.GetOrCreateForTesting("retry", () => throw new FormatException()));
        Assert.Equal(0, RegexPool.Count);
        Assert.Equal(0, RegexPool.HitCount + RegexPool.MissCount);
        var result = RegexPool.GetOrCreateForTesting("retry", () => new Regex("retry"));
        Assert.Same(result, RegexPool.GetOrCreate("retry", RegexOptions.None));
        Assert.Equal(1, RegexPool.HitCount);
        Assert.Equal(1, RegexPool.MissCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OldFailure_DoesNotRemoveReplacement(bool clear)
    {
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var old = Task.Run(() => RegexPool.GetOrCreateForTesting("race", () =>
        {
            started.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            throw new FormatException("old");
        }));
        Regex replacement;
        try
        {
            Assert.True(started.Wait(TimeSpan.FromSeconds(10)));
            if (clear) RegexPool.Clear(); else Assert.True(RegexPool.Remove("race", RegexOptions.None));
            replacement = RegexPool.GetOrCreateForTesting("race", () => new Regex("race"));
        }
        finally { release.Set(); }
        await Assert.ThrowsAsync<FormatException>(() => old);
        Assert.Same(replacement, RegexPool.GetOrCreate("race", RegexOptions.None));
    }

    [Fact]
    public async Task DifferentKeys_ConstructOutsideAdmissionLock()
    {
        using var both = new Barrier(2);
        Regex Construct(string key) => RegexPool.GetOrCreateForTesting(key, () =>
        {
            Assert.True(both.SignalAndWait(TimeSpan.FromSeconds(10)));
            return new Regex(key);
        });
        await Task.WhenAll(Task.Run(() => Construct("first")), Task.Run(() => Construct("second")));
        Assert.Equal(2, RegexPool.MissCount);
    }
}
