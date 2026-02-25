namespace Bing.Helpers;

/// <summary>
/// 测试类：Id 生成器回归测试
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class IdRegressionTest : IDisposable
{
    /// <summary>
    /// 测试初始化
    /// </summary>
    public IdRegressionTest()
    {
        Id.Reset();
        Id.ResetLong();
        Id.ResetString();
        Id.ResetGuid();
    }

    /// <summary>
    /// 测试清理
    /// </summary>
    public void Dispose()
    {
        Id.Reset();
        Id.ResetLong();
        Id.ResetString();
        Id.ResetGuid();
    }

    /// <summary>
    /// 测试用例：未配置 Long 生成器且无上下文 Id 时，应抛出明确的配置异常
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "IdUtils.Id.UnconfiguredLongGenerator")]
    public void CreateLong_NoGeneratorConfigured_ShouldThrowInvalidOperationException()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Id.CreateLong());

        exception.Message.ShouldContain("LongGenerateFunc");
    }
}
