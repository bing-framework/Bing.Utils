#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bing.Json.Schema;
using Xunit;
using EngineBuildOptions = global::Json.Schema.BuildOptions;
using EngineDialect = global::Json.Schema.Dialect;
using EngineHandler = global::Json.Schema.IKeywordHandler;
using EngineKeywordData = global::Json.Schema.KeywordData;
using EngineNode = global::Json.Schema.JsonSchemaNode;
using EngineRegistry = global::Json.Schema.SchemaRegistry;
using EngineSchema = global::Json.Schema.JsonSchema;

namespace Bing.Utils.JsonSchema.Tests;

[Collection("Schema registry globals")]
public sealed class JsonSchemaValidatorGlobalDialectTests
{
    private const string Draft7Uri = "http://json-schema.org/draft-07/schema#";
    private const string Draft202012Uri = "https://json-schema.org/draft/2020-12/schema";

    public static IEnumerable<object[]> GlobalDialectCases()
    {
        foreach (var defaultDialect in new[] { JsonSchemaDialect.Draft7, JsonSchemaDialect.Draft202012 })
        {
            yield return new object[]
                { defaultDialect, Draft7Uri, JsonSchemaValidationStatus.Valid };
            yield return new object[]
                { defaultDialect, Draft202012Uri, JsonSchemaValidationStatus.SchemaMismatch };
        }
    }

    public static IEnumerable<object[]> LocalDefaultDialectCases()
    {
        yield return new object[] { JsonSchemaDialect.Draft7 };
        yield return new object[] { JsonSchemaDialect.Draft202012 };
    }

    [Theory]
    [MemberData(nameof(GlobalDialectCases))]
    public void F5_global_document_ref_siblings_follow_the_registered_documents_dialect(
        JsonSchemaDialect defaultDialect, string globalDialectUri, JsonSchemaValidationStatus expectedStatus)
    {
        var globalUri = NewUri("global-schema.json");
        var rootSchema = EngineSchema.FromText($$"""
            {
              "$schema": "{{globalDialectUri}}",
              "$id": "{{globalUri.AbsoluteUri}}",
              "definitions": { "base": { "type": "number" } },
              "$ref": "#/definitions/base",
              "minimum": 10
            }
            """, new EngineBuildOptions { SchemaRegistry = EngineRegistry.Global }, globalUri);
        Assert.Same(rootSchema, EngineRegistry.Global.Get(globalUri));
        var originalNodes = CaptureNodes(rootSchema);

        var validator = JsonSchemaValidator.Create(
            $$"""{ "$ref": "{{globalUri.AbsoluteUri}}" }""",
            new JsonSchemaValidatorOptions
            {
                BaseUri = NewUri("local-root.json"),
                DefaultDialect = defaultDialect
            });
        var result = validator.Validate("1");

        Assert.Equal(expectedStatus, result.Status);
        AssertGlobalSchemaUnchanged(rootSchema, originalNodes);
    }

