namespace Bing.Helpers;

/// <summary>
/// 共用 <see cref="Web.HttpContextAccessor"/> 静态状态的串行测试集合。
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class WebHttpContextCollection
{
    /// <summary>
    /// 供测试类引用此串行集合的名称。
    /// </summary>
    public const string Name = "WebHttpContextCollection";
}
