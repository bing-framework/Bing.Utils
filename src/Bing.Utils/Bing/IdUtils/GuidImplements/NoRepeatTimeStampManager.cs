using System.Runtime.CompilerServices;
using Bing.Date;
using Bing.IdUtils.CombImplements.Providers;

namespace Bing.IdUtils.GuidImplements
{
    /// <summary>
    /// 不重复时间戳管理器
    /// </summary>
    internal static class NoRepeatTimeStampManager
    {
        /// <summary>
        /// 不重复时间戳工厂
        /// </summary>
        private static readonly NoRepeatTimeStampFactory _factory;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static NoRepeatTimeStampManager() => _factory = new NoRepeatTimeStampFactory();

        /// <summary>
        /// 获取工厂
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NoRepeatTimeStampFactory GetFactory() => _factory;

        /// <summary>
        /// 获取时间戳提供程序
        /// </summary>
        /// <param name="mode">不重复模式</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InternalTimeStampProvider GetTimeStampProvider(NoRepeatMode mode)
        {
            if (mode == NoRepeatMode.On)
                return GetFactory().GetTimeStamp;
            return null;
        }
    }
}