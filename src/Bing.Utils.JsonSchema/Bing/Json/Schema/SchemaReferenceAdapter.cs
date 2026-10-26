using System.Text.Json;
using global::Json.Schema;
using global::Json.Schema.Keywords;
using EngineSchema = global::Json.Schema.JsonSchema;
using Draft202012DynamicRef = global::Json.Schema.Keywords.Draft202012.DynamicRefKeyword;

namespace Bing.Json.Schema;

/// <summary>通过引擎的关键字扩展接口修正 9.4.1 的动态回退与布尔引用位置。</summary>
internal static class SchemaReferenceAdapter
{
    internal static void Apply(IReadOnlyCollection<JsonSchemaNode> nodes, SchemaRegistry registry,
        SchemaErrorLocations locations)
    {
        var anchors = new Dictionary<(Uri Resource, string Anchor), JsonSchemaNode>();
        foreach (var node in nodes)
        foreach (var keyword in node.Keywords.Where(x => x.Handler is DynamicAnchorKeyword))
            anchors[(node.BaseUri, keyword.RawValue.GetString()!)] = node;

        foreach (var node in nodes)
        {
            for (var i = 0; i < node.Keywords.Length; i++)
            {
                var keyword = node.Keywords[i];
                if (keyword.Handler is not (RefKeyword or Draft202012DynamicRef))
                    continue;

                var reference = new Uri(node.BaseUri, keyword.RawValue.GetString());
                JsonSchemaNode? initialTarget = null;
                if (keyword.Handler is Draft202012DynamicRef && keyword.Subschemas.Length == 0)
                {
                    // 检索 URI 可以是别名；动态锚点归属于注册文档的规范资源 URI。
                    var resource = registry.Get(reference);
                    var anchor = reference.Fragment.TrimStart('#');
                    if (resource is null || !anchors.TryGetValue((resource.BaseUri, anchor), out initialTarget))
                        throw new JsonSchemaConfigurationException($"无法解析动态引用：{reference}。");
                }

                // 只适配当前校验器构建的节点，不修改宿主全局注册表中的共享节点。
                if (!ReferenceEquals(BuildContext.From(keyword).Options.SchemaRegistry, registry))
                    continue;

                IKeywordHandler? handler = null;
                if (initialTarget is not null)
                    handler = new DynamicReference(initialTarget, reference.Fragment[1..], anchors);
                else if (keyword.Subschemas.FirstOrDefault()?.Source.ValueKind == JsonValueKind.False)
                {
                    var document = registry.Get(reference)!;
                    var documentUri = document is EngineSchema { BoolValue: not null }
                        ? reference
                        : document.BaseUri;
                    var location = locations.GetBooleanSchemaLocation(keyword.Subschemas[0])
                        ?? new Uri(documentUri, reference.Fragment.Length == 0 ? "#" : reference.Fragment);
                    handler = keyword.Handler is Draft202012DynamicRef
                        ? new BooleanDynamicReference(location, locations)
                        : new BooleanReference(location, locations);
                }

                if (handler is not null)
                    node.Keywords[i] = ReplaceHandler(keyword, handler, node.BaseUri);
            }
        }
    }

    internal static KeywordData ReplaceHandler(KeywordData original, IKeywordHandler handler, Uri baseUri)
    {
        // KeywordData 的构造器不是公开 API；用原上下文的局部方言生成替代关键字，
        // 保留原始引用数据和已解析目标，不注册新资源，也不改变全局方言。
        var context = BuildContext.From(original);
        context.BaseUri = baseUri;
        context.Dialect = context.Dialect.With([handler]);
        context.LocalSchema = JsonSerializer.SerializeToElement(
            new Dictionary<string, JsonElement> { [handler.Name] = original.RawValue });
        var replacement = EngineSchema.BuildNode(context).Keywords.Single();
        replacement.Value = original.Value;
        replacement.Subschemas = original.Subschemas;
        return replacement;
    }

    /// <summary>2020-12 动态引用先确定初始目标，再由最外层同名动态锚点覆盖。</summary>
    private sealed class DynamicReference(
        JsonSchemaNode initialTarget,
        string anchor,
        IReadOnlyDictionary<(Uri Resource, string Anchor), JsonSchemaNode> anchors) : Draft202012DynamicRef
    {
        public override KeywordEvaluation Evaluate(KeywordData keyword, EvaluationContext context)
        {
            var target = initialTarget;
            foreach (var resource in context.Scope.Reverse())
            {
                if (!anchors.TryGetValue((resource, anchor), out var candidate))
                    continue;
                target = candidate;
                break;
            }

            var result = target.Evaluate(context with
            {
                EvaluationPath = context.EvaluationPath.Combine(Name)
            });
            return new KeywordEvaluation { Keyword = Name, IsValid = result.IsValid, Details = [result] };
        }
    }

    /// <summary>记录静态引用到布尔 Schema 时的文档位置，不修改引擎共享布尔节点。</summary>
    private sealed class BooleanReference(Uri location, SchemaErrorLocations locations) : RefKeyword
    {
        public override KeywordEvaluation Evaluate(KeywordData keyword, EvaluationContext context)
        {
            var result = base.Evaluate(keyword, context);
            locations.Record(result.Details![0], location);
            return result;
        }
    }

    /// <summary>动态引用退化为指针或文档引用时，同样保留布尔目标的位置。</summary>
    private sealed class BooleanDynamicReference(Uri location, SchemaErrorLocations locations) : Draft202012DynamicRef
    {
        public override KeywordEvaluation Evaluate(KeywordData keyword, EvaluationContext context)
        {
            var result = base.Evaluate(keyword, context);
            locations.Record(result.Details![0], location);
            return result;
        }
    }
}
