using System.Runtime.CompilerServices;

namespace Bing.Reflection;

/// <summary>实例化、方法及动态类型缓存的按请求诊断计数。</summary>
#if NET5_0_OR_GREATER
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
    "Trimming",
    "IL2091",
    Justification = "Lazy<T> 的链接器注解针对无参构造激活；此诊断层只保存工厂创建的委托或元数据，不会实例化 T。")]
#endif
internal static class ReflectionCacheMetrics
{
    internal static volatile bool Enabled;
    private static readonly ConditionalWeakTable<object, object> FailedEntries = new();
    private static readonly object FailureMarker = new();
    private static long _hits;
    private static long _misses;
    private static long _pending;
    private static long _bypasses;
    private static long _structuralBypasses;
    private static long _failures;

    /// <summary>获取方法结构签名 key 因长度或容量限制而旁路的次数。</summary>
    internal static long StructuralBypassCount => System.Threading.Interlocked.Read(ref _structuralBypasses);

    internal static (long Hits, long Misses, long Pending, long Bypasses, long Failures) Snapshot() =>
        (System.Threading.Interlocked.Read(ref _hits), System.Threading.Interlocked.Read(ref _misses),
            System.Threading.Interlocked.Read(ref _pending), System.Threading.Interlocked.Read(ref _bypasses),
            System.Threading.Interlocked.Read(ref _failures));

    internal static Lazy<T> CreateLazy<T>(Func<T> factory)
    {
        Lazy<T> entry = null;
        entry = new Lazy<T>(() =>
        {
            try
            {
                return factory();
            }
            catch
            {
                FailedEntries.GetValue(entry, _ => FailureMarker);
                throw;
            }
        }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        return entry;
    }

    internal static void Record<T>(Lazy<T> entry)
    {
        if (!Enabled)
            return;
        if (entry == null)
            RecordMiss();
        else if (FailedEntries.TryGetValue(entry, out _))
            System.Threading.Interlocked.Increment(ref _failures);
        else if (entry.IsValueCreated)
            RecordHit();
        else
            RecordPending();
    }

    internal static void RecordHit()
    {
        if (Enabled)
            System.Threading.Interlocked.Increment(ref _hits);
    }

    internal static void RecordMiss()
    {
        if (Enabled)
            System.Threading.Interlocked.Increment(ref _misses);
    }

    internal static void RecordPending()
    {
        if (Enabled)
            System.Threading.Interlocked.Increment(ref _pending);
    }

    internal static void RecordBypass()
    {
        if (Enabled)
            System.Threading.Interlocked.Increment(ref _bypasses);
    }

    internal static void RecordStructuralBypass()
    {
        if (Enabled)
            System.Threading.Interlocked.Increment(ref _structuralBypasses);
    }
}
