using System.Globalization;

namespace Bing.Tabular;

/// <summary>
/// 配置表格映射的名称、转换、校验和未知列行为。
/// </summary>
public sealed class TabularMapOptions
{
    /// <summary>获取或设置字段名称是否忽略大小写，默认值为 true。</summary>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>获取或设置默认转换文化，未设置时使用不变文化。</summary>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>获取或设置未知源列的处理方式，默认忽略。</summary>
    public UnknownColumnBehavior UnknownColumns { get; set; } = UnknownColumnBehavior.Ignore;

    /// <summary>获取或设置是否执行 DataAnnotations 对象校验，默认值为 true。</summary>
    public bool ValidateDataAnnotations { get; set; } = true;

    internal TabularMapOptions Snapshot()
    {
        if (!Enum.IsDefined(typeof(UnknownColumnBehavior), UnknownColumns))
            throw new ArgumentOutOfRangeException(nameof(UnknownColumns));
        return new TabularMapOptions
        {
            IgnoreCase = IgnoreCase,
            Culture = CultureInfo.ReadOnly((CultureInfo)(Culture ?? CultureInfo.InvariantCulture).Clone()),
            UnknownColumns = UnknownColumns,
            ValidateDataAnnotations = ValidateDataAnnotations
        };
    }
}

/// <summary>
/// 定义未知源列的处理方式。
/// </summary>
public enum UnknownColumnBehavior
{
    /// <summary>忽略未知列。</summary>
    Ignore,
    /// <summary>将未知列作为警告返回。</summary>
    Warning,
    /// <summary>将未知列作为错误返回。</summary>
    Error
}
