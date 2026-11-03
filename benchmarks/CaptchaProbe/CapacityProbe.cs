using Bing.Captcha;
using System.Diagnostics;
using System.Globalization;

internal static class CapacityProbe
{
    // 有限满容量探针：不绘图、不建立正式性能阈值，状态必须始终有界。
    public static async Task RunAsync()
    {
        const int capacity = 10000;
        const int writes = 100;
        foreach (var expireFirst in new[] { false, true })
        {
            var clock = new ProbeClock();
            using var store = new MemoryCaptchaStore(new MemoryCaptchaStoreOptions
            {
                Capacity = capacity,
                CleanupInterval = TimeSpan.FromDays(1)
            }, clock);
            var entry = new CaptchaEntry("entry", new byte[32], "probe", clock.UtcNow.AddMinutes(2), true);
            for (var i = 0; i < capacity; i++) await store.ReplaceAsync(Context(i), entry);
            if (expireFirst) clock.UtcNow = entry.ExpiresAt;
            var replacement = new CaptchaEntry("replacement", new byte[32], "probe", clock.UtcNow.AddMinutes(2), true);
            var elapsed = new double[writes];
            var allocated = GC.GetTotalAllocatedBytes(true);
            var total = Stopwatch.StartNew();
            for (var i = 0; i < writes; i++)
            {
                var start = Stopwatch.GetTimestamp();
                await store.ReplaceAsync(Context(capacity + i), replacement);
                elapsed[i] = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
                if (store.Count > capacity) throw new InvalidOperationException("Capacity exceeded.");
            }
            var totalMs = total.Elapsed.TotalMilliseconds;
            var allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocated;
            Array.Sort(elapsed);
            Console.WriteLine(FormattableString.Invariant($"capacity={capacity} initial_expired={expireFirst} writes={writes} requestedConcurrency=1 maxActualParallelism=1 maxQueuedRequests=0 total_ms={totalMs:F3} mean_ms={elapsed.Average():F3} p95_ms={elapsed[94]:F3} max_ms={elapsed[99]:F3} managed_allocated_bytes={allocatedBytes} remaining={store.Count}"));
        }
    }
    private static CaptchaContext Context(int id) => new("login", "capacity-probe", id.ToString(CultureInfo.InvariantCulture));
    private sealed class ProbeClock : ICaptchaClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    }
}
