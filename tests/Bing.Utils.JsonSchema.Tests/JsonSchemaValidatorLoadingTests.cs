using System;
using Bing.Json.Schema;
using Xunit;
using EngineRegistry = global::Json.Schema.SchemaRegistry;
using EngineSchema = global::Json.Schema.JsonSchema;

namespace Bing.Utils.JsonSchema.Tests;

[CollectionDefinition("Schema registry globals", DisableParallelization = true)]
public sealed class SchemaRegistryGlobalCollection;

[Collection("Schema registry globals")]
public sealed class JsonSchemaValidatorLoadingTests
{
    [Fact]
    public void Missing_references_should_not_invoke_an_application_global_fetcher()
    {
        var originalFetcher = EngineRegistry.Global.Fetch;
        var fetchCalls = 0;
        EngineRegistry.Global.Fetch = (_, _) =>
        {
            fetchCalls++;
            return EngineSchema.True;
        };
        try
        {
            Assert.Throws<JsonSchemaConfigurationException>(() => JsonSchemaValidator.Create("""
                { "properties": { "unused": { "$ref": "https://unregistered.example.test/value" } } }
                """));
            Assert.Equal(0, fetchCalls);
        }
        finally
        {
            EngineRegistry.Global.Fetch = originalFetcher;
        }
    }
}
