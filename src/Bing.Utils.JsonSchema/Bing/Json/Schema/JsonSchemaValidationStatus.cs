namespace Bing.Json.Schema;

/// <summary>区分成功、JSON 语法错误和 Schema 规则不匹配。</summary>
public enum JsonSchemaValidationStatus
{
    /// <summary>JSON 满足 Schema。</summary>
    Valid,
    /// <summary>输入文本不是可解析的 JSON。</summary>
    InvalidJson,
    /// <summary>JSON 可解析，但不满足 Schema。</summary>
    SchemaMismatch
}
