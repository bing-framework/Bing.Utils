using System.Text.Json;
using System.Text.Json.Nodes;
using global::Json.Pointer;
using global::Json.Schema;
using global::Json.Schema.Keywords;
using EngineSchema = global::Json.Schema.JsonSchema;

namespace Bing.Json.Schema;

/// <summary>先构建私有文档结构和资源快照，再允许引擎解析引用，避免写入共享目标。</summary>
internal sealed class SchemaDocumentStore
{
    private readonly BuildOptions _options;
    private readonly BuildContext _context;
    private readonly List<Document> _documents = [];
    private readonly HashSet<JsonSchemaNode> _scanned = [];

    internal SchemaDocumentStore(BuildOptions options)
    {
        _options = options;
        // 通过公开 API 获取构建上下文；此种子不含引用，不触及宿主资源。
        var seed = EngineSchema.Build(JsonSerializer.SerializeToElement(new { title = "" }), options,
            new Uri("urn:bing-utils:schema-context:" + Guid.NewGuid().ToString("N")));
        _context = BuildContext.From(seed.Root.Keywords.Single());
    }

    internal IEnumerable<(JsonSchemaNode Node, Uri Resource, JsonPointer Path)> Roots =>
        _documents.SelectMany(x => x.Roots);

    internal void Add(JsonElement source, Uri retrievalUri, Dialect? dialect = null)
    {
        var resourceUri = retrievalUri;
        if (source.ValueKind == JsonValueKind.Object && source.TryGetProperty("$id", out var id))
        {
            resourceUri = new Uri(retrievalUri, id.GetString());
            // Draft 6/7 依赖原始 #anchor 形式登记锚点，不能改成绝对 URI。
            if (!id.GetString()!.StartsWith('#'))
            {
                var normalized = JsonNode.Parse(source.GetRawText())!.AsObject();
                normalized["$id"] = resourceUri.OriginalString;
                source = JsonSerializer.SerializeToElement(normalized);
            }
        }

        var context = _context with
        {
            BaseUri = resourceUri,
            LocalSchema = source,
            Dialect = dialect ?? _options.Dialect
        };
        var document = new Document(context, ScanReferences);
        _options.SchemaRegistry.Register(retrievalUri, document);
        _documents.Add(document);
    }

    internal void Prepare()
    {
        // 先登记所有显式文档的内嵌资源，再扫描外部引用，确保实例文档优先。
        foreach (var document in _documents)
            document.Build();
        for (var i = 0; i < _documents.Count; i++)
            ScanReferences(_documents[i].Root);
    }

    private void ScanReferences(JsonSchemaNode node)
    {
        if (!_scanned.Add(node))
            return;
        foreach (var keyword in node.Keywords)
        {
            if (keyword.Handler is RefKeyword or DynamicRefKeyword)
            {
                var reference = new Uri(node.BaseUri, keyword.RawValue.GetString());
                if (_options.SchemaRegistry.Get(reference) is not EngineSchema shared
                    || IsPrivate(shared))
                    continue;

                // 全局布尔单例没有自己的检索地址，使用当前引用的文档地址。
                var resourceUri = shared.BoolValue is null
                    ? shared.BaseUri
                    : new Uri(reference.OriginalString.Split('#')[0]);
                var existing = _options.SchemaRegistry.Get(resourceUri);
                if (existing is Document || existing is EngineSchema existingSchema && IsPrivate(existingSchema))
                {
                    _options.SchemaRegistry.Register(reference, existing);
                    continue;
                }

                var source = shared.Root.Source;
                var dialect = shared.Root.Keywords.FirstOrDefault() is { } first
                    ? BuildContext.From(first).Dialect
                    : _options.Dialect;
                // 已知规范地址，避免再次解析原始文档中的相对 $id。
                if (source.ValueKind == JsonValueKind.Object && source.TryGetProperty("$id", out var id)
                    && !id.GetString()!.StartsWith('#'))
                {
                    var normalized = JsonNode.Parse(source.GetRawText())!.AsObject();
                    normalized["$id"] = resourceUri.OriginalString;
                    source = JsonSerializer.SerializeToElement(normalized);
                }
                Add(source, resourceUri, dialect);
                var snapshot = _documents[^1];
                _options.SchemaRegistry.Register(reference, snapshot);
                snapshot.Build();
                // 不在这里调用引擎 Build；传递引用同样先复制，再统一解析。
                ScanReferences(snapshot.Root);
            }
            else
            {
                foreach (var child in keyword.Subschemas)
                    ScanReferences(child);
            }
        }
    }

    private bool IsPrivate(EngineSchema schema) =>
        schema.Root.Keywords.FirstOrDefault() is { } first
        && ReferenceEquals(BuildContext.From(first).Options.SchemaRegistry, _options.SchemaRegistry);

    /// <summary>缓存结构节点和指针目标；实现引擎公开文档接口，不提前解析引用。</summary>
    private sealed class Document(BuildContext context, Action<JsonSchemaNode> prepareReferences) : IBaseDocument
    {
        private readonly Dictionary<JsonPointer, JsonSchemaNode> _discovered = [];
        private JsonSchemaNode? _root;

        public Uri BaseUri => context.BaseUri;
        internal JsonSchemaNode Root => _root ?? throw new InvalidOperationException("Schema 文档尚未准备。");
        internal IEnumerable<(JsonSchemaNode Node, Uri Resource, JsonPointer Path)> Roots =>
            new[] { (Root, BaseUri, JsonPointer.Empty) }
                .Concat(_discovered.Select(x => (x.Value, BaseUri, x.Key)));

        internal void Build() => _root ??= EngineSchema.BuildNode(context);

        public JsonSchemaNode? FindSubschema(JsonPointer pointer, BuildContext _)
        {
            if (_discovered.TryGetValue(pointer, out var cached))
                return cached;
            var node = Root;
            var remaining = pointer;
            var found = true;
            while (remaining.SegmentCount != 0)
            {
                var keyword = node.Keywords.FirstOrDefault(x =>
                    remaining.StartsWith(JsonPointer.Create(x.Handler.Name)));
                if (keyword is null)
                {
                    found = false;
                    break;
                }
                remaining = remaining.GetLocal(remaining.SegmentCount - 1);
                var child = keyword.Subschemas.FirstOrDefault(x => remaining.StartsWith(x.RelativePath));
                if (child is null)
                {
                    found = false;
                    break;
                }
                node = child;
                remaining = remaining.GetLocal(remaining.SegmentCount - child.RelativePath.SegmentCount);
            }
            if (found)
                return node;

            // JSON Pointer 可以指向非标准关键字内的 Schema；仍使用所属文档方言构建。
            var source = pointer.Evaluate(Root.Source);
            if (source is null)
                return null;
            var targetContext = context with
            {
                LocalSchema = source.Value,
                Dialect = Root.Keywords.FirstOrDefault() is { } first
                    ? BuildContext.From(first).Dialect
                    : context.Dialect
            };
#pragma warning disable CS0618
            targetContext.PathFromResourceRoot = pointer;
            node = EngineSchema.BuildNode(targetContext);
            node.PathFromResourceRoot = pointer;
#pragma warning restore CS0618
            _discovered[pointer] = node;
            prepareReferences(node);
            return node;
        }
    }
}
