namespace Bing.IdUtils.SnowflakeImplements.Providers;

/// <summary>
/// 雪花ID 提供程序基类
/// </summary>
internal abstract class BaseSnowflakeProvider : ISnowflakeProvider
{
    /// <inheritdoc />
    public abstract long NextId();

    /// <inheritdoc />
    public long[] NextIds(uint size)
    {
        var ids = new long[size];
        for (var i = 0; i < size; i++)
            ids[i] = NextId();
        return ids;
    }

    /// <summary>
    /// 获取当前时间戳
    /// </summary>
    protected virtual long TimeGen() => TimeExtensions.CurrentTimeMills();

    /// <summary>
    /// 时间扩展
    /// </summary>
    private static class TimeExtensions
    {
        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private static Func<long> CurrentTimeFunc = InternalCurrentTimeMillis;

        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        public static long CurrentTimeMills() => CurrentTimeFunc();

        /// <summary>
        /// 重置当前时间戳
        /// </summary>
        /// <param name="func">函数</param>
        public static IDisposable StubCurrentTime(Func<long> func)
        {
            CurrentTimeFunc = func;
            return new DisposeAction(() => { CurrentTimeFunc = InternalCurrentTimeMillis; });
        }

        /// <summary>
        /// 重置当前时间戳
        /// </summary>
        /// <param name="millis">毫秒数</param>
        public static IDisposable StubCurrentTime(long millis)
        {
            CurrentTimeFunc = () => millis;
            return new DisposeAction(() => { CurrentTimeFunc = InternalCurrentTimeMillis; });
        }

        /// <summary>
        /// 1970年
        /// </summary>
        private static readonly DateTime Jan1St1970 = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 默认当前时间戳
        /// </summary>
        private static long InternalCurrentTimeMillis() => (long)(DateTime.UtcNow - Jan1St1970).TotalMilliseconds;
    }
}