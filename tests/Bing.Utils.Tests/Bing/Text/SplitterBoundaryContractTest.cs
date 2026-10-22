using System.Text.RegularExpressions;
using Bing.Text.Splitters;
namespace Bing.Text;
/// <summary>
/// 测试类：覆盖 `SplitterBoundaryContract` 相关行为。
/// </summary>
[Trait("Bing.Text", "Splitter.Boundary")]
public class SplitterBoundaryContractTest
{
    /// <summary>
    /// 测试用例：零长度在构造分割器时即被拒绝，不延迟到分割阶段。
    /// </summary>
    [Fact]
    public void FixedLength_ZeroLength_ThrowsArgumentOutOfRangeExceptionAtCreation()
    {
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => Splitter.FixedLength(0));
        exception.ParamName.ShouldBe("length");
    }
    /// <summary>
    /// 测试用例：验证 `OnPattern` 在 `NullPattern` 场景下，结果为 `ThrowsArgumentNullExceptionWhenSplit`。
    /// </summary>
    [Fact]
    public void OnPattern_NullPattern_ThrowsArgumentNullExceptionWhenSplit()
    {
        var splitter = Splitter.OnPattern(null);
        var ex = Should.Throw<ArgumentNullException>(() => splitter.SplitToArray("a,b"));
        ex.ParamName.ShouldBe("pattern");
    }
    /// <summary>
    /// 测试用例：验证 `On` 在 `TrimResultsWithNullFunc` 场景下，结果为 `FallsBackToDefaultTrim`。
    /// </summary>
    [Fact]
    public void On_TrimResultsWithNullFunc_FallsBackToDefaultTrim()
    {
        var result = Splitter.On(',')
            .TrimResults(null)
            .SplitToArray(" a , b ");
        result.ShouldBe(["a", "b"]);
    }
    /// <summary>
    /// 测试用例：验证 `On` 在 `LimitZero` 场景下，结果为 `CurrentBehaviorTreatsAsNoLimit`。
    /// </summary>
    [Fact]
    public void On_LimitZero_CurrentBehaviorTreatsAsNoLimit()
    {
        var result = Splitter.On(',')
            .Limit(0)
            .SplitToArray("a,b,c");
        result.ShouldBe(["a", "b", "c"]);
    }
}

