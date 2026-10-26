using System;
using Bing.Json.Schema;
using Xunit;

namespace Bing.Utils.JsonSchema.Tests;

public sealed class JsonSchemaValidatorReferenceTests
{
    [Theory]
    [InlineData("{\"type\":\"string\"}", true)]
    [InlineData("false", false)]
    public void Create_should_resolve_single_segment_pointers_into_extension_keywords(string target, bool acceptsText)
    {
        var validator = JsonSchemaValidator.Create($$"""
            { "$ref": "#/customSchema", "customSchema": {{target}} }
            """, new JsonSchemaValidatorOptions { BaseUri = new Uri("https://schemas.example.test/extension.json") });

        Assert.Equal(acceptsText, validator.Validate("\"text\"").IsValid);
        var failure = validator.Validate("1");
        Assert.False(failure.IsValid);
        Assert.Equal("https://schemas.example.test/extension.json#/customSchema", Assert.Single(failure.Errors).SchemaLocation);
    }

    [Fact]
    public void Create_should_interpret_a_referenced_schema_map_as_its_own_schema()
    {
        var validator = JsonSchemaValidator.Create("""
            { "type": "object", "$defs": { "number": { "type": "number" } },
              "properties": { "value": { "$ref": "#/$defs" } } }
            """);

        // 引用对象是整个 $defs 映射，其中 number 为未知关键字，不是根的 type:object。
        Assert.True(validator.Validate("""{"value":1}""").IsValid);
    }

    [Fact]
    public void Create_should_resolve_internal_forward_references()
    {
        var validator = JsonSchemaValidator.Create("""
            {
              "properties": {
                "id": { "$ref": "#/$defs/positiveInteger" }
              },
              "required": ["id"],
              "$defs": {
                "positiveInteger": { "type": "integer", "minimum": 1 }
              }
            }
            """);

        Assert.Equal(JsonSchemaValidationStatus.Valid, validator.Validate("{\"id\":1}").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, validator.Validate("{\"id\":0}").Status);
    }

    [Fact]
    public void Create_should_preload_cross_file_references_and_resolve_them_from_absolute_base_uri()
    {
        var baseUri = new Uri("https://schemas.example.test/root.json");
        var firstUri = new Uri("https://schemas.example.test/first.json");
        var secondUri = new Uri("https://schemas.example.test/second.json");
        var options = new JsonSchemaValidatorOptions { BaseUri = baseUri };

        // first.json 在字典中先于其依赖项注册，创建时仍应解析完整引用图。
        options.ReferencedSchemas.Add(firstUri, """
            { "$id": "https://schemas.example.test/first.json", "$ref": "second.json" }
            """);
        options.ReferencedSchemas.Add(secondUri, """
            { "$id": "https://schemas.example.test/second.json", "type": "string", "minLength": 2 }
            """);
        var validator = JsonSchemaValidator.Create("{\"$ref\":\"first.json\"}", options);

        Assert.Equal(JsonSchemaValidationStatus.Valid, validator.Validate("\"ok\"").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, validator.Validate("\"x\"").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, validator.Validate("2").Status);
    }

    [Fact]
    public void Create_should_resolve_anchors_in_registered_reference_documents()
    {
        var commonUri = new Uri("https://schemas.example.test/common.json");
        var options = new JsonSchemaValidatorOptions();
        options.ReferencedSchemas.Add(commonUri, """
            {
              "$id": "https://schemas.example.test/common.json",
              "$defs": {
                "label": { "$anchor": "label", "type": "string", "minLength": 2 }
              }
            }
            """);
        var validator = JsonSchemaValidator.Create("""
            { "$ref": "https://schemas.example.test/common.json#label" }
            """, options);

        Assert.Equal(JsonSchemaValidationStatus.Valid, validator.Validate("\"ok\"").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, validator.Validate("\"x\"").Status);
    }

    [Fact]
    public void Create_should_support_recursive_internal_references()
    {
        var validator = JsonSchemaValidator.Create("""
            {
              "$id": "https://schemas.example.test/tree.json",
              "$ref": "#/$defs/node",
              "$defs": {
                "node": {
                  "type": "object",
                  "required": ["value"],
                  "properties": {
                    "value": { "type": "integer" },
                    "next": { "$ref": "#/$defs/node" }
                  }
                }
              }
            }
            """);

        Assert.Equal(JsonSchemaValidationStatus.Valid,
            validator.Validate("{\"value\":1,\"next\":{\"value\":2}}").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch,
            validator.Validate("{\"value\":1,\"next\":{}}").Status);
    }

    [Theory]
    [InlineData("http://json-schema.org/draft-06/schema#")]
    [InlineData("http://json-schema.org/draft-07/schema#")]
    public void Create_should_preserve_legacy_id_fragment_anchors(string dialectUri)
    {
        var baseUri = new Uri("https://schemas.example.test/legacy-anchor.json");
        var validator = JsonSchemaValidator.Create($$"""
            {
              "$schema": "{{dialectUri}}",
              "$id": "#node",
              "type": "object",
              "properties": {
                "next": { "$ref": "#node" }
              }
            }
            """, new JsonSchemaValidatorOptions { BaseUri = baseUri });

        Assert.Equal(JsonSchemaValidationStatus.Valid, validator.Validate("{\"next\":{}}").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, validator.Validate("{\"next\":1}").Status);
    }

    [Fact]
    public void Create_should_reject_unresolved_references_even_under_unmatched_properties()
    {
        var directReference = """
            { "$ref": "https://schemas.example.test/missing.json" }
            """;
        var unvisitedPropertyReference = """
            {
              "type": "object",
              "properties": {
                "unused": { "$ref": "https://schemas.example.test/missing.json" }
              }
            }
            """;

        Assert.Throws<JsonSchemaConfigurationException>(() => JsonSchemaValidator.Create(directReference));
        Assert.Throws<JsonSchemaConfigurationException>(() => JsonSchemaValidator.Create(unvisitedPropertyReference));
    }

    [Fact]
    public void Validators_with_the_same_schema_id_should_keep_reference_contents_isolated()
    {
        var root = """
            {
              "$id": "https://schemas.example.test/root.json",
              "$ref": "child.json"
            }
            """;
        var childUri = new Uri("https://schemas.example.test/child.json");
        var stringOptions = new JsonSchemaValidatorOptions
        {
            BaseUri = new Uri("https://schemas.example.test/root.json")
        };
        var integerOptions = new JsonSchemaValidatorOptions
        {
            BaseUri = new Uri("https://schemas.example.test/root.json")
        };
        stringOptions.ReferencedSchemas.Add(childUri, "{\"type\":\"string\"}");
        integerOptions.ReferencedSchemas.Add(childUri, "{\"type\":\"integer\"}");

        var stringValidator = JsonSchemaValidator.Create(root, stringOptions);
        var integerValidator = JsonSchemaValidator.Create(root, integerOptions);

        Assert.Equal(JsonSchemaValidationStatus.Valid, stringValidator.Validate("\"text\"").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, stringValidator.Validate("1").Status);
        Assert.Equal(JsonSchemaValidationStatus.Valid, integerValidator.Validate("1").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, integerValidator.Validate("\"text\"").Status);
    }
}
