namespace Bing.Reflection;
/// <summary>
/// 验证方法签名匹配、成员可见性及属性元数据的边界行为。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.MethodsAndProperties.Boundary")]
public class TypeVisitMethodsAndPropertiesBoundaryTest
{
    /// <summary>
    /// 验证按方法签名查找时拒绝空类型，并标明参数名。
    /// </summary>
    [Fact]
    public void GetMethodBySignature_NullType_ThrowsArgumentNullException()
    {
        var method = typeof(MethodSample).GetMethod(nameof(MethodSample.Execute), new[] { typeof(int) });
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.GetMethodBySignature(null, method));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 验证按方法签名查找时拒绝空方法，并标明参数名。
    /// </summary>
    [Fact]
    public void GetMethodBySignature_NullMethod_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.GetMethodBySignature(typeof(MethodSample), null));
        ex.ParamName.ShouldBe("method");
    }
    /// <summary>
    /// 验证按名称及参数类型匹配对应的方法重载。
    /// </summary>
    [Fact]
    public void GetMethodBySignature_WithMatchingSignature_ReturnsMethod()
    {
        var method = typeof(MethodSample).GetMethod(nameof(MethodSample.Execute), new[] { typeof(int) });
        var result = TypeVisit.GetMethodBySignature(typeof(MethodSample), method);
        result.ShouldNotBeNull();
        result!.Name.ShouldBe(nameof(MethodSample.Execute));
        result.GetParameters()[0].ParameterType.ShouldBe(typeof(int));
    }
    /// <summary>
    /// 验证重写方法可以追溯到基类声明。
    /// </summary>
    [Fact]
    public void GetBaseMethod_OnOverrideMethod_ReturnsBaseMethod()
    {
        var method = typeof(DerivedMethodSample).GetMethod(nameof(DerivedMethodSample.VirtualMethod));
        var baseMethod = TypeVisit.GetBaseMethod(method);
        baseMethod.ShouldNotBeNull();
        baseMethod!.DeclaringType.ShouldBe(typeof(BaseMethodSample));
        baseMethod.Name.ShouldBe(nameof(BaseMethodSample.VirtualMethod));
    }

    /// <summary>
    /// 验证空方法的基方法查询结果为空。
    /// </summary>
    [Fact]
    public void GetBaseMethod_NullMethod_ReturnsNull()
    {
        var baseMethod = TypeVisit.GetBaseMethod(null);

        baseMethod.ShouldBeNull();
    }
    /// <summary>
    /// 验证公开虚方法同时满足可见性及虚方法判定。
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_WithVisibleVirtualMethod_ReturnsTrue()
    {
        var method = typeof(BaseMethodSample).GetMethod(nameof(BaseMethodSample.VirtualMethod));
        var result = TypeVisit.IsVisibleAndVirtual(method);
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 验证私有方法不满足可见性判定。
    /// </summary>
    [Fact]
    public void IsVisible_WithPrivateMethod_ReturnsFalse()
    {
        var method = typeof(MethodSample).GetMethod("Hidden", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = TypeVisit.IsVisible(method);
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 验证方法可见性检查拒绝空方法，并标明参数名。
    /// </summary>
    [Fact]
    public void IsVisible_NullMethod_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.IsVisible(null));
        ex.ParamName.ShouldBe("method");
    }

    /// <summary>
    /// 验证可见虚方法检查拒绝空方法，并标明参数名。
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_NullMethod_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.IsVisibleAndVirtual((MethodInfo)null));

        ex.ParamName.ShouldBe("method");
    }
    /// <summary>
    /// 验证不同返回类型的异步方法识别结果。
    /// </summary>
    /// <remarks>保留当前对 <see cref="Task"/>、<see cref="Task{TResult}"/> 和 <see cref="ValueTask"/> 的识别，以及对 <see cref="ValueTask{TResult}"/> 和普通整数返回类型的否定判定。</remarks>
    [Fact]
    public void IsAsyncMethod_ShouldReturnExpectedResult()
    {
        var taskMethod = typeof(AsyncMethodSample).GetMethod(nameof(AsyncMethodSample.ReturnTask));
        var taskOfTMethod = typeof(AsyncMethodSample).GetMethod(nameof(AsyncMethodSample.ReturnTaskOfT));
        var valueTaskMethod = typeof(AsyncMethodSample).GetMethod(nameof(AsyncMethodSample.ReturnValueTask));
        var valueTaskOfTMethod = typeof(AsyncMethodSample).GetMethod(nameof(AsyncMethodSample.ReturnValueTaskOfT));
        var normalMethod = typeof(AsyncMethodSample).GetMethod(nameof(AsyncMethodSample.ReturnInt));
        taskMethod.IsAsyncMethod().ShouldBeTrue();
        taskOfTMethod.IsAsyncMethod().ShouldBeTrue();
        valueTaskMethod.IsAsyncMethod().ShouldBeTrue();
        valueTaskOfTMethod.IsAsyncMethod().ShouldBeFalse();
        normalMethod.IsAsyncMethod().ShouldBeFalse();
    }
    /// <summary>
    /// 验证方法名称格式包含方法名，完全限定名称还包含参数括号。
    /// </summary>
    [Fact]
    public void GetFullNameAndFullyQualifiedName_ShouldContainMethodName()
    {
        var method = typeof(MethodSample).GetMethod(nameof(MethodSample.Execute), new[] { typeof(string) });
        var fullName = TypeVisit.GetFullName(method);
        var qualifiedName = TypeVisit.GetFullyQualifiedName(method);
        fullName.ShouldContain(nameof(MethodSample.Execute));
        qualifiedName.ShouldContain(nameof(MethodSample.Execute));
        qualifiedName.ShouldContain("(");
        qualifiedName.ShouldContain(")");
    }

    /// <summary>
    /// 验证空方法的完整名称查询当前抛出空引用异常。
    /// </summary>
    [Fact]
    public void GetFullName_NullMethod_CurrentBehaviorThrowsNullReferenceException()
    {
        Should.Throw<NullReferenceException>(() => TypeVisit.GetFullName((MethodInfo)null));
    }

    /// <summary>
    /// 验证空方法的完全限定名称查询当前抛出空引用异常。
    /// </summary>
    [Fact]
    public void GetFullyQualifiedName_NullMethod_CurrentBehaviorThrowsNullReferenceException()
    {
        Should.Throw<NullReferenceException>(() => TypeVisit.GetFullyQualifiedName((MethodInfo)null));
    }
    /// <summary>
    /// 验证属性枚举拒绝空类型，并标明参数名。
    /// </summary>
    [Fact]
    public void GetProperties_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.GetProperties(null, PropertyAccessOptions.Both).ToList());
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 验证属性枚举拒绝未定义的访问选项。
    /// </summary>
    [Fact]
    public void GetProperties_UnknownAccessOption_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() =>
            TypeVisit.GetProperties(typeof(PropertySample), (PropertyAccessOptions)999).ToList());
    }
    /// <summary>
    /// 验证属性排除操作拒绝空属性集合或空表达式集合。
    /// </summary>
    [Fact]
    public void Exclude_NullArguments_ThrowArgumentNullException()
    {
        var ex1 = Should.Throw<ArgumentNullException>(() =>
            TypeVisit.Exclude<PropertySample>(null, new Expression<Func<PropertySample, object>>[] { x => x.Normal }).ToList());
        ex1.ParamName.ShouldBe("properties");
        var properties = TypeVisit.GetProperties(typeof(PropertySample), PropertyAccessOptions.Both);
        var ex2 = Should.Throw<ArgumentNullException>(() =>
            TypeVisit.Exclude<PropertySample>(properties, (IEnumerable<Expression<Func<PropertySample, object>>>)null).ToList());
        ex2.ParamName.ShouldBe("expressions");
    }
    /// <summary>
    /// 验证只写虚属性的可见虚成员检查当前抛出参数空异常。
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_WriteOnlyProperty_CurrentlyThrowsArgumentNullException()
    {
        var property = typeof(WriteOnlyVirtualPropertySample).GetProperty(nameof(WriteOnlyVirtualPropertySample.WriteOnly));
        Should.Throw<ArgumentNullException>(() => TypeVisit.IsVisibleAndVirtual(property));
    }
    /// <summary>
    /// 验证抽象属性的重写实现及普通属性的虚拟性和抽象性判定。
    /// </summary>
    [Fact]
    public void PropertyMetaExtensions_IsVirtualAndIsAbstract_ReturnExpected()
    {
        var virtualProperty = typeof(DerivedAbstractPropertySample).GetProperty(nameof(DerivedAbstractPropertySample.Value));
        var normalProperty = typeof(PropertySample).GetProperty(nameof(PropertySample.Normal));
        virtualProperty.IsVirtual().ShouldBeTrue();
        virtualProperty.IsAbstract().ShouldBeFalse();
        normalProperty.IsVirtual().ShouldBeFalse();
        normalProperty.IsAbstract().ShouldBeFalse();
    }
    /// <summary>
    /// 验证空属性的虚拟性及抽象性检查当前均抛出空引用异常。
    /// </summary>
    [Fact]
    public void PropertyMetaExtensions_NullProperty_CurrentBehaviorThrowsNullReferenceException()
    {
        PropertyInfo property = null;
        Should.Throw<NullReferenceException>(() => property.IsVirtual());
        Should.Throw<NullReferenceException>(() => property.IsAbstract());
    }
    /// <summary>
    /// 提供不同参数类型的方法重载及私有方法的反射样例。
    /// </summary>
    private class MethodSample
    {
        /// <summary>
        /// 提供用于按参数类型匹配签名的空操作重载。
        /// </summary>
        /// <param name="value">用于形成整数参数签名的样例值，调用时不作处理。</param>
        public void Execute(int value)
        {
            _ = value;
        }
        /// <summary>
        /// 提供用于按参数类型匹配签名的空操作重载。
        /// </summary>
        /// <param name="value">用于形成字符串参数签名的样例值，调用时不作处理。</param>
        public void Execute(string value)
        {
            _ = value;
        }
        /// <summary>
        /// 提供用于验证私有方法可见性的空操作。
        /// </summary>
        private void Hidden()
        {
        }
    }
    /// <summary>
    /// 用于验证虚方法重写关系的基类样例。
    /// </summary>
    private class BaseMethodSample
    {
        /// <summary>
        /// 提供用于验证虚方法重写关系的空操作。
        /// </summary>
        public virtual void VirtualMethod()
        {
        }
    }
    /// <summary>
    /// 提供虚方法重写实现以验证基类方法追溯。
    /// </summary>
    private class DerivedMethodSample : BaseMethodSample
    {
        /// <inheritdoc />
        public override void VirtualMethod()
        {
        }
    }
    /// <summary>
    /// 提供不同返回类型的方法以验证异步方法识别。
    /// </summary>
    private class AsyncMethodSample
    {
        /// <summary>
        /// 返回已完成的异步操作样例。
        /// </summary>
        public Task ReturnTask() => Task.CompletedTask;
        /// <summary>
        /// 返回包含固定整数结果的异步操作样例。
        /// </summary>
        /// <returns>最终结果为 <c>1</c> 的已完成任务。</returns>
        public Task<int> ReturnTaskOfT() => Task.FromResult(1);
        /// <summary>
        /// 返回已完成的异步操作样例。
        /// </summary>
        public ValueTask ReturnValueTask() => default;
        /// <summary>
        /// 返回包含固定整数结果的异步操作样例。
        /// </summary>
        /// <returns>最终结果为 <c>1</c> 的已完成值任务。</returns>
        public ValueTask<int> ReturnValueTaskOfT() => new(1);
        /// <summary>
        /// 返回用于对照异步方法识别的固定整数。
        /// </summary>
        /// <returns>固定整数 <c>1</c>。</returns>
        public int ReturnInt() => 1;
    }
    /// <summary>
    /// 提供普通读写属性以验证属性元数据。
    /// </summary>
    private class PropertySample
    {
        /// <summary>
        /// 获取或设置用于属性元数据判定的样例整数。
        /// </summary>
        public int Normal { get; set; }
    }
    /// <summary>
    /// 用于验证只写虚属性元数据的样例。
    /// </summary>
    private class WriteOnlyVirtualPropertySample
    {
        /// <summary>
        /// 设置用于验证只写虚属性的整数。
        /// </summary>
        /// <remarks>设置操作不保存传入值。</remarks>
        public virtual int WriteOnly
        {
            set { _ = value; }
        }
    }
    /// <summary>
    /// 用于验证抽象属性及其重写关系的基类样例。
    /// </summary>
    private abstract class AbstractPropertySample
    {
        /// <summary>
        /// 获取或设置用于验证抽象属性的整数。
        /// </summary>
        public abstract int Value { get; set; }
    }
    /// <summary>
    /// 提供抽象属性的具体重写实现以验证属性元数据。
    /// </summary>
    private class DerivedAbstractPropertySample : AbstractPropertySample
    {
        /// <inheritdoc />
        public override int Value { get; set; }
    }
}

