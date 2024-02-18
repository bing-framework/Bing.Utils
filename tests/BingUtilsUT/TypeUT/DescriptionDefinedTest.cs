 using Bing.Reflection;

 namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "TypeReflections.IsDescriptionDefined")]
public class DescriptionDefinedTest
{
    public Type EntryOne = typeof(NormalDescriptionClass);
    public Type EntryTwo = typeof(NormalDescriptionOrClass);
    public Type EntryThree = typeof(NormalDisplayNameOrClass);
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