    [Theory]
    [MemberData(nameof(LocalDefaultDialectCases))]
    public void F5_global_relative_id_alias_keeps_the_registered_inherited_dialect(
        JsonSchemaDialect defaultDialect)
    {
        var retrievalUri = NewUri("retrieval.json");
        var canonicalUri = new Uri(retrievalUri, "canonical.json");
        var rootSchema = EngineSchema.FromText("""
            {
              "$id": "canonical.json",
              "definitions": { "base": { "type": "number" } },
              "$ref": "#/definitions/base",
              "minimum": 10
            }
            """, new EngineBuildOptions
        {
            SchemaRegistry = EngineRegistry.Global,
            Dialect = EngineDialect.Draft202012
        }, retrievalUri);
        EngineRegistry.Global.Register(retrievalUri, rootSchema);

        Assert.Equal(canonicalUri, rootSchema.BaseUri);
        Assert.Same(rootSchema, EngineRegistry.Global.Get(canonicalUri));
        Assert.Same(rootSchema, EngineRegistry.Global.Get(retrievalUri));
        Assert.False(rootSchema.Root.Source.TryGetProperty("$schema", out _));
        var originalNodes = CaptureNodes(rootSchema);

        var validator = JsonSchemaValidator.Create(
            $$"""{ "$ref": "{{retrievalUri.AbsoluteUri}}" }""",
            new JsonSchemaValidatorOptions
            {
                BaseUri = NewUri("relative-id-local-root.json"),
                DefaultDialect = defaultDialect
            });
        var result = validator.Validate("1");

        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, result.Status);
        AssertGlobalSchemaUnchanged(rootSchema, originalNodes);
        Assert.Same(rootSchema, EngineRegistry.Global.Get(retrievalUri));
    }

    [Fact]
    public void F5_local_document_with_global_uri_should_not_mutate_another_global_documents_reference()
    {
        var bUri = NewUri("global-b.json");
        var aUri = NewUri("global-a.json");
        var globalB = EngineSchema.FromText($$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "$id": "{{bUri.AbsoluteUri}}",
              "type": "number"
            }
            """, new EngineBuildOptions { SchemaRegistry = EngineRegistry.Global }, bUri);
        var globalA = EngineSchema.FromText($$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "$id": "{{aUri.AbsoluteUri}}",
              "$ref": "{{bUri.AbsoluteUri}}"
            }
            """, new EngineBuildOptions { SchemaRegistry = EngineRegistry.Global }, aUri);
        var aRef = Assert.Single(globalA.Root.Keywords.Where(keyword => keyword.Handler.Name == "$ref"));
        var originalTarget = Assert.Single(aRef.Subschemas);
        Assert.Same(globalB.Root, originalTarget);
        var originalANodes = CaptureNodes(globalA);
        var originalBNodes = CaptureNodes(globalB);
        var options = new JsonSchemaValidatorOptions
        {
            BaseUri = NewUri("override-root.json"),
            DefaultDialect = JsonSchemaDialect.Draft202012
        };
        options.ReferencedSchemas.Add(bUri, $$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "$id": "{{bUri.AbsoluteUri}}",
              "type": "string"
            }
            """);

        var validator = JsonSchemaValidator.Create($$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "$ref": "{{aUri.AbsoluteUri}}"
            }
            """, options);

        Assert.Equal(JsonSchemaValidationStatus.Valid, validator.Validate("\"text\"").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, validator.Validate("1").Status);
        using (var number = JsonDocument.Parse("1"))
            Assert.True(globalA.Evaluate(number.RootElement).IsValid);
        Assert.Same(originalTarget, Assert.Single(aRef.Subschemas));
        AssertGlobalSchemaUnchanged(globalA, originalANodes);
        AssertGlobalSchemaUnchanged(globalB, originalBNodes);
    }

    [Fact]
    public void F5_chained_global_resources_and_direct_all_of_reference_should_preserve_original_nodes()
    {
        var cUri = NewUri("chain-c.json");
        var bUri = NewUri("chain-b.json");
        var aUri = NewUri("chain-a.json");
        var globalC = EngineSchema.FromText($$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "$id": "{{cUri.AbsoluteUri}}",
              "type": "number"
            }
            """, new EngineBuildOptions { SchemaRegistry = EngineRegistry.Global }, cUri);
        var globalB = EngineSchema.FromText($$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "$id": "{{bUri.AbsoluteUri}}",
              "$ref": "{{cUri.AbsoluteUri}}"
            }
            """, new EngineBuildOptions { SchemaRegistry = EngineRegistry.Global }, bUri);
        var globalA = EngineSchema.FromText($$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "$id": "{{aUri.AbsoluteUri}}",
              "$ref": "{{bUri.AbsoluteUri}}"
            }
            """, new EngineBuildOptions { SchemaRegistry = EngineRegistry.Global }, aUri);
        var originalANodes = CaptureNodes(globalA);
        var originalBNodes = CaptureNodes(globalB);
        var originalCNodes = CaptureNodes(globalC);

        var validator = JsonSchemaValidator.Create($$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "allOf": [
                { "$ref": "{{aUri.AbsoluteUri}}" },
                { "$ref": "{{cUri.AbsoluteUri}}" }
              ]
            }
            """, new JsonSchemaValidatorOptions
        {
            BaseUri = NewUri("chain-root.json"),
            DefaultDialect = JsonSchemaDialect.Draft202012
        });

        Assert.Equal(JsonSchemaValidationStatus.Valid, validator.Validate("1").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, validator.Validate("\"text\"").Status);
        AssertGlobalSchemaUnchanged(globalA, originalANodes);
        AssertGlobalSchemaUnchanged(globalB, originalBNodes);
        AssertGlobalSchemaUnchanged(globalC, originalCNodes);
    }

    private static Uri NewUri(string name) =>
        new($"https://schemas.example.test/global/{Guid.NewGuid():N}/{name}");

