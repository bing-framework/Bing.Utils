namespace Bing.Reflection;
/// <summary>
/// 测试类：覆盖 `TypeVisitMethodsAndPropertiesBoundary` 相关行为。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.MethodsAndProperties.Boundary")]
public class TypeVisitMethodsAndPropertiesBoundaryTest
{
    /// <summary>
    /// 测试用例：验证 `GetMethodBySignature` 在 `NullType` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void GetMethodBySignature_NullType_ThrowsArgumentNullException()
    {
        var method = typeof(MethodSample).GetMethod(nameof(MethodSample.Execute), new[] { typeof(int) });
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.GetMethodBySignature(null, method));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 测试用例：验证 `GetMethodBySignature` 在 `NullMethod` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void GetMethodBySignature_NullMethod_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.GetMethodBySignature(typeof(MethodSample), null));
        ex.ParamName.ShouldBe("method");
    }
    /// <summary>
    /// 测试用例：验证 `GetMethodBySignature` 在 `WithMatchingSignature` 场景下，结果为 `ReturnsMethod`。
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
    /// 测试用例：验证 `GetBaseMethod` 在 `OnOverrideMethod` 场景下，结果为 `ReturnsBaseMethod`。
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
    /// 测试用例：验证 `GetBaseMethod` 在 `NullMethod` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void GetBaseMethod_NullMethod_ReturnsNull()
    {
        var baseMethod = TypeVisit.GetBaseMethod(null);

        baseMethod.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `IsVisibleAndVirtual` 在 `WithVisibleVirtualMethod` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_WithVisibleVirtualMethod_ReturnsTrue()
    {
        var method = typeof(BaseMethodSample).GetMethod(nameof(BaseMethodSample.VirtualMethod));
        var result = TypeVisit.IsVisibleAndVirtual(method);
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsVisible` 在 `WithPrivateMethod` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsVisible_WithPrivateMethod_ReturnsFalse()
    {
        var method = typeof(MethodSample).GetMethod("Hidden", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = TypeVisit.IsVisible(method);
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsVisible` 在 `NullMethod` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void IsVisible_NullMethod_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.IsVisible(null));
        ex.ParamName.ShouldBe("method");
    }

    /// <summary>
    /// 测试用例：验证 `IsVisibleAndVirtual` 在 `NullMethod` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_NullMethod_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.IsVisibleAndVirtual((MethodInfo)null));

        ex.ParamName.ShouldBe("method");
    }
    /// <summary>
    /// 测试用例：验证 `IsAsyncMethod` 在 `ShouldReturnExpectedResult` 场景下的行为。
    /// </summary>
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
    /// 测试用例：验证 `GetFullNameAndFullyQualifiedName` 在 `ShouldContainMethodName` 场景下的行为。
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
    /// 测试用例：验证 `GetFullName` 在 `NullMethod` 场景下，结果为 `CurrentBehaviorThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void GetFullName_NullMethod_CurrentBehaviorThrowsNullReferenceException()
    {
        Should.Throw<NullReferenceException>(() => TypeVisit.GetFullName((MethodInfo)null));
    }

    /// <summary>
    /// 测试用例：验证 `GetFullyQualifiedName` 在 `NullMethod` 场景下，结果为 `CurrentBehaviorThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void GetFullyQualifiedName_NullMethod_CurrentBehaviorThrowsNullReferenceException()
    {
        Should.Throw<NullReferenceException>(() => TypeVisit.GetFullyQualifiedName((MethodInfo)null));
    }
    /// <summary>
    /// 测试用例：验证 `GetProperties` 在 `NullType` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void GetProperties_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.GetProperties(null, PropertyAccessOptions.Both).ToList());
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 测试用例：验证 `GetProperties` 在 `UnknownAccessOption` 场景下，结果为 `ThrowsInvalidOperationException`。
    /// </summary>
    [Fact]
    public void GetProperties_UnknownAccessOption_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() =>
            TypeVisit.GetProperties(typeof(PropertySample), (PropertyAccessOptions)999).ToList());
    }
    /// <summary>
    /// 测试用例：验证 `Exclude` 在 `NullArguments` 场景下，结果为 `ThrowArgumentNullException`。
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
    /// 测试用例：验证 `IsVisibleAndVirtual` 在 `WriteOnlyProperty` 场景下，结果为 `CurrentlyThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_WriteOnlyProperty_CurrentlyThrowsArgumentNullException()
    {
        var property = typeof(WriteOnlyVirtualPropertySample).GetProperty(nameof(WriteOnlyVirtualPropertySample.WriteOnly));
        Should.Throw<ArgumentNullException>(() => TypeVisit.IsVisibleAndVirtual(property));
    }
    /// <summary>
    /// 测试用例：验证 `PropertyMetaExtensions` 在 `IsVirtualAndIsAbstract` 场景下，结果为 `ReturnExpected`。
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
    /// 测试用例：验证 `PropertyMetaExtensions` 在 `NullProperty` 场景下，结果为 `CurrentBehaviorThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void PropertyMetaExtensions_NullProperty_CurrentBehaviorThrowsNullReferenceException()
    {
        PropertyInfo property = null;
        Should.Throw<NullReferenceException>(() => property.IsVirtual());
        Should.Throw<NullReferenceException>(() => property.IsAbstract());
    }
    private class MethodSample
    {
        /// <summary>
        /// 测试辅助：提供 `Execute` 的测试支撑逻辑。
        /// </summary>
        public void Execute(int value)
        {
            _ = value;
        }
        /// <summary>
        /// 测试辅助：提供 `Execute` 的测试支撑逻辑。
        /// </summary>
        public void Execute(string value)
        {
            _ = value;
        }
        private void Hidden()
        {
        }
    }
    private class BaseMethodSample
    {
        public virtual void VirtualMethod()
        {
        }
    }
    private class DerivedMethodSample : BaseMethodSample
    {
        public override void VirtualMethod()
        {
        }
    }
    private class AsyncMethodSample
    {
        /// <summary>
        /// 测试辅助：提供 `ReturnTask` 的测试支撑逻辑。
        /// </summary>
        public Task ReturnTask() => Task.CompletedTask;
        /// <summary>
        /// 测试辅助：提供 `ReturnTaskOfT` 的测试支撑逻辑。
        /// </summary>
        public Task<int> ReturnTaskOfT() => Task.FromResult(1);
        public ValueTask ReturnValueTask() => default;
        public ValueTask<int> ReturnValueTaskOfT() => new(1);
        public int ReturnInt() => 1;
    }
    private class PropertySample
    {
        public int Normal { get; set; }
    }
    private class WriteOnlyVirtualPropertySample
    {
        public virtual int WriteOnly
        {
            set { _ = value; }
        }
    }
    private abstract class AbstractPropertySample
    {
        public abstract int Value { get; set; }
    }
    private class DerivedAbstractPropertySample : AbstractPropertySample
    {
        public override int Value { get; set; }
    }
}

