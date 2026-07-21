using Bing.Text.Templating;

namespace Bing.Utils.Tests.Bing.Text;

/// <summary>
/// <see cref="TemplateRenderer"/> 单元测试
/// </summary>
public class TemplateRendererTest
{
    /// <summary>
    /// 测试目的：应提取纯键名、按首次出现顺序去重。
    /// </summary>
    [Fact]
    public void GetKeys_RepeatedPlaceholders_ReturnsDistinctKeysInFirstAppearanceOrder()
    {
        // Arrange
        const string template = "{{name}}/{{name}}/{{age}}";

        // Act
        var result = TemplateRenderer.GetKeys(template);

        // Assert
        result.ShouldBe(new[] { "name", "age" });
    }

    /// <summary>
    /// 测试目的：相邻与重复占位符均应被正确渲染。
    /// </summary>
    [Fact]
    public void Render_RepeatedAndAdjacentPlaceholders_ReplacesEveryOccurrence()
    {
        // Arrange
        var values = new Dictionary<string, object>
        {
            ["first"] = "A",
            ["second"] = "B"
        };

        // Act
        var result = TemplateRenderer.Render("{{first}}{{second}}/{{first}}", values);

        // Assert
        result.ShouldBe("AB/A");
    }

    /// <summary>
    /// 测试目的：缺失键可按选项保留占位符或替换为空字符串。
    /// </summary>
    [Fact]
    public void Render_MissingValue_UsesConfiguredPreservationBehavior()
    {
        // Arrange
        var values = new Dictionary<string, object>();

        // Act
        var preserved = TemplateRenderer.Render("before {{missing}} after", values);
        var removed = TemplateRenderer.Render("before {{missing}} after", values,
            new TemplateOptions { PreserveUnresolvedPlaceholder = false });

        // Assert
        preserved.ShouldBe("before {{missing}} after");
        removed.ShouldBe("before  after");
    }

    /// <summary>
    /// 测试目的：大小写比较应由选项中的比较器决定，空值应渲染为空字符串。
    /// </summary>
    [Fact]
    public void Render_KeyComparerAndNullValue_UsesConfiguredComparisonAndEmptyText()
    {
        // Arrange
        var values = new Dictionary<string, object>
        {
            ["Name"] = "张三",
            ["Optional"] = null
        };
        var options = new TemplateOptions { KeyComparer = StringComparer.OrdinalIgnoreCase };

        // Act
        var result = TemplateRenderer.Render("{{name}}/{{optional}}", values, options);

        // Assert
        result.ShouldBe("张三/");
    }

    /// <summary>
    /// 测试目的：自定义分隔符、特殊字符和对象模型应共同生效。
    /// </summary>
    [Fact]
    public void Render_CustomDelimiterAndObjectModel_ReturnsRenderedText()
    {
        // Arrange
        var options = new TemplateOptions { StartDelimiter = "${", EndDelimiter = "}" };
        var model = new TemplateModel { Name = "A&B", Count = 2 };

        // Act
        var result = TemplateRenderer.Render("${Name}:${Count}", model, options);

        // Assert
        result.ShouldBe("A&B:2");
    }

    /// <summary>
    /// 测试目的：空和未闭合占位符应视为普通文本。
    /// </summary>
    [Fact]
    public void Render_EmptyOrIncompletePlaceholder_PreservesOriginalText()
    {
        // Arrange
        var values = new Dictionary<string, object>();

        // Act
        var empty = TemplateRenderer.Render("a{{}}b", values);
        var incomplete = TemplateRenderer.Render("a{{missing", values);

        // Assert
        empty.ShouldBe("a{{}}b");
        incomplete.ShouldBe("a{{missing");
    }

    /// <summary>
    /// 测试目的：比较器规则下重复的字典键应被拒绝，避免渲染歧义。
    /// </summary>
    [Fact]
    public void Render_DuplicateKeysUnderComparer_ThrowsArgumentException()
    {
        // Arrange
        IReadOnlyDictionary<string, object> values = new Dictionary<string, object>
        {
            ["name"] = "A",
            ["NAME"] = "B"
        };
        var options = new TemplateOptions { KeyComparer = StringComparer.OrdinalIgnoreCase };

        // Act and Assert
        Should.Throw<ArgumentException>(() => TemplateRenderer.Render("{{name}}", values, options)).ParamName.ShouldBe("values");
    }

    /// <summary>
    /// 测试目的：空模板和空对象模型应返回空字符串。
    /// </summary>
    [Fact]
    public void Render_EmptyTemplateAndNullModel_ReturnsEmptyString()
    {
        // Act
        var emptyTemplate = TemplateRenderer.Render(string.Empty, new Dictionary<string, object>());
        var nullModel = TemplateRenderer.Render(string.Empty, (object)null);

        // Assert
        emptyTemplate.ShouldBe(string.Empty);
        nullModel.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 模板对象模型。
    /// </summary>
    private sealed class TemplateModel
    {
        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// 数量。
        /// </summary>
        public int Count { get; init; }
    }
}