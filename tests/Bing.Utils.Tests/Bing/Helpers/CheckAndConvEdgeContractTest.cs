using System.ComponentModel;

namespace Bing.Helpers;

/// <summary>
/// 测试类：覆盖 Check 与 Conv 的边界契约行为。
/// </summary>
[Trait("Bing.Helpers", "CheckAndConv.EdgeContract")]
public class CheckAndConvEdgeContractTest
{
    /// <summary>
    /// 测试用例：Check.NotNullOrEmpty(字典) 在字典为 null 时应抛出 ArgumentNullException，且参数名正确。
    /// </summary>
    [Fact]
    public void NotNullOrEmpty_DictionaryNull_ThrowsArgumentNullExceptionWithParamName()
    {
        IDictionary<string, int> dictionary = null;

        var ex = Should.Throw<ArgumentNullException>(() => Check.NotNullOrEmpty(dictionary, nameof(dictionary)));

        ex.ParamName.ShouldBe(nameof(dictionary));
    }

    /// <summary>
    /// 测试用例：Check.NotNullOrEmpty(字典) 在空字典/非空字典场景下，当前实现均会抛出 FormatException。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetDictionaryInputsForCurrentFormatExceptionCases))]
    public void NotNullOrEmpty_DictionaryInputs_CurrentlyThrowFormatException(IDictionary<string, int> dictionary)
    {
        Should.Throw<FormatException>(() => Check.NotNullOrEmpty(dictionary, nameof(dictionary)));
    }

    /// <summary>
    /// 测试用例：Check.Required 指定异常类型且存在 message 构造函数时，应抛出该异常并带消息。
    /// </summary>
    [Fact]
    public void Required_GenericExceptionWithMessageCtor_ThrowsTypedExceptionWithMessage()
    {
        var ex = Should.Throw<CustomMessageException>(() =>
            Check.Required<int, CustomMessageException>(1, x => x > 10, "boom"));

        ex.Message.ShouldContain("boom");
    }

    /// <summary>
    /// 测试用例：Check.Required 指定异常类型但不存在 message 构造函数时，当前实现会抛出 MissingMethodException。
    /// </summary>
    [Fact]
    public void Required_GenericExceptionWithoutMessageCtor_CurrentlyThrowsMissingMethodException()
    {
        Should.Throw<MissingMethodException>(() =>
            Check.Required<int, CustomNoMessageCtorException>(1, x => x > 10, "boom"));
    }

    /// <summary>
    /// 测试用例：Conv.ToDictionary(useDisplayName=true) 在显示名重复场景下，当前实现应抛出重复键异常。
    /// </summary>
    [Fact]
    public void ToDictionary_UseDisplayNameWithDuplicateKeys_CurrentlyThrowsArgumentException()
    {
        var source = new DuplicateDisplayNameSample
        {
            First = "A",
            Second = "B"
        };

        Should.Throw<ArgumentException>(() => Conv.ToDictionary(source, useDisplayName: true));
    }

    /// <summary>
    /// 测试用例：Conv.ToDictionary 对键值对集合输入时，useDisplayName 参数不应影响键名。
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ToDictionary_KeyValueEnumerableInput_UseDisplayNameFlag_DoesNotChangeKeys(bool useDisplayName)
    {
        var source = new Dictionary<string, object>
        {
            ["Code"] = "A1",
            ["Price"] = 10
        };

        var result = Conv.ToDictionary(source, useDisplayName);

        result.Keys.ShouldBe(new[] { "Code", "Price" }, ignoreOrder: true);
        result["Code"].ShouldBe("A1");
        result["Price"].ShouldBe(10);
    }

    /// <summary>
    /// 测试用例：Conv.ToDictionary(useDisplayName=true) 在同时存在 Description/DisplayName 时，应优先使用 Description。
    /// </summary>
    [Fact]
    public void ToDictionary_UseDisplayName_PrefersDescriptionOverDisplayName()
    {
        var source = new DescriptionAndDisplayNameSample
        {
            Name = "Alpha"
        };

        var result = Conv.ToDictionary(source, useDisplayName: true);

        result.Keys.ShouldContain("描述名");
        result.Keys.ShouldNotContain("显示名");
        result["描述名"].ShouldBe("Alpha");
    }

