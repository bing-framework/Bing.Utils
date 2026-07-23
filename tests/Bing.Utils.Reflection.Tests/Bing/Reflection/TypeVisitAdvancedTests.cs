using Bing.Reflection.Tests;
using Shouldly;
using System.Reflection;

namespace Bing.Reflection;

/// <summary>
/// 测试类：TypeVisit 严格方法签名匹配和实例创建功能。
/// </summary>
[Trait("ReflectionUT", "TypeVisit.Advanced")]
public class TypeVisitAdvancedTests
{
    /// <summary>
    /// 测试目的：泛型方法的泛型参数和返回类型一致时，应判定为签名兼容。
    /// </summary>
    [Fact]
    public void IsSignatureCompatible_WithEquivalentGenericMethods_ReturnsTrue()
    {
        // Arrange
        var candidate = typeof(SignatureTarget).GetMethod(nameof(SignatureTarget.Echo))!;
        var method = typeof(SignatureSource).GetMethod(nameof(SignatureSource.Echo))!;

        // Act
        var result = TypeVisit.IsSignatureCompatible(candidate, method);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：返回类型不一致的方法不应被判定为签名兼容。
    /// </summary>
    [Fact]
    public void IsSignatureCompatible_WithDifferentReturnType_ReturnsFalse()
    {
        // Arrange
        var candidate = typeof(SignatureTarget).GetMethod(nameof(SignatureTarget.GetValue))!;
        var method = typeof(SignatureSource).GetMethod(nameof(SignatureSource.GetValue))!;

        // Act
        var result = TypeVisit.IsSignatureCompatible(candidate, method);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：FindMethod 应返回目标类型中的唯一签名兼容方法。
    /// </summary>
    [Fact]
    public void FindMethod_WithUniqueCompatibleMethod_ReturnsTargetMethod()
    {
        // Arrange
        var method = typeof(SignatureSource).GetMethod(nameof(SignatureSource.Echo))!;

        // Act
        var result = TypeVisit.FindMethod(typeof(SignatureTarget), method);

        // Assert
        result.ShouldNotBeNull();
        result.DeclaringType.ShouldBe(typeof(SignatureTarget));
    }

    /// <summary>
    /// 测试目的：批量创建应保留 null 构造函数参数，并按参数集合顺序返回实例。
    /// </summary>
    [Fact]
    public void CreateInstances_WithNullAndNonNullArguments_CreatesInstancesInOrder()
    {
        // Arrange
        var argumentSets = new[]
        {
            new object[] { null! },
            new object[] { "second" }
        };

        // Act
        var result = TypeVisit.CreateInstances(typeof(NullableConstructorEntity), argumentSets);

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBeOfType<NullableConstructorEntity>().Value.ShouldBeNull();
        result[1].ShouldBeOfType<NullableConstructorEntity>().Value.ShouldBe("second");
    }

    /// <summary>
    /// 测试目的：开放泛型定义和类型实参应创建对应的封闭泛型实例。
    /// </summary>
    [Fact]
    public void CreateGenericInstance_WithOpenGenericType_ReturnsClosedGenericInstance()
    {
        // Act
        var result = TypeVisit.CreateGenericInstance(typeof(SampleGeneric<>), new[] { typeof(string) }, "value");

        // Assert
        var generic = result.ShouldBeOfType<SampleGeneric<string>>();
        generic.Value.ShouldBe("value");
    }

    /// <summary>
    /// 测试目的：非开放泛型定义不应传入 CreateGenericInstance。
    /// </summary>
    [Fact]
    public void CreateGenericInstance_WithClosedGenericType_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            TypeVisit.CreateGenericInstance(typeof(SampleGeneric<string>), new[] { typeof(string) }, "value"));
    }

