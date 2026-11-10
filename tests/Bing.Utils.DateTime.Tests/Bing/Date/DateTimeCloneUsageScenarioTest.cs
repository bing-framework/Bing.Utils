using System;
using Bing.Date;

namespace Bing.Utils.DateTimeScenario;

/// <summary>
/// 验证普通业务命名空间同时引用 System 和 Bing.Date 时可直接克隆日期值。
/// </summary>
public class DateTimeCloneUsageScenarioTest
{
    /// <summary>
    /// 验证扩展调用没有编译歧义，且两个既有静态入口保留相同的复制行为。
    /// </summary>
    /// <param name="kind">日期值的种类。</param>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Clone_WithSystemAndDateImports_CompilesAndPreservesValue(DateTimeKind kind)
    {
        var legacyClone = (Func<System.DateTime, System.DateTime>)typeof(DateInfo).Assembly
            .GetType("Bing.Date.DateTimeExtensions")!
            .GetMethod("Clone", new[] { typeof(System.DateTime) })!
            .CreateDelegate(typeof(Func<System.DateTime, System.DateTime>));
        foreach (var value in new[]
        {
            System.DateTime.MinValue,
            new System.DateTime(2024, 6, 15, 12, 30, 0).AddTicks(1234567),
            System.DateTime.MaxValue
        })
        {
            var source = System.DateTime.SpecifyKind(value, kind);
            var clone = source.Clone();

            clone.Ticks.ShouldBe(source.Ticks);
            clone.Kind.ShouldBe(kind);
            clone.ToBinary().ShouldBe(source.ToBinary());
            System.BingDateTimeExtensions.Clone(source).ToBinary().ShouldBe(source.ToBinary());
            legacyClone(source).ToBinary().ShouldBe(source.ToBinary());
        }
    }
}
