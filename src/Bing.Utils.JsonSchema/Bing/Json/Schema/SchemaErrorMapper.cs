using global::Json.Schema;

namespace Bing.Json.Schema;

/// <summary>按失败关键字筛选诊断，保留组合分支并排除仅用于条件判断的失败。</summary>
internal static class SchemaErrorMapper
{
    internal static IReadOnlyList<JsonSchemaValidationError> Map(EvaluationResults evaluation, SchemaErrorLocations locations)
    {
        if (evaluation.IsValid)
            return Array.Empty<JsonSchemaValidationError>();

        var result = new List<JsonSchemaValidationError>();
        var groups = (evaluation.Details ?? [])
            .Where(detail => !detail.IsValid)
            .GroupBy(detail => GetKeyword(evaluation, detail))
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        foreach (var error in evaluation.Errors ?? [])
        {
            var children = groups.TryGetValue(error.Key, out var details)
                ? details.SelectMany(detail => Map(detail, locations)).ToArray()
                : Array.Empty<JsonSchemaValidationError>();
            // 结构遍历的汇总说明不遮挡字段错误，组合规则仍保留分支层级。
            if (children.Length > 0 && error.Key is "properties" or "items" or "prefixItems" or "patternProperties")
                result.AddRange(children);
            else
                result.Add(new JsonSchemaValidationError(evaluation.InstanceLocation.ToString(),
                    locations.Get(evaluation), error.Key, error.Value, children));
        }

        // 引用关键字只转发目标结果，通常不产生自己的 Errors 项。
        foreach (var name in new[] { "$ref", "$dynamicRef", "$recursiveRef" })
        {
            if (evaluation.Errors?.ContainsKey(name) != true && groups.TryGetValue(name, out var details))
                result.AddRange(details.SelectMany(detail => Map(detail, locations)));
        }
        return result;
    }

    private static string GetKeyword(EvaluationResults parent, EvaluationResults child)
    {
        var parentPath = parent.EvaluationPath.ToString();
        var childPath = child.EvaluationPath.ToString();
        var relative = childPath[(parentPath.Length + 1)..];
        var separator = relative.IndexOf('/');
        return separator < 0 ? relative : relative[..separator];
    }
}
