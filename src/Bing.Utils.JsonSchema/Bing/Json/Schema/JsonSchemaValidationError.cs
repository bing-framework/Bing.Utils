namespace Bing.Json.Schema;

/// <summary>描述一个失败规则及其相关的子分支诊断。</summary>
public sealed class JsonSchemaValidationError
{
    /// <summary>获取数据的 JSON Pointer；空字符串表示根值。</summary>
    public string InstancePath { get; }

    /// <summary>获取失败规则所属 Schema 的绝对 URI 和片段。</summary>
    public string SchemaLocation { get; }

    /// <summary>获取失败关键字；布尔 false Schema 使用空字符串。</summary>
    public string Keyword { get; }

    /// <summary>获取用于展示的错误说明；程序逻辑应使用关键字及路径。</summary>
    public string Message { get; }

    /// <summary>获取该规则下的失败分支；成功分支的内部失败不会列入其中。</summary>
    public IReadOnlyList<JsonSchemaValidationError> Children { get; }

    internal JsonSchemaValidationError(string instancePath, string schemaLocation, string keyword,
        string message, IEnumerable<JsonSchemaValidationError> children)
    {
        InstancePath = instancePath;
        SchemaLocation = schemaLocation;
        Keyword = keyword;
        Message = message;
        Children = Array.AsReadOnly(children.ToArray());
    }
}
