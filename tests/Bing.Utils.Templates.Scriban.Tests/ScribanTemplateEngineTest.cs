using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Bing.Text.Templating.Scriban;
using Shouldly;
using Xunit;

namespace Bing.Utils.Templates.Scriban.Tests;

/// <summary>Scriban 模板引擎单元测试。</summary>
public sealed class ScribanTemplateEngineTest
{
    /// <summary>测试目的：DTO 属性、条件和循环应在独立上下文中正确渲染。</summary>
    [Fact]
    public async Task RenderAsync_WithDtoAndLoop_ShouldRenderExpectedText()
    {
        var template = new ScribanTemplateEngine().Compile(
            "{{ if enabled }}{{ for item in items }}{{ item }}{{ end }}{{ end }}");

        var result = await template.RenderAsync(new { enabled = true, items = new[] { "A", "B" } });

        result.ShouldBe("AB");
    }

    /// <summary>测试目的：可复用模板的并发渲染不得共享全局模型值。</summary>
    [Fact]
    public async Task RenderAsync_ConcurrentModels_ShouldUseIsolatedContexts()
    {
        var template = new ScribanTemplateEngine().Compile("{{ value }}");

        var first = template.RenderAsync(new { value = "甲" });
        var second = template.RenderAsync(new { value = "乙" });
        var results = await Task.WhenAll(first, second);

        results.ShouldBe(new[] { "甲", "乙" });
    }

    /// <summary>测试目的：泛型字典、只读字典、嵌套输入及中文键均应保留。</summary>
    [Fact]
    public async Task RenderAsync_WithTypedAndNestedDictionaries_ShouldPreserveKeys()
    {
        IReadOnlyDictionary<string, int> counts = new ReadOnlyDictionary<string, int>(
            new Dictionary<string, int> { ["数量"] = 7 });
        var model = new
        {
            values = new Dictionary<string, string> { ["姓名"] = "张三" },
            stats = counts
        };
        var template = new ScribanTemplateEngine().Compile("{{ values[\"姓名\"] }}|{{ stats[\"数量\"] }}");

        var result = await template.RenderAsync(model);

        result.ShouldBe("张三|7");
    }

    /// <summary>测试目的：编译诊断应包含模板名、原始诊断和 UTF-16 语义的 1-based 位置。</summary>
    [Fact]
    public void Compile_WithMultilineUnicodeError_ShouldReportOneBasedLocation()
    {
        var options = new ScribanTemplateOptions { TemplateName = "日报" };

        var error = Should.Throw<ScribanTemplateException>(() =>
            new ScribanTemplateEngine().Compile("first\n😀{{ if }}", options));

        error.ErrorCode.ShouldBe("template.compile");
        error.TemplateName.ShouldBe("日报");
        error.Line.ShouldBe(2);
        error.Column.ShouldBe(9);
        error.Diagnostic.ShouldContain("Expecting");
        error.InnerException.ShouldBeOfType<FormatException>();
    }

    /// <summary>测试目的：未定义变量应使用独立错误码并携带渲染异常。</summary>
    [Fact]
    public async Task RenderAsync_WhenVariableIsMissing_ShouldReturnStructuredError()
    {
        var template = new ScribanTemplateEngine().Compile("first\n{{ missing }}",
            new ScribanTemplateOptions { TemplateName = "缺失变量测试" });

        var error = await Should.ThrowAsync<ScribanTemplateException>(() => template.RenderAsync(null));

        error.ErrorCode.ShouldBe("template.variable_missing");
        error.TemplateName.ShouldBe("缺失变量测试");
        error.Line.ShouldBe(2);
        error.Column.HasValue.ShouldBeTrue();
        error.Column.Value.ShouldBeGreaterThan(0);
        error.Diagnostic.ShouldContain("missing");
        error.InnerException.ShouldNotBeNull();
    }

    /// <summary>测试目的：输出预算应在模板持续生成内容时触发，而不是生成后才检查。</summary>
    [Fact]
    public async Task RenderAsync_WhenLoopExceedsOutputBudget_ShouldThrowOutputLimitError()
    {
        var template = new ScribanTemplateEngine().Compile(
            "{{ for i in 0..100 }}1234567890{{ end }}",
            new ScribanTemplateOptions { MaximumOutputLength = 20 });

        var error = await Should.ThrowAsync<ScribanTemplateException>(() => template.RenderAsync(null));

        error.ErrorCode.ShouldBe("template.output_limit");
        error.InnerException.ShouldNotBeNull();
    }

