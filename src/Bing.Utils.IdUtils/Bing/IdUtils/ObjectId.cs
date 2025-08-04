using System.Diagnostics;
using System.Security.Cryptography;

namespace Bing.IdUtils;

/// <summary>
/// MongoDB 风格的 ObjectId 生成器，提供全局唯一的24位16进制字符串标识符。
/// </summary>
/// <remarks>
/// ObjectId 由以下部分组成：
/// - 4字节时间戳（Unix时间）
/// - 3字节机器标识符
/// - 2字节进程ID
/// - 3字节递增计数器
/// 
/// 代码改编自：https://github.com/tangxuehua/ecommon/blob/master/src/ECommon/Utilities/ObjectId.cs
/// </remarks>
public struct ObjectId : IComparable<ObjectId>, IEquatable<ObjectId>
{
    #region 私有静态字段

    /// <summary>
    /// Unix 纪元时间（1970年1月1日）
    /// </summary>
    private static readonly DateTime __unixEpoch;

    /// <summary>
    /// DateTime.MaxValue 相对于 Unix 纪元的毫秒数
    /// </summary>
    private static readonly long __dateTimeMaxValueMillisecondsSinceEpoch;

    /// <summary>
    /// DateTime.MinValue 相对于 Unix 纪元的毫秒数
    /// </summary>
    private static readonly long __dateTimeMinValueMillisecondsSinceEpoch;

    /// <summary>
    /// 空 ObjectId 实例
    /// </summary>
    private static ObjectId __emptyInstance = default(ObjectId);

    /// <summary>
    /// 机器标识符（基于机器名计算的哈希值）
    /// </summary>
    private static int __staticMachine;

    /// <summary>
    /// 进程标识符
    /// </summary>
    private static short __staticPid;

    /// <summary>
    /// 递增计数器（线程安全）
    /// </summary>
    private static int __staticIncrement; // high byte will be masked out when generating new ObjectId

    /// <summary>
    /// 16进制字符串转换查找表，用于优化性能
    /// </summary>
    private static uint[] _lookup32 = Enumerable.Range(0, 256).Select(i =>
    {
        string s = i.ToString("x2");
        return ((uint)s[0]) + ((uint)s[1] << 16);
    }).ToArray();

    #endregion

    #region 实例字段

    // we're using 14 bytes instead of 12 to hold the ObjectId in memory but unlike a byte[] there is no additional object on the heap
    // the extra two bytes are not visible to anyone outside of this class and they buy us considerable simplification
    // an additional advantage of this representation is that it will serialize to JSON without any 64 bit overflow problems
    /// <summary>
    /// 时间戳部分（4字节）
    /// </summary>
    private int _timestamp;

    /// <summary>
    /// 机器标识符部分（3字节）
    /// </summary>
    private int _machine;

    /// <summary>
    /// 进程标识符部分（2字节）
    /// </summary>
    private short _pid;

    /// <summary>
    /// 递增计数器部分（3字节）
    /// </summary>
    private int _increment;

    #endregion

    #region 静态构造函数

