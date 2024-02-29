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
    public static Func<long> LongGenerateFunc { get; set; }

    /// <summary>
    /// String 生成函数
    /// </summary>
    public static Func<string> StringGenerateFunc { get; set; }

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
    /// 创建Long标识
    /// </summary>
    public static long CreateLong() => string.IsNullOrWhiteSpace(_id.Value) ? LongGenerateFunc() : _id.Value.ToLong();

    /// <summary>
    /// 创建String标识
    /// </summary>
    public static string CreateString() => string.IsNullOrWhiteSpace(_id.Value) ? StringGenerateFunc() : _id.Value;

    /// <summary>
    /// 创建ObjectId标识
    /// </summary>
    public static string CreateObjectId() => ObjectId.GenerateNewStringId();

    /// <summary>
    /// 创建时间戳标识
    /// </summary>
    public static string CreateTimestampId() => TimestampId.GetInstance().GetId();
}