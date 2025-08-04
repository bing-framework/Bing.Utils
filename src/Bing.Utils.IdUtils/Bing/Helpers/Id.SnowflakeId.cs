using Bing.Extensions;
using Bing.IdUtils;

namespace Bing.Helpers;

// 标识生成器 - 雪花ID
public static partial class Id
{
    /// <summary>
    /// 默认雪花ID生成器实例（单例）
    /// </summary>
    private static readonly Lazy<ISnowflakeId> _defaultSnowflakeIdInstance = new(() => SnowflakeGenerator.Create(1), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// 当前雪花ID生成器实例
    /// </summary>
    private static volatile ISnowflakeId _snowflakeIdInstance;

    /// <summary>
    /// 同步锁，用于保护生成器实例的线程安全
    /// </summary>
    private static readonly object _snowflakeIdLock = new();

    /// <summary>
    /// 获取当前雪花ID生成器实例
    /// </summary>
    private static ISnowflakeId CurrentSnowflakeIdInstance
    {
        get
        {
            if (_snowflakeIdInstance == null)
            {
                lock (_snowflakeIdLock)
                {
                    _snowflakeIdInstance = _snowflakeIdInstance ?? _defaultSnowflakeIdInstance.Value;
                }
            }
            return _snowflakeIdInstance;
        }
    }

    /// <summary>
    /// 默认雪花ID生成器工厂，使用机器ID为1的Seata算法实现
    /// </summary>
    private static readonly Func<ISnowflakeId> _defaultSnowflakeIdGenerateFunc = () => SnowflakeGenerator.Create(1);

    /// <summary>
    /// 当前雪花ID生成器工厂函数
    /// </summary>
    private static Func<ISnowflakeId> _snowflakeIdGenerateFunc = _defaultSnowflakeIdGenerateFunc;

    /// <summary>
    /// 配置雪花ID生成器工厂函数
    /// </summary>
    /// <param name="generatorFactory">雪花ID生成器工厂函数，用于创建ISnowflakeId实例</param>
    /// <exception cref="ArgumentNullException">当generatorFactory为null时抛出</exception>
    /// <remarks>
    /// 此方法允许自定义雪花ID生成器的创建逻辑，支持不同的机器ID、数据中心ID配置。
    /// 配置后的生成器将影响后续所有CreateSnowflakeId()方法的调用。
    /// 注意：此方法是线程安全的，但配置时会替换当前的生成器实例。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 使用Twitter算法配置雪花ID生成器
    /// Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(workerId: 1, dataCenterId: 1));
    /// 
    /// // 使用Seata算法配置雪花ID生成器
    /// Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(workerId: 2));
    /// </code>
    /// </example>
    public static void ConfigureSnowflakeId(Func<ISnowflakeId> generatorFactory)
    {
        if (generatorFactory == null)
            throw new ArgumentNullException(nameof(generatorFactory));
        lock (_snowflakeIdLock)
            _snowflakeIdInstance = generatorFactory();
    }

    /// <summary>
    /// 重置雪花ID生成器为默认配置
    /// </summary>
    /// <remarks>
    /// 将雪花ID生成器重置为默认的Seata算法实现，机器ID为1。
    /// 调用此方法后，后续CreateSnowflakeId()将使用默认配置生成ID。
    /// </remarks>
    public static void ResetSnowflakeId()
    {
        lock (_snowflakeIdLock)
            _snowflakeIdInstance = _defaultSnowflakeIdInstance.Value;
    }

    /// <summary>
    /// 创建雪花ID
    /// </summary>
    /// <returns>64位长整型雪花ID</returns>
    /// <remarks>
    /// <para>生成规则：</para>
    /// <para>1. 如果当前上下文设置了ID值（通过SetId方法），则将该值转换为长整型返回</para>
    /// <para>2. 否则使用配置的雪花ID生成器创建新的唯一ID</para>
    /// <para>雪花ID具有以下特点：</para>
    /// <para>- 全局唯一性：在分布式环境中确保ID不重复</para>
    /// <para>- 递增趋势：生成的ID具有时间序列特性</para>
    /// <para>- 高性能：单机每毫秒可生成数千个ID</para>
    /// <para>- 64位长度：适合数据库主键等场景</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 直接生成雪花ID
    /// long snowflakeId = Id.CreateSnowflakeId();
    /// 
    /// // 设置上下文ID后生成
    /// Id.SetId("123456789");
    /// long contextId = Id.CreateSnowflakeId(); // 返回123456789
    /// 
    /// Id.Reset();
    /// long newSnowflakeId = Id.CreateSnowflakeId(); // 返回新生成的雪花ID
    /// </code>
    /// </example>
    public static long CreateSnowflakeId()
    {
        if (!string.IsNullOrWhiteSpace(_id.Value))
            return _id.Value.ToLong();
        // 使用单例实例生成ID，无需加锁因为实现内部已经线程安全
        return CurrentSnowflakeIdInstance.NextId();
    }

    /// <summary>
    /// 批量创建雪花ID
    /// </summary>
    /// <param name="count">需要生成的ID数量，范围：1-100000</param>
    /// <returns>雪花ID数组</returns>
    /// <exception cref="ArgumentOutOfRangeException">当count小于1或大于100000时抛出</exception>
    /// <remarks>
    /// <para>此方法用于批量生成雪花ID，比多次调用CreateSnowflakeId()更高效。</para>
    /// <para>注意：如果当前上下文设置了ID值，此方法将忽略该值，始终生成新的雪花ID。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 批量生成100个雪花ID
    /// long[] ids = Id.CreateSnowflakeIds(100);
    /// 
    /// // 处理生成的ID
    /// foreach (long id in ids)
    /// {
    ///     Console.WriteLine($"Generated ID: {id}");
    /// }
    /// </code>
    /// </example>
    public static long[] CreateSnowflakeIds(uint count)
    {
        if (count < 1 || count > 100000)
            throw new ArgumentOutOfRangeException(nameof(count), count, "生成数量必须在1到100000之间");
        return CurrentSnowflakeIdInstance.NextIds(count);
    }

    #region 兼容性方法（保持向后兼容）

    /// <summary>
    /// 配置雪花ID生成函数
    /// </summary>
    /// <param name="provider">雪花ID提供程序</param>
    /// <exception cref="ArgumentNullException">当provider为null时抛出</exception>
    /// <remarks>此方法已过时，建议使用ConfigureSnowflakeId方法</remarks>
    [Obsolete("请使用 ConfigureSnowflakeId 方法替代此方法", false)]
    public static void Configure(Func<ISnowflakeId> provider) => ConfigureSnowflakeId(provider);

    #endregion
}