    /// <summary>
    /// 测试目的：普通方法查找应支持公开、私有、静态和基类方法。
    /// </summary>
    [Fact]
    public void FindMethod_WithSimpleTypes_FindsExpectedMethods()
    {
        // Act
        var publicMethod = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.Public), typeof(string));
        var privateMethod = TypeVisit.FindMethod(typeof(MethodTarget), "Private", typeof(int));
        var staticMethod = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.Static), typeof(Guid));
        var baseMethod = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodBase.Base), typeof(long));

        // Assert
        publicMethod.ShouldNotBeNull();
        privateMethod.ShouldNotBeNull();
        staticMethod.ShouldNotBeNull();
        baseMethod.ShouldNotBeNull();
    }

    /// <summary>
    /// 测试目的：参数描述符应精确区分按值、ref、out 和泛型参数数量。
    /// </summary>
    [Fact]
    public void FindMethod_WithParameterSignatures_DistinguishesPassingKindsAndGenericArity()
    {
        // Act
        var valueMethod = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.ChangeValue), 0,
            new MethodParameterSignature(typeof(int)));
        var refMethod = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.ChangeRef), 0,
            new MethodParameterSignature(typeof(int), ParameterPassingKind.Ref));
        var outMethod = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.ChangeOut), 0,
            new MethodParameterSignature(typeof(int), ParameterPassingKind.Out));
        var genericMethod = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.Generic), 1,
            new MethodParameterSignature(typeof(List<string>)));

        // Assert
        valueMethod.ShouldNotBeNull();
        refMethod.ShouldNotBeNull();
        outMethod.ShouldNotBeNull();
        genericMethod.ShouldNotBeNull();
        genericMethod.IsGenericMethodDefinition.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：方法查找应支持接口的父接口成员，并在无匹配时返回 null。
    /// </summary>
    [Fact]
    public void FindMethod_WithInheritedInterfaceOrMissingSignature_ReturnsExpectedResult()
    {
        // Act
        var inheritedMethod = TypeVisit.FindMethod(typeof(IChildContract), nameof(IParentContract.Parent), typeof(string));
        var missingMethod = TypeVisit.FindMethod(typeof(IChildContract), "Missing", typeof(string));

        // Assert
        inheritedMethod.ShouldNotBeNull();
        missingMethod.ShouldBeNull();
    }

    /// <summary>
    /// 测试目的：存在同名同参但不同返回类型的显式接口成员时，查找应报告歧义。
    /// </summary>
    [Fact]
    public void FindMethod_WithAmbiguousInterfaceMethods_ThrowsAmbiguousMatchException()
    {
        // Act & Assert
        Should.Throw<AmbiguousMatchException>(() => TypeVisit.FindMethod(typeof(IAmbiguousContract), "Get", Type.EmptyTypes));
    }

    /// <summary>
    /// 测试目的：多个运行时类型应按输入顺序以相同构造参数创建实例。
    /// </summary>
    [Fact]
    public void CreateInstances_WithMultipleTypes_CreatesInstancesInOrder()
    {
        // Act
        var result = TypeVisit.CreateInstances(new[] { typeof(FirstCreated), typeof(SecondCreated) }, "value");

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBeOfType<FirstCreated>().Value.ShouldBe("value");
        result[1].ShouldBeOfType<SecondCreated>().Value.ShouldBe("value");
    }

    /// <summary>
    /// 测试目的：严格批量创建遇到类型不匹配、接口或构造函数异常时，应提供索引并保留内部异常。
    /// </summary>
    [Fact]
    public void CreateInstances_WithInvalidOrThrowingType_ThrowsContextualException()
    {
        // Act
        var noConstructor = Should.Throw<ArgumentException>(() => TypeVisit.CreateInstances(new[] { typeof(NoMatchingConstructor) }, "value"));
        var interfaceType = Should.Throw<ArgumentException>(() => TypeVisit.CreateInstances(new[] { typeof(IParentContract) }));
        var throwing = Should.Throw<InvalidOperationException>(() => TypeVisit.CreateInstances(new[] { typeof(ThrowingCreated) }));

        // Assert
        noConstructor.Message.ShouldContain("第 0 个");
        interfaceType.Message.ShouldContain("第 0 个");
        throwing.InnerException.ShouldNotBeNull();
    }

    /// <summary>
    /// 测试目的：强类型批量创建应验证可赋值关系并支持空集合。
    /// </summary>
    [Fact]
    public void CreateInstances_Generic_WithAssignableTypesAndEmptyCollection_ReturnsTypedResults()
    {
        // Act
        var result = TypeVisit.CreateInstances<ICreated>(new[] { typeof(FirstCreated), typeof(SecondCreated) }, "value");
        var empty = TypeVisit.CreateInstances<ICreated>(Array.Empty<Type>(), "value");

        // Assert
        result.Count.ShouldBe(2);
        empty.ShouldBeEmpty();
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstances<ICreated>(new[] { typeof(MethodTarget) }));
    }

    /// <summary>
    /// 测试目的：强类型泛型创建应从 TInstance 推导泛型实参，并验证显式实参可赋值关系。
    /// </summary>
    [Fact]
    public void CreateGenericInstance_GenericOverloads_CreateAndValidateTypedInstances()
    {
        // Act
        var inferred = TypeVisit.CreateGenericInstance<SampleGeneric<string>>(typeof(SampleGeneric<>), "value");
        var explicitInstance = TypeVisit.CreateGenericInstance<object>(typeof(SampleGeneric<>), new[] { typeof(int) }, 3);

        // Assert
        inferred.Value.ShouldBe("value");
        explicitInstance.ShouldBeOfType<SampleGeneric<int>>().Value.ShouldBe(3);
        Should.Throw<ArgumentException>(() => TypeVisit.CreateGenericInstance<string>(typeof(SampleGeneric<>), "value"));
        Should.Throw<ArgumentException>(() => TypeVisit.CreateGenericInstance(typeof(SampleGeneric<>), Type.EmptyTypes));
    }

    /// <summary>
    /// 测试目的：BindingFlags 应限制方法候选范围，并支持忽略大小写和 Type 扩展入口。
    /// </summary>
    [Fact]
    public void FindMethod_WithBindingFlagsAndExtension_RestrictsCandidates()
    {
        // Act
        var publicMethod = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.Public),
            BindingFlags.Instance | BindingFlags.Public, typeof(string));
        var privateMethod = TypeVisit.FindMethod(typeof(MethodTarget), "Private",
            BindingFlags.Instance | BindingFlags.NonPublic, typeof(int));
        var ignoredCase = TypeVisit.FindMethod(typeof(MethodTarget), "public",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase, typeof(string));
        var extensionMethod = typeof(MethodTarget).FindMethod(nameof(MethodTarget.Public), typeof(string));
        var excluded = TypeVisit.FindMethod(typeof(MethodTarget), "Private",
            BindingFlags.Instance | BindingFlags.Public, typeof(int));

        // Assert
        publicMethod.ShouldNotBeNull();
        privateMethod.ShouldNotBeNull();
        ignoredCase.ShouldNotBeNull();
        extensionMethod.ShouldNotBeNull();
        excluded.ShouldBeNull();
    }

    /// <summary>
    /// 测试目的：参数描述符查找应区分一维和多维数组，并解析嵌套泛型结构。
    /// </summary>
    [Fact]
    public void FindMethod_WithArrayAndNestedGenericSignatures_MatchesExactStructure()
    {
        // Act
        var oneDimensional = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.OneDimensional), 0,
            new MethodParameterSignature(typeof(string[])));
        var twoDimensional = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.TwoDimensional), 0,
            new MethodParameterSignature(typeof(string[,])));
        var nestedGeneric = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.NestedGeneric), 1,
            new MethodParameterSignature(typeof(Dictionary<string, List<int>>)));
        var wrongRank = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.OneDimensional), 0,
            new MethodParameterSignature(typeof(string[,])));

        // Assert
        oneDimensional.ShouldNotBeNull();
        twoDimensional.ShouldNotBeNull();
        nestedGeneric.ShouldNotBeNull();
        wrongRank.ShouldBeNull();
    }

    /// <summary>
    /// 测试目的：MethodInfo 签名比较应区分静态性、ref/out 和数组秩。
    /// </summary>
    [Fact]
    public void IsSignatureCompatible_WithStaticRefOutOrArrayRankDifference_ReturnsFalse()
    {
        // Arrange
        var staticMethod = typeof(SignatureDifferenceTarget).GetMethod(nameof(SignatureDifferenceTarget.Static))!;
        var instanceMethod = typeof(SignatureDifferenceSource).GetMethod(nameof(SignatureDifferenceSource.Static))!;
        var refMethod = typeof(SignatureDifferenceTarget).GetMethod(nameof(SignatureDifferenceTarget.Change))!;
        var outMethod = typeof(SignatureDifferenceSource).GetMethod(nameof(SignatureDifferenceSource.Change))!;
        var oneDimensional = typeof(SignatureDifferenceTarget).GetMethod(nameof(SignatureDifferenceTarget.Array))!;
        var twoDimensional = typeof(SignatureDifferenceSource).GetMethod(nameof(SignatureDifferenceSource.Array))!;

        // Act & Assert
        TypeVisit.IsSignatureCompatible(staticMethod, instanceMethod).ShouldBeFalse();
        TypeVisit.IsSignatureCompatible(refMethod, outMethod).ShouldBeFalse();
        TypeVisit.IsSignatureCompatible(oneDimensional, twoDimensional).ShouldBeFalse();
        Should.Throw<ArgumentNullException>(() => TypeVisit.IsSignatureCompatible(null!, instanceMethod));
        Should.Throw<ArgumentNullException>(() => TypeVisit.FindMethod(typeof(MethodTarget), (MethodInfo)null!));
    }

    /// <summary>
    /// 测试目的：方法查找应验证名称、类型、参数签名和泛型参数数量。
    /// </summary>
    [Fact]
    public void FindMethod_WithInvalidArguments_ThrowsExpectedException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => TypeVisit.FindMethod(null!, nameof(MethodTarget.Public), typeof(string)));
        Should.Throw<ArgumentException>(() => TypeVisit.FindMethod(typeof(MethodTarget), " ", typeof(string)));
        Should.Throw<ArgumentNullException>(() => TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.Public), (Type[])null!));
        Should.Throw<ArgumentException>(() => TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.Public), new Type[] { null! }));
        Should.Throw<ArgumentException>(() => TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.Public), -1,
            new MethodParameterSignature(typeof(string))));
        Should.Throw<ArgumentException>(() => TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.Public), 0,
            new MethodParameterSignature[] { null! }));
        Should.Throw<ArgumentNullException>(() => new MethodParameterSignature(null!));
    }

    /// <summary>
    /// 测试目的：DeclaredOnly 查找接口时不应返回父接口成员，描述符扩展入口应正常工作。
    /// </summary>
    [Fact]
    public void FindMethod_WithDeclaredOnlyInterface_ExcludesParentMember()
    {
        // Act
        var declaredOnlyParent = TypeVisit.FindMethod(typeof(IChildContract), nameof(IParentContract.Parent),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, 0,
            new MethodParameterSignature(typeof(string)));
        var extensionOwn = typeof(MethodTarget).FindMethod(nameof(MethodTarget.ChangeRef),
            BindingFlags.Instance | BindingFlags.Public, 0,
            new MethodParameterSignature(typeof(int), ParameterPassingKind.Ref));

        // Assert
        declaredOnlyParent.ShouldBeNull();
        extensionOwn.ShouldNotBeNull();
    }

    /// <summary>
    /// 测试目的：参数描述符的传递方式与泛型参数数量不一致时，方法查找应返回 null。
    /// </summary>
    [Fact]
    public void FindMethod_WithMismatchedPassingKindOrGenericArity_ReturnsNull()
    {
        // Act
        var valueInsteadOfRef = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.ChangeRef), 0,
            new MethodParameterSignature(typeof(int)));
        var wrongArity = TypeVisit.FindMethod(typeof(MethodTarget), nameof(MethodTarget.Generic), 2,
            new MethodParameterSignature(typeof(List<string>)));

        // Assert
        valueInsteadOfRef.ShouldBeNull();
        wrongArity.ShouldBeNull();
    }

    /// <summary>
    /// 测试目的：多个运行时类型创建应接受空集合，并验证 null 集合和 null 类型元素。
    /// </summary>
    [Fact]
    public void CreateInstances_WithEmptyOrNullTypes_ValidatesInput()
    {
        // Act
        var empty = TypeVisit.CreateInstances(Array.Empty<Type>());

        // Assert
        empty.ShouldBeEmpty();
        Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstances((IEnumerable<Type>)null!));
        var nullType = Should.Throw<ArgumentException>(() => TypeVisit.CreateInstances(new Type[] { null! }));
        nullType.Message.ShouldContain("第 0 个");
    }

    /// <summary>
    /// 测试目的：多个运行时类型创建应拒绝抽象、开放泛型和指针类型，并保留类型索引上下文。
    /// </summary>
    [Fact]
    public void CreateInstances_WithNonCreatableTypes_ThrowsContextualException()
    {
        // Act
        var abstractType = Should.Throw<ArgumentException>(() => TypeVisit.CreateInstances(new[] { typeof(AbstractCreated) }));
        var openGenericType = Should.Throw<ArgumentException>(() => TypeVisit.CreateInstances(new[] { typeof(SampleGeneric<>) }, "value"));
        var pointerType = Should.Throw<ArgumentException>(() => TypeVisit.CreateInstances(new[] { typeof(int).MakePointerType() }));

        // Assert
        abstractType.Message.ShouldContain("第 0 个");
        openGenericType.Message.ShouldContain("第 0 个");
        pointerType.Message.ShouldContain("第 0 个");
    }

    /// <summary>
    /// 测试目的：强类型批量创建应验证 null 集合、null 元素和不可赋值类型。
    /// </summary>
    [Fact]
    public void CreateInstances_GenericWithTypes_ValidatesInputAndAssignability()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstances<ICreated>((IEnumerable<Type>)null!));
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstances<ICreated>(new Type[] { null! }));
        Should.Throw<ArgumentException>(() => TypeVisit.CreateInstances<ICreated>(new[] { typeof(MethodTarget) }));
    }

    /// <summary>
    /// 测试目的：泛型创建应验证 null、泛型实参数量、约束、构造函数和强类型赋值关系。
    /// </summary>
    [Fact]
    public void CreateGenericInstance_WithInvalidInputOrConstraint_ThrowsExpectedException()
    {
        // Act
        var constraintException = Should.Throw<ArgumentException>(() => TypeVisit.CreateGenericInstance(typeof(StructOnlyGeneric<>),
            new[] { typeof(string) }));

        // Assert
        Should.Throw<ArgumentNullException>(() => TypeVisit.CreateGenericInstance(null!, new[] { typeof(string) }));
        Should.Throw<ArgumentNullException>(() => TypeVisit.CreateGenericInstance(typeof(SampleGeneric<>), null!));
        Should.Throw<ArgumentException>(() => TypeVisit.CreateGenericInstance(typeof(SampleGeneric<>), new Type[] { null! }));
        constraintException.InnerException.ShouldNotBeNull();
        Should.Throw<ArgumentException>(() => TypeVisit.CreateGenericInstance<SampleGeneric<string>>(typeof(SampleGeneric<>)));
        Should.Throw<InvalidCastException>(() => TypeVisit.CreateGenericInstance<string>(typeof(SampleGeneric<>), new[] { typeof(int) }, 1));
    }

    public sealed class SignatureSource
    {
        public T Echo<T>(T value) => value;
        public int GetValue() => 1;
    }

    public sealed class SignatureTarget
    {
        public T Echo<T>(T value) => value;
        public string GetValue() => "value";
    }

    public class MethodBase
    {
        public void Base(long value) { }
    }

    public sealed class MethodTarget : MethodBase
    {
        public void Public(string value) { }
        private void Private(int value) { }
        public static void Static(Guid value) { }
        public void ChangeValue(int value) { }
        public void ChangeRef(ref int value) { }
        public void ChangeOut(out int value) => value = 0;
        public void Generic<T>(List<T> values) { }
        public void OneDimensional(string[] values) { }
        public void TwoDimensional(string[,] values) { }
        public void NestedGeneric<T>(Dictionary<string, List<T>> values) { }
    }

    public sealed class SignatureDifferenceSource
    {
        public void Static() { }
        public void Change(out int value) => value = 0;
        public void Array(string[,] values) { }
    }

    public sealed class SignatureDifferenceTarget
    {
        public static void Static() { }
        public void Change(ref int value) { }
        public void Array(string[] values) { }
    }

    public interface IParentContract { void Parent(string value); }
    public interface IChildContract : IParentContract { }
    public interface IStringGet { string Get(); }
    public interface IIntGet { int Get(); }
    public interface IAmbiguousContract : IStringGet, IIntGet { }

    public interface ICreated { string Value { get; } }
    public sealed class FirstCreated : ICreated
    {
        public FirstCreated(string value) => Value = value;
        public string Value { get; }
    }

    public sealed class SecondCreated : ICreated
    {
        public SecondCreated(string value) => Value = value;
        public string Value { get; }
    }

    public sealed class NoMatchingConstructor
    {
        public NoMatchingConstructor(int value) { }
    }

    public sealed class ThrowingCreated
    {
        public ThrowingCreated() => throw new InvalidOperationException("constructor");
    }

    public abstract class AbstractCreated
    {
    }

    public sealed class StructOnlyGeneric<T> where T : struct
    {
        public StructOnlyGeneric() { }
    }

    public sealed class NullableConstructorEntity
    {
        public NullableConstructorEntity(string value) => Value = value;
        public string Value { get; }
    }
}
