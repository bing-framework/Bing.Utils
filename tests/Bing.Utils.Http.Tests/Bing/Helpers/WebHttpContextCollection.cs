namespace Bing.Helpers;

/// <summary>
/// 使用 Web.HttpContextAccessor 静态状态的测试集合
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class WebHttpContextCollection
{
    public const string Name = "WebHttpContextCollection";
}
