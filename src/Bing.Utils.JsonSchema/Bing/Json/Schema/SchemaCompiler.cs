using System.Text.Json;
using System.Text.Json.Nodes;
using global::Json.Schema;
using global::Json.Schema.Keywords;
using global::Json.Schema.Keywords.Draft201909;
using EngineSchema = global::Json.Schema.JsonSchema;

namespace Bing.Json.Schema;

/// <summary>在私有注册表中分两阶段装入文档并完成静态引用检查。</summary>
internal static class SchemaCompiler
{
    private static readonly Uri DefaultBaseUri = new("https://bing-utils.invalid/schema");
    private static readonly HashSet<Uri> SupportedDialects =
    [
        new("http://json-schema.org/draft-06/schema#"),
        new("http://json-schema.org/draft-07/schema#"),
        new("https://json-schema.org/draft/2019-09/schema"),
        new("https://json-schema.org/draft/2020-12/schema")
    ];

    internal static (EngineSchema Schema, SchemaErrorLocations Locations) Compile(JsonElement root, JsonSchemaValidatorOptions options)
    {
        var dialect = options.DefaultDialect switch
        {
            JsonSchemaDialect.Draft6 => Dialect.Draft06,
            JsonSchemaDialect.Draft7 => Dialect.Draft07,
            JsonSchemaDialect.Draft201909 => Dialect.Draft201909,
            JsonSchemaDialect.Draft202012 => Dialect.Draft202012,
            _ => throw new ArgumentOutOfRangeException(nameof(options), "不支持指定的默认 Schema 方言。")
        };
        var baseUri = options.BaseUri ?? DefaultBaseUri;
        CheckDocumentUri(baseUri, nameof(options.BaseUri));
        var references = options.ReferencedSchemas.ToArray();
        foreach (var reference in references)
        {
            CheckDocumentUri(reference.Key, nameof(options.ReferencedSchemas));
            if (reference.Value is null)
                throw new ArgumentException("引用 Schema 文本不能为 null。", nameof(options.ReferencedSchemas));
        }

        var registry = new SchemaRegistry { Fetch = (uri, _) => throw new JsonSchemaConfigurationException(
            $"未注册引用 Schema：{uri}。校验器不会自动读取网络或文件。") };
        var buildOptions = new BuildOptions
        {
            Dialect = dialect,
            SchemaRegistry = registry,
            DialectRegistry = new DialectRegistry(),
            VocabularyRegistry = new VocabularyRegistry()
        };
        try
        {
            CheckDialect(root);
            var documents = new SchemaDocumentStore(buildOptions);
            documents.Add(root, baseUri);
            foreach (var reference in references)
            {
                using var document = JsonDocument.Parse(reference.Value);
                CheckDialect(document.RootElement);
                documents.Add(document.RootElement.Clone(), reference.Key);
            }

            documents.Prepare();

            // 只构建引用图，不以某一个样例数据决定哪些分支需要检查。
            // 引擎的 Build 会遍历 allOf 的全部目标并进行引用环检查。
            var links = new JsonArray { new JsonObject { ["$ref"] = baseUri.OriginalString } };
            foreach (var reference in references)
                links.Add(new JsonObject { ["$ref"] = reference.Key.OriginalString });
            var preparation = new JsonObject { ["allOf"] = links };
            using var preparationDocument = JsonDocument.Parse(preparation.ToJsonString());
            var prepared = EngineSchema.Build(preparationDocument.RootElement.Clone(), buildOptions,
                new Uri("urn:bing-utils:schema-preparation:" + Guid.NewGuid().ToString("N")));
            var nodes = CheckPreparedGraph(prepared.Root);
            var locations = new SchemaErrorLocations();
            SchemaResourceAdapter.Apply(documents.Roots, locations);

            // 引擎 9.4.1 的引用同级开关属于整个求值上下文。使用允许同级规则的
            // 私有入口，旧方言的忽略行为已按实际节点方言处理，跨资源时不再串用。
            var entry = JsonSerializer.SerializeToElement(
                new Dictionary<string, string> { ["$ref"] = baseUri.OriginalString });
            var evaluationSchema = EngineSchema.Build(entry, new BuildOptions
            {
                Dialect = Dialect.Draft202012,
                SchemaRegistry = registry,
                DialectRegistry = buildOptions.DialectRegistry,
                VocabularyRegistry = buildOptions.VocabularyRegistry
            }, new Uri("urn:bing-utils:schema-evaluation:" + Guid.NewGuid().ToString("N")));
            SchemaReferenceAdapter.Apply(nodes.Append(evaluationSchema.Root).ToArray(), registry, locations);
            return (evaluationSchema, locations);
        }
        catch (JsonSchemaConfigurationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException
            or InvalidOperationException or global::Json.Schema.JsonSchemaException or RefResolutionException
            or FormatException)
        {
            throw new JsonSchemaConfigurationException("Schema 配置无效：" + exception.Message, exception);
        }
    }

    private static IReadOnlyCollection<JsonSchemaNode> CheckPreparedGraph(JsonSchemaNode root)
    {
        var visited = new HashSet<JsonSchemaNode>();
        var pending = new Stack<JsonSchemaNode>();
        pending.Push(root);
        while (pending.TryPop(out var node))
        {
            if (!visited.Add(node))
                continue;
            CheckDialect(node.Source);
            foreach (var keyword in node.Keywords)
            {
                if (keyword.Handler is RefKeyword && keyword.Subschemas.Length == 0
                    && !(keyword.Handler is RecursiveRefKeyword && keyword.Value is null))
                    throw new JsonSchemaConfigurationException(
                        $"无法解析引用 {keyword.RawValue}，所在 Schema：{node.BaseUri}。");
                foreach (var child in keyword.Subschemas)
                    pending.Push(child);
            }
        }
        return visited;
    }

    private static void CheckDialect(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object || !schema.TryGetProperty("$schema", out var declaration))
            return;
        if (declaration.ValueKind != JsonValueKind.String
            || !Uri.TryCreate(declaration.GetString(), UriKind.Absolute, out var dialect)
            || dialect.Fragment.Length > 1
            || !SupportedDialects.Contains(dialect))
            throw new JsonSchemaConfigurationException($"不支持的 Schema 方言：{declaration}。");
    }

    private static void CheckDocumentUri(Uri? uri, string parameter)
    {
        if (uri is null || !uri.IsAbsoluteUri || uri.Fragment.Length > 1)
            throw new ArgumentException("Schema 文档 URI 必须是绝对 URI，且不能包含非空片段。", parameter);
    }
}
