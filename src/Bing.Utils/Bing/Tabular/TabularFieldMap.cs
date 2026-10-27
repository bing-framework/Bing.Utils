using System.Globalization;
using System.Reflection;

namespace Bing.Tabular;

/// <summary>
/// 表示一个目标成员的表格字段映射配置。
/// </summary>
/// <typeparam name="TTarget">目标对象类型。</typeparam>
/// <typeparam name="TMember">目标成员类型。</typeparam>
public sealed class TabularFieldMap<TTarget, TMember>
{
    private readonly TabularFieldMapDefinition _definition;

    internal TabularFieldMap(TabularFieldMapDefinition definition)
    {
        _definition = definition;
    }

    /// <summary>获取目标成员名称。</summary>
    public string MemberName => _definition.MemberName;

    /// <summary>
    /// 配置一个或多个源字段别名。
    /// </summary>
    public TabularFieldMap<TTarget, TMember> From(params string[] names)
    {
        _definition.EnsureMutable();
        if (names == null || names.Length == 0) throw new ArgumentException("至少需要一个源字段名称。", nameof(names));
        if (_definition.SourceOrdinal.HasValue)
            throw new TabularMappingException("mapping.duplicate_mapping", $"目标成员“{MemberName}”不能同时配置字段名称和字段序号。");
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("源字段名称不能为空。", nameof(names));
            if (!_definition.SourceNames.Contains(name, StringComparer.Ordinal))
                _definition.SourceNames.Add(name);
        }

        return this;
    }

    /// <summary>
    /// 按零基字段序号指定源字段。
    /// </summary>
    public TabularFieldMap<TTarget, TMember> FromOrdinal(int ordinal)
    {
        _definition.EnsureMutable();
        if (ordinal < 0) throw new ArgumentOutOfRangeException(nameof(ordinal));
        if (_definition.SourceNames.Count > 0)
            throw new TabularMappingException("mapping.duplicate_mapping", $"目标成员“{MemberName}”不能同时配置字段名称和字段序号。");
        _definition.SourceOrdinal = ordinal;
        return this;
    }

    /// <summary>要求源字段必须存在。</summary>
    public TabularFieldMap<TTarget, TMember> RequiredColumn()
    {
        _definition.EnsureMutable();
        _definition.RequiredColumn = true;
        return this;
    }

    /// <summary>要求源字段必须包含非空值。</summary>
    public TabularFieldMap<TTarget, TMember> RequiredValue()
    {
        _definition.EnsureMutable();
        _definition.RequiredValue = true;
        return this;
    }

    /// <summary>
    /// 配置自定义字段转换器。
    /// </summary>
    /// <param name="converter">接收原始值和文化信息并返回目标值的转换器。</param>
    public TabularFieldMap<TTarget, TMember> ConvertUsing(Func<object, CultureInfo, TMember> converter)
    {
        _definition.EnsureMutable();
        if (converter == null) throw new ArgumentNullException(nameof(converter));
        _definition.Converter = (value, culture) => converter(value, culture);
        return this;
    }

}

internal sealed class TabularFieldMapDefinition
{
    internal TabularFieldMapDefinition(MemberInfo member)
    {
        Member = member;
        MemberName = member.Name;
        MemberType = member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;
    }

    internal MemberInfo Member { get; }
    internal string MemberName { get; }
    internal Type MemberType { get; }
    internal List<string> SourceNames { get; } = new();
    internal int? SourceOrdinal { get; set; }
    internal bool RequiredColumn { get; set; }
    internal bool RequiredValue { get; set; }
    internal Func<object, CultureInfo, object> Converter { get; set; }
    internal Action<object, object> Setter { get; set; }
    internal bool IsSealed { get; set; }

    internal void EnsureMutable()
    {
        if (IsSealed) throw new InvalidOperationException("映射配置已经创建，不能再修改。");
    }

    internal string PrimarySourceName => SourceNames.Count == 0 ? MemberName : SourceNames[0];
}
