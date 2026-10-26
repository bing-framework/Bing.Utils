using System.Text.Json;
using global::Json.Pointer;
using global::Json.Schema;
using global::Json.Schema.Keywords;

namespace Bing.Json.Schema;

/// <summary>按文档结构修正资源内路径，并保留各节点所属方言的引用语义。</summary>
internal static class SchemaResourceAdapter
{
    internal static void Apply(IEnumerable<(JsonSchemaNode Node, Uri Resource, JsonPointer Path)> roots,
        SchemaErrorLocations locations)
    {
        var visited = new HashSet<JsonSchemaNode>();
        foreach (var (node, resource, path) in roots)
            Visit(node, resource, path, visited, locations);
    }

    private static void Visit(JsonSchemaNode node, Uri resourceUri, JsonPointer path,
        HashSet<JsonSchemaNode> visited, SchemaErrorLocations locations)
    {
        if (!visited.Add(node))
            return;

        if (node.Source.ValueKind == JsonValueKind.Object && node.BaseUri != resourceUri)
        {
            resourceUri = node.BaseUri;
            path = JsonPointer.Empty;
        }

        // 仅使用引擎公开的高级路径属性，避免修改源码 JSON 或依赖私有字段。
#pragma warning disable CS0618
        node.PathFromResourceRoot = path;
#pragma warning restore CS0618
        if (node.Source.ValueKind == JsonValueKind.False)
            locations.RegisterBooleanSchema(node, new Uri(resourceUri, "#" + path));

        var reference = node.Keywords.FirstOrDefault(x => x.Handler is RefKeyword && x.Handler.Name == "$ref");
        var ignoreSiblings = reference is not null && BuildContext.From(reference).Dialect.RefIgnoresSiblingKeywords;
        for (var i = 0; i < node.Keywords.Length; i++)
        {
            var keyword = node.Keywords[i];
            // 引用边不是文档包含关系；目标路径由其原始文档单独遍历。
            if (keyword.Handler is not (RefKeyword or DynamicRefKeyword))
            {
                foreach (var child in keyword.Subschemas)
                    Visit(child, resourceUri, path.Combine(keyword.Handler.Name).Combine(child.RelativePath), visited, locations);
            }

            if (ignoreSiblings && !ReferenceEquals(keyword, reference))
                node.Keywords[i] = SchemaReferenceAdapter.ReplaceHandler(
                    keyword, new IgnoredSibling(keyword.Handler.Name), node.BaseUri);
        }
    }

    /// <summary>旧方言中 $ref 的同级规则不参与求值；保留其结构以支持引用寻址。</summary>
    private sealed class IgnoredSibling(string name) : IKeywordHandler
    {
        public string Name => name;
        public object? ValidateKeywordValue(JsonElement value) => null;
        public void BuildSubschemas(KeywordData keyword, BuildContext context) { }
        public KeywordEvaluation Evaluate(KeywordData keyword, EvaluationContext context) => KeywordEvaluation.Ignore;
    }
}
