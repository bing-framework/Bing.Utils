using System.ComponentModel;

namespace ReflectionTrimProbe.StrongNameConflictDependency;

/// <summary>使用系统程序集名称但不携带强名称标识的冲突依赖夹具。</summary>
[Description("Unsigned System.Text.Json name conflict dependency")]
public static class StrongNameConflictDependency
{
    /// <summary>返回用于确认实际加载程序集的固定标记。</summary>
    public static int GetMarker() => 41;
}
