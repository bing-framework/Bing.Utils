using Bing.Diagnostics;

namespace Bing.Tabular;

/// <summary>
/// 表示一行数据的映射结果及其诊断信息。
/// </summary>
/// <typeparam name="T">目标对象类型。</typeparam>
public sealed class RowResult<T>
{
    private RowResult(T value, IReadOnlyList<DataIssue> issues, DataLocation location)
    {
        Value = value;
        Issues = issues ?? Array.Empty<DataIssue>();
        Location = location;
        IsSuccess = !Issues.Any(issue => issue.Severity == DataIssueSeverity.Error);
    }

    /// <summary>获取映射是否成功。只有错误级别问题会使结果失败。</summary>
    public bool IsSuccess { get; }
    /// <summary>获取成功时的目标对象；失败时为默认值。</summary>
    public T Value { get; }
    /// <summary>获取当前行的全部问题。</summary>
    public IReadOnlyList<DataIssue> Issues { get; }
    /// <summary>获取当前行的位置。</summary>
    public DataLocation Location { get; }

    /// <summary>创建成功结果。</summary>
    public static RowResult<T> Success(T value, DataLocation location = null, IEnumerable<DataIssue> issues = null)
    {
        var snapshot = SnapshotIssues(issues);
        if (snapshot.Any(issue => issue.Severity == DataIssueSeverity.Error))
            throw new ArgumentException("成功结果不能包含错误级别问题。", nameof(issues));
        return new RowResult<T>(value, snapshot, location);
    }

    /// <summary>创建失败结果；失败结果不会携带半成品目标对象。</summary>
    public static RowResult<T> Failure(IEnumerable<DataIssue> issues, DataLocation location = null)
    {
        var snapshot = SnapshotIssues(issues);
        if (!snapshot.Any(issue => issue.Severity == DataIssueSeverity.Error))
            throw new ArgumentException("失败结果至少需要一个错误级别问题。", nameof(issues));
        return new RowResult<T>(default, snapshot, location);
    }

    private static IReadOnlyList<DataIssue> SnapshotIssues(IEnumerable<DataIssue> issues)
    {
        if (issues == null) return Array.Empty<DataIssue>();
        var snapshot = issues.ToArray();
        if (snapshot.Any(issue => issue == null)) throw new ArgumentException("问题列表不能包含 null。", nameof(issues));
        return Array.AsReadOnly(snapshot);
    }
}
