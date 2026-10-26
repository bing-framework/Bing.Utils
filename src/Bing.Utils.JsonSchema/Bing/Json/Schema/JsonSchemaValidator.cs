using System.Globalization;
using System.Text.Json;
using global::Json.Schema;
using EngineSchema = global::Json.Schema.JsonSchema;

namespace Bing.Json.Schema;

/// <summary>准备 JSON Schema 并对原始 JSON 执行可重复的只读校验。</summary>
/// <remarks>
/// 创建时复制 Schema 和配置。实例可由多个线程调用，同一实例同步访问引擎；
/// 调用方负责在 Validate(JsonElement) 返回前保持输入文档存活。
/// </remarks>
public sealed class JsonSchemaValidator
{
    private readonly EngineSchema _schema;
    private readonly SchemaErrorLocations _errorLocations;
    private readonly EvaluationOptions _evaluationOptions;
    private readonly object _evaluationLock = new();

    private JsonSchemaValidator(EngineSchema schema, SchemaErrorLocations errorLocations, bool validateFormats, CultureInfo culture)
    {
        _schema = schema;
        _errorLocations = errorLocations;
        _evaluationOptions = new EvaluationOptions
        {
            OutputFormat = OutputFormat.Hierarchical,
            RequireFormatValidation = validateFormats,
            IncludeApplicatorErrors = true,
            Culture = CultureInfo.ReadOnly((CultureInfo)culture.Clone())
        };
    }

    /// <summary>解析并准备 Schema；所有引用必须由文档内部或预注册文档提供。</summary>
    /// <param name="schemaJson">对象或布尔值形式的 JSON Schema 文本。</param>
    /// <param name="options">创建配置；省略时使用 Draft 2020-12 和固定文化。</param>
    /// <returns>持有独立配置快照的校验器。</returns>
    /// <exception cref="ArgumentNullException">schemaJson 为 null。</exception>
    /// <exception cref="ArgumentException">配置参数无效。</exception>
    /// <exception cref="JsonSchemaConfigurationException">Schema、方言或引用无效。</exception>
    public static JsonSchemaValidator Create(string schemaJson, JsonSchemaValidatorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(schemaJson);
        try
        {
            using var document = JsonDocument.Parse(schemaJson);
            return Create(document.RootElement, options);
        }
        catch (JsonException exception)
        {
            throw new JsonSchemaConfigurationException("Schema 文本不是有效的 JSON。", exception);
        }
    }

    /// <summary>复制并准备 Schema；创建后调用方可以释放原始文档。</summary>
    /// <param name="schema">对象或布尔值形式的 Schema。</param>
    /// <param name="options">创建配置。</param>
    /// <returns>持有独立配置快照的校验器。</returns>
    /// <exception cref="ArgumentException">schema 未初始化或配置参数无效。</exception>
    /// <exception cref="ObjectDisposedException">schema 所属文档已释放。</exception>
    /// <exception cref="JsonSchemaConfigurationException">Schema、方言或引用无效。</exception>
    public static JsonSchemaValidator Create(JsonElement schema, JsonSchemaValidatorOptions? options = null)
    {
        if (schema.ValueKind == JsonValueKind.Undefined)
            throw new ArgumentException("Schema 必须是已初始化的 JSON 值。", nameof(schema));
        options ??= new JsonSchemaValidatorOptions();
        ArgumentNullException.ThrowIfNull(options.Culture);
        var compiled = SchemaCompiler.Compile(schema.Clone(), options);
        return new JsonSchemaValidator(compiled.Schema, compiled.Locations, options.ValidateFormats, options.Culture);
    }

    /// <summary>校验 JSON 文本；语法错误作为 InvalidJson 结果返回。</summary>
    /// <param name="json">待校验文本；字符串内容 null 是合法 JSON，C# null 参数无效。</param>
    /// <returns>成功结果、规则错误或语法错误。</returns>
    /// <exception cref="ArgumentNullException">json 为 null。</exception>
    /// <exception cref="JsonSchemaConfigurationException">求值过程中发现无效的 Schema 引用或循环。</exception>
    public JsonSchemaValidationResult Validate(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            return new JsonSchemaValidationResult(JsonSchemaValidationStatus.InvalidJson,
                syntaxError: new JsonSchemaSyntaxError(exception));
        }
        using (document)
            return Validate(document.RootElement);
    }

    /// <summary>直接校验 JSON 值；不序列化为 CLR 对象，也不更改输入。</summary>
    /// <param name="instance">待校验值；所属文档在调用期间必须保持存活。</param>
    /// <returns>独立于输入文档生命周期的结果。</returns>
    /// <exception cref="ArgumentException">instance 是未初始化的 JsonElement。</exception>
    /// <exception cref="ObjectDisposedException">输入文档已释放。</exception>
    /// <exception cref="JsonSchemaConfigurationException">求值过程中发现无效的 Schema 引用或循环。</exception>
    public JsonSchemaValidationResult Validate(JsonElement instance)
    {
        if (instance.ValueKind == JsonValueKind.Undefined)
            throw new ArgumentException("待校验值必须是已初始化的 JSON 值。", nameof(instance));
        // 动态/递归引用可能触发引擎内部的延迟解析，同一实例串行进入引擎。
        lock (_evaluationLock)
        {
            try
            {
                var evaluation = _schema.Evaluate(instance, _evaluationOptions);
                return evaluation.IsValid
                    ? new JsonSchemaValidationResult(JsonSchemaValidationStatus.Valid)
                    : new JsonSchemaValidationResult(JsonSchemaValidationStatus.SchemaMismatch,
                        SchemaErrorMapper.Map(evaluation, _errorLocations));
            }
            catch (Exception exception) when (exception is global::Json.Schema.JsonSchemaException
                or RefResolutionException)
            {
                throw new JsonSchemaConfigurationException("Schema 求值失败：" + exception.Message, exception);
            }
            finally
            {
                // 诊断已复制到公开结果；不跨调用保留引擎求值树。
                _errorLocations.Clear();
            }
        }
    }
}
