namespace ReflectionTrimProbe.PluginModels;

/// <summary>
/// 由独立 linker root 保留的第二个跨程序集反射模型。
/// </summary>
public sealed class PluginModel
{
    private PluginNode Child { get; }

    public PluginModel(int value) => Child = new PluginNode(value);
}

/// <summary>
/// 提供嵌套的私有属性，验证成员路径的显式非公开访问。
/// </summary>
public sealed class PluginNode
{
    private int Secret { get; set; }

    public PluginNode(int value) => Secret = value;
}
