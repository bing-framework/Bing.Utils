using Bing.Reflection;
namespace BingUtilsUT.TypeUT;

/// <summary>
/// 验证类型及其成员是否定义 Description 元数据。
/// </summary>
[Trait("TypeUT", "TypeReflections.IsDescriptionDefined")]
public class DescriptionDefinedTest
{
    /// <summary>
    /// 获取带 Description 特性的测试类型。
    /// </summary>
    public Type EntryOne = typeof(NormalDescriptionClass);
    /// <summary>
    /// 获取带继承式 Description 元数据的测试类型。
    /// </summary>
    public Type EntryTwo = typeof(NormalDescriptionOrClass);
    /// <summary>
    /// 获取带 DisplayName 元数据的测试类型。
    /// </summary>
    public Type EntryThree = typeof(NormalDisplayNameOrClass);
    /// <summary>
    /// 获取包装其他类型的测试类型。
    /// </summary>
    public Type Wrapper = typeof(NormalDescriptionWrapper);
    /// <summary>
    /// 测试 - 类型 - 是否定义 <see cref="DescriptionAttribute"/> 或 <see cref="DisplayAttribute"/> 
    /// </summary>
    [Fact]
    public void Test_DirectType_IsDescriptionDefined()
    {
        TypeReflections.IsDescriptionDefined(EntryOne).ShouldBeTrue();
        TypeReflections.IsDescriptionDefined(EntryTwo).ShouldBeTrue();
        TypeReflections.IsDescriptionDefined(EntryThree).ShouldBeTrue();
        TypeReflections.IsDescriptionDefined(Wrapper).ShouldBeFalse();
        TypeReflections.IsDescriptionDefined(Wrapper, ReflectionOptions.Inherit).ShouldBeTrue();
    }
}
