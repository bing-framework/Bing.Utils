namespace Bing.Json.Schema;

/// <summary>一次只读 JSON 校验的不可变结果，不持有输入文档。</summary>
public sealed class JsonSchemaValidationResult
{
    /// <summary>获取校验状态。</summary>
    public JsonSchemaValidationStatus Status { get; }

    /// <summary>获取输入是否通过校验。</summary>
    public bool IsValid => Status == JsonSchemaValidationStatus.Valid;

    /// <summary>获取分层规则错误；成功或 JSON 语法错误时为空。</summary>
    public IReadOnlyList<JsonSchemaValidationError> Errors { get; }

    /// <summary>获取 JSON 语法错误；其他状态为 null。</summary>
    public JsonSchemaSyntaxError? SyntaxError { get; }

    internal JsonSchemaValidationResult(JsonSchemaValidationStatus status,
        IEnumerable<JsonSchemaValidationError>? errors = null, JsonSchemaSyntaxError? syntaxError = null)
    {
        Status = status;
        Errors = Array.AsReadOnly(errors?.ToArray() ?? []);
        SyntaxError = syntaxError;
    }
}
