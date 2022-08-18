using System;
using Bing.IdUtils.CombImplements.Strategies;

namespace Bing.IdUtils.CombImplements.Providers
{
    /// <summary>
    /// MSSQL 类型的 COMB 提供程序
    /// </summary>
    internal class MsSqlCombProvider : BaseCombProvider
    {
        /// <summary>
        /// 嵌入索引
        /// </summary>
        private const int EMBED_AT_INDEX = 10;

        /// <summary>
        /// 初始化一个<see cref="MsSqlCombProvider"/>类型的实例
        /// </summary>
        /// <param name="strategy">日期时间策略</param>
        /// <param name="customTimeStampProvider">自定义时间戳提供程序</param>
        /// <param name="customGuidProvider">自定义Guid提供程序</param>
        public MsSqlCombProvider(ICombDateTimeStrategy strategy, InternalTimeStampProvider customTimeStampProvider = null, InternalGuidProvider customGuidProvider = null)
            : base(strategy, customTimeStampProvider, customGuidProvider)
        {
        }

        /// <summary>
        /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个指定的 <see cref="Guid"/> 和提供的时间戳组成。
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="timestamp">时间戳</param>
        public override Guid Create(Guid value, DateTime timestamp)
        {
            var gBytes = value.ToByteArray();
            var dBytes = _dateTimeStrategy.DateTimeToBytes(timestamp);
            Array.Copy(dBytes, 0, gBytes, EMBED_AT_INDEX, _dateTimeStrategy.NumDateBytes);
            return new Guid(gBytes);
        }

        /// <summary>
        /// 从提供的 COMB GUID 中获取时间戳。
        /// </summary>
        /// <param name="value">COMB GUID</param>
        public override DateTime GetTimeStamp(Guid value)
        {
            var gBytes = value.ToByteArray();
            var dBytes = new byte[_dateTimeStrategy.NumDateBytes];
            Array.Copy(gBytes, EMBED_AT_INDEX, dBytes, 0, _dateTimeStrategy.NumDateBytes);
            return _dateTimeStrategy.BytesToDateTime(dBytes);
        }
    }
}