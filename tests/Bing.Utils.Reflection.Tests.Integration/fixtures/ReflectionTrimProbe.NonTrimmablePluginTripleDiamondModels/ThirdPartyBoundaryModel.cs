using System.ComponentModel;
using ReflectionTrimProbe.NonTrimmablePluginDependency;
using ReflectionTrimProbe.NonTrimmablePluginSecondaryDependency;
using ReflectionTrimProbe.NonTrimmablePluginTertiaryDependency;

namespace ReflectionTrimProbe.NonTrimmablePluginModels;

/// <summary>
/// 同时引用三条未声明 IsTrimmable 依赖边的第三方插件模型。
/// </summary>
[Description("Third-party non-trimmable type")]
public sealed class ThirdPartyBoundaryModel
{
    /// <summary>用于验证字段元数据和写入。</summary>
    [Description("Third-party value field")]
    public int Value;

    /// <summary>用于验证属性元数据和读写。</summary>
    [Description("Third-party name property")]
    public string Name { get; set; } = "initial";

    /// <summary>用于验证方法元数据和调用。</summary>
    [Description("Third-party execute method")]
    public int Execute(int value) => value + Value
        + ThirdPartyBoundaryDependency.GetBonus()
        + ThirdPartyBoundarySecondaryDependency.GetBonus()
        + ThirdPartyBoundaryTertiaryDependency.GetBonus();

    /// <summary>用于验证公共无参构造函数元数据。</summary>
    public ThirdPartyBoundaryModel()
    {
    }
}
