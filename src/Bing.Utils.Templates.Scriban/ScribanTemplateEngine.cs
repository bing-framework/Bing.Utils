using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Bing.Text.Templating.Scriban;

/// <summary>配置受限 Scriban 模板的输入与运行预算。</summary>
public sealed class ScribanTemplateOptions
{
    /// <summary>获取或设置模板名称，用于诊断显示。</summary>
    public string TemplateName { get; set; } = "模板";

    /// <summary>获取或设置最大模板长度，单位为 UTF-16 字符。</summary>
    public int MaximumTemplateLength { get; set; } = 256 * 1024;

    /// <summary>获取或设置最大输出长度，单位为 UTF-16 字符。</summary>
    public int MaximumOutputLength { get; set; } = 1024 * 1024;

    /// <summary>获取或设置所有嵌套循环允许的最大迭代次数。</summary>
    public int MaximumLoopCount { get; set; } = 10_000;

    /// <summary>获取或设置模板函数允许的最大递归深度。</summary>
    public int MaximumRecursionDepth { get; set; } = 32;

    /// <summary>获取或设置模板模型允许的最大嵌套深度。</summary>
    public int MaximumModelDepth { get; set; } = 32;

    /// <summary>获取或设置单个字典或集合允许的最大项数。</summary>
    public int MaximumModelItems { get; set; } = 10_000;

    /// <summary>获取或设置数字和日期渲染文化，默认使用不变文化。</summary>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    internal ScribanTemplateOptions Snapshot()
    {
        if (MaximumTemplateLength < 0) throw new ArgumentOutOfRangeException(nameof(MaximumTemplateLength));
        if (MaximumOutputLength < 1) throw new ArgumentOutOfRangeException(nameof(MaximumOutputLength));
        if (MaximumLoopCount < 1) throw new ArgumentOutOfRangeException(nameof(MaximumLoopCount));
        if (MaximumRecursionDepth < 1) throw new ArgumentOutOfRangeException(nameof(MaximumRecursionDepth));
        if (MaximumModelDepth < 1) throw new ArgumentOutOfRangeException(nameof(MaximumModelDepth));
        if (MaximumModelItems < 1) throw new ArgumentOutOfRangeException(nameof(MaximumModelItems));

        return new ScribanTemplateOptions
        {
            TemplateName = string.IsNullOrWhiteSpace(TemplateName) ? "模板" : TemplateName,
            MaximumTemplateLength = MaximumTemplateLength,
            MaximumOutputLength = MaximumOutputLength,
            MaximumLoopCount = MaximumLoopCount,
            MaximumRecursionDepth = MaximumRecursionDepth,
            MaximumModelDepth = MaximumModelDepth,
            MaximumModelItems = MaximumModelItems,
            Culture = CultureInfo.ReadOnly((CultureInfo)(Culture ?? CultureInfo.InvariantCulture).Clone())
        };
    }
}

/// <summary>统一的模板编译和渲染异常。</summary>
public sealed class ScribanTemplateException : Exception
{
    internal ScribanTemplateException(string errorCode, string templateName, int? line, int? column,
        string diagnostic, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        TemplateName = templateName;
        Line = line;
        Column = column;
        Diagnostic = diagnostic;
    }

    /// <summary>获取稳定错误码。</summary>
    public string ErrorCode { get; }

    /// <summary>获取模板名称。</summary>
    public string TemplateName { get; }

    /// <summary>获取 1-based 源码行号；模型或配置错误可能没有位置。</summary>
    public int? Line { get; }

    /// <summary>获取 1-based UTF-16 列号；模型或配置错误可能没有位置。</summary>
    public int? Column { get; }

    /// <summary>获取 Scriban 原始诊断或输入模型错误详情。</summary>
    public string Diagnostic { get; }
}

/// <summary>已编译且可并发复用的受限 Scriban 模板。</summary>
public sealed class CompiledTemplate
{
    private readonly Template _template;
    private readonly ScribanTemplateOptions _options;

    internal CompiledTemplate(Template template, ScribanTemplateOptions options)
    {
        _template = template;
        _options = options;
    }

