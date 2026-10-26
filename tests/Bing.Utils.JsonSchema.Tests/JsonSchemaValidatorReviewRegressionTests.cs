using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bing.Json.Schema;
using Xunit;

namespace Bing.Utils.JsonSchema.Tests;

public sealed class JsonSchemaValidatorReviewRegressionTests
{
    private const string Draft202012 = "https://json-schema.org/draft/2020-12/schema";

    [Fact]
    public void F1_external_dynamic_anchor_should_validate_strings_and_reject_numbers()
    {
        var validator = CreateCanonicalDynamicReferenceValidator();

        Assert.Equal(JsonSchemaValidationStatus.Valid, validator.Validate("\"text\"").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, validator.Validate("42").Status);
    }

    [Fact]
    public void F1_outer_dynamic_anchor_should_override_the_base_tree_anchor()
    {
        var baseTreeUri = new Uri("https://schemas.example.test/base-tree.json");
        var strictTreeUri = new Uri("https://schemas.example.test/strict-tree.json");
        var options = new JsonSchemaValidatorOptions { BaseUri = strictTreeUri };
        options.ReferencedSchemas.Add(baseTreeUri, """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "https://schemas.example.test/base-tree.json",
              "$dynamicAnchor": "node",
              "type": "object",
              "properties": {
                "children": {
                  "type": "array",
                  "items": { "$dynamicRef": "#node" }
                }
              }
            }
            """);

        var validator = JsonSchemaValidator.Create("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "https://schemas.example.test/strict-tree.json",
              "$dynamicAnchor": "node",
              "$ref": "https://schemas.example.test/base-tree.json",
              "required": ["strictOnly"],
              "properties": { "strictOnly": { "const": true } }
            }
            """, options);

        Assert.Equal(JsonSchemaValidationStatus.Valid,
            validator.Validate("""{"strictOnly":true,"children":[{"strictOnly":true,"children":[]}]}""").Status);

        var invalid = validator.Validate("""{"strictOnly":true,"children":[{"children":[]}]}""");
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, invalid.Status);
        Assert.Contains(Flatten(invalid.Errors), error =>
            error.Keyword == "required" && error.InstancePath == "/children/0");
    }

    [Fact]
    public async Task F1_same_validator_should_support_concurrent_repeated_calls()
    {
        var validator = CreateCanonicalDynamicReferenceValidator();
        var calls = Enumerable.Range(0, 64)
            .Select(index => Task.Run(() => validator.Validate(index % 2 == 0 ? "\"text\"" : "42").Status))
            .ToArray();

        var results = await Task.WhenAll(calls);

        for (var index = 0; index < results.Length; index++)
        {
            var expected = index % 2 == 0
                ? JsonSchemaValidationStatus.Valid
                : JsonSchemaValidationStatus.SchemaMismatch;
            Assert.Equal(expected, results[index]);
        }
    }

    [Fact]
    public void F2_dynamic_and_static_references_should_resolve_through_a_retrieval_uri_alias()
    {
        var lookupUri = new Uri("https://schemas.example.test/lookup.json");
        var options = new JsonSchemaValidatorOptions
        {
            BaseUri = new Uri("https://schemas.example.test/root.json")
        };
        options.ReferencedSchemas.Add(lookupUri, """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "https://schemas.example.test/canonical.json",
              "$dynamicAnchor": "node",
              "$anchor": "plain",
              "type": "string"
            }
            """);

        var dynamicValidator = JsonSchemaValidator.Create("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "properties": { "lookup": { "$dynamicRef": "lookup.json#node" } }
            }
            """, options);

        Assert.Equal(JsonSchemaValidationStatus.Valid, dynamicValidator.Validate("{}").Status);
        Assert.Equal(JsonSchemaValidationStatus.Valid, dynamicValidator.Validate("{\"lookup\":\"text\"}").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, dynamicValidator.Validate("{\"lookup\":42}").Status);

        var staticValidator = JsonSchemaValidator.Create("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "properties": { "lookup": { "$ref": "lookup.json#plain" } }
            }
            """, options);

        Assert.Equal(JsonSchemaValidationStatus.Valid, staticValidator.Validate("{}").Status);
        Assert.Equal(JsonSchemaValidationStatus.Valid, staticValidator.Validate("{\"lookup\":\"text\"}").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, staticValidator.Validate("{\"lookup\":42}").Status);
    }

    [Fact]
    public void F3_false_root_should_report_the_configured_base_uri()
    {
        var baseUri = new Uri("https://schemas.example.test/deny-root.json");
        var validator = JsonSchemaValidator.Create("false", new JsonSchemaValidatorOptions { BaseUri = baseUri });

        var result = validator.Validate("null");

        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, result.Status);
        var error = Assert.Single(result.Errors);
        Assert.Equal(string.Empty, error.Keyword);
        Assert.Equal(baseUri.AbsoluteUri + "#", error.SchemaLocation);
    }

    [Fact]
    public void F3_external_false_references_should_report_each_target_document()
    {
        var baseUri = new Uri("https://schemas.example.test/root.json");
        var refUri = new Uri("https://schemas.example.test/deny-ref.json");
        var dynamicRefUri = new Uri("https://schemas.example.test/deny-dynamic.json");
        var options = new JsonSchemaValidatorOptions { BaseUri = baseUri };
        options.ReferencedSchemas.Add(refUri, "false");
        options.ReferencedSchemas.Add(dynamicRefUri, "false");

        var validator = JsonSchemaValidator.Create("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "allOf": [
                { "$ref": "deny-ref.json" },
                { "$dynamicRef": "deny-dynamic.json" }
              ]
            }
            """, options);

        var result = validator.Validate("null");

        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, result.Status);
        var falseErrors = Flatten(result.Errors).Where(error => error.Keyword.Length == 0).ToArray();
        Assert.Contains(falseErrors, error => error.SchemaLocation == refUri.AbsoluteUri + "#");
        Assert.Contains(falseErrors, error => error.SchemaLocation == dynamicRefUri.AbsoluteUri + "#");
    }

    [Fact]
    public void F3_nested_false_should_keep_its_schema_pointer_location()
    {
        var baseUri = new Uri("https://schemas.example.test/nested.json");
        var validator = JsonSchemaValidator.Create("""
            {
              "type": "object",
              "properties": { "deny": false }
            }
            """, new JsonSchemaValidatorOptions { BaseUri = baseUri });

        var result = validator.Validate("{\"deny\":null}");

        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, result.Status);
        var error = Assert.Single(result.Errors);
        Assert.Equal(string.Empty, error.Keyword);
        Assert.Equal(new Uri(baseUri, "#/properties/deny").OriginalString, error.SchemaLocation);
    }

    [Theory]
    [InlineData("http://json-schema.org/draft-06/schema", "#other")]
    [InlineData("http://json-schema.org/draft-06/schema", "#/x")]
    [InlineData("http://json-schema.org/draft-07/schema", "#other")]
    [InlineData("http://json-schema.org/draft-07/schema", "#/x")]
    [InlineData("https://json-schema.org/draft/2019-09/schema", "#other")]
    [InlineData("https://json-schema.org/draft/2019-09/schema", "#/x")]
    [InlineData("https://json-schema.org/draft/2020-12/schema", "#other")]
    [InlineData("https://json-schema.org/draft/2020-12/schema", "#/x")]
    public void F4_supported_dialect_with_unknown_fragment_should_be_rejected(string dialectUri, string fragment)
    {
        var schema = $$"""
            { "$schema": "{{dialectUri}}{{fragment}}", "type": "string" }
            """;

        Assert.Throws<JsonSchemaConfigurationException>(() => JsonSchemaValidator.Create(schema));
    }

    [Theory]
    [InlineData("http://json-schema.org/draft-06/schema")]
    [InlineData("http://json-schema.org/draft-06/schema#")]
    [InlineData("http://json-schema.org/draft-07/schema")]
    [InlineData("http://json-schema.org/draft-07/schema#")]
    [InlineData("https://json-schema.org/draft/2019-09/schema")]
    [InlineData("https://json-schema.org/draft/2019-09/schema#")]
    [InlineData("https://json-schema.org/draft/2020-12/schema")]
    [InlineData("https://json-schema.org/draft/2020-12/schema#")]
    public void F4_supported_dialect_without_a_nonempty_fragment_should_remain_usable(string dialectUri)
    {
        var schema = $$"""
            { "$schema": "{{dialectUri}}", "type": "string" }
            """;
        var validator = JsonSchemaValidator.Create(schema);

        Assert.Equal(JsonSchemaValidationStatus.Valid, validator.Validate("\"text\"").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, validator.Validate("42").Status);
    }

    private static JsonSchemaValidator CreateCanonicalDynamicReferenceValidator()
    {
        var canonicalUri = new Uri("https://schemas.example.test/canonical.json");
        var options = new JsonSchemaValidatorOptions
        {
            BaseUri = new Uri("https://schemas.example.test/root.json")
        };
        options.ReferencedSchemas.Add(canonicalUri, """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "https://schemas.example.test/canonical.json",
              "$dynamicAnchor": "node",
              "type": "string"
            }
            """);

        return JsonSchemaValidator.Create("""
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$dynamicRef": "https://schemas.example.test/canonical.json#node"
            }
            """, options);
    }

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
