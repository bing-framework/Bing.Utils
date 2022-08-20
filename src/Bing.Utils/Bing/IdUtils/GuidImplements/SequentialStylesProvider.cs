using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bing.IdUtils.GuidImplements
{
    /// <summary>
    /// 顺序风格提供程序
    /// </summary>
    internal static class SequentialStylesProvider
    {
        /// <summary>
        /// 随机生成器
        /// </summary>
        private static readonly RandomNumberGenerator RandomGenerator = RandomNumberGenerator.Create();

        /// <summary>
        /// 创建一个有序的 <see cref="Guid"/>
        /// </summary>
        /// <param name="type">顺序Guid类型</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid Create(SequentialGuidTypes type) =>
            Create(DateTime.UtcNow, type);

        /// <summary>
        /// 创建一个有序的 <see cref="Guid"/>
        /// </summary>
        /// <param name="type">顺序Guid类型</param>
        /// <param name="mode">不重复模式</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid Create(SequentialGuidTypes type, NoRepeatMode mode) =>
            Create(mode == NoRepeatMode.On ? NoRepeatTimeStampManager.GetFactory().GetUtcTimeStamp() : DateTime.UtcNow, type);

        /// <summary>
        /// 创建一个有序的 <see cref="Guid"/>
        /// </summary>
        /// <param name="secureTimestamp">安全时间戳</param>
        /// <param name="type">顺序Guid类型</param>
        public static Guid Create(DateTime secureTimestamp, SequentialGuidTypes type)
        {
            var randomBytes = new byte[10];
            RandomGenerator.GetBytes(randomBytes);

            var timestamp = secureTimestamp.Ticks / 1_000L;

            var timestampBytes = BitConverter.GetBytes(timestamp);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(timestampBytes);

            var guidBytes = new byte[16];

            switch (type)
            {
                case SequentialGuidTypes.SequentialAsString:
                case SequentialGuidTypes.SequentialAsBinary:
                    Buffer.BlockCopy(timestampBytes, 2, guidBytes, 0, 6);
                    Buffer.BlockCopy(randomBytes, 0, guidBytes, 6, 10);

                    if (type == SequentialGuidTypes.SequentialAsString && BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(guidBytes, 0, 4);
                        Array.Reverse(guidBytes, 4, 2);
                    }

                    break;

                case SequentialGuidTypes.SequentialAtEnd:
                    Buffer.BlockCopy(randomBytes, 0, guidBytes, 0, 10);
                    Buffer.BlockCopy(timestampBytes, 2, guidBytes, 10, 6);
                    break;
            }

            return new Guid(guidBytes);
        }
    }
}