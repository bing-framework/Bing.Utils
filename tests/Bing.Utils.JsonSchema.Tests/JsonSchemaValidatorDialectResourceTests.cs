#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Bing.Json.Schema;
using Xunit;

namespace Bing.Utils.JsonSchema.Tests;

/// <summary>覆盖 $ref 方言语义和嵌入资源诊断位置。</summary>
public sealed class JsonSchemaValidatorDialectResourceTests
{
    private const string Draft6Uri = "http://json-schema.org/draft-06/schema#";
    private const string Draft7Uri = "http://json-schema.org/draft-07/schema#";
    private const string Draft201909Uri = "https://json-schema.org/draft/2019-09/schema";
    private const string Draft202012Uri = "https://json-schema.org/draft/2020-12/schema";

    private static readonly (JsonSchemaDialect Dialect, string Uri, bool AppliesRefSibling)[] Dialects =
    [
        (JsonSchemaDialect.Draft6, Draft6Uri, false),
        (JsonSchemaDialect.Draft7, Draft7Uri, false),
        (JsonSchemaDialect.Draft201909, Draft201909Uri, true),
        (JsonSchemaDialect.Draft202012, Draft202012Uri, true)
    ];

    public static IEnumerable<object[]> RootDialectCases()
    {
        foreach (var defaultDialect in Enum.GetValues<JsonSchemaDialect>())
        foreach (var explicitDialect in Dialects)
            yield return new object[] { defaultDialect, explicitDialect.Uri, explicitDialect.AppliesRefSibling };
    }

    public static IEnumerable<object[]> DefaultDialectCases()
    {
        foreach (var dialect in Dialects)
            yield return new object[] { dialect.Dialect, dialect.AppliesRefSibling };
    }

    public static IEnumerable<object[]> ExternalDialectCases()
    {
        foreach (var rootDialect in new[] { Dialects[1], Dialects[3] })
        foreach (var targetDialect in new[] { Dialects[1], Dialects[3] })
            yield return new object[] { rootDialect.Uri, targetDialect.Uri, rootDialect.AppliesRefSibling };
    }

    public static IEnumerable<object[]> ExternalTargetDialectCases()
    {
        foreach (var rootDialect in new[] { Dialects[1], Dialects[3] })
        foreach (var targetDialect in new[] { Dialects[1], Dialects[3] })
            yield return new object[] { rootDialect.Uri, targetDialect.Uri, targetDialect.AppliesRefSibling };
    }

    [Theory]
    [MemberData(nameof(RootDialectCases))]
    public void F5_root_schema_dialect_controls_ref_sibling_semantics(
        JsonSchemaDialect defaultDialect, string explicitDialectUri, bool appliesRefSibling)
    {
        var validator = JsonSchemaValidator.Create(
            BuildSiblingSchema(explicitDialectUri),
            new JsonSchemaValidatorOptions { DefaultDialect = defaultDialect });

        Assert.Equal(
            appliesRefSibling ? JsonSchemaValidationStatus.SchemaMismatch : JsonSchemaValidationStatus.Valid,
            validator.Validate("{\"x\":1}").Status);
    }

    [Theory]
    [MemberData(nameof(DefaultDialectCases))]
    public void F5_missing_schema_uses_the_configured_default_dialect(
        JsonSchemaDialect defaultDialect, bool appliesRefSibling)
    {
        var validator = JsonSchemaValidator.Create(
            BuildSiblingSchema(schemaDialectUri: null),
            new JsonSchemaValidatorOptions { DefaultDialect = defaultDialect });

        Assert.Equal(
            appliesRefSibling ? JsonSchemaValidationStatus.SchemaMismatch : JsonSchemaValidationStatus.Valid,
            validator.Validate("{\"x\":1}").Status);
    }

