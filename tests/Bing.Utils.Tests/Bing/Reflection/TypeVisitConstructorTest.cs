namespace Bing.Reflection;
/// <summary>
/// 测试类：覆盖 `TypeVisitConstructor` 相关行为。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.Constructor")]
public class TypeVisitConstructorTest
{
    /// <summary>
    /// 测试用例：验证 `HasParameterlessConstructor` 在 `WhenTypeIsNull` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void HasParameterlessConstructor_WhenTypeIsNull_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.HasParameterlessConstructor(null));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 测试用例：验证 `HasParameterlessConstructor` 在 `WithParameterlessCtor` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void HasParameterlessConstructor_WithParameterlessCtor_ReturnsTrue()
    {
        var result = TypeVisit.HasParameterlessConstructor(typeof(ParameterlessCtorClass));
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `HasParameterlessConstructor` 在 `WithoutParameterlessCtor` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void HasParameterlessConstructor_WithoutParameterlessCtor_ReturnsFalse()
    {
        var result = TypeVisit.HasParameterlessConstructor(typeof(OnlyParameterizedCtorClass));
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `GetParameterlessConstructor` 在 `WithParameterlessCtor` 场景下，结果为 `ReturnsCtor`。
    /// </summary>
    [Fact]
    public void GetParameterlessConstructor_WithParameterlessCtor_ReturnsCtor()
    {
        var ctor = TypeVisit.GetParameterlessConstructor(typeof(ParameterlessCtorClass));
        ctor.ShouldNotBeNull();
        ctor!.GetParameters().Length.ShouldBe(0);
    }
    /// <summary>
    /// 测试用例：验证 `GetParameterlessConstructor` 在 `WithoutParameterlessCtor` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void GetParameterlessConstructor_WithoutParameterlessCtor_ReturnsNull()
    {
        var ctor = TypeVisit.GetParameterlessConstructor(typeof(OnlyParameterizedCtorClass));
        ctor.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `GetMatchingConstructor` 在 `WithMatchingParameterTypes` 场景下，结果为 `ReturnsCtor`。
    /// </summary>
    [Fact]
    public void GetMatchingConstructor_WithMatchingParameterTypes_ReturnsCtor()
    {
        var ctor = TypeVisit.GetMatchingConstructor(typeof(MultiCtorClass), new[] { typeof(string), typeof(int) });
        ctor.ShouldNotBeNull();
        var parameters = ctor!.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[1].ParameterType.ShouldBe(typeof(int));
    }
    /// <summary>
    /// 测试用例：验证 `GetMatchingConstructor` 在 `WithEmptyParameterTypes` 场景下，结果为 `ReturnsParameterlessCtor`。
    /// </summary>
    [Fact]
    public void GetMatchingConstructor_WithEmptyParameterTypes_ReturnsParameterlessCtor()
    {
        var ctor = TypeVisit.GetMatchingConstructor(typeof(ParameterlessCtorClass), Array.Empty<Type>());
        ctor.ShouldNotBeNull();
        ctor!.GetParameters().Length.ShouldBe(0);
    }
    /// <summary>
    /// 测试用例：验证 `TypeMetaVisitExtensions` 在 `WrapTypeVisitCorrectly` 场景下的行为。
    /// </summary>
    [Fact]
    public void TypeMetaVisitExtensions_WrapTypeVisitCorrectly()
    {
        var type = typeof(MultiCtorClass);
        type.HasParameterlessConstructor().ShouldBeTrue();
        type.GetParameterlessConstructor().ShouldNotBeNull();
        type.GetMatchingConstructor(new[] { typeof(string), typeof(int) }).ShouldNotBeNull();
    }
    private class ParameterlessCtorClass
    {
    }
    private class OnlyParameterizedCtorClass
    {
        public OnlyParameterizedCtorClass(string value)
        {
            Value = value;
        }
        public string Value { get; }
    }
    private class MultiCtorClass
    {
        public MultiCtorClass()
        {
        }
        public MultiCtorClass(string name, int age)
        {
            Name = name;
            Age = age;
        }
        public string Name { get; }
        public int Age { get; }
    }
}

