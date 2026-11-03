using System.Security.Cryptography;

namespace Bing.IdUtils;

/// <summary>
/// 按 RFC 9562 生成 UUIDv1。
/// </summary>
/// <remarks>
/// 时间字段使用 Gregorian epoch（1582-10-15）以来的 100 纳秒间隔；节点字段使用进程内随机节点，
/// 并设置 multicast 位以表示该节点不是硬件 MAC 地址。时钟序列和最近时间戳仅在当前进程内维护。
/// </remarks>
public static class Uuid1
{
    /// <summary>
    /// UUID 时间字段的起始时刻。
    /// </summary>
    private static readonly DateTimeOffset GregorianEpoch = new(1582, 10, 15, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// UUIDv1 可编码的最大时间字段。
    /// </summary>
    private const long MaximumTimestamp = 0x0FFF_FFFF_FFFF_FFFF;

    /// <summary>
    /// 保护进程内时钟状态的锁。
    /// </summary>
    private static readonly object SyncRoot = new();

    /// <summary>
    /// 进程内随机节点标识。
    /// </summary>
    private static readonly byte[] Node = CreateRandomNode();

    /// <summary>
    /// 初始化的时钟序列。
    /// </summary>
    private static ushort _clockSequence = CreateRandomClockSequence();

    /// <summary>
    /// 最近一次实际编码的时间字段；尚未生成时为 -1。
    /// </summary>
    private static long _lastTimestamp = -1;

    /// <summary>
    /// 最近一次成功调用收到的物理时间，用于区分真实回拨和逻辑时间领先。
    /// </summary>
    private static long _lastPhysicalTimestamp = -1;

    /// <summary>
    /// 生成包含当前 UTC 时刻的 UUIDv1。
    /// </summary>
    /// <returns>符合 RFC 9562 布局的 UUIDv1。</returns>
    public static Guid Generate()
    {
        // 在锁内读取当前时间，避免线程排队使时间采样顺序与状态提交顺序相反。
        lock (SyncRoot)
            return GenerateCore(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 使用指定时刻生成 UUIDv1。
    /// </summary>
    /// <param name="timestamp">用于编码的时刻；必须不早于 Gregorian epoch 且适合 UUIDv1 的 60 位字段。</param>
    /// <returns>符合 RFC 9562 布局的 UUIDv1。</returns>
    /// <exception cref="ArgumentOutOfRangeException">指定时刻不能编码为 UUIDv1 时间字段。</exception>
    public static Guid Generate(DateTimeOffset timestamp)
    {
        lock (SyncRoot)
            return GenerateCore(timestamp);
    }

    /// <summary>
    /// 在持有状态锁时分配逻辑时间与时钟序列，生成成功后统一提交状态。
    /// </summary>
    private static Guid GenerateCore(DateTimeOffset timestamp)
    {
        var physicalTimestamp = ToTimestamp(timestamp);
        var timestampValue = physicalTimestamp;
        if (timestampValue <= _lastTimestamp)
        {
            if (_lastTimestamp == MaximumTimestamp)
                throw new ArgumentOutOfRangeException(nameof(timestamp), timestamp, "UUIDv1 时间字段已耗尽。");
            timestampValue = _lastTimestamp + 1;
        }

        var clockSequence = _clockSequence;
        if (physicalTimestamp < _lastPhysicalTimestamp)
            clockSequence = (ushort)((clockSequence + 1) & 0x3FFF);

        var result = Create(timestampValue, clockSequence, Node);
        _lastTimestamp = timestampValue;
        _lastPhysicalTimestamp = physicalTimestamp;
        _clockSequence = clockSequence;
        return result;
    }

    /// <summary>
    /// 使用显式时间、时钟序列和节点构造 UUIDv1。
    /// </summary>
    /// <remarks>此入口仅供模块测试使用，不修改进程内生成状态。</remarks>
    /// <param name="timestamp">用于编码的时刻。</param>
    /// <param name="clockSequence">14 位时钟序列。</param>
    /// <param name="node">6 字节节点标识。</param>
    /// <returns>符合 RFC 9562 布局的 UUIDv1。</returns>
    internal static Guid CreateDeterministic(DateTimeOffset timestamp, ushort clockSequence, byte[] node)
    {
        var timestampValue = ToTimestamp(timestamp);
        ValidateClockSequence(clockSequence);
        ValidateNode(node);
        return Create(timestampValue, clockSequence, node);
    }

    /// <summary>
    /// 为测试清理进程内的逻辑时钟状态。
    /// </summary>
    internal static void ResetStateForTests(ushort clockSequence = 0)
    {
        ValidateClockSequence(clockSequence);
        lock (SyncRoot)
        {
            _lastTimestamp = -1;
            _lastPhysicalTimestamp = -1;
            _clockSequence = clockSequence;
        }
    }

    /// <summary>
    /// 将时刻转换为 UUIDv1 的 60 位 Gregorian 100ns 时间字段。
    /// </summary>
    private static long ToTimestamp(DateTimeOffset timestamp)
    {
        var timestampValue = timestamp.UtcDateTime.Ticks - GregorianEpoch.UtcDateTime.Ticks;
        if (timestampValue < 0 || timestampValue > MaximumTimestamp)
            throw new ArgumentOutOfRangeException(nameof(timestamp), timestamp, "UUIDv1 时间戳必须位于 Gregorian epoch 后的 60 位范围内。");

        return timestampValue;
    }

    /// <summary>
    /// 根据 RFC 字段构造 .NET <see cref="Guid"/>。
    /// </summary>
    private static Guid Create(long timestamp, ushort clockSequence, byte[] node)
    {
        // 按字段构造可避免网络序和混合端序的两个临时数组，兼容 netstandard2.0。
        return new Guid(
            unchecked((int)timestamp),
            unchecked((short)(timestamp >> 32)),
            (short)(0x1000 | ((timestamp >> 48) & 0x0FFF)),
            (byte)(0x80 | (clockSequence >> 8)),
            (byte)clockSequence,
            node[0], node[1], node[2], node[3], node[4], node[5]);
    }

    /// <summary>
    /// 生成进程内随机节点并设置 multicast 位。
    /// </summary>
    private static byte[] CreateRandomNode()
    {
        var node = new byte[6];
        using (var random = RandomNumberGenerator.Create())
            random.GetBytes(node);
        node[0] |= 0x01;
        return node;
    }

    /// <summary>
    /// 生成 14 位初始时钟序列。
    /// </summary>
    private static ushort CreateRandomClockSequence()
    {
        var bytes = new byte[2];
        using (var random = RandomNumberGenerator.Create())
            random.GetBytes(bytes);
        return (ushort)(((bytes[0] << 8) | bytes[1]) & 0x3FFF);
    }

    /// <summary>
    /// 校验时钟序列范围。
    /// </summary>
    private static void ValidateClockSequence(ushort clockSequence)
    {
        if (clockSequence > 0x3FFF)
            throw new ArgumentOutOfRangeException(nameof(clockSequence), clockSequence, "UUIDv1 时钟序列必须是 14 位无符号整数。");
    }

    /// <summary>
    /// 校验节点字段长度。
    /// </summary>
    private static void ValidateNode(byte[] node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (node.Length != 6)
            throw new ArgumentException("UUIDv1 节点字段必须正好包含 6 个字节。", nameof(node));
    }
}