    [Theory]
    [MemberData(nameof(ExternalDialectCases))]
    public void F5_external_target_dialect_does_not_replace_referring_node_dialect(
        string rootDialectUri, string targetDialectUri, bool appliesRefSibling)
    {
        var rootUri = new Uri("https://schemas.example.test/root.json");
        var targetUri = new Uri("https://schemas.example.test/target.json");
        var defaultDialect = rootDialectUri == Draft7Uri
            ? JsonSchemaDialect.Draft202012
            : JsonSchemaDialect.Draft7;
        var options = new JsonSchemaValidatorOptions { BaseUri = rootUri, DefaultDialect = defaultDialect };
        options.ReferencedSchemas.Add(targetUri, $$"""
            {
              "$schema": "{{targetDialectUri}}",
              "$id": "{{targetUri.AbsoluteUri}}",
              "type": "number"
            }
            """);

        var validator = JsonSchemaValidator.Create($$"""
            {
              "$schema": "{{rootDialectUri}}",
              "$id": "{{rootUri.AbsoluteUri}}",
              "$ref": "{{targetUri.AbsoluteUri}}",
              "minimum": 10
            }
            """, options);

        Assert.Equal(
            appliesRefSibling ? JsonSchemaValidationStatus.SchemaMismatch : JsonSchemaValidationStatus.Valid,
            validator.Validate("1").Status);
    }

    [Theory]
    [MemberData(nameof(ExternalTargetDialectCases))]
    public void F5_external_document_ref_siblings_follow_the_target_documents_dialect(
        string rootDialectUri, string targetDialectUri, bool targetAppliesRefSibling)
    {
        var rootUri = new Uri("https://schemas.example.test/external-root.json");
        var targetUri = new Uri("https://schemas.example.test/external-target.json");
        var defaultDialect = rootDialectUri == Draft7Uri
            ? JsonSchemaDialect.Draft202012
            : JsonSchemaDialect.Draft7;
        var options = new JsonSchemaValidatorOptions { BaseUri = rootUri, DefaultDialect = defaultDialect };
        options.ReferencedSchemas.Add(targetUri, $$"""
            {
              "$schema": "{{targetDialectUri}}",
              "$id": "{{targetUri.AbsoluteUri}}",
              "definitions": { "base": { "type": "number" } },
              "$ref": "#/definitions/base",
              "minimum": 10
            }
            """);

        var validator = JsonSchemaValidator.Create($$"""
            {
              "$schema": "{{rootDialectUri}}",
              "$id": "{{rootUri.AbsoluteUri}}",
              "$ref": "{{targetUri.AbsoluteUri}}"
            }
            """, options);

        Assert.Equal(
            targetAppliesRefSibling ? JsonSchemaValidationStatus.SchemaMismatch : JsonSchemaValidationStatus.Valid,
            validator.Validate("1").Status);
    }