    /// <summary>
    /// 静态构造函数，初始化静态字段
    /// </summary>
    static ObjectId()
    {
        __unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        __dateTimeMaxValueMillisecondsSinceEpoch = (DateTime.MaxValue - __unixEpoch).Ticks / 10000;
        __dateTimeMinValueMillisecondsSinceEpoch = (DateTime.MinValue - __unixEpoch).Ticks / 10000;
        __staticMachine = GetMachineHash();
        __staticIncrement = (new System.Random()).Next();
        __staticPid = (short)GetCurrentProcessId();
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="ObjectId"/> 类型的实例。
    /// </summary>
    /// <param name="bytes">12字节的字节数组</param>
    /// <exception cref="ArgumentNullException">当 bytes 为 null 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当字节数组长度不是12时抛出</exception>
    public ObjectId(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        Unpack(bytes, out _timestamp, out _machine, out _pid, out _increment);
    }

    /// <summary>
    /// 初始化一个<see cref="ObjectId"/> 类型的实例。
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    /// <param name="machine">机器标识符</param>
    /// <param name="pid">进程标识符</param>
    /// <param name="increment">递增计数器</param>
    public ObjectId(DateTime timestamp, int machine, short pid, int increment)
        : this(GetTimestampFromDateTime(timestamp), machine, pid, increment)
    {
    }

    /// <summary>
    /// 初始化一个<see cref="ObjectId"/> 类型的实例。
    /// </summary>
    /// <param name="timestamp">Unix 时间戳</param>
    /// <param name="machine">机器标识符（必须在0-16777215范围内）</param>
    /// <param name="pid">进程标识符</param>
    /// <param name="increment">递增计数器（必须在0-16777215范围内）</param>
    /// <exception cref="ArgumentOutOfRangeException">当机器标识符或递增计数器超出范围时抛出</exception>
    public ObjectId(int timestamp, int machine, short pid, int increment)
    {
        if ((machine & 0xff000000) != 0)
            throw new ArgumentOutOfRangeException(nameof(machine), "机器标识符必须在0到16777215之间（必须适合3字节）");
        if ((increment & 0xff000000) != 0)
            throw new ArgumentOutOfRangeException(nameof(increment), "递增计数器必须在0到16777215之间（必须适合3字节）");

        _timestamp = timestamp;
        _machine = machine;
        _pid = pid;
        _increment = increment;
    }

    /// <summary>
    /// 初始化一个<see cref="ObjectId"/> 类型的实例。
    /// </summary>
    /// <param name="value">24位16进制字符串</param>
    /// <exception cref="ArgumentNullException">当 value 为 null 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当字符串长度不是24时抛出</exception>
    public ObjectId(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (value.Length != 24)
            throw new ArgumentOutOfRangeException(nameof(value), "ObjectId 字符串必须是24个字符");
        Unpack(ParseHexString(value), out _timestamp, out _machine, out _pid, out _increment);
    }

    #endregion

    #region 公共静态属性

    /// <summary>
    /// 获取空 ObjectId 实例。
    /// </summary>
    public static ObjectId Empty => __emptyInstance;

    #endregion

    #region 属性

    /// <summary>
    /// 获取时间戳部分。
    /// </summary>
    public int Timestamp => _timestamp;

    /// <summary>
    /// 获取机器标识符部分。
    /// </summary>
    public int Machine => _machine;

    /// <summary>
    /// 获取进程标识符部分。
    /// </summary>
    public short Pid => _pid;

    /// <summary>
    /// 获取递增计数器部分。
    /// </summary>
    public int Increment => _increment;

    /// <summary>
    /// 获取创建时间（从时间戳推导）。
    /// </summary>
    public DateTime CreationTime => __unixEpoch.AddSeconds(_timestamp);

    #endregion

    #region 运算符重载

    /// <summary>
    /// 比较两个 ObjectId 的大小关系。
    /// </summary>
    /// <param name="lhs">第一个 ObjectId</param>
    /// <param name="rhs">第二个 ObjectId</param>
    /// <returns>如果第一个 ObjectId 小于第二个则返回 true</returns>
    public static bool operator <(ObjectId lhs, ObjectId rhs) => lhs.CompareTo(rhs) < 0;

    /// <summary>
    /// 比较两个 ObjectId 的大小关系。
    /// </summary>
    /// <param name="lhs">第一个 ObjectId</param>
    /// <param name="rhs">第二个 ObjectId</param>
    /// <returns>如果第一个 ObjectId 小于等于第二个则返回 true</returns>
    public static bool operator <=(ObjectId lhs, ObjectId rhs) => lhs.CompareTo(rhs) <= 0;

    /// <summary>
    /// 比较两个 ObjectId 是否相等。
    /// </summary>
    /// <param name="lhs">第一个 ObjectId</param>
    /// <param name="rhs">第二个 ObjectId</param>
    /// <returns>如果两个 ObjectId 相等则返回 true</returns>
    public static bool operator ==(ObjectId lhs, ObjectId rhs) => lhs.Equals(rhs);

    /// <summary>
    /// 比较两个 ObjectId 是否不相等。
    /// </summary>
    /// <param name="lhs">第一个 ObjectId</param>
    /// <param name="rhs">第二个 ObjectId</param>
    /// <returns>如果两个 ObjectId 不相等则返回 true</returns>
    public static bool operator !=(ObjectId lhs, ObjectId rhs) => !(lhs == rhs);

    /// <summary>
    /// 比较两个 ObjectId 的大小关系。
    /// </summary>
    /// <param name="lhs">第一个 ObjectId</param>
    /// <param name="rhs">第二个 ObjectId</param>
    /// <returns>如果第一个 ObjectId 大于等于第二个则返回 true</returns>
    public static bool operator >=(ObjectId lhs, ObjectId rhs) => lhs.CompareTo(rhs) >= 0;

    /// <summary>
    /// 比较两个 ObjectId 的大小关系。
    /// </summary>
    /// <param name="lhs">第一个 ObjectId</param>
    /// <param name="rhs">第二个 ObjectId</param>
    /// <returns>如果第一个 ObjectId 大于第二个则返回 true</returns>
    public static bool operator >(ObjectId lhs, ObjectId rhs) => lhs.CompareTo(rhs) > 0;

    #endregion

    #region 公共静态方法

    /// <summary>
    /// 生成一个新的具有唯一值的 ObjectId。
    /// </summary>
    /// <returns>新生成的 ObjectId</returns>
    public static ObjectId GenerateNewId() => GenerateNewId(GetTimestampFromDateTime(DateTime.UtcNow));

    /// <summary>
    /// 基于指定时间戳生成一个新的 ObjectId。
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    /// <returns>新生成的 ObjectId</returns>
    public static ObjectId GenerateNewId(DateTime timestamp) => GenerateNewId(GetTimestampFromDateTime(timestamp));

    /// <summary>
    /// 基于指定数值时间戳生成一个新的 ObjectId。
    /// </summary>
    /// <param name="timestamp">Unix 时间戳</param>
    /// <returns>新生成的 ObjectId</returns>
    public static ObjectId GenerateNewId(int timestamp)
    {
        int increment = Interlocked.Increment(ref __staticIncrement) & 0x00ffffff; // 只使用低24位
        return new ObjectId(timestamp, __staticMachine, __staticPid, increment);
    }

    /// <summary>
    /// 生成一个新的 ObjectId 字符串。
    /// </summary>
    /// <returns>新生成的 ObjectId 的字符串表示</returns>
    public static string GenerateNewStringId() => GenerateNewId().ToString();

    /// <summary>
    /// 将 ObjectId 组件打包为字节数组。
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    /// <param name="machine">机器标识符</param>
    /// <param name="pid">进程标识符</param>
    /// <param name="increment">递增计数器</param>
    /// <returns>12字节的字节数组</returns>
    /// <exception cref="ArgumentOutOfRangeException">当机器标识符或递增计数器超出范围时抛出</exception>
    public static byte[] Pack(int timestamp, int machine, short pid, int increment)
    {
        if ((machine & 0xff000000) != 0)
            throw new ArgumentOutOfRangeException(nameof(machine), "机器标识符必须在0到16777215之间（必须适合3字节）");

        if ((increment & 0xff000000) != 0)
            throw new ArgumentOutOfRangeException(nameof(increment), "递增计数器必须在0到16777215之间（必须适合3字节）");

        byte[] bytes = new byte[12];
        bytes[0] = (byte)(timestamp >> 24);
        bytes[1] = (byte)(timestamp >> 16);
        bytes[2] = (byte)(timestamp >> 8);
        bytes[3] = (byte)(timestamp);
        bytes[4] = (byte)(machine >> 16);
        bytes[5] = (byte)(machine >> 8);
        bytes[6] = (byte)(machine);
        bytes[7] = (byte)(pid >> 8);
        bytes[8] = (byte)(pid);
        bytes[9] = (byte)(increment >> 16);
        bytes[10] = (byte)(increment >> 8);
        bytes[11] = (byte)(increment);
        return bytes;
    }

    /// <summary>
    /// 解析字符串并创建新的 ObjectId。
    /// </summary>
    /// <param name="s">24位16进制字符串</param>
    /// <returns>解析得到的 ObjectId</returns>
    /// <exception cref="ArgumentNullException">当字符串为 null 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当字符串长度不是24时抛出</exception>
    public static ObjectId Parse(string s)
    {
        if (s == null)
            throw new ArgumentNullException(nameof(s));
        if (s.Length != 24)
            throw new ArgumentOutOfRangeException(nameof(s), "ObjectId 字符串必须是24个字符");
        return new ObjectId(ParseHexString(s));
    }

    /// <summary>
    /// 尝试解析字符串并创建新的 ObjectId。
    /// </summary>
    /// <param name="s">24位16进制字符串</param>
    /// <param name="objectId">解析成功时输出的 ObjectId</param>
    /// <returns>解析是否成功</returns>
    public static bool TryParse(string s, out ObjectId objectId)
    {
        try
        {
            objectId = Parse(s);
            return true;
        }
        catch
        {
            objectId = Empty;
            return false;
        }
    }

    /// <summary>
    /// 将字节数组解包为 ObjectId 组件。
    /// </summary>
    /// <param name="bytes">12字节的字节数组</param>
    /// <param name="timestamp">输出的时间戳</param>
    /// <param name="machine">输出的机器标识符</param>
    /// <param name="pid">输出的进程标识符</param>
    /// <param name="increment">输出的递增计数器</param>
    /// <exception cref="ArgumentNullException">当字节数组为 null 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当字节数组长度不是12时抛出</exception>
    public static void Unpack(byte[] bytes, out int timestamp, out int machine, out short pid, out int increment)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length != 12)
            throw new ArgumentOutOfRangeException(nameof(bytes), "字节数组必须是12字节长");

