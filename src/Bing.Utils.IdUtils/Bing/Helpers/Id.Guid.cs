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
    private static volatile Func<Guid> _guidGenerateFunc = _defaultGuidGenerateFunc;

    /// <summary>
    /// 同步锁对象，用于保护 Guid 生成函数的线程安全配置
    /// </summary>
    private static readonly object _guidLock = new();

    /// <summary>
    /// 配置自定义的 Guid 生成函数。
    /// </summary>
    /// <param name="provider">Guid 生成函数，不能为 null</param>
    /// <exception cref="ArgumentNullException">当 provider 为 null 时抛出</exception>
    /// <remarks>
    /// 此方法是线程安全的。配置后，所有后续的 Guid 生成将使用新的生成函数。
    /// </remarks>
    public static void Configure(Func<Guid> provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));
        lock (_guidLock) 
            _guidGenerateFunc = provider;
    }

    /// <summary>
    /// 重置 Guid 生成函数为默认实现。
    /// </summary>
    /// <remarks>
    /// 默认实现使用 <see cref="Guid.NewGuid"/> 方法生成随机 Guid。
    /// 此方法是线程安全的。
    /// </remarks>
    public static void ResetGuid()
    {
        lock (_guidLock) 
            _guidGenerateFunc = _defaultGuidGenerateFunc;
    }

    /// <summary>
    /// 创建简化格式的 Guid 标识字符串，去除连字符。
    /// </summary>
    /// <returns>32位字符的 Guid 字符串，例如："83b0233ca24f49fd80831337209ebc9a"</returns>
    /// <remarks>
    /// 如果当前线程已设置 Id 值，则使用设置的值；否则使用配置的 Guid 生成函数生成新的 Guid。
    /// 生成的字符串格式为小写，不包含连字符和大括号。
    /// </remarks>
    public static string CreateSimpleGuid()
    {
        if (!string.IsNullOrWhiteSpace(_id.Value))
            return _id.Value;
        var currentProvider = _guidGenerateFunc; // 获取当前快照，避免并发问题
        return currentProvider().ToString("N");
    }

    /// <summary>
    /// 创建 Guid 标识。
    /// </summary>
    /// <returns>新生成的 Guid 或从当前线程 Id 值转换的 Guid</returns>
    /// <remarks>
    /// 如果当前线程已设置 Id 值，则尝试将其转换为 Guid；否则使用配置的 Guid 生成函数生成新的 Guid。
    /// 当 Id 值无法转换为有效 Guid 时，返回 Guid.Empty。
    /// </remarks>
    public static Guid CreateGuid()
    {
        if (!string.IsNullOrWhiteSpace(_id.Value))
            return _id.Value.ToGuid();
        var currentProvider = _guidGenerateFunc; // 获取当前快照，避免并发问题
        return currentProvider();
    }
}