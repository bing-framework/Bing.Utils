using Bing.Extensions;

namespace Bing.Helpers;

// 标识生成器 - Guid
public static partial class Id
{
    /// <summary>
    /// 默认 Guid 生成函数
    /// </summary>
    private static readonly Func<Guid> _defaultGuidGenerateFunc = Guid.NewGuid;

    /// <summary>
    /// Guid 生成函数
    /// </summary>
    private static Func<Guid> _guidGenerateFunc = _defaultGuidGenerateFunc;

    /// <summary>
    /// 配置 Guid 生成函数
    /// </summary>
    /// <param name="provider">Guid提供程序</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Configure(Func<Guid> provider) => _guidGenerateFunc = provider ?? throw new ArgumentNullException(nameof(provider));

    /// <summary>
    /// 重置 Guid 生成函数
    /// </summary>
    public static void ResetGuid() => Configure(_defaultGuidGenerateFunc);

    /// <summary>
    /// 创建简化的Guid标识，去掉了横线
    /// </summary>
    public static string CreateSimpleGuid()=>string.IsNullOrWhiteSpace(_id.Value) ? _guidGenerateFunc().ToString("N") : _id.Value;

    /// <summary>
    /// 创建Guid标识
    /// </summary>
    public static Guid CreateGuid() => string.IsNullOrWhiteSpace(_id.Value) ? _guidGenerateFunc() : _id.Value.ToGuid();
}