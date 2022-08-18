using System;

namespace Bing.Date
{
    /// <summary>
    /// 不重复时间戳工厂
    /// </summary>
    public class NoRepeatTimeStampFactory
    {
        /// <summary>
        /// 最后值
        /// </summary>
        private DateTime _lastValue = DateTime.MinValue;

        /// <summary>
        /// 对象锁
        /// </summary>
        private readonly object _lockObj = new object();

        /// <summary>
        /// 毫秒增量
        /// </summary>
        public double IncrementMs { get; set; } = 4;

        /// <summary>
        /// 获取时间戳
        /// </summary>
        public DateTime GetTimeStamp() => GetTimeStampCore(DateTime.Now);

        /// <summary>
        /// 获取 UTC 时间戳
        /// </summary>
        public DateTime GetUtcTimeStamp() => GetTimeStampCore(DateTime.UtcNow);

        /// <summary>
        /// 获取时间戳对象
        /// </summary>
        public TimeStamp GetTimeStampObject() => new(GetTimeStamp());

        /// <summary>
        /// 获取 UTC 时间戳对象
        /// </summary>
        public TimeStamp GetUtcTimeStampObject() => new(GetUtcTimeStamp());

        /// <summary>
        /// 获取 Unix 时间戳对象
        /// </summary>
        public UnixTimeStamp GetUnixTimeStampObject() => new(GetTimeStamp());

        /// <summary>
        /// 获取 UTC Unix 时间戳对象
        /// </summary>
        public UnixTimeStamp GetUtcUnixTimeStampObject() => new(GetUtcTimeStamp());

        /// <summary>
        /// 获取时间戳的核心方法
        /// </summary>
        /// <param name="refDt">引用时间</param>
        private DateTime GetTimeStampCore(DateTime refDt)
        {
            var now = refDt;
            lock (_lockObj)
            {
                if ((now - _lastValue).TotalMilliseconds < IncrementMs)
                    now = _lastValue.AddMilliseconds(IncrementMs);
                _lastValue = now;
            }
            return now;
        }
    }
}
