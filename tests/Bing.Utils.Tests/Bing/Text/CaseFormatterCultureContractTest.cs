using System.Globalization;
using System.Text;

namespace Bing.Text;

/// <summary>
/// 测试类：覆盖 `CaseFormatter` 的文化相关与 Unicode 契约行为。
/// </summary>
[Trait("Bing.Text", "CaseFormatter.CultureContract")]
public class CaseFormatterCultureContractTest
{
    /// <summary>
    /// 测试用例：验证 `To` 在 `TurkishCultureUpperUnderscore` 场景下，结果为 `UsesCurrentCultureUppercaseRules`。
    /// </summary>
    [Fact]
    public void To_TurkishCultureUpperUnderscore_UsesCurrentCultureUppercaseRules()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var turkish = new CultureInfo("tr-TR");
            CultureInfo.CurrentCulture = turkish;
            CultureInfo.CurrentUICulture = turkish;

            var result = CaseFormatter.Instance.To(CaseFormatter.Style.UpperUnderscore, "id");

            result.ShouldBe("İD");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    /// <summary>
    /// 测试用例：验证 `To` 在 `TurkishCultureLowerUnderscore` 场景下，结果为 `UsesCurrentCultureLowercaseRules`。
    /// </summary>
    [Fact]
    public void To_TurkishCultureLowerUnderscore_UsesCurrentCultureLowercaseRules()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var turkish = new CultureInfo("tr-TR");
            CultureInfo.CurrentCulture = turkish;
            CultureInfo.CurrentUICulture = turkish;

            var result = CaseFormatter.Instance.To(CaseFormatter.Style.LowerUnderscore, "I_D");

            result.ShouldBe("ı_d");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    /// <summary>
    /// 测试用例：验证 `To` 在 `EmojiTokenInput` 场景下，结果为 `KeepsSurrogatePairTokenIntact`。
    /// </summary>
    [Fact]
    public void To_EmojiTokenInput_KeepsSurrogatePairTokenIntact()
    {
        var result = CaseFormatter.Instance.To(CaseFormatter.Style.UpperCamelWithWhiteSpace, "hello-😀-world");

        result.ShouldBe("Hello 😀 World");
        result.ShouldContain("😀");
    }

    /// <summary>
    /// 测试用例：验证 `To` 在 `ComposedAndDecomposedInput` 场景下，结果为 `EqualsAfterNormalization`。
    /// </summary>
    [Fact]
    public void To_ComposedAndDecomposedInput_EqualsAfterNormalization()
    {
        var composed = "café-au-lait";
        var decomposed = "cafe\u0301-au-lait";

        var composedResult = CaseFormatter.Instance.To(CaseFormatter.Style.UpperCamel, composed);
        var decomposedResult = CaseFormatter.Instance.To(CaseFormatter.Style.UpperCamel, decomposed);

        composedResult.Normalize(NormalizationForm.FormC)
            .ShouldBe(decomposedResult.Normalize(NormalizationForm.FormC));
    }
}
