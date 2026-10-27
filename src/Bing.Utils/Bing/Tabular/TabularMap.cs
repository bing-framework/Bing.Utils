using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Bing.Diagnostics;
using Bing.Operations;

namespace Bing.Tabular;

/// <summary>按显式配置将表格行映射为新对象，并返回带源位置的逐行诊断。</summary>
/// <typeparam name="T">具有公共无参构造函数的目标类型。</typeparam>
/// <remarks>
/// Create 完成后配置固定；成员写入委托编译一次，列绑定按有序列名结构缓存。
/// 缺少可选列时保留目标成员默认值。失败不返回对象，但无法撤销构造函数、setter 或用户委托的外部副作用。
/// 映射可并发复用；用户提供的转换器和校验器需自行保证线程安全。
/// </remarks>
public sealed class TabularMap<T> where T : new()
{
    private readonly TabularMapOptions _options;
    private readonly List<TabularFieldMapDefinition> _fields = new();
    private readonly List<Func<T, IEnumerable<string>>> _validators = new();
    private readonly ConcurrentDictionary<string, Lazy<MappingPlan>> _plans = new(StringComparer.Ordinal);
    private bool _sealed;

    private TabularMap(TabularMapOptions options) => _options = (options ?? new TabularMapOptions()).Snapshot();

