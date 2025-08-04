using Bing.Extensions;
using Bing.IdUtils;

namespace Bing.Helpers;

/// <summary>
/// 标识生成器
/// </summary>
public static partial class Id
{
    /// <summary>
    /// 标识
    /// </summary>
    private static readonly AsyncLocal<string> _id = new();

    /// <summary>
    /// Long 生成函数
    /// </summary>
    public static Func<long> LongGenerateFunc { get; private set; }

    /// <summary>
    /// String 生成函数
    /// </summary>
    public static Func<string> StringGenerateFunc { get; private set; }

    /// <summary>
    /// 设置Id
    /// </summary>
    /// <param name="id">Id</param>
    public static void SetId(string id) => _id.Value = id;

    /// <summary>
    /// 重置Id
    /// </summary>
    public static void Reset() => _id.Value = null;

    /// <summary>
    /// 配置Long类型标识的生成函数。
    /// </summary>
    /// <param name="provider">Long生成函数，不能为null</param>
    /// <exception cref="ArgumentNullException">当provider为null时抛出</exception>
    /// <remarks>
    /// 此方法配置后，CreateLong方法将使用指定的生成函数。
    /// 如果当前线程设置了Id值，仍会优先使用设置的值。
    /// </remarks>
    public static void ConfigureLong(Func<long> provider)
    {
        LongGenerateFunc = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>
    /// 配置String类型标识的生成函数。
    /// </summary>
    /// <param name="provider">String生成函数，不能为null</param>
    /// <exception cref="ArgumentNullException">当provider为null时抛出</exception>
    /// <remarks>
    /// 此方法配置后，CreateString方法将使用指定的生成函数。
    /// 如果当前线程设置了Id值，仍会优先使用设置的值。
    /// </remarks>
    public static void ConfigureString(Func<string> provider)
    {
        StringGenerateFunc = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>
    /// 重置Long类型标识的生成函数为null。
    /// </summary>
    /// <remarks>
    /// 重置后，如果调用CreateLong方法且当前线程未设置Id值，将抛出异常。
    /// </remarks>
    public static void ResetLong()
    {
        LongGenerateFunc = null;
    }

    /// <summary>
    /// 重置String类型标识的生成函数为null。
    /// </summary>
    /// <remarks>
    /// 重置后，如果调用CreateString方法且当前线程未设置Id值，将抛出异常。
    /// </remarks>
    public static void ResetString()
    {
        StringGenerateFunc = null;
    }

    /// <summary>
    /// 创建Long类型标识。
    /// </summary>
    /// <returns>Long类型的标识值</returns>
    /// <exception cref="InvalidOperationException">当LongGenerateFunc未配置且当前线程未设置Id时抛出</exception>
    /// <remarks>
    /// 如果当前线程已设置Id值，则尝试将其转换为long；
    /// 否则使用配置的LongGenerateFunc生成新值。
    /// </remarks>
    public static long CreateLong()
    {
        if (!string.IsNullOrWhiteSpace(_id.Value))
            return _id.Value.ToLong();
        if (LongGenerateFunc == null)
            throw new InvalidOperationException("LongGenerateFunc未配置，请先调用ConfigureLong方法配置生成函数");
        return LongGenerateFunc();
    }

    /// <summary>
    /// 创建String类型标识。
    /// </summary>
    /// <returns>String类型的标识值</returns>
    /// <exception cref="InvalidOperationException">当StringGenerateFunc未配置且当前线程未设置Id时抛出</exception>
    /// <remarks>
    /// 如果当前线程已设置Id值，则直接返回该值；
    /// 否则使用配置的StringGenerateFunc生成新值。
    /// </remarks>
    public static string CreateString()
    {
        if (!string.IsNullOrWhiteSpace(_id.Value))
            return _id.Value;
        if (StringGenerateFunc == null)
            throw new InvalidOperationException("StringGenerateFunc未配置，请先调用ConfigureString方法配置生成函数");
        return StringGenerateFunc();
    }

    /// <summary>
    /// 创建ObjectId标识。
    /// </summary>
    /// <returns>ObjectId格式的字符串标识</returns>
    /// <remarks>
    /// 此方法始终生成新的ObjectId，不受当前线程Id设置影响。
    /// ObjectId是MongoDB风格的24位16进制字符串标识。
    /// </remarks>
    public static string CreateObjectId() => ObjectId.GenerateNewStringId();

    /// <summary>
    /// 创建时间戳标识。
    /// </summary>
    /// <returns>基于时间戳的字符串标识</returns>
    /// <remarks>
    /// 此方法始终生成新的时间戳标识，不受当前线程Id设置影响。
    /// 生成的标识基于当前时间，具有时序性。
    /// </remarks>
    public static string CreateTimestampId() => TimestampId.GetInstance().GetId();
}