    [Theory]
    [InlineData(true, JsonSchemaValidationStatus.Valid)]
    [InlineData(false, JsonSchemaValidationStatus.SchemaMismatch)]
    public void F5_embedded_id_resource_uses_explicit_or_inherited_dialect(
        bool declaresLegacyDialect, JsonSchemaValidationStatus expectedStatus)
    {
        var options = new JsonSchemaValidatorOptions
        {
            BaseUri = new Uri("https://schemas.example.test/embedded-root.json"),
            DefaultDialect = JsonSchemaDialect.Draft6
        };
        var childDialect = declaresLegacyDialect
            ? $"\"$schema\":\"{Draft7Uri}\","
            : string.Empty;
        var schema = $$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "$id": "https://schemas.example.test/embedded-root.json",
              "$defs": {
                "child": {
                  "$id": "child.json",
                  {{childDialect}}
                  "definitions": { "base": { "type": "number" } },
                  "properties": {
                    "x": { "$ref": "#/definitions/base", "minimum": 10 }
                  }
                }
              },
              "$ref": "child.json"
            }
            """;
        var validator = JsonSchemaValidator.Create(schema, options);

        Assert.Equal(expectedStatus, validator.Validate("{\"x\":1}").Status);
    }

    [Theory]
    [InlineData("https://schemas.example.test/child.json")]
    [InlineData("#/$defs/child")]
    public void F6_embedded_resource_root_location_is_independent_of_reference_entry(string reference)
    {
        var rootUri = new Uri("https://schemas.example.test/root.json");
        var childUri = new Uri(rootUri, "child.json");
        var validator = JsonSchemaValidator.Create($$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "$id": "{{rootUri.AbsoluteUri}}",
              "$defs": {
                "child": { "$id": "child.json", "type": "string" }
              },
              "$ref": "{{reference}}"
            }
            """, new JsonSchemaValidatorOptions { BaseUri = rootUri });

        var result = validator.Validate("1");

        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, result.Status);
        Assert.Contains(Flatten(result.Errors), error =>
            error.Keyword == "type" && error.SchemaLocation == Location(childUri));
    }

    [Fact]
    public void F6_embedded_resource_property_error_uses_child_resource_location()
    {
        var rootUri = new Uri("https://schemas.example.test/property-root.json");
        var childUri = new Uri(rootUri, "child.json");
        var validator = JsonSchemaValidator.Create($$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "$id": "{{rootUri.AbsoluteUri}}",
              "$defs": {
                "child": {
                  "$id": "child.json",
                  "type": "object",
                  "properties": { "value": { "type": "string" } }
                }
              },
              "$ref": "child.json"
            }
            """, new JsonSchemaValidatorOptions { BaseUri = rootUri });

        var result = validator.Validate("{\"value\":1}");

        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, result.Status);
        Assert.Contains(Flatten(result.Errors), error =>
            error.Keyword == "type"
            && error.InstancePath == "/value"
            && error.SchemaLocation == Location(childUri, "/properties/value"));
    }

    [Fact]
    public void F6_nested_resources_and_escaped_property_use_deepest_resource_location()
    {
        var rootUri = new Uri("https://schemas.example.test/nested-root.json");
        var level2Uri = new Uri(rootUri, "level1/level2.json");
        var validator = JsonSchemaValidator.Create($$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "$id": "{{rootUri.AbsoluteUri}}",
              "$defs": {
                "level1": {
                  "$id": "level1/level1.json",
                  "$defs": {
                    "level2": {
                      "$id": "level2.json",
                      "type": "object",
                      "properties": { "a/b~c": { "type": "string" } }
                    }
                  },
                  "$ref": "level2.json"
                }
              },
              "$ref": "level1/level1.json"
            }
            """, new JsonSchemaValidatorOptions { BaseUri = rootUri });

        var result = validator.Validate("{\"a/b~c\":1}");

        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, result.Status);
        Assert.Contains(Flatten(result.Errors), error =>
            error.Keyword == "type"
            && error.InstancePath == "/a~1b~0c"
            && error.SchemaLocation == Location(level2Uri, "/properties/a~1b~0c"));
    }

    [Fact]
    public void F6_embedded_false_schema_location_uses_child_resource_and_pointer()
    {
        var rootUri = new Uri("https://schemas.example.test/false-root.json");
        var childUri = new Uri(rootUri, "child.json");
        var validator = JsonSchemaValidator.Create($$"""
            {
              "$schema": "{{Draft202012Uri}}",
              "$id": "{{rootUri.AbsoluteUri}}",
              "$defs": {
                "child": {
                  "$id": "child.json",
                  "type": "object",
                  "properties": { "deny": false }
                }
              },
              "$ref": "child.json"
            }
            """, new JsonSchemaValidatorOptions { BaseUri = rootUri });

        var result = validator.Validate("{\"deny\":null}");

        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, result.Status);
        Assert.Contains(Flatten(result.Errors), error =>
            error.Keyword.Length == 0
            && error.InstancePath == "/deny"
            && error.SchemaLocation == Location(childUri, "/properties/deny"));
    }

    private static string BuildSiblingSchema(string? schemaDialectUri)
    {
        var schemaDialect = schemaDialectUri is null ? string.Empty : $"\"$schema\":\"{schemaDialectUri}\",";
        return $$"""
            {
              {{schemaDialect}}
              "definitions": { "base": { "type": "number" } },
              "properties": {
                "x": { "$ref": "#/definitions/base", "minimum": 10 }
              }
            }
            """;
    }

    private static string Location(Uri resourceUri, string pointer = "") => resourceUri.AbsoluteUri + "#" + pointer;

    private static IEnumerable<JsonSchemaValidationError> Flatten(IEnumerable<JsonSchemaValidationError> errors)
    {
        foreach (var error in errors)
        {
            yield return error;
            foreach (var child in Flatten(error.Children))
                yield return child;
        }
    }
}
