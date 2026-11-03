using Bing.Extensions;
using Bing.IdUtils;

namespace Bing.Helpers;

// 标识生成器 - 雪花ID
public static partial class Id
{
    /// <summary>
    /// 当前雪花ID生成器实例
    /// </summary>
    private static volatile ISnowflakeId _snowflakeIdInstance;

    /// <summary>
    /// 是否已经成功发号。成功发号后禁止替换生成器，避免丢失序列状态。
    /// </summary>
    private static bool _snowflakeIdStarted;

    /// <summary>
    /// 当前正在执行的发号调用数量。
    /// </summary>
    private static int _snowflakeIdGenerationInProgress;

    /// <summary>
    /// 配置变更版本，防止锁外创建的旧候选覆盖更新后的配置。
    /// </summary>
    private static long _snowflakeIdConfigurationVersion;

    /// <summary>
    /// 同步锁，用于保护生成器实例的线程安全
    /// </summary>
    private static readonly object _snowflakeIdLock = new();

    /// <summary>
    /// 获取当前雪花ID生成器实例，并登记一次待提交的发号调用。
    /// </summary>
    private static ISnowflakeId BeginSnowflakeIdGeneration()
    {
        lock (_snowflakeIdLock)
        {
            if (_snowflakeIdInstance == null)
                throw new InvalidOperationException("雪花ID生成器尚未配置，请在首次发号前调用 ConfigureSnowflakeId。");

            _snowflakeIdGenerationInProgress++;
            return _snowflakeIdInstance;
        }
    }

    /// <summary>
    /// 提交或放弃一次发号调用。
    /// </summary>
    /// <param name="succeeded">调用是否成功返回生成结果。</param>
    private static void CompleteSnowflakeIdGeneration(bool succeeded)
    {
        lock (_snowflakeIdLock)
        {
            _snowflakeIdGenerationInProgress--;
            if (succeeded)
                _snowflakeIdStarted = true;
        }
    }

    /// <summary>
    /// 配置雪花ID生成器工厂函数
    /// </summary>
    /// <param name="generatorFactory">雪花ID生成器工厂函数，用于创建ISnowflakeId实例</param>
    /// <exception cref="ArgumentNullException">当generatorFactory为null时抛出</exception>
    /// <exception cref="InvalidOperationException">工厂返回 null、已有成功发号、仍有发号调用，或工厂执行期间配置发生变化时抛出。</exception>
    /// <remarks>
    /// 此方法允许自定义雪花ID生成器的创建逻辑，支持不同的机器ID、数据中心ID配置。
    /// 配置后的生成器影响后续所有 CreateSnowflakeId() 方法的调用。
    /// 必须在首次成功发号前配置，成功发号后不能替换实例；正在发号时也不能修改配置。
    /// 如果所有发号调用均失败且从未成功返回结果，可以重新配置。
    /// 工厂在配置锁外执行；如果执行期间已有成功发号、仍有在途调用或配置被修改，则拒绝发布该候选。
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

        long configurationVersion;
        lock (_snowflakeIdLock)
        {
            if (_snowflakeIdStarted || _snowflakeIdGenerationInProgress > 0)
                throw new InvalidOperationException("雪花ID生成器已经成功发号或正在发号，不能重新配置。");
            configurationVersion = _snowflakeIdConfigurationVersion;
        }

        // 用户工厂可能等待其他线程调用 Id，不能在持有配置锁时执行。
        var generator = generatorFactory();
        if (generator == null)
            throw new InvalidOperationException("雪花ID生成器工厂不能返回 null。");

        lock (_snowflakeIdLock)
        {
            if (_snowflakeIdStarted || _snowflakeIdGenerationInProgress > 0)
                throw new InvalidOperationException("雪花ID生成器已经成功发号或正在发号，不能重新配置。");
            if (configurationVersion != _snowflakeIdConfigurationVersion)
                throw new InvalidOperationException("工厂执行期间雪花ID配置已发生变化，不能覆盖当前配置。");

            _snowflakeIdInstance = generator;
            _snowflakeIdConfigurationVersion++;
        }
    }

    /// <summary>
    /// 重置尚未成功发号且没有正在发号调用的雪花ID生成器配置。
    /// </summary>
    /// <remarks>
    /// 该方法只能在首次成功发号前且没有正在发号调用时调用，不能绕过生命周期约束。
    /// </remarks>
    /// <exception cref="InvalidOperationException">已有成功发号或仍有发号调用在执行时抛出。</exception>
    public static void ResetSnowflakeId()
    {
        lock (_snowflakeIdLock)
        {
            if (_snowflakeIdStarted || _snowflakeIdGenerationInProgress > 0)
                throw new InvalidOperationException("雪花ID生成器已经成功发号或正在发号，不能重置。");

            _snowflakeIdInstance = null;
            _snowflakeIdConfigurationVersion++;
        }
    }

    /// <summary>
    /// 为测试程序集清理静态雪花配置状态。
    /// </summary>
    /// <remarks>该方法不是生产调用契约，仅用于隔离共享静态状态的单元测试。</remarks>
    internal static void ResetSnowflakeIdForTests()
    {
        lock (_snowflakeIdLock)
        {
            if (_snowflakeIdGenerationInProgress > 0)
                throw new InvalidOperationException("必须等待发号调用全部退出后才能清理测试状态。");
            _snowflakeIdInstance = null;
            _snowflakeIdGenerationInProgress = 0;
            _snowflakeIdStarted = false;
            _snowflakeIdConfigurationVersion++;
        }
    }

    /// <summary>
    /// 创建雪花ID
    /// </summary>
    /// <returns>64位长整型雪花ID</returns>
    /// <exception cref="InvalidOperationException">未配置生成器且当前上下文未设置 ID 时抛出。</exception>
    /// <remarks>
    /// <para>生成规则：</para>
    /// <para>1. 如果当前上下文设置了ID值（通过SetId方法），则将该值转换为长整型返回</para>
    /// <para>2. 否则使用配置的雪花ID生成器创建新的唯一ID</para>
    /// <para>雪花ID具有以下特点：</para>
    /// <para>- 唯一性：依赖节点 ID 独占、实例状态连续以及安全的重启和时钟策略</para>
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
        var generator = BeginSnowflakeIdGeneration();
        var succeeded = false;
        try
        {
            var id = generator.NextId();
            succeeded = true;
            return id;
        }
        finally
        {
            CompleteSnowflakeIdGeneration(succeeded);
        }
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
    /// <para>只有整批成功返回才冻结配置；异常时不返回部分结果，也不回滚 provider 已消耗的序号。</para>
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
        var generator = BeginSnowflakeIdGeneration();
        var succeeded = false;
        try
        {
            var ids = generator.NextIds(count);
            succeeded = true;
            return ids;
        }
        finally
        {
            CompleteSnowflakeIdGeneration(succeeded);
        }
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
