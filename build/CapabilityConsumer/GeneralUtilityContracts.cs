using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bing.Date.Cron;
using Bing.Text.Diff;
using Bing.Threading;

namespace CapabilityConsumer;

/// <summary>
/// 在四个消费目标上编译通用工具公开契约。
/// </summary>
public static class GeneralUtilityContracts
{
    /// <summary>
    /// 验证按键互斥和共享执行的公开签名。
    /// </summary>
    /// <param name="token">调用方取消令牌。</param>
    /// <returns>共享工厂的结果。</returns>
    public static async Task<int> Concurrency(CancellationToken token)
    {
        var locks = new KeyedAsyncLock<string>(StringComparer.Ordinal);
        using (await locks.LockAsync("key", token)) { }
        using (var lease = await locks.TryLockAsync("key", TimeSpan.Zero, token)) { }
        var flight = new SingleFlight<string, int>((key, owner) => Task.FromResult(key.Length), lifetimeCancellationToken: token);
        return await flight.RunAsync("key", token);
    }

    /// <summary>
    /// 验证五段和六段 Cron、显式时区及有限枚举签名。
    /// </summary>
    /// <param name="from">起点。</param>
    /// <param name="to">终点。</param>
    /// <param name="token">枚举取消令牌。</param>
    /// <returns>最多十个触发时刻。</returns>
    public static IEnumerable<DateTimeOffset> Cron(DateTimeOffset from, DateTimeOffset to, CancellationToken token)
    {
        CronSchedule.TryParse("0 0 * * *", out var schedule, out var error);
        var seconds = CronSchedule.Parse("0 * * * * *", CronExpressionFormat.IncludeSeconds);
        DateTimeOffset? next = seconds.GetNextOccurrence(from, TimeZoneInfo.Utc, inclusive: true);
        return schedule.GetOccurrences(from, to, TimeZoneInfo.Utc, 10, token);
    }

    /// <summary>
    /// 验证可取消行比较及结构化差异块签名。
    /// </summary>
    /// <param name="token">计算取消令牌。</param>
    /// <returns>只读完整结果。</returns>
    public static TextDiffResult Diff(CancellationToken token)
    {
        var result = TextDiff.Compare("旧行", "新行", new TextDiffOptions
        {
            MaxLinesPerInput = 200_000,
            MaxCharsPerInput = 16_000_000,
            MaxDuration = TimeSpan.FromSeconds(30),
            IgnoreCase = true,
            IgnoreLeadingTrailingWhitespace = true
        }, token);
        TextDiffKind kind = result.Blocks[0].Kind;
        return result;
    }
}