    /// <summary>
    /// 测试用例：Conv.ToDictionary(useDisplayName=true) 在仅存在 DisplayName 特性时应使用 DisplayName。
    /// </summary>
    [Fact]
    public void ToDictionary_UseDisplayName_UsesDisplayNameWhenDescriptionMissing()
    {
        var source = new DisplayNameOnlySample
        {
            Name = "Beta"
        };

        var result = Conv.ToDictionary(source, useDisplayName: true);

        result.Keys.ShouldContain("显示名称");
        result["显示名称"].ShouldBe("Beta");
    }

    /// <summary>
    /// 测试用例：Conv.ToDictionary(useDisplayName=false) 即使存在 Description/DisplayName，也应使用原始属性名。
    /// </summary>
    [Fact]
    public void ToDictionary_UseDisplayNameFalse_UsesRawPropertyName()
    {
        var source = new DescriptionAndDisplayNameSample
        {
            Name = "Alpha"
        };

        var result = Conv.ToDictionary(source, useDisplayName: false);

        result.Keys.ShouldContain(nameof(DescriptionAndDisplayNameSample.Name));
        result[nameof(DescriptionAndDisplayNameSample.Name)].ShouldBe("Alpha");
    }

    /// <summary>
    /// 测试用例：Conv.ToDictionary(键值对集合) 遇到重复键时，应抛出 ArgumentException。
    /// </summary>
    [Fact]
    public void ToDictionary_KeyValueEnumerableWithDuplicateKeys_ThrowsArgumentException()
    {
        var source = new List<KeyValuePair<string, object>>
        {
            new("Code", "A1"),
            new("Code", "A2")
        };

        Should.Throw<ArgumentException>(() => Conv.ToDictionary(source, useDisplayName: false));
    }

    /// <summary>
    /// 测试用例：Check.Required 当断言函数为空时，应抛出 ArgumentNullException 并包含参数名 assertionFunc。
    /// </summary>
    [Fact]
    public void Required_NullAssertionFunc_ThrowsArgumentNullExceptionWithParamName()
    {
        var ex = Should.Throw<ArgumentNullException>(() => Check.Required(1, null, "msg"));

        ex.ParamName.ShouldBe("assertionFunc");
    }

    /// <summary>
    /// 测试用例：Conv.ToDictionary 在输入为 null 时应返回空字典。
    /// </summary>
    [Fact]
    public void ToDictionary_NullInput_ReturnsEmptyDictionary()
    {
        var result = Conv.ToDictionary(null, useDisplayName: true);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试数据：Check.NotNullOrEmpty(字典) 当前会抛出 FormatException 的输入。
    /// </summary>
    public static IEnumerable<object[]> GetDictionaryInputsForCurrentFormatExceptionCases()
    {
        yield return new object[] { new Dictionary<string, int>() };
        yield return new object[] { new Dictionary<string, int> { ["A"] = 1 } };
    }

    private sealed class CustomMessageException : Exception
    {
        public CustomMessageException(string message) : base(message)
        {
        }
    }

    private sealed class CustomNoMessageCtorException : Exception
    {
        public CustomNoMessageCtorException()
        {
        }
    }

    private sealed class DuplicateDisplayNameSample
    {
        [Description("重复键")]
        public string First { get; set; }

        [Description("重复键")]
        public string Second { get; set; }
    }

    private sealed class DescriptionAndDisplayNameSample
    {
        [Description("描述名")]
        [DisplayName("显示名")]
        public string Name { get; set; }
    }

    private sealed class DisplayNameOnlySample
    {
        [DisplayName("显示名称")]
        public string Name { get; set; }
    }
}
