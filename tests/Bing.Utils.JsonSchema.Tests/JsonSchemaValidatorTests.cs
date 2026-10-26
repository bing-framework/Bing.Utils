using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bing.Json.Schema;
using Xunit;

namespace Bing.Utils.JsonSchema.Tests;

public sealed class JsonSchemaValidatorTests
{
    private const string PersonSchema = """
        {
          "type": "object",
          "required": ["name"],
          "properties": {
            "name": { "type": "string" },
            "age": { "type": "integer" }
          },
          "additionalProperties": false
        }
        """;

    [Theory]
    [InlineData("{\"name\":\"Ada\",\"age\":37}", JsonSchemaValidationStatus.Valid)]
    [InlineData("{\"age\":37}", JsonSchemaValidationStatus.SchemaMismatch)]
    [InlineData("{\"name\":null}", JsonSchemaValidationStatus.SchemaMismatch)]
    [InlineData("{\"name\":\"Ada\",\"extra\":true}", JsonSchemaValidationStatus.SchemaMismatch)]
    public void Validate_should_apply_types_required_and_object_rules(
        string json, JsonSchemaValidationStatus expectedStatus)
    {
        var validator = JsonSchemaValidator.Create(PersonSchema);

        var result = validator.Validate(json);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedStatus == JsonSchemaValidationStatus.Valid, result.IsValid);
        Assert.Equal(expectedStatus == JsonSchemaValidationStatus.Valid, result.Errors.Count == 0);
    }

    [Fact]
    public void Validate_should_report_required_at_the_parent_object()
    {
        var validator = JsonSchemaValidator.Create(PersonSchema);

        var result = validator.Validate("{\"age\":37}");

        var requiredError = Flatten(result.Errors).Single(error => error.Keyword == "required");
        Assert.Equal(string.Empty, requiredError.InstancePath);
        Assert.False(string.IsNullOrWhiteSpace(requiredError.SchemaLocation));
    }

    [Fact]
    public void Validate_should_support_root_scalars_and_boolean_schemas()
    {
        var integerValidator = JsonSchemaValidator.Create("{\"type\":\"integer\"}");
        var allowValidator = JsonSchemaValidator.Create("true");
        var denyValidator = JsonSchemaValidator.Create("false");

        Assert.Equal(JsonSchemaValidationStatus.Valid, integerValidator.Validate("12").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, integerValidator.Validate("12.5").Status);
        Assert.Equal(JsonSchemaValidationStatus.Valid, allowValidator.Validate("null").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, denyValidator.Validate("null").Status);
    }

    [Fact]
    public void Create_without_schema_uri_should_use_draft_2020_12()
    {
        var validator = JsonSchemaValidator.Create("""
            { "prefixItems": [{ "type": "integer" }], "items": false }
            """);

        Assert.Equal(JsonSchemaValidationStatus.Valid, validator.Validate("[1]").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, validator.Validate("[1,2]").Status);
    }

    [Fact]
    public void Validate_should_return_json_syntax_diagnostics_without_schema_errors()
    {
        var validator = JsonSchemaValidator.Create("true");

        var result = validator.Validate("{\n  \"name\": }\n");

        Assert.Equal(JsonSchemaValidationStatus.InvalidJson, result.Status);
        Assert.False(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.SyntaxError);
        Assert.False(string.IsNullOrWhiteSpace(result.SyntaxError.Message));
        Assert.True(result.SyntaxError.LineNumber is >= 0);
        Assert.True(result.SyntaxError.BytePositionInLine is >= 0);
    }

    [Fact]
    public void Validate_should_not_report_internals_of_a_successful_any_of_branch()
    {
        var validator = JsonSchemaValidator.Create("""
            {
              "type": "object",
              "properties": {
                "kind": {
                  "anyOf": [
                    { "const": "ready" },
                    { "type": "string", "minLength": 20 }
                  ]
                },
                "name": { "type": "string", "const": "expected" }
              }
            }
            """);

        var result = validator.Validate("{\"kind\":\"ready\",\"name\":\"actual\"}");

        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, result.Status);
        var errors = Flatten(result.Errors).ToArray();
        Assert.Contains(errors, error => error.InstancePath == "/name");
        Assert.DoesNotContain(errors, error => error.InstancePath == "/kind");
        Assert.DoesNotContain(errors, error => error.Keyword == "anyOf");
        Assert.DoesNotContain(errors, error => error.Keyword == "minLength");
    }

    [Fact]
    public void Validate_should_apply_if_then_else_and_unevaluated_properties()
    {
        var conditional = JsonSchemaValidator.Create("""
            {
              "type": "object",
              "if": {
                "properties": { "mode": { "const": "strict" } },
                "required": ["mode"]
              },
              "then": { "required": ["value"] },
              "else": { "properties": { "value": { "type": "string" } } }
            }
            """);
        var evaluated = JsonSchemaValidator.Create("""
            {
              "allOf": [
                { "properties": { "known": { "type": "string" } } }
              ],
              "unevaluatedProperties": false
            }
            """);

        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch,
            conditional.Validate("{\"mode\":\"strict\"}").Status);
        Assert.Equal(JsonSchemaValidationStatus.Valid,
            conditional.Validate("{\"mode\":\"loose\",\"value\":\"text\"}").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch,
            conditional.Validate("{\"mode\":\"loose\",\"value\":1}").Status);
        Assert.Equal(JsonSchemaValidationStatus.Valid, evaluated.Validate("{\"known\":\"text\"}").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, evaluated.Validate("{\"unknown\":1}").Status);
    }

    [Fact]
    public void Validate_should_report_one_of_when_multiple_branches_succeed()
    {
        var validator = JsonSchemaValidator.Create("""
            {
              "oneOf": [
                { "type": "string", "minLength": 2 },
                { "type": "string", "maxLength": 5 }
              ]
            }
            """);

        var result = validator.Validate("\"abc\"");

        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, result.Status);
        var oneOfError = Flatten(result.Errors).Single(error => error.Keyword == "oneOf");
        Assert.False(string.IsNullOrWhiteSpace(oneOfError.Message));
    }

    [Fact]
    public void Validate_should_preserve_failed_one_of_branch_diagnostics()
    {
        var validator = JsonSchemaValidator.Create("""
            { "oneOf": [{ "const": "left" }, { "const": "right" }] }
            """);

        var result = validator.Validate("\"other\"");

        var oneOfError = Flatten(result.Errors).Single(error => error.Keyword == "oneOf");
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, result.Status);
        Assert.NotEmpty(oneOfError.Children);
        Assert.Contains(Flatten(oneOfError.Children), error => error.Keyword == "const");
    }

    [Fact]
    public void Validate_should_escape_property_names_in_json_pointer_paths()
    {
        var validator = JsonSchemaValidator.Create("""
            { "properties": { "a/b~c": { "type": "integer" } } }
            """);

        var result = validator.Validate("{\"a/b~c\":\"wrong\"}");

        Assert.Contains(Flatten(result.Errors), error =>
            error.Keyword == "type" && error.InstancePath == "/a~1b~0c");
    }

    [Fact]
    public void Create_should_reject_unknown_dialects_as_configuration_errors()
    {
        var schema = """
            {
              "$schema": "https://example.test/json-schema/unknown-dialect",
              "type": "string"
            }
            """;

        Assert.Throws<JsonSchemaConfigurationException>(() => JsonSchemaValidator.Create(schema));
        Assert.Throws<JsonSchemaConfigurationException>(() => JsonSchemaValidator.Create("{"));
    }

    [Fact]
    public void Format_assertion_should_be_opt_in()
    {
        var annotationOnly = JsonSchemaValidator.Create("""
            { "type": "string", "format": "email" }
            """);
        var asserting = JsonSchemaValidator.Create("""
            { "type": "string", "format": "email" }
            """, new JsonSchemaValidatorOptions { ValidateFormats = true });

        Assert.Equal(JsonSchemaValidationStatus.Valid, annotationOnly.Validate("\"not-an-email\"").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, asserting.Validate("\"not-an-email\"").Status);
    }

    [Fact]
    public void Create_should_copy_schema_element_before_its_document_is_disposed()
    {
        JsonSchemaValidator validator;
        using (var document = JsonDocument.Parse("{\"type\":\"string\"}"))
        {
            validator = JsonSchemaValidator.Create(document.RootElement);
        }

        Assert.Equal(JsonSchemaValidationStatus.Valid, validator.Validate("\"value\"").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, validator.Validate("1").Status);
    }

    [Fact]
    public void Create_should_snapshot_options_and_referenced_schema_text()
    {
        var referenceUri = new Uri("https://schemas.example.test/value.json");
        var options = new JsonSchemaValidatorOptions();
        options.ReferencedSchemas.Add(referenceUri, "{\"type\":\"string\",\"format\":\"email\"}");
        var validator = JsonSchemaValidator.Create("{\"$ref\":\"https://schemas.example.test/value.json\"}", options);

        options.ReferencedSchemas[referenceUri] = "{\"type\":\"integer\"}";
        options.ValidateFormats = true;
        options.DefaultDialect = JsonSchemaDialect.Draft7;

        Assert.Equal(JsonSchemaValidationStatus.Valid, validator.Validate("\"not-an-email\"").Status);
        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, validator.Validate("1").Status);

        var dialectOptions = new JsonSchemaValidatorOptions();
        var dialectValidator = JsonSchemaValidator.Create("""
            { "prefixItems": [{ "type": "integer" }], "items": false }
            """, dialectOptions);
        dialectOptions.DefaultDialect = JsonSchemaDialect.Draft7;

        Assert.Equal(JsonSchemaValidationStatus.SchemaMismatch, dialectValidator.Validate("[1,2]").Status);
    }

    [Fact]
    public void Validate_should_be_reusable_concurrently_and_leave_input_unchanged()
    {
        var validator = JsonSchemaValidator.Create("""
            {
              "type": "object",
              "properties": {
                "count": { "type": "integer", "default": 0 }
              }
            }
            """);
        using var document = JsonDocument.Parse("{}");

        var first = validator.Validate(document.RootElement);
        var validations = Enumerable.Range(0, 64)
            .Select(index => Task.Run(() => validator.Validate(index % 2 == 0 ? "{\"count\":1}" : "{\"count\":\"bad\"}")))
            .ToArray();

        Task.WaitAll(validations);
        Assert.Equal("{}", document.RootElement.GetRawText());
        Assert.Equal(JsonSchemaValidationStatus.Valid, first.Status);
        for (var index = 0; index < validations.Length; index++)
        {
            var expected = index % 2 == 0
                ? JsonSchemaValidationStatus.Valid
                : JsonSchemaValidationStatus.SchemaMismatch;
            Assert.Equal(expected, validations[index].Result.Status);
        }
    }

    [Fact]
    public void Options_should_default_to_draft_2020_12_and_invariant_culture()
    {
        var options = new JsonSchemaValidatorOptions();

        Assert.Equal(JsonSchemaDialect.Draft202012, options.DefaultDialect);
        Assert.Equal(CultureInfo.InvariantCulture, options.Culture);
    }

    [Fact]
    public void Create_and_validate_should_reject_null_or_default_inputs()
    {
        var validator = JsonSchemaValidator.Create("true");

        Assert.Throws<ArgumentNullException>(() => JsonSchemaValidator.Create((string)null!));
        Assert.Throws<ArgumentNullException>(() => validator.Validate((string)null!));
        Assert.Throws<ArgumentException>(() => JsonSchemaValidator.Create(default(JsonElement)));
        Assert.Throws<ArgumentException>(() => validator.Validate(default(JsonElement)));
    }

    private static System.Collections.Generic.IEnumerable<JsonSchemaValidationError> Flatten(
        System.Collections.Generic.IEnumerable<JsonSchemaValidationError> errors)
    {
        foreach (var error in errors)
        {
            yield return error;
            foreach (var child in Flatten(error.Children))
            {
                yield return child;
            }
        }
    }
}
