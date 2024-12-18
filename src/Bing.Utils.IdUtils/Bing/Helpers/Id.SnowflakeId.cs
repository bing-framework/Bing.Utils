using Bing.Extensions;
using Bing.IdUtils;

namespace Bing.Helpers;

// 标识生成器 - 雪花ID
public static partial class Id
{
    /// <summary>
    /// 默认 雪花ID 生成函数
    /// </summary>
    private static readonly Func<ISnowflakeId> _defaultSnowflakeIdGenerateFunc = () => SnowflakeGenerator.Create(1);

    /// <summary>
    /// 雪花ID 生成函数
    /// </summary>
    private static Func<ISnowflakeId> _snowflakeIdGenerateFunc = _defaultSnowflakeIdGenerateFunc;

    /// <summary>
    /// 配置 雪花ID 生成函数
    /// </summary>
    /// <param name="provider">雪花ID提供程序</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Configure(Func<ISnowflakeId> provider) => _snowflakeIdGenerateFunc = provider ?? throw new ArgumentNullException(nameof(provider));

    /// <summary>
    /// 重置 雪花ID 生成函数
    /// </summary>
    public static void ResetSnowflakeId() => Configure(_defaultSnowflakeIdGenerateFunc);

    /// <summary>
    /// 创建 雪花ID
    /// </summary>
    public static long CreateSnowflakeId() => string.IsNullOrWhiteSpace(_id.Value) ? _snowflakeIdGenerateFunc().NextId() : _id.Value.ToLong();
}