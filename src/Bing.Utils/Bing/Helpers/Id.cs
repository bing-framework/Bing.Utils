using Bing.Utils.IdGenerators.Abstractions;
using Bing.Utils.IdGenerators.Core;

namespace Bing.Helpers;

/// <summary>
/// Id 生成器
/// </summary>
public static partial class Id
{
    /// <summary>
    /// 标识
    /// </summary>
    private static readonly AsyncLocal<string> _id = new();

    /// <summary>
    /// Guid 生成器
    /// </summary>
    public static IGuidGenerator GuidGenerator { get; set; } = SequentialGuidGenerator.Current;

    /// <summary>
    /// Long 生成器
    /// </summary>
    public static ILongGenerator LongGenerator { get; set; } = SnowflakeIdGenerator.Current;

    /// <summary>
    /// String 生成器
    /// </summary>
    public static IStringGenerator StringGenerator { get; set; } = TimestampIdGenerator.Current;

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
    /// 用Guid创建标识，去掉分隔符
    /// </summary>
    public static string Guid() => string.IsNullOrWhiteSpace(_id.Value) ? System.Guid.NewGuid().ToString("N") : _id.Value;

    /// <summary>
    /// 创建 Guid ID
    /// </summary>
    public static Guid NewGuid() => GuidGenerator.Create();

    /// <summary>
    /// 创建 Long ID
    /// </summary>
    public static long NewLong() => LongGenerator.Create();

    /// <summary>
    /// 创建 String ID
    /// </summary>
    public static string NewString() => StringGenerator.Create();
}