    /// <summary>使用独立上下文和可取消的 Scriban 异步求值渲染模板。</summary>
    /// <param name="model">字符串键字典或公开可读属性 DTO；嵌套字典和有限集合会递归复制。</param>
    /// <param name="cancellationToken">传递给 Scriban 求值器的取消令牌。</param>
    /// <returns>受生成期字符预算限制的渲染文本。</returns>
    public async Task<string> RenderAsync(object model, CancellationToken cancellationToken = default)
    {
        string missingVariable = null;
        SourceSpan? missingVariableSpan = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var context = CreateContext(cancellationToken);
            context.TryGetVariable = (TemplateContext _, SourceSpan span, ScriptVariable variable, out object value) =>
            {
                missingVariable = variable.Name;
                missingVariableSpan = span;
                value = null;
                return false;
            };
            context.PushGlobal(CreateScriptObject(model, depth: 0));

            var rendered = await _template.RenderAsync(context).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (rendered.Length > _options.MaximumOutputLength)
                throw CreateOutputLimitException(null);
            return rendered;
        }
        catch (ScriptAbortException error)
        {
            throw new OperationCanceledException($"模板“{_options.TemplateName}”渲染已取消。", error, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ScribanTemplateException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw CreateRenderException(error, missingVariable, missingVariableSpan);
        }
    }

    private TemplateContext CreateContext(CancellationToken cancellationToken)
    {
        var context = new TemplateContext
        {
            StrictVariables = true,
            EnableRelaxedTargetAccess = false,
            EnableRelaxedMemberAccess = false,
            EnableRelaxedIndexerAccess = false,
            EnableRelaxedFunctionAccess = false,
            LoopLimit = _options.MaximumLoopCount,
            RecursiveLimit = _options.MaximumRecursionDepth,
            LimitToString = Math.Max(1, _options.MaximumOutputLength),
            OnStringLimit = ScriptLimitBehavior.Throw,
            OutputLimit = Math.Max(1, _options.MaximumOutputLength),
            OnOutputLimit = ScriptLimitBehavior.Throw,
            CancellationToken = cancellationToken,
            MemberRenamer = member => member.Name,
            TemplateLoader = null
        };
        context.BuiltinObject.Remove("include");
        context.PushCulture(_options.Culture);
        return context;
    }

    private ScriptObject CreateScriptObject(object model, int depth)
    {
        var result = new ScriptObject();
        if (model == null) return result;
        if (TryCreateDictionary(model, depth, out var dictionary)) return dictionary;

        var count = 0;
        foreach (var property in model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(item => item.CanRead && item.GetIndexParameters().Length == 0))
        {
            EnsureModelItemLimit(++count);
            result[property.Name] = ToScriptValue(property.GetValue(model), depth + 1);
        }

        return result;
    }

    private bool TryCreateDictionary(object model, int depth, out ScriptObject result)
    {
        result = null;
        if (model is IDictionary dictionary)
        {
            result = new ScriptObject();
            var count = 0;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (!(entry.Key is string key)) throw new TemplateModelException("模板字典的键必须是字符串。");
                EnsureModelItemLimit(++count);
                result[key] = ToScriptValue(entry.Value, depth + 1);
            }

            return true;
        }

        var dictionaryInterface = model.GetType().GetInterfaces().Concat(new[] { model.GetType() })
            .FirstOrDefault(type => type.IsGenericType &&
                                    (type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                                     type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));
        if (dictionaryInterface == null) return false;
        if (dictionaryInterface.GetGenericArguments()[0] != typeof(string))
            throw new TemplateModelException("模板字典的键必须是字符串。");

        result = new ScriptObject();
        var itemCount = 0;
        foreach (var entry in (IEnumerable)model)
        {
            var entryType = entry.GetType();
            var key = (string)entryType.GetProperty("Key").GetValue(entry);
            var value = entryType.GetProperty("Value").GetValue(entry);
            EnsureModelItemLimit(++itemCount);
            result[key] = ToScriptValue(value, depth + 1);
        }

