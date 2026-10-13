using Bing.IdUtils.CombImplements.Strategies;
using Bing.IdUtils.GuidImplements;

namespace Bing.IdUtils.CombImplements.Providers;

/// <summary>
/// 内部Guid提供程序委托
/// </summary>
/// <returns>用于生成组合 ID 的 GUID 部分。</returns>
internal delegate Guid InternalGuidProvider();

/// <summary>
/// 内部时间戳提供程序委托
/// </summary>
/// <returns>用于生成组合 ID 的时间戳部分。</returns>
internal delegate DateTime InternalTimeStampProvider();

/// <summary>
/// COMB GUID 提供程序基类
/// </summary>
internal abstract class BaseCombProvider : ICombProvider
{
    /// <summary>
    /// 日期时间策略
    /// </summary>
    protected ICombDateTimeStrategy _dateTimeStrategy;

    /// <summary>
    /// 初始化一个 <see cref="BaseCombProvider"/> 类型的实例。
    /// </summary>
    /// <param name="strategy">日期时间策略</param>
    /// <param name="customTimeStampProvider">自定义时间戳提供程序</param>
    /// <param name="customGuidProvider">自定义Guid提供程序</param>
    protected BaseCombProvider(ICombDateTimeStrategy strategy,
        InternalTimeStampProvider customTimeStampProvider = null,
        InternalGuidProvider customGuidProvider = null)
    {
        if (strategy.NumDateBytes != 4 && strategy.NumDateBytes != 6)
            throw new NotSupportedException("ICombDateTimeStrategy is limited to either 4 or 6 bytes.");
        _dateTimeStrategy = strategy;
        InternalTimeStampProvider = customTimeStampProvider ?? DefaultTimeStampProvider;
        InternalGuidProvider = customGuidProvider ?? NoParamGuidImplementProxy.Basic;
    }

    /// <summary>
    /// 获取内部时间戳提供程序
    /// </summary>
    public InternalTimeStampProvider InternalTimeStampProvider { get; }

    /// <summary>
    /// 获取内部 Guid 提供程序
    /// </summary>
    public InternalGuidProvider InternalGuidProvider { get; }

    /// <summary>
    /// 默认的时间戳提供程序
    /// </summary>
    /// <returns>当前 UTC 时间。</returns>
    protected static DateTime DefaultTimeStampProvider() => DateTime.UtcNow;

    /// <summary>
    /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个随机的 <see cref="Guid"/> 和当前的 UTC 时间戳组成。
    /// </summary>
    /// <returns>生成的 COMB <see cref="Guid"/>。</returns>
    public Guid Create() => Create(InternalGuidProvider.Invoke(), InternalTimeStampProvider.Invoke());

    /// <summary>
    /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个指定的 <see cref="Guid"/> 和当前的 UTC 时间戳组成。
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>生成的 COMB <see cref="Guid"/>。</returns>
    public Guid Create(Guid value) => Create(value, InternalTimeStampProvider.Invoke());

    /// <summary>
    /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个随机的 <see cref="Guid"/> 和提供的时间戳组成。
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    /// <returns>生成的 COMB <see cref="Guid"/>。</returns>
    public Guid Create(DateTime timestamp) => Create(InternalGuidProvider.Invoke(), timestamp);

    /// <summary>
    /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个指定的 <see cref="Guid"/> 和提供的时间戳组成。
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="timestamp">时间戳</param>
    /// <returns>生成的 COMB <see cref="Guid"/>。</returns>
    public abstract Guid Create(Guid value, DateTime timestamp);

    /// <summary>
    /// 从提供的 COMB GUID 中获取时间戳。
    /// </summary>
    /// <param name="value">COMB GUID</param>
    /// <returns>从 COMB <see cref="Guid"/> 中解析出的时间戳。</returns>
    public abstract DateTime GetTimeStamp(Guid value);
}
