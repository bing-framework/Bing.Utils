using System;

namespace Bing.IdUtils.CombImplements.Strategies
{
    /// <summary>
    /// 基于 Unix 的 COMB 日期时间策略
    /// </summary>
    internal class UnixDateTimeStrategy : ICombDateTimeStrategy
    {
        /// <summary>
        /// 每毫秒时钟数
        /// </summary>
        private const long TICKS_PER_MILLISECOND = 10_000;

        /// <summary>
        /// 日期字节数
        /// </summary>
        public int NumDateBytes => 6;

        /// <summary>
        /// 最小日期时间
        /// </summary>
        public DateTime MinDateTimeValue { get; } = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 最大日期时间
        /// </summary>
        public DateTime MaxDateTimeValue => MinDateTimeValue.AddMilliseconds(2 ^ (8 * NumDateBytes));

        /// <summary>
        /// 将 <see cref="DateTime"/> 转换为 byte[]
        /// </summary>
        /// <param name="timestamp">时间戳</param>
        public byte[] DateTimeToBytes(DateTime timestamp)
        {
            var ms = ToUnixTimeMilliseconds(timestamp);
            var msBytes = BitConverter.GetBytes(ms);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(msBytes);

            var ret = new byte[NumDateBytes];
            var index = msBytes.GetUpperBound(0) + 1 - NumDateBytes;

            Array.Copy(msBytes, index, ret, 0, NumDateBytes);

            return ret;
        }

        /// <summary>
        /// 将 byte[] 转换为 <see cref="DateTime"/>
        /// </summary>
        /// <param name="value">字节数组</param>
        public DateTime BytesToDateTime(byte[] value)
        {
            var msBytes = new byte[8];
            var index = 8 - NumDateBytes;

            Array.Copy(value, 0, msBytes, index, NumDateBytes);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(msBytes);

            var ms = BitConverter.ToInt64(msBytes, 0);

            return FromUnixTimeMilliseconds(ms);
        }

        /// <summary>
        /// 将 <see cref="DateTime"/> 转换为 Unix时间戳
        /// </summary>
        /// <param name="timestamp">日期时间</param>
        private long ToUnixTimeMilliseconds(DateTime timestamp) => (timestamp.Ticks - MinDateTimeValue.Ticks) / TICKS_PER_MILLISECOND;

        /// <summary>
        /// 从 Unix时间戳 转换为 <see cref="DateTime"/>
        /// </summary>
        /// <param name="ms">Unix时间戳</param>
        private DateTime FromUnixTimeMilliseconds(long ms) => MinDateTimeValue.AddTicks(ms * TICKS_PER_MILLISECOND);
    }
}