    /// <summary>测试目的：模板源码长度超过配置预算时应在解析前拒绝。</summary>
    [Fact]
    public void Compile_WhenTemplateExceedsLengthLimit_ShouldThrowInputLimitError()
    {
        var error = Should.Throw<ScribanTemplateException>(() =>
            new ScribanTemplateEngine().Compile("123", new ScribanTemplateOptions { MaximumTemplateLength = 2 }));

        error.ErrorCode.ShouldBe("template.input_limit");
        error.Diagnostic.ShouldContain("超过");
    }

    /// <summary>测试目的：循环和递归均应受上下文运行预算约束。</summary>
    [Fact]
    public async Task RenderAsync_WhenLoopOrRecursionExceedsLimit_ShouldFail()
    {
        var engine = new ScribanTemplateEngine();
        var loop = engine.Compile("{{ for i in 0..100 }}x{{ end }}",
            new ScribanTemplateOptions { MaximumLoopCount = 10 });
        var recursion = engine.Compile(
            "{{ func recurse(n) }}{{ if n > 0 }}{{ recurse(n - 1) }}{{ end }}{{ end }}{{ recurse(100) }}",
            new ScribanTemplateOptions { MaximumRecursionDepth = 5 });

        var loopError = await Should.ThrowAsync<ScribanTemplateException>(() => loop.RenderAsync(null));
        var recursionError = await Should.ThrowAsync<ScribanTemplateException>(() => recursion.RenderAsync(null));

        loopError.ErrorCode.ShouldBe("template.render");
        recursionError.ErrorCode.ShouldBe("template.render");
    }

    /// <summary>测试目的：未配置模板加载器时 include 必须不可用。</summary>
    [Fact]
    public async Task RenderAsync_WhenTemplateUsesInclude_ShouldRejectIt()
    {
        var template = new ScribanTemplateEngine().Compile("{{ include \"external\" }}");

        var error = await Should.ThrowAsync<ScribanTemplateException>(() => template.RenderAsync(null));

        error.ErrorCode.ShouldBe("template.variable_missing");
    }

    /// <summary>测试目的：模板不能调用模型 DTO 上的宿主方法。</summary>
    [Fact]
    public async Task RenderAsync_WhenModelHasHostMethod_ShouldNotExposeMethod()
    {
        var template = new ScribanTemplateEngine().Compile("{{ input.RevealSecret() }}");

        var error = await Should.ThrowAsync<ScribanTemplateException>(() =>
            template.RenderAsync(new { input = new HostObject() }));

        error.ErrorCode.ShouldBe("template.render");
        error.Diagnostic.ShouldNotContain("secret-value");
    }

    /// <summary>测试目的：异步循环执行过程中应观察令牌并映射为取消异常。</summary>
    [Fact]
    public async Task RenderAsync_WhenCanceledDuringLongLoop_ShouldThrowOperationCanceled()
    {
        var template = new ScribanTemplateEngine().Compile(
            "{{ for i in 0..100000000 }}{{ end }}",
            new ScribanTemplateOptions { MaximumLoopCount = 200_000_000 });
        using var cancellation = new CancellationTokenSource();
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(25));

        await Should.ThrowAsync<OperationCanceledException>(() =>
            template.RenderAsync(null, cancellation.Token));
    }

    /// <summary>测试目的：显式文化应控制数字渲染，且配置在编译后固定。</summary>
    [Fact]
    public async Task RenderAsync_WithExplicitCulture_ShouldUseCultureSnapshot()
    {
        var options = new ScribanTemplateOptions { Culture = CultureInfo.GetCultureInfo("fr-FR") };
        var template = new ScribanTemplateEngine().Compile("{{ price }}", options);
        options.Culture = CultureInfo.InvariantCulture;

        var result = await template.RenderAsync(new { price = 1234.5 });

        result.ShouldBe("1234,5");
    }

    private sealed class HostObject
    {
        public string RevealSecret() => "secret-value";
    }
}
