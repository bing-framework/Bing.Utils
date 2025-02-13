using System.Security.Cryptography;

namespace Bing.IdUtils.GuidImplements;

/// <summary>
/// 顺序风格提供程序
/// </summary>
/// <remarks>参考链接：<see href="https://github.com/jhtodd/SequentialGuid/blob/master/SequentialGuid/Classes/SequentialGuid.cs"/></remarks>
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
        // We start with 16 bytes of cryptographically strong random data.
        var randomBytes = new byte[10];
        RandomGenerator.GetBytes(randomBytes);

        // An alternate method: use a normally-created GUID to get our initial
        // random data:
        // byte[] randomBytes = Guid.NewGuid().ToByteArray();
        // This is faster than using RNGCryptoServiceProvider, but I don't
        // recommend it because the .NET Framework makes no guarantee of the
        // randomness of GUID data, and future versions (or different
        // implementations like Mono) might use a different method.

        // Now we have the random basis for our GUID.  Next, we need to
        // create the six-byte block which will be our timestamp.

        // We start with the number of milliseconds that have elapsed since
        // DateTime.MinValue.  This will form the timestamp.  There's no use
        // being more specific than milliseconds, since DateTime.Now has
        // limited resolution.

        // Using millisecond resolution for our 48-bit timestamp gives us
        // about 5900 years before the timestamp overflows and cycles.
        // Hopefully this should be sufficient for most purposes. :)
        var timestamp = secureTimestamp.Ticks / 1_000L;

        // Then get the bytes
        var timestampBytes = BitConverter.GetBytes(timestamp);

        // Since we're converting from an Int64, we have to reverse on
        // little-endian systems.
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timestampBytes);

        var guidBytes = new byte[16];

        switch (type)
        {
            case SequentialGuidTypes.SequentialAsString:
            case SequentialGuidTypes.SequentialAsBinary:

                // For string and byte-array version, we copy the timestamp first, followed
                // by the random data.
                Buffer.BlockCopy(timestampBytes, 2, guidBytes, 0, 6);
                Buffer.BlockCopy(randomBytes, 0, guidBytes, 6, 10);

                // If formatting as a string, we have to compensate for the fact
                // that .NET regards the Data1 and Data2 block as an Int32 and an Int16,
                // respectively.  That means that it switches the order on little-endian
                // systems.  So again, we have to reverse.
                if (type == SequentialGuidTypes.SequentialAsString && BitConverter.IsLittleEndian)
                {
                    Array.Reverse(guidBytes, 0, 4);
                    Array.Reverse(guidBytes, 4, 2);
                }

                break;

            case SequentialGuidTypes.SequentialAtEnd:

                // For sequential-at-the-end versions, we copy the random data first,
                // followed by the timestamp.
                Buffer.BlockCopy(randomBytes, 0, guidBytes, 0, 10);
                Buffer.BlockCopy(timestampBytes, 2, guidBytes, 10, 6);
                break;
        }

        return new Guid(guidBytes);
    }
}