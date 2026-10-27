namespace Bing.Diagnostics;

/// <summary>
/// 表示 JSON 文档中的 JSON Pointer 位置。
/// </summary>
public sealed class JsonPointerLocation : DataLocation
{
    /// <summary>
    /// 初始化 JSON Pointer 位置；空字符串表示根节点。
    /// </summary>
    /// <param name="pointer">JSON Pointer。</param>
    public JsonPointerLocation(string pointer)
    {
        if (pointer == null) throw new ArgumentNullException(nameof(pointer));
        if (pointer.Length > 0 && pointer[0] != '/')
            throw new ArgumentException("JSON Pointer 必须为空字符串或以“/”开头。", nameof(pointer));

        Pointer = pointer;
    }

    /// <inheritdoc />
    public override string Kind => "json-pointer";

    /// <summary>
    /// 获取 JSON Pointer；空字符串表示根节点。
    /// </summary>
    public string Pointer { get; }

    /// <summary>
    /// 获取与 JSON Schema 错误模型兼容的实例路径别名。
    /// </summary>
    public string InstancePath => Pointer;

    /// <inheritdoc />
    public override string ToString() => Pointer;
}