        timestamp = (bytes[0] << 24) + (bytes[1] << 16) + (bytes[2] << 8) + bytes[3];
        machine = (bytes[4] << 16) + (bytes[5] << 8) + bytes[6];
        pid = (short)((bytes[7] << 8) + bytes[8]);
        increment = (bytes[9] << 16) + (bytes[10] << 8) + bytes[11];
    }

    /// <summary>
    /// 将16进制字符串解析为等效的字节数组。
    /// </summary>
    /// <param name="s">要解析的16进制字符串</param>
    /// <returns>等效的字节数组</returns>
    /// <exception cref="ArgumentNullException">当字符串为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当字符串长度为奇数时抛出</exception>
    public static byte[] ParseHexString(string s)
    {
        if (s == null)
            throw new ArgumentNullException(nameof(s));
        if (s.Length % 2 == 1)
            throw new ArgumentException("16进制字符串不能有奇数个字符", nameof(s));

        var arr = new byte[s.Length >> 1];

        for (var i = 0; i < s.Length >> 1; ++i)
            arr[i] = (byte)((GetHexVal(s[i << 1]) << 4) + (GetHexVal(s[(i << 1) + 1])));
        return arr;
    }

    /// <summary>
    /// 将字节数组转换为16进制字符串。
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <returns>16进制字符串</returns>
    /// <exception cref="ArgumentNullException">当字节数组为 null 时抛出</exception>
    public static string ToHexString(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));

        var result = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var val = _lookup32[bytes[i]];
            result[2 * i] = (char)val;
            result[2 * i + 1] = (char)(val >> 16);
        }
        return new string(result);
    }

    /// <summary>
    /// 将 DateTime 转换为自 Unix 纪元以来的毫秒数。
    /// </summary>
    /// <param name="dateTime">DateTime 时间</param>
    /// <returns>自 Unix 纪元以来的毫秒数</returns>
    public static long ToMillisecondsSinceEpoch(DateTime dateTime)
    {
        var utcDateTime = ToUniversalTime(dateTime);
        return (utcDateTime - __unixEpoch).Ticks / 10000;
    }

    /// <summary>
    /// 将 DateTime 转换为 UTC 时间（对 MinValue 和 MaxValue 进行特殊处理）。
    /// </summary>
    /// <param name="dateTime">要转换的 DateTime</param>
    /// <returns>UTC 时间</returns>
    public static DateTime ToUniversalTime(DateTime dateTime)
    {
        if (dateTime == DateTime.MinValue)
            return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        if (dateTime == DateTime.MaxValue)
            return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        return dateTime.ToUniversalTime();
    }

    #endregion

    #region 私有静态方法

    /// <summary>
    /// 获取当前进程ID。
    /// </summary>
    /// <remarks>
    /// 此方法存在是因为 CAS 在调用堆栈上的操作方式，在执行方法之前检查权限。
    /// 因此，如果我们内联此调用，调用方法将不会在抛出异常之前执行。
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetCurrentProcessId() => Process.GetCurrentProcess().Id;

    /// <summary>
    /// 获取机器哈希值。
    /// </summary>
    /// <returns>基于机器名的哈希值</returns>
    private static int GetMachineHash()
    {
        var hostName = Environment.MachineName; // 使用此方法而不是 Dns.HostName，以便离线工作
        var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(hostName));
        return (hash[0] << 16) + (hash[1] << 8) + hash[2]; // 使用哈希的前3字节
    }

    /// <summary>
    /// 从 DateTime 获取时间戳。
    /// </summary>
    /// <param name="timestamp">DateTime 时间戳</param>
    /// <returns>Unix 时间戳</returns>
    private static int GetTimestampFromDateTime(DateTime timestamp)
    {
        return (int)Math.Floor((ToUniversalTime(timestamp) - __unixEpoch).TotalSeconds);
    }

    /// <summary>
    /// 获取16进制字符的数值。
    /// </summary>
    /// <param name="hex">16进制字符</param>
    /// <returns>对应的数值</returns>
    private static int GetHexVal(char hex)
    {
        int val = (int)hex;
        //For uppercase A-F letters:
        //return val - (val < 58 ? 48 : 55);
        //For lowercase a-f letters:
        //return val - (val < 58 ? 48 : 87);
        //Or the two combined, but a bit slower:
        return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 比较此 ObjectId 与另一个 ObjectId。
    /// </summary>
    /// <param name="other">要比较的另一个 ObjectId</param>
    /// <returns>表示此 ObjectId 是否小于、等于或大于另一个的32位有符号整数</returns>
    public int CompareTo(ObjectId other)
    {
        int r = _timestamp.CompareTo(other._timestamp);
        if (r != 0)
            return r;
        r = _machine.CompareTo(other._machine);
        if (r != 0)
            return r;
        r = _pid.CompareTo(other._pid);
        if (r != 0)
            return r;
        return _increment.CompareTo(other._increment);
    }

    /// <summary>
    /// 比较此 ObjectId 与另一个 ObjectId 是否相等。
    /// </summary>
    /// <param name="rhs">要比较的另一个 ObjectId</param>
    /// <returns>如果两个 ObjectId 相等则返回 true</returns>
    public bool Equals(ObjectId rhs)
    {
        return
            _timestamp == rhs._timestamp &&
            _machine == rhs._machine &&
            _pid == rhs._pid &&
            _increment == rhs._increment;
    }

    /// <summary>
    /// 比较此 ObjectId 与另一个对象是否相等。
    /// </summary>
    /// <param name="obj">要比较的另一个对象</param>
    /// <returns>如果另一个对象是 ObjectId 且与此相等则返回 true</returns>
    public override bool Equals(object obj)
    {
        return obj is ObjectId other && Equals(other);
    }

    /// <summary>
    /// 获取哈希码。
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        int hash = 17;
        hash = 37 * hash + _timestamp.GetHashCode();
        hash = 37 * hash + _machine.GetHashCode();
        hash = 37 * hash + _pid.GetHashCode();
        hash = 37 * hash + _increment.GetHashCode();
        return hash;
    }

    /// <summary>
    /// 将 ObjectId 转换为字节数组。
    /// </summary>
    /// <returns>12字节的字节数组</returns>
    public byte[] ToByteArray() => Pack(_timestamp, _machine, _pid, _increment);

    /// <summary>
    /// 返回 ObjectId 的字符串表示。
    /// </summary>
    /// <returns>24位16进制字符串</returns>
    public override string ToString() => ToHexString(ToByteArray());

    #endregion
}