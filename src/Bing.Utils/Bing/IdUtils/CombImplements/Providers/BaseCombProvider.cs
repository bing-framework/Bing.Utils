using System;
using Bing.IdUtils.CombImplements.Strategies;

namespace Bing.IdUtils.CombImplements.Providers
{
    /// <summary>
    /// 内部Guid提供程序委托
    /// </summary>
    internal delegate Guid InternalGuidProvider();

    /// <summary>
    /// 内部时间戳提供程序委托
    /// </summary>
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
        /// 初始化一个<see cref="BaseCombProvider"/>类型的实例
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
            InternalGuidProvider = customGuidProvider ?? DefaultGuidProvider;
        }

        /// <summary>
        /// 内部时间戳提供程序
        /// </summary>
        public InternalTimeStampProvider InternalTimeStampProvider { get; }

        /// <summary>
        /// 内部 Guid 提供程序
        /// </summary>
        public InternalGuidProvider InternalGuidProvider { get; }

        /// <summary>
        /// 默认的时间戳提供程序
        /// </summary>
        protected static DateTime DefaultTimeStampProvider() => DateTime.UtcNow;

        /// <summary>
        /// 默认的 Guid 提供程序
        /// </summary>
        protected static Guid DefaultGuidProvider() => Guid.NewGuid();

        /// <summary>
        /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个随机的 <see cref="Guid"/> 和当前的 UTC 时间戳组成。
        /// </summary>
        public Guid Create() => Create(InternalGuidProvider.Invoke(), InternalTimeStampProvider.Invoke());

        /// <summary>
        /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个指定的 <see cref="Guid"/> 和当前的 UTC 时间戳组成。
        /// </summary>
        /// <param name="value">值</param>
        public Guid Create(Guid value) => Create(value, InternalTimeStampProvider.Invoke());

        /// <summary>
        /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个随机的 <see cref="Guid"/> 和提供的时间戳组成。
        /// </summary>
        /// <param name="timestamp">时间戳</param>
        public Guid Create(DateTime timestamp) => Create(InternalGuidProvider.Invoke(), timestamp);

        /// <summary>
        /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个指定的 <see cref="Guid"/> 和提供的时间戳组成。
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="timestamp">时间戳</param>
        public abstract Guid Create(Guid value, DateTime timestamp);

        /// <summary>
        /// 从提供的 COMB GUID 中获取时间戳。
        /// </summary>
        /// <param name="value">COMB GUID</param>
        public abstract DateTime GetTimeStamp(Guid value);
    }
}