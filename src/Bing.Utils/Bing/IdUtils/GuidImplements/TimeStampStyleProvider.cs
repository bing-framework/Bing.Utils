using System;
using System.Runtime.CompilerServices;

namespace Bing.IdUtils.GuidImplements
{
    /// <summary>
    /// 时间戳风格提供程序
    /// </summary>
    internal static class TimeStampStyleProvider
    {
        /// <summary>
        /// 创建一个 <see cref="Guid"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid Create() => Create(NoRepeatMode.Off);

        /// <summary>
        /// 创建一个 <see cref="Guid"/>
        /// </summary>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid Create(Guid value) => Create(value, NoRepeatMode.Off);

        /// <summary>
        /// 创建一个 <see cref="Guid"/>
        /// </summary>
        /// <param name="mode">不重复模式</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid Create(NoRepeatMode mode) => Create(Guid.NewGuid(), mode);

        /// <summary>
        /// 创建一个 <see cref="Guid"/>
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="mode">不重复模式</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid Create(Guid value, NoRepeatMode mode) => Create(value, mode == NoRepeatMode.On ? NoRepeatTimeStampManager.GetFactory().GetUtcTimeStamp() : DateTime.UtcNow);

        /// <summary>
        /// 创建一个 <see cref="Guid"/>
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="secureTimestamp">安全时间戳</param>
        public static Guid Create(Guid value, DateTime secureTimestamp)
        {
            byte[] guidArray = value.ToByteArray();
            DateTime basedTime = new(1_900, 1, 1);

            // 获取天数和毫秒数
            TimeSpan days = new(secureTimestamp.Ticks - basedTime.Ticks);
            TimeSpan msecs = new(secureTimestamp.Ticks - new DateTime(secureTimestamp.Year, secureTimestamp.Month, secureTimestamp.Day).Ticks);

            // 转换byte数组
            byte[] daysArray = BitConverter.GetBytes(days.Days);
            byte[] msecsArray = BitConverter.GetBytes((long)(msecs.TotalMilliseconds / 3.333_333));

            // 反转字节数组
            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);

            // 复制字节数组到Guid
            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

            return new Guid(guidArray);
        }
    }
}