        return true;
    }

    private object ToScriptValue(object value, int depth)
    {
        if (value == null) return null;
        if (IsScalar(value)) return value is Enum ? value.ToString() : value;
        if (depth > _options.MaximumModelDepth)
            throw new TemplateModelException($"模板模型嵌套超过 {_options.MaximumModelDepth} 层。");
        if (TryCreateDictionary(value, depth, out var dictionary)) return dictionary;
        if (value is IEnumerable sequence && !(value is string))
        {
            var array = new ScriptArray();
            var count = 0;
            foreach (var item in sequence)
            {
                EnsureModelItemLimit(++count);
                array.Add(ToScriptValue(item, depth + 1));
            }

            return array;
        }

        return CreateScriptObject(value, depth);
    }

    private void EnsureModelItemLimit(int count)
    {
        if (count > _options.MaximumModelItems)
            throw new TemplateModelException($"模板模型单个字典或集合超过 {_options.MaximumModelItems} 项。");
    }

    private static bool IsScalar(object value)
    {
        var type = value.GetType();
        return type.IsPrimitive || type.IsEnum || value is decimal || value is string || value is DateTime ||
               value is DateTimeOffset || value is TimeSpan || value is Guid;
    }

    private ScribanTemplateException CreateRenderException(Exception error, string missingVariable,
        SourceSpan? missingVariableSpan)
    {
        if (error is TemplateModelException modelError)
            return new ScribanTemplateException("template.model_invalid", _options.TemplateName, null, null,
                modelError.Message, $"模板“{_options.TemplateName}”输入模型无效。", modelError);

        var runtimeError = error as ScriptRuntimeException;
        var diagnostic = runtimeError?.OriginalMessage ?? error.Message;
        var position = missingVariableSpan ?? runtimeError?.Span;
        var (line, column) = GetLocation(position);
        if (diagnostic.IndexOf("OutputLimit", StringComparison.OrdinalIgnoreCase) >= 0 ||
            diagnostic.IndexOf("LimitToString", StringComparison.OrdinalIgnoreCase) >= 0)
            return CreateOutputLimitException(error, line, column, diagnostic);

        if (missingVariable != null)
        {
            var detail = $"{missingVariable}：{diagnostic}";
            return new ScribanTemplateException("template.variable_missing", _options.TemplateName, line, column,
                detail, FormatLocationMessage($"引用了未定义变量“{missingVariable}”。", line, column), error);
        }

        return new ScribanTemplateException("template.render", _options.TemplateName, line, column, diagnostic,
            FormatLocationMessage("渲染失败。", line, column), error);
    }

    private ScribanTemplateException CreateOutputLimitException(Exception innerException, int? line = null,
        int? column = null, string diagnostic = null)
    {
        diagnostic ??= $"输出超过 {_options.MaximumOutputLength} 个 UTF-16 字符限制。";
        innerException ??= new InvalidOperationException(diagnostic);
        return new ScribanTemplateException("template.output_limit", _options.TemplateName, line, column,
            diagnostic, FormatLocationMessage($"输出超过 {_options.MaximumOutputLength} 个 UTF-16 字符限制。", line, column),
            innerException);
    }

    private string FormatLocationMessage(string description, int? line, int? column)
    {
        var location = line.HasValue && column.HasValue ? $"第 {line} 行第 {column} 列" : "未提供源码位置";
        return $"模板“{_options.TemplateName}”{location}{description}";
    }

    private static (int? Line, int? Column) GetLocation(SourceSpan? span)
    {
        if (!span.HasValue) return (null, null);
        return (span.Value.Start.Line + 1, span.Value.Start.Column + 1);
    }

    private sealed class TemplateModelException : Exception
    {
        internal TemplateModelException(string message) : base(message) { }
    }
}

/// <summary>创建可复用的受限 Scriban 模板。</summary>
public sealed class ScribanTemplateEngine
{
    /// <summary>编译模板文本并在失败时返回带源码位置的结构化诊断。</summary>
    /// <param name="template">Scriban 模板文本。</param>
    /// <param name="options">编译和渲染限制；创建模板时会复制配置。</param>
    /// <returns>可复用且每次渲染使用独立上下文的已编译模板。</returns>
    public CompiledTemplate Compile(string template, ScribanTemplateOptions options = null)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        var snapshot = (options ?? new ScribanTemplateOptions()).Snapshot();
        if (template.Length > snapshot.MaximumTemplateLength)
        {
            var diagnostic = $"模板长度 {template.Length} 超过 {snapshot.MaximumTemplateLength} 个 UTF-16 字符限制。";
            throw new ScribanTemplateException("template.input_limit", snapshot.TemplateName, null, null, diagnostic,
                $"模板“{snapshot.TemplateName}”超过输入长度限制。", new FormatException(diagnostic));
        }

        var parsed = Template.Parse(template, snapshot.TemplateName);
        if (parsed.HasErrors)
        {
            var diagnostics = parsed.Messages.Select(message =>
            {
                var line = message.Span.Start.Line + 1;
                var column = message.Span.Start.Column + 1;
                return (message.Message, Line: line, Column: column);
            }).ToArray();
            var primary = diagnostics[0];
            var rawDiagnostic = string.Join(Environment.NewLine,
                diagnostics.Select(item => $"{item.Line}:{item.Column}: {item.Message}"));
            var innerException = new FormatException(rawDiagnostic);
            throw new ScribanTemplateException("template.compile", snapshot.TemplateName, primary.Line,
                primary.Column, rawDiagnostic,
                $"模板“{snapshot.TemplateName}”第 {primary.Line} 行第 {primary.Column} 列编译失败。", innerException);
        }

        return new CompiledTemplate(parsed, snapshot);
    }
}
