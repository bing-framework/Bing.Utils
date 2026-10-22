namespace Bing.Reflection;
/// <summary>
/// 验证无参构造函数查找及参数签名匹配。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.Constructor")]
public class TypeVisitConstructorTest
{
    /// <summary>
    /// 验证无参构造函数检查拒绝空类型。
    /// </summary>
    [Fact]
    public void HasParameterlessConstructor_WhenTypeIsNull_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.HasParameterlessConstructor(null));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 验证能够识别公开无参构造函数。
    /// </summary>
    [Fact]
    public void HasParameterlessConstructor_WithParameterlessCtor_ReturnsTrue()
    {
        var result = TypeVisit.HasParameterlessConstructor(typeof(ParameterlessCtorClass));
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 验证仅有带参构造函数的类型不被判定为具有无参构造函数。
    /// </summary>
    [Fact]
    public void HasParameterlessConstructor_WithoutParameterlessCtor_ReturnsFalse()
    {
        var result = TypeVisit.HasParameterlessConstructor(typeof(OnlyParameterizedCtorClass));
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 验证查找到的无参构造函数不含参数。
    /// </summary>
    [Fact]
    public void GetParameterlessConstructor_WithParameterlessCtor_ReturnsCtor()
    {
        var ctor = TypeVisit.GetParameterlessConstructor(typeof(ParameterlessCtorClass));
        ctor.ShouldNotBeNull();
        ctor!.GetParameters().Length.ShouldBe(0);
    }
    /// <summary>
    /// 验证不存在公开无参构造函数时抛出参数异常。
    /// </summary>
    [Fact]
    public void GetParameterlessConstructor_WithoutParameterlessCtor_ThrowsArgumentException()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            TypeVisit.GetParameterlessConstructor(typeof(OnlyParameterizedCtorClass)));
        exception.ParamName.ShouldBe("args");
    }
    /// <summary>
    /// 验证按字符串与整数参数签名匹配构造函数。
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
    /// 验证空参数类型数组匹配无参构造函数。
    /// </summary>
    [Fact]
    public void GetMatchingConstructor_WithEmptyParameterTypes_ReturnsParameterlessCtor()
    {
        var ctor = TypeVisit.GetMatchingConstructor(typeof(ParameterlessCtorClass), Array.Empty<Type>());
        ctor.ShouldNotBeNull();
        ctor!.GetParameters().Length.ShouldBe(0);
    }
    /// <summary>
    /// 验证类型扩展入口提供相同的构造函数查找能力。
    /// </summary>
    [Fact]
    public void TypeMetaVisitExtensions_WrapTypeVisitCorrectly()
    {
        var type = typeof(MultiCtorClass);
        type.HasParameterlessConstructor().ShouldBeTrue();
        type.GetParameterlessConstructor().ShouldNotBeNull();
        type.GetMatchingConstructor(new[] { typeof(string), typeof(int) }).ShouldNotBeNull();
    }
    /// <summary>
    /// 提供隐式公开无参构造函数的测试类型。
    /// </summary>
    private class ParameterlessCtorClass
    {
    }
    /// <summary>
    /// 提供仅含带参构造函数的测试类型。
    /// </summary>
    private class OnlyParameterizedCtorClass
    {
        /// <summary>
        /// 初始化一个 <see cref="OnlyParameterizedCtorClass"/> 类型的实例。
        /// </summary>
        /// <param name="value">用于初始化只读属性的测试文本。</param>
        public OnlyParameterizedCtorClass(string value)
        {
            Value = value;
        }
        /// <summary>
        /// 获取构造函数传入的测试文本。
        /// </summary>
        public string Value { get; }
    }
    /// <summary>
    /// 提供无参与双参数构造函数的测试类型。
    /// </summary>
    private class MultiCtorClass
    {
        /// <summary>
        /// 初始化一个 <see cref="MultiCtorClass"/> 类型的实例。
        /// </summary>
        public MultiCtorClass()
        {
        }
        /// <summary>
        /// 初始化一个 <see cref="MultiCtorClass"/> 类型的实例。
        /// </summary>
        /// <param name="name">测试名称。</param>
        /// <param name="age">测试年龄。</param>
        public MultiCtorClass(string name, int age)
        {
            Name = name;
            Age = age;
        }
        /// <summary>
        /// 获取构造函数传入的测试名称。
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 获取构造函数传入的测试年龄。
        /// </summary>
        public int Age { get; }
    }
}