    /// <summary>创建映射；仅处理通过 Field 显式配置的目标成员。</summary>
    /// <param name="configure">字段与对象校验配置回调。</param>
    /// <param name="options">名称比较、文化和未知列选项；创建时复制。</param>
    /// <returns>配置已固定的可复用映射器。</returns>
    public static TabularMap<T> Create(Action<TabularMap<T>> configure, TabularMapOptions options = null)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var map = new TabularMap<T>(options);
        configure(map);
        map.Seal();
        return map;
    }

    /// <summary>配置一个直接公共可写属性或字段；不支持嵌套成员和索引器。</summary>
    /// <typeparam name="TMember">目标成员类型。</typeparam>
    /// <param name="memberExpression">直接成员表达式。</param>
    /// <returns>字段配置对象，Create 返回后禁止继续修改。</returns>
    public TabularFieldMap<T, TMember> Field<TMember>(Expression<Func<T, TMember>> memberExpression)
    {
        EnsureMutable();
        if (memberExpression == null) throw new ArgumentNullException(nameof(memberExpression));
        Expression body = memberExpression.Body;
        if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert) body = unary.Operand;
        if (!(body is MemberExpression access) || access.Expression != memberExpression.Parameters[0])
            throw new ArgumentException("成员表达式必须直接指向属性或字段。", nameof(memberExpression));
        var member = access.Member;
        var writable = member is PropertyInfo property && property.SetMethod != null && property.SetMethod.IsPublic
                       && !property.SetMethod.IsStatic && property.GetIndexParameters().Length == 0
                       || member is FieldInfo field && field.IsPublic && !field.IsInitOnly && !field.IsLiteral && !field.IsStatic;
        if (!writable) throw new ArgumentException("目标成员必须是公共可写属性或字段。", nameof(memberExpression));
        if (_fields.Any(field => field.MemberName == member.Name)) throw Duplicate($"目标成员“{member.Name}”被重复配置。");
        var definition = new TabularFieldMapDefinition(member);
        _fields.Add(definition);
        return new TabularFieldMap<T, TMember>(definition);
    }

    /// <summary>添加对象校验器；返回的非空消息形成 mapping.validation 错误。</summary>
    /// <param name="validator">在所有字段转换和赋值成功后运行的校验器。</param>
    /// <returns>当前映射配置。</returns>
    public TabularMap<T> Validate(Func<T, IEnumerable<string>> validator)
    {
        EnsureMutable();
        _validators.Add(validator ?? throw new ArgumentNullException(nameof(validator)));
        return this;
    }

    /// <summary>预检列结构，不读取数据值；结构歧义抛出 TabularMappingException。</summary>
    /// <param name="columnNames">有序列名；无表头的列可以为 null。</param>
    /// <param name="location">可选的结构位置。</param>
    /// <returns>缺失必需列及未知列问题。</returns>
    public IReadOnlyList<DataIssue> ValidateColumns(IReadOnlyList<string> columnNames, DataLocation location = null)
    {
        EnsureSealed();
        if (columnNames == null) throw new ArgumentNullException(nameof(columnNames));
        return Array.AsReadOnly(GetSchemaIssues(GetPlan(columnNames.ToArray()), location).ToArray());
    }

    /// <summary>映射一行数据；数据问题返回结果，取消原样传播，配置歧义在读取值前抛出。</summary>
    /// <param name="row">原始数据行；本方法不接管其所属资源。</param>
    /// <param name="cancellationToken">在每个字段和校验阶段观察的取消令牌。</param>
    /// <returns>成功对象或包含一项以上错误的失败结果。</returns>
    public RowResult<T> Map(ITabularRow row, CancellationToken cancellationToken = default)
    {
        EnsureSealed();
        if (row == null) throw new ArgumentNullException(nameof(row));
        cancellationToken.ThrowIfCancellationRequested();
        var location = row.Location ?? new TabularLocation(row.RowNumber);
        var names = new string[row.Count];
        for (var index = 0; index < names.Length; index++) names[index] = row.GetName(index);
        var plan = GetPlan(names);
        var issues = GetSchemaIssues(plan, location);
        var values = new object[_fields.Count];
        for (var index = 0; index < _fields.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var field = _fields[index];
            var ordinal = plan.Ordinals[index];
            if (ordinal < 0) continue;
            var name = plan.Names[ordinal];
            object rawValue;
            try { rawValue = row.GetValue(ordinal); }
            catch (Exception error)
            {
                issues.Add(Issue("mapping.conversion", $"读取源字段“{name ?? field.PrimarySourceName}”失败。",
                    location, name, ordinal, field.MemberName, error, stage: OperationStage.Read));
                continue;
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (field.RequiredValue && (rawValue == null || rawValue == DBNull.Value || rawValue is string text && string.IsNullOrWhiteSpace(text)))
            {
                issues.Add(Issue("mapping.required_value", $"源字段“{name ?? field.PrimarySourceName}”不能为空。", location, name, ordinal, field.MemberName));
                continue;
            }
            try { values[index] = field.Converter(rawValue, _options.Culture); }
            catch (Exception error)
            {
                issues.Add(Issue("mapping.conversion", $"源字段“{name ?? field.PrimarySourceName}”无法转换为目标成员“{field.MemberName}”。",
                    location, name, ordinal, field.MemberName, error));
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (HasErrors(issues)) return RowResult<T>.Failure(issues, location);

        // 对值类型仅装箱一次，缓存的 setter 在同一个盒内赋值，最后再拆箱。
        object target;
        try { target = new T(); }
        catch (Exception error)
        {
            issues.Add(Issue("mapping.validation", "创建目标对象失败。", location, exception: error));
            return RowResult<T>.Failure(issues, location);
        }
        for (var index = 0; index < _fields.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ordinal = plan.Ordinals[index];
            if (ordinal < 0) continue;
            try { _fields[index].Setter(target, values[index]); }
            catch (Exception error)
            {
                issues.Add(Issue("mapping.validation", $"写入目标成员“{_fields[index].MemberName}”失败。", location,
                    plan.Names[ordinal], ordinal, _fields[index].MemberName, error));
            }
        }
        if (!HasErrors(issues)) ValidateTarget((T)target, plan, location, issues, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return HasErrors(issues) ? RowResult<T>.Failure(issues, location) : RowResult<T>.Success((T)target, location, issues);
    }

    private void Seal()
    {
        var aliases = new Dictionary<string, string>(_options.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var ordinals = new HashSet<int>();
        foreach (var field in _fields)
        {
            if (field.SourceOrdinal.HasValue)
            {
                if (!ordinals.Add(field.SourceOrdinal.Value)) throw Duplicate($"源序号 {field.SourceOrdinal.Value} 被重复映射。");
            }
            else
            {
                foreach (var name in field.SourceNames.DefaultIfEmpty(field.MemberName))
                {
                    if (aliases.TryGetValue(name, out var existing) && existing != field.MemberName)
                        throw Duplicate($"源列“{name}”同时映射到“{existing}”和“{field.MemberName}”。");
                    aliases[name] = field.MemberName;
                }
            }
            var instance = Expression.Parameter(typeof(object), "target");
            var value = Expression.Parameter(typeof(object), "value");
            Expression typedInstance = typeof(T).IsValueType ? Expression.Unbox(instance, typeof(T)) : Expression.Convert(instance, typeof(T));
            var assign = Expression.Assign(Expression.MakeMemberAccess(typedInstance, field.Member), Expression.Convert(value, field.MemberType));
            field.Setter = Expression.Lambda<Action<object, object>>(assign, instance, value).Compile();
            field.Converter ??= TabularValueConverter.Create(field.MemberType);
            field.IsSealed = true;
        }
        _sealed = true;
    }

    private MappingPlan GetPlan(string[] names)
    {
        var key = new System.Text.StringBuilder();
        foreach (var name in names) key.Append(name?.Length ?? -1).Append(':').Append(name).Append('|');
        return _plans.GetOrAdd(key.ToString(), _ => new Lazy<MappingPlan>(() => BuildPlan(names))).Value;
    }

    private MappingPlan BuildPlan(string[] names)
    {
        var comparer = _options.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var ordinals = Enumerable.Repeat(-1, _fields.Count).ToArray();
        var used = new HashSet<int>();
        for (var fieldIndex = 0; fieldIndex < _fields.Count; fieldIndex++)
        {
            var field = _fields[fieldIndex];
            var selected = -1;
            if (field.SourceOrdinal.HasValue)
            {
                if (field.SourceOrdinal.Value < names.Length) selected = field.SourceOrdinal.Value;
            }
            else
            {
                var candidates = field.SourceNames.DefaultIfEmpty(field.MemberName).ToArray();
                for (var ordinal = 0; ordinal < names.Length; ordinal++)
                {
                    if (!candidates.Any(candidate => comparer.Equals(candidate, names[ordinal]))) continue;
                    if (selected >= 0) throw Duplicate($"目标成员“{field.MemberName}”匹配多个源列，请改用唯一名称或序号。");
                    selected = ordinal;
                }
            }
            if (selected >= 0 && !used.Add(selected)) throw Duplicate($"源字段序号 {selected} 同时绑定到多个目标成员。");
            ordinals[fieldIndex] = selected;
        }
        return new MappingPlan(names, ordinals, used);
    }

    private List<DataIssue> GetSchemaIssues(MappingPlan plan, DataLocation location)
    {
        var issues = new List<DataIssue>();
        for (var index = 0; index < _fields.Count; index++)
        {
            var field = _fields[index];
            if (plan.Ordinals[index] < 0 && field.RequiredColumn)
                issues.Add(Issue("mapping.missing_column", $"目标成员“{field.MemberName}”缺少必需源列。",
                    location, field.SourceOrdinal.HasValue ? null : field.PrimarySourceName,
                    ordinal: field.SourceOrdinal, targetMember: field.MemberName));
        }
        if (_options.UnknownColumns == UnknownColumnBehavior.Ignore) return issues;
        var severity = _options.UnknownColumns == UnknownColumnBehavior.Error ? DataIssueSeverity.Error : DataIssueSeverity.Warning;
        for (var ordinal = 0; ordinal < plan.Names.Length; ordinal++)
            if (!plan.UsedOrdinals.Contains(ordinal)) issues.Add(Issue("mapping.unknown_column", $"源字段序号 {ordinal} 未配置映射。", location, plan.Names[ordinal], ordinal, severity: severity));
        return issues;
    }

    private void ValidateTarget(T target, MappingPlan plan, DataLocation location, List<DataIssue> issues, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (_options.ValidateDataAnnotations)
        {
            try
            {
                var results = new List<ValidationResult>();
                object validationTarget = target;
                Validator.TryValidateObject(validationTarget, new ValidationContext(validationTarget), results, true);
                foreach (var result in results)
                foreach (var member in result.MemberNames.DefaultIfEmpty(null))
                {
                    var index = _fields.FindIndex(field => field.MemberName == member);
                    var ordinal = index < 0 ? -1 : plan.Ordinals[index];
                    issues.Add(Issue("mapping.validation", result.ErrorMessage ?? "目标对象校验失败。", location,
                        ordinal < 0 ? null : plan.Names[ordinal], ordinal < 0 ? (int?)null : ordinal, member));
                }
            }
            catch (Exception error) { issues.Add(Issue("mapping.validation", "目标对象注解校验执行失败。", location, exception: error)); }
        }
        foreach (var validator in _validators)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                // 在保护范围内枚举，延迟校验器的异常也形成行诊断。
                foreach (var message in validator(target) ?? Array.Empty<string>())
                {
                    token.ThrowIfCancellationRequested();
                    if (!string.IsNullOrWhiteSpace(message)) issues.Add(Issue("mapping.validation", message, location));
                }
            }
            catch (Exception error) { issues.Add(Issue("mapping.validation", "目标对象自定义校验执行失败。", location, exception: error)); }
        }
    }

    private static DataIssue Issue(string code, string message, DataLocation location, string sourceName = null,
        int? ordinal = null, string targetMember = null, Exception exception = null,
        DataIssueSeverity severity = DataIssueSeverity.Error, OperationStage stage = OperationStage.Transform)
    {
        while (exception is TargetInvocationException invocation && invocation.InnerException != null) exception = invocation.InnerException;
        if (exception is OperationCanceledException) ExceptionDispatchInfo.Capture(exception).Throw();
        if (ordinal.HasValue)
        {
            // 缺列诊断可引用尚不存在的序号；无法表示为 int 的一基列号时保留整行位置。
            if (location is CsvLocation csv && ordinal.Value < int.MaxValue)
                location = new CsvLocation(csv.RecordNumber, ordinal.Value + 1, sourceName);
            else if (location is TabularLocation table) location = new TabularLocation(table.RowNumber, ordinal.Value, sourceName);
        }
        return new DataIssue(code, severity, stage, message, location, sourceName, ordinal, targetMember, exception?.GetType().FullName);
    }

    private static bool HasErrors(IEnumerable<DataIssue> issues) => issues.Any(issue => issue.Severity == DataIssueSeverity.Error);
    private static TabularMappingException Duplicate(string message) => new("mapping.duplicate_mapping", message);
    private void EnsureMutable()
    {
        if (_sealed) throw new InvalidOperationException("映射配置已经创建，不能再修改。");
    }
    private void EnsureSealed()
    {
        if (!_sealed) throw new InvalidOperationException("必须先完成 Create 配置，才能执行映射或列预检。");
    }

    private sealed class MappingPlan
    {
        internal MappingPlan(string[] names, int[] ordinals, HashSet<int> usedOrdinals)
        {
            Names = names;
            Ordinals = ordinals;
            UsedOrdinals = usedOrdinals;
        }
        internal string[] Names { get; }
        internal int[] Ordinals { get; }
        internal HashSet<int> UsedOrdinals { get; }
    }
}
