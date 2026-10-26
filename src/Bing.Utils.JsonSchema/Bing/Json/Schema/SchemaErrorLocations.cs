using global::Json.Schema;

namespace Bing.Json.Schema;

/// <summary>保存布尔 Schema 的编译位置和本次引用结果的位置；求值状态由实例锁保护。</summary>
internal sealed class SchemaErrorLocations
{
    private readonly Dictionary<JsonSchemaNode, Uri> _booleanSchemas = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<EvaluationResults, Uri> _references = new(ReferenceEqualityComparer.Instance);

    internal void RegisterBooleanSchema(JsonSchemaNode node, Uri location) => _booleanSchemas[node] = location;

    internal Uri? GetBooleanSchemaLocation(JsonSchemaNode node) => _booleanSchemas.GetValueOrDefault(node);

    internal void Record(EvaluationResults result, Uri location) => _references[result] = location;

    internal string Get(EvaluationResults result) => _references.TryGetValue(result, out var location)
        ? location.OriginalString
        : result.SchemaLocation.OriginalString;

    // 编译位置属于不可变 Schema 元数据；仅释放单次求值产生的结果树。
    internal void Clear() => _references.Clear();
}
