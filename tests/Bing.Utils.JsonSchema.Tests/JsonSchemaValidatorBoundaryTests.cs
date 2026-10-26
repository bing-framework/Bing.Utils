using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bing.Json.Schema;
using Xunit;

namespace Bing.Utils.JsonSchema.Tests;

/// <summary>覆盖方言选择、引用准备和返回结果的公开边界。</summary>
public sealed class JsonSchemaValidatorBoundaryTests
{
    [Theory]
    [InlineData(JsonSchemaDialect.Draft6, "http://json-schema.org/draft-06/schema#")]
    [InlineData(JsonSchemaDialect.Draft7, "http://json-schema.org/draft-07/schema#")]
    [InlineData(JsonSchemaDialect.Draft201909, "https://json-schema.org/draft/2019-09/schema")]
    [InlineData(JsonSchemaDialect.Draft202012, "https://json-schema.org/draft/2020-12/schema")]
    public void Supported_dialects_should_apply_numeric_and_array_constraints(
        JsonSchemaDialect dialect, string schemaUri)
    {
        var schema = $$"""
            {
              "$schema": "{{schemaUri}}",
              "type": "array",
              "minItems": 1,
              "maxItems": 2,
              "items": { "type": "number", "minimum": 0, "exclusiveMaximum": 10 }
            }
            """;
        var validator = JsonSchemaValidator.Create(schema, new JsonSchemaValidatorOptions { DefaultDialect = dialect });

        Assert.True(validator.Validate("[0,9.5]").IsValid);
        foreach (var input in new[] { "[]", "[10]", "[-1]", "[1,2,3]", "[\"1\"]" })
        {
            var result = validator.Validate(input);
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }
    }

    [Fact]
    public void Explicit_dialect_should_override_the_default_dialect()
    {
        var validator = JsonSchemaValidator.Create("""
            {
              "$schema": "http://json-schema.org/draft-07/schema#",
              "items": [{ "type": "integer" }],
              "additionalItems": false
            }
            """, new JsonSchemaValidatorOptions { DefaultDialect = JsonSchemaDialect.Draft202012 });

        Assert.True(validator.Validate("[1]").IsValid);
        Assert.False(validator.Validate("[1,2]").IsValid);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("{\"type\":42}")]
    [InlineData("{\"required\":true}")]
    [InlineData("{\"minimum\":\"bad\"}")]
    [InlineData("{\"properties\":{\"x\":3}}")]
    [InlineData("{\"$id\":false}")]
    public void Malformed_schemas_should_be_configuration_errors(string schema)
    {
        Assert.Throws<JsonSchemaConfigurationException>(() => JsonSchemaValidator.Create(schema));
    }

    [Fact]
    public void Dialect_like_data_inside_const_should_not_be_interpreted_as_schema()
    {
        var validator = JsonSchemaValidator.Create("""
            { "const": { "$schema": "https://example.test/not-a-dialect" } }
            """);

        Assert.True(validator.Validate("""{"$schema":"https://example.test/not-a-dialect"}""").IsValid);
    }

    [Theory]
    [InlineData("{\"properties\":{\"unused\":{\"$ref\":\"#/$defs/missing\"}}}")]
    [InlineData("{\"properties\":{\"unused\":{\"$ref\":\"#missing\"}}}")]
    [InlineData("{\"properties\":{\"unused\":{\"$dynamicRef\":\"#missing\"}}}")]
    [InlineData("{\"$ref\":\"#\"}")]
    public void Unresolvable_or_unproductive_references_should_fail_during_creation(string schema)
    {
        Assert.Throws<JsonSchemaConfigurationException>(() => JsonSchemaValidator.Create(schema));
    }

    [Theory]
    [InlineData("https://json-schema.org/draft/2020-12/schema", "\"$dynamicAnchor\":\"node\"", "\"$dynamicRef\":\"#node\"")]
    [InlineData("https://json-schema.org/draft/2019-09/schema", "\"$recursiveAnchor\":true", "\"$recursiveRef\":\"#\"")]
    public void Dynamic_and_recursive_references_should_validate_descendants(
        string dialect, string anchor, string reference)
    {
        var validator = JsonSchemaValidator.Create($$"""
            {
              "$schema": "{{dialect}}",
              {{anchor}},
              "type": "object",
              "required": ["value"],
              "properties": {
                "value": { "type": "integer" },
                "next": { {{reference}} }
              }
            }
            """);

        Assert.True(validator.Validate("""{"value":1,"next":{"value":2}}""").IsValid);
        var result = validator.Validate("""{"value":1,"next":{"value":"bad"}}""");
        Assert.False(result.IsValid);
        Assert.Contains(Flatten(result.Errors), x => x.Keyword == "type" && x.InstancePath == "/next/value");
    }

    [Fact]
    public void Results_should_survive_disposed_input_and_reject_collection_mutation()
    {
        var validator = JsonSchemaValidator.Create("""{"oneOf":[{"type":"string"},{"type":"boolean"}]}""");
        JsonSchemaValidationResult result;
        using (var document = JsonDocument.Parse("5"))
            result = validator.Validate(document.RootElement);

        var error = Assert.Single(result.Errors);
        Assert.Equal("oneOf", error.Keyword);
        Assert.NotEmpty(error.Children);
        Assert.All(error.Children, child => Assert.False(string.IsNullOrWhiteSpace(child.Message)));
        Assert.Throws<NotSupportedException>(() => ((IList<JsonSchemaValidationError>)result.Errors).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<JsonSchemaValidationError>)error.Children).Clear());
    }

    [Fact]
    public void Failed_if_condition_should_not_appear_as_a_rule_error()
    {
        var validator = JsonSchemaValidator.Create("""
            {
              "if": { "type": "string" },
              "then": { "minLength": 5 },
              "else": { "minimum": 10 }
            }
            """);
        var result = validator.Validate("1");

        Assert.False(result.IsValid);
        Assert.Contains(Flatten(result.Errors), x => x.Keyword == "minimum");
        Assert.DoesNotContain(Flatten(result.Errors), x => x.Keyword == "type" || x.Keyword == "if");
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
