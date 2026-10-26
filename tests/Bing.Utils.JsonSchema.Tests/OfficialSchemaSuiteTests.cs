using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Bing.Json.Schema;
using Xunit;

namespace Bing.Utils.JsonSchema.Tests;

public sealed class OfficialSchemaSuiteTests
{
    private static readonly string[] FixtureFiles =
    [
        "type.json",
        "required.json",
        "properties.json",
        "anyOf.json",
        "oneOf.json",
        "if-then-else.json",
        "unevaluatedProperties.json"
    ];

    public static IEnumerable<object[]> OfficialCases => LoadOfficialCases();

    [Theory]
    [MemberData(nameof(OfficialCases))]
    public void Draft_2020_12_official_case_should_match_expected_result(
        string fileName,
        string groupDescription,
        string testDescription,
        string schemaJson,
        string instanceJson,
        bool expectedValid)
    {
        var validator = JsonSchemaValidator.Create(schemaJson);
        var result = validator.Validate(instanceJson);
        var caseName = $"{fileName} / {groupDescription} / {testDescription}";

        Assert.True(expectedValid == result.IsValid,
            $"Official suite case '{caseName}' expected valid={expectedValid}, actual status={result.Status}.");
        if (expectedValid)
        {
            Assert.Empty(result.Errors);
        }
        else
        {
            Assert.NotEmpty(result.Errors);
        }
    }

    private static IEnumerable<object[]> LoadOfficialCases()
    {
        var fixturesDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        foreach (var fileName in FixtureFiles)
        {
            var fixturePath = Path.Combine(fixturesDirectory, fileName);
            using var document = JsonDocument.Parse(File.ReadAllText(fixturePath, Encoding.UTF8));
            foreach (var group in document.RootElement.EnumerateArray())
            {
                var groupDescription = group.GetProperty("description").GetString() ?? "(no group description)";
                var schemaJson = group.GetProperty("schema").GetRawText();
                foreach (var test in group.GetProperty("tests").EnumerateArray())
                {
                    var testDescription = test.GetProperty("description").GetString() ?? "(no test description)";
                    yield return
                    [
                        fileName,
                        groupDescription,
                        testDescription,
                        schemaJson,
                        test.GetProperty("data").GetRawText(),
                        test.GetProperty("valid").GetBoolean()
                    ];
                }
            }
        }
    }
}