#pragma warning disable CS0618
    private static IReadOnlyList<NodeSnapshot> CaptureNodes(EngineSchema schema)
    {
        var snapshots = new List<NodeSnapshot>();
        var pending = new Stack<EngineNode>();
        var visited = new HashSet<EngineNode>();
        pending.Push(schema.Root);
        while (pending.TryPop(out var node))
        {
            if (!visited.Add(node))
                continue;

            var keywords = node.Keywords.Select(keyword => new KeywordSnapshot(
                keyword, keyword.Handler, keyword.Subschemas.ToArray())).ToArray();
            snapshots.Add(new NodeSnapshot(
                node,
                node.BaseUri,
                node.Source.GetRawText(),
                node.PathFromResourceRoot.ToString(),
                keywords));
            foreach (var child in keywords.SelectMany(keyword => keyword.Subschemas))
                pending.Push(child);
        }

        return snapshots;
    }
#pragma warning restore CS0618

    private static void AssertGlobalSchemaUnchanged(
        EngineSchema schema, IReadOnlyList<NodeSnapshot> originalNodes)
    {
        Assert.Same(schema, EngineRegistry.Global.Get(schema.BaseUri));
        var currentNodes = CaptureNodes(schema);
        Assert.Equal(originalNodes.Count, currentNodes.Count);
        for (var nodeIndex = 0; nodeIndex < originalNodes.Count; nodeIndex++)
        {
            var original = originalNodes[nodeIndex];
            var current = currentNodes[nodeIndex];
            Assert.Same(original.Node, current.Node);
            Assert.Equal(original.BaseUri, current.BaseUri);
            Assert.Equal(original.Source, current.Source);
            Assert.Equal(original.Path, current.Path);
            Assert.Equal(original.Keywords.Length, current.Keywords.Length);
            for (var keywordIndex = 0; keywordIndex < original.Keywords.Length; keywordIndex++)
            {
                var originalKeyword = original.Keywords[keywordIndex];
                var currentKeyword = current.Keywords[keywordIndex];
                Assert.Same(originalKeyword.Keyword, currentKeyword.Keyword);
                Assert.Same(originalKeyword.Handler, currentKeyword.Handler);
                Assert.Equal(originalKeyword.Subschemas.Length, currentKeyword.Subschemas.Length);
                for (var childIndex = 0; childIndex < originalKeyword.Subschemas.Length; childIndex++)
                    Assert.Same(originalKeyword.Subschemas[childIndex], currentKeyword.Subschemas[childIndex]);
            }
        }
    }

    private sealed record NodeSnapshot(
        EngineNode Node, Uri BaseUri, string Source, string Path, KeywordSnapshot[] Keywords);

    private sealed record KeywordSnapshot(
        EngineKeywordData Keyword, EngineHandler Handler, EngineNode[] Subschemas);
}
