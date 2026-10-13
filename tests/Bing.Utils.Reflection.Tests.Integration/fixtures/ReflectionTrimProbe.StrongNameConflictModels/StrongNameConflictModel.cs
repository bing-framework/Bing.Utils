namespace ReflectionTrimProbe.StrongNameConflictModels;

/// <summary>引用同名未签名依赖的外部插件模型。</summary>
public sealed class StrongNameConflictModel
{
    /// <summary>调用冲突依赖并返回固定结果。</summary>
    public int GetDependencyMarker() =>
        global::ReflectionTrimProbe.StrongNameConflictDependency.StrongNameConflictDependency.GetMarker();
}
