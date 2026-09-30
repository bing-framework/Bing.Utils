using System.Collections.Concurrent;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Bing.Conversions;

namespace Bing.Helpers;

/// <summary>
/// <see cref="Conv"/> 泛型转换器、边界输入和资源生命周期回归测试。
/// </summary>
[Trait("Bing.Helpers", "Conv.ConversionContract")]
public class ConvConversionContractTest
{
    /// <summary>
    /// 内置转换成功返回默认值时，TryTo 仍应报告成功；失败时应报告 false。
    /// </summary>
    [Fact]
    public void TryTo_BuiltInConversion_DistinguishesSuccessFromDefaultValue()
    {
        Conv.TryTo<int>("0", out var zero).ShouldBeTrue();
        zero.ShouldBe(0);

        Conv.TryTo<bool>("false", out var falseValue).ShouldBeTrue();
        falseValue.ShouldBeFalse();

        Conv.TryTo<int>("invalid", out var invalid).ShouldBeFalse();
        invalid.ShouldBe(0);

        Conv.TryTo<int>(null, out var nullValue).ShouldBeFalse();
        nullValue.ShouldBe(0);

        Conv.TryTo<int>(DBNull.Value, out var dbNullValue).ShouldBeFalse();
        dbNullValue.ShouldBe(0);
    }

    /// <summary>
    /// 自定义转换应优先于内置转换，并且所有公开入口应使用同一个转换器实例。
    /// </summary>
    [Fact]
    public void CustomConverter_TakesPriorityAcrossAllEntryPoints()
    {
        var converter = new ConvConverterBuilder()
            .Register<string, int>((string _, out int result) =>
            {
                result = 42;
                return true;
            })
            .Build();

        converter.To<int>("7").ShouldBe(42);
        converter.TryTo<int>("7", out var instanceResult).ShouldBeTrue();
        instanceResult.ShouldBe(42);

        Conv.To<int>("7", converter).ShouldBe(42);
        Conv.TryTo<int>("7", converter, out var staticResult).ShouldBeTrue();
        staticResult.ShouldBe(42);
    }

    /// <summary>
    /// 自定义转换成功返回 false 值或 null 时，TryTo 应保持成功状态。
    /// </summary>
    [Fact]
    public void CustomConverter_SuccessWithDefaultResult_RemainsSuccessful()
    {
        var converter = new ConvConverterBuilder()
            .Register<string, bool>((string _, out bool result) =>
            {
                result = false;
                return true;
            })
            .Register<int, string>((int _, out string result) =>
            {
                result = null;
                return true;
            })
            .Build();

        converter.TryTo<bool>("false", out var falseResult).ShouldBeTrue();
        falseResult.ShouldBeFalse();

        converter.TryTo<string>(1, out var nullResult).ShouldBeTrue();
        nullResult.ShouldBeNull();
    }

    /// <summary>
    /// 注册项必须同时匹配输入运行时类型和目标类型，不应按基类或其他目标类型搜索。
    /// </summary>
    [Fact]
    public void CustomConverter_UsesExactSourceAndTargetTypes()
    {
        var converter = new ConvConverterBuilder()
            .Register<object, int>((object _, out int result) =>
            {
                result = 99;
                return true;
            })
            .Register<string, long>((string _, out long result) =>
            {
                result = 88;
                return true;
            })
            .Build();

        // string 是 object 的派生类型，不能命中 object -> int 注册。
        converter.To<int>("12").ShouldBe(12);
        converter.To<int>(new object()).ShouldBe(99);
        // 目标类型不匹配时，string -> long 注册不能影响 int? 转换。
        converter.TryTo<int?>("12", out var nullable).ShouldBeTrue();
        nullable.ShouldBe(12);
        converter.To<long>("12").ShouldBe(88);
    }

    /// <summary>
    /// 验证可空目标类型必须显式注册。
    /// </summary>
    [Fact]
    public void CustomConverter_NullableTarget_RequiresExactRegistration()
    {
        var builder = new ConvConverterBuilder()
            .Register<string, int>((string _, out int result) =>
            {
                result = 41;
                return true;
            });
        var first = builder.Build();
        first.To<int?>("7").ShouldBe(7);

        var second = builder.Register<string, int?>((string _, out int? result) =>
        {
            result = 42;
            return true;
        }).Build();
        second.To<int?>("7").ShouldBe(42);
        second.To<int>("7").ShouldBe(41);
        first.To<int?>("7").ShouldBe(7);
    }

    /// <summary>
    /// 验证自定义转换可以处理空白字符串且不会接收空引用。
    /// </summary>
    [Fact]
    public void CustomConverter_WhitespaceAndNull_UseConfiguredContract()
    {
        var converter = new ConvConverterBuilder()
            .Register<string, int>((string text, out int result) =>
            {
                result = text.Length;
                return true;
            })
            .Build();

        converter.TryTo<int>("   ", out var value).ShouldBeTrue();
        value.ShouldBe(3);
        converter.TryTo<int>(null, out var nullResult).ShouldBeFalse();
        nullResult.ShouldBe(0);
    }

    /// <summary>
    /// 自定义转换失败或抛出异常时，应停止转换并返回目标类型默认值，不再回退内置逻辑。
    /// </summary>
    [Fact]
    public void CustomConverter_FailureAndException_DoNotFallbackToBuiltInConversion()
    {
        var failedConverter = new ConvConverterBuilder()
            .Register<string, int>((string _, out int result) =>
            {
                result = 777;
                return false;
            })
            .Build();

        failedConverter.TryTo<int>("123", out var failedResult).ShouldBeFalse();
        failedResult.ShouldBe(0);
        failedConverter.To<int>("123").ShouldBe(0);

        var throwingConverter = new ConvConverterBuilder()
            .Register<string, int>((string _, out int result) =>
            {
                result = 0;
                throw new InvalidOperationException("custom converter failure");
            })
            .Build();

        throwingConverter.TryTo<int>("123", out var thrownResult).ShouldBeFalse();
        thrownResult.ShouldBe(0);
        throwingConverter.To<int>("123").ShouldBe(0);
    }

    /// <summary>
    /// 验证取消异常会继续传递给调用方。
    /// </summary>
    [Fact]
    public void CustomConverter_CancellationException_Propagates()
    {
        var converter = new ConvConverterBuilder()
            .Register<string, int>((string _, out int result) =>
            {
                result = 0;
                throw new TaskCanceledException();
            })
            .Build();

        Should.Throw<TaskCanceledException>(() => converter.TryTo<int>("7", out _));
    }

    /// <summary>
    /// builder 应拒绝空委托和重复类型对注册。
    /// </summary>
    [Fact]
    public void ConverterBuilder_InvalidRegistration_Throws()
    {
        var builder = new ConvConverterBuilder();

        Should.Throw<ArgumentNullException>(() => builder.Register<string, int>(null));
        builder.Register<string, int>((string _, out int result) =>
        {
            result = 1;
            return true;
        });

        Should.Throw<InvalidOperationException>(() => builder.Register<string, int>((string _, out int result) =>
        {
            result = 2;
            return true;
        }));
    }

    /// <summary>
    /// Build 应复制注册表；后续注册和其他 builder 不应改变已构建实例。
    /// </summary>
    [Fact]
    public void ConverterBuilder_BuildSnapshot_IsolatedBetweenInstances()
    {
        var builder = new ConvConverterBuilder()
            .Register<string, int>((string _, out int result) =>
            {
                result = 11;
                return true;
            });
        var first = builder.Build();

        builder.Register<string, long>((string _, out long result) =>
        {
            result = 22;
            return true;
        });
        var second = builder.Build();
        var independent = new ConvConverterBuilder().Build();

        first.To<int>("7").ShouldBe(11);
        first.To<long>("7").ShouldBe(7);
        second.To<long>("7").ShouldBe(22);
        independent.To<int>("7").ShouldBe(7);
    }

    /// <summary>
    /// converter 构建完成后应支持并发读取和调用。
    /// </summary>
    [Fact]
    public void Converter_ConcurrentReads_AreStable()
    {
        var converter = new ConvConverterBuilder()
            .Register<string, int>((string input, out int result) =>
            {
                return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            })
            .Build();
        var failures = new ConcurrentBag<string>();

        Parallel.For(0, 1024, index =>
        {
            var input = index.ToString(CultureInfo.InvariantCulture);
            if (!converter.TryTo<int>(input, out var result) || result != index)
                failures.Add(input);
        });

        failures.ShouldBeEmpty();
    }

    /// <summary>
    /// converter 参数为空时，静态重载应直接报告参数错误。
    /// </summary>
    [Fact]
    public void StaticConverterOverloads_NullConverter_ThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Conv.To<int>("1", null));
        Should.Throw<ArgumentNullException>(() => Conv.TryTo<int>("1", null, out _));
    }

    /// <summary>
    /// 数值转换应覆盖边界、舍入以及非有限浮点数输入。
    /// </summary>
    [Fact]
    public void NumericConversion_Boundaries_RoundingAndNonFiniteValues()
    {
        Conv.ToIntOrNull(int.MaxValue).ShouldBe(int.MaxValue);
        Conv.ToIntOrNull(long.MaxValue).ShouldBeNull();
        Conv.ToUIntOrNull(-1).ShouldBeNull();
        Conv.ToULongOrNull(long.MaxValue).ShouldBe((ulong)long.MaxValue);
        Conv.ToLongOrNull(ulong.MaxValue).ShouldBeNull();

        Conv.ToIntOrNull(1.5d).ShouldBe(2);
        Conv.ToIntOrNull(double.NaN).ShouldBeNull();
        Conv.ToIntOrNull(double.PositiveInfinity).ShouldBeNull();

        Conv.ToDecimalOrNull(1.005m, 2).ShouldBe(1.01m);
        Conv.ToDoubleOrNull(12.345d, 2).ShouldBe(12.35d);
    }

    /// <summary>
    /// 专用数值转换使用当前区域性，泛型转换保持 invariant 语义。
    /// </summary>
    [Fact]
    public void NumericConversion_RespectsExpectedCultureSemantics()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;

            Conv.ToDecimalOrNull("1.234,56").ShouldBe(1234.56m);
            Conv.To<decimal>("1234.56").ShouldBe(1234.56m);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    /// <summary>
    /// DateTime 直接转换应保留完整 ticks 和 Kind。
    /// </summary>
    [Fact]
    public void DateConversion_PreservesTicksAndKind()
    {
        var input = new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc).AddTicks(9012);

        var result = Conv.ToDateOrNull(input);
        result.ShouldNotBeNull();
        result.Value.Ticks.ShouldBe(input.Ticks);
        result.Value.Kind.ShouldBe(input.Kind);

        Conv.To<DateTime>(input).ShouldBe(input);
    }

    /// <summary>
    /// long 和 ulong 枚举的非零底层值都应能转换为 true，不发生窄化溢出。
    /// </summary>
    [Fact]
    public void EnumToBoolean_SupportsLongAndULongUnderlyingValues()
    {
        Conv.ToBoolOrNull(LongEnum.Zero).ShouldBe(false);
        Conv.ToBoolOrNull(LongEnum.Minimum).ShouldBe(true);
        Conv.ToBoolOrNull(ULongEnum.Zero).ShouldBe(false);
        Conv.ToBoolOrNull(ULongEnum.Maximum).ShouldBe(true);
    }

    /// <summary>
    /// JsonElement 转换应直接生成目标对象，并通过 TryTo 报告成功。
    /// </summary>
    [Fact]
    public void JsonElementConversion_ReturnsTypedObject()
    {
        using var document = JsonDocument.Parse("{\"Name\":\"Ada\",\"Count\":3}");

        var result = Conv.To<JsonTarget>(document.RootElement);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Ada");
        result.Count.ShouldBe(3);

        Conv.TryTo<JsonTarget>(document.RootElement, out var tryResult).ShouldBeTrue();
        tryResult.ShouldNotBeNull();
        tryResult.Name.ShouldBe("Ada");
    }

    /// <summary>
    /// 验证 JSON 标量转换保留字符串、Guid 和枚举的既有路径。
    /// </summary>
    [Fact]
    public void JsonElementConversion_ScalarTypes_PreserveExistingSemantics()
    {
        var guid = System.Guid.NewGuid();
        using var text = JsonDocument.Parse("\"hello\"");
        using var identifier = JsonDocument.Parse($"\"{guid}\"");
        using var enumName = JsonDocument.Parse("\"Minimum\"");

        Conv.To<string>(text.RootElement).ShouldBe("hello");
        Conv.To<System.Guid>(identifier.RootElement).ShouldBe(guid);
        Conv.To<LongEnum>(enumName.RootElement).ShouldBe(LongEnum.Minimum);
    }

    /// <summary>
    /// 验证同名自定义类型不会进入内置 string 或 Guid 分支。
    /// </summary>
    [Fact]
    public void GenericConversion_CustomTypesNamedLikeBuiltIns_UseTypeIdentity()
    {
        var namedString = new String();
        var namedGuid = new Guid();

        Conv.TryTo<String>(namedString, out var stringResult).ShouldBeTrue();
        stringResult.ShouldBeSameAs(namedString);
        Conv.TryTo<Guid>(namedGuid, out var guidResult).ShouldBeTrue();
        guidResult.ShouldBeSameAs(namedGuid);
        Conv.TryTo<String>("hello", out _).ShouldBeFalse();
        Conv.TryTo<Guid>("00000000-0000-0000-0000-000000000000", out _).ShouldBeFalse();
    }

    /// <summary>
    /// 动态对象应保留动态属性；列表转换应过滤空项并保留失败项的默认值。
    /// </summary>
    [Fact]
    public void DynamicPropertiesAndListConversion_PreserveExistingContracts()
    {
        var dynamicObject = new ExpandoObject();
        var properties = (IDictionary<string, object>)dynamicObject;
        properties["Name"] = "Ada";
        properties["Count"] = 3;

        var dictionary = Conv.ToDictionary(dynamicObject);
        dictionary["Name"].ShouldBe("Ada");
        dictionary["Count"].ShouldBe(3);

        Conv.ToList<int>("1, ,invalid,2").ShouldBe(new[] { 1, 0, 2 });
    }

    /// <summary>
    /// 验证同一运行时类型的不同实例可以报告不同属性集合。
    /// </summary>
    [Fact]
    public void ToDictionary_UsesPropertiesFromEachCustomDescriptorInstance()
    {
        var first = Conv.ToDictionary(new InstanceProperties("A") { A = 1, B = 2 });
        var second = Conv.ToDictionary(new InstanceProperties("B") { A = 3, B = 4 });

        first.Count.ShouldBe(1);
        first["A"].ShouldBe(1);
        second.Count.ShouldBe(1);
        second["B"].ShouldBe(4);
    }

    /// <summary>
    /// 转换器不应持有已经处理过的输入对象。
    /// </summary>
    [Fact]
    public void Converter_DoesNotRetainConvertedInput()
    {
        var converter = new ConvConverterBuilder()
            .Register<EphemeralInput, int>((EphemeralInput input, out int result) =>
            {
                result = input.Value;
                return true;
            })
            .Build();

        var weakReference = CreateInputWeakReference(converter);
        ForceCollection();

        weakReference.TryGetTarget(out _).ShouldBeFalse();
        GC.KeepAlive(converter);
    }

    /// <summary>
    /// 丢弃 builder 和 converter 后，注册委托捕获的对象应可回收。
    /// </summary>
    [Fact]
    public void Converter_DoesNotRetainCapturedRegistrationState()
    {
        var weakReference = CreateCapturedStateWeakReference();
        ForceCollection();

        weakReference.TryGetTarget(out _).ShouldBeFalse();
    }

    /// <summary>
    /// 创建只引用临时输入对象的弱引用。
    /// </summary>
    /// <param name="converter">用于处理临时输入对象的转换器。</param>
    /// <returns>临时输入对象的弱引用。</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<EphemeralInput> CreateInputWeakReference(ConvConverter converter)
    {
        var input = new EphemeralInput { Value = 7 };
        converter.TryTo<int>(input, out _).ShouldBeTrue();
        return new WeakReference<EphemeralInput>(input);
    }

    /// <summary>
    /// 创建只引用临时捕获状态的弱引用。
    /// </summary>
    /// <returns>捕获状态对象的弱引用。</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<CapturedState> CreateCapturedStateWeakReference()
    {
        var captured = new CapturedState { Value = 11 };
        var builder = new ConvConverterBuilder()
            .Register<string, int>((string _, out int result) =>
            {
                result = captured.Value;
                return true;
            });
        _ = builder.Build();
        return new WeakReference<CapturedState>(captured);
    }

    /// <summary>
    /// 执行多轮完整垃圾回收。
    /// </summary>
    private static void ForceCollection()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        }
    }

    /// <summary>
    /// 表示用于回收测试的临时输入对象。
    /// </summary>
    private sealed class EphemeralInput
    {
        /// <summary>
        /// 获取或初始化输入值。
        /// </summary>
        public int Value { get; init; }
    }

    /// <summary>
    /// 表示由自定义转换委托捕获的状态对象。
    /// </summary>
    private sealed class CapturedState
    {
        /// <summary>
        /// 获取或初始化捕获的状态值。
        /// </summary>
        public int Value { get; init; }
    }

    /// <summary>
    /// 表示 JSON 对象转换的测试目标。
    /// </summary>
    private sealed class JsonTarget
    {
        /// <summary>
        /// 获取或设置目标名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置目标数量。
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// 提供按实例选择属性的自定义类型描述器。
    /// </summary>
    private sealed class InstanceProperties : CustomTypeDescriptor
    {
        /// <summary>
        /// 当前实例公开的属性名称。
        /// </summary>
        private readonly string _visibleProperty;

        /// <summary>
        /// 初始化自定义类型描述器。
        /// </summary>
        /// <param name="visibleProperty">当前实例公开的属性名称。</param>
        public InstanceProperties(string visibleProperty) => _visibleProperty = visibleProperty;

        /// <summary>
        /// 获取属性 A 的测试值。
        /// </summary>
        public int A { get; init; }

        /// <summary>
        /// 获取属性 B 的测试值。
        /// </summary>
        public int B { get; init; }

        /// <inheritdoc />
        public override PropertyDescriptorCollection GetProperties()
        {
            var property = TypeDescriptor.GetProperties(typeof(InstanceProperties))[_visibleProperty];
            return new PropertyDescriptorCollection(new[] { property });
        }

        /// <inheritdoc />
        public override object GetPropertyOwner(PropertyDescriptor property) => this;
    }

    /// <summary>
    /// 表示名称与内置 string 相同的测试类型。
    /// </summary>
    private sealed class String { }

    /// <summary>
    /// 表示名称与内置 Guid 相同的测试类型。
    /// </summary>
    private sealed class Guid { }

    /// <summary>
    /// 表示使用 long 作为底层类型的测试枚举。
    /// </summary>
    private enum LongEnum : long
    {
        /// <summary>
        /// 表示零值。
        /// </summary>
        Zero = 0,

        /// <summary>
        /// 表示 long 的最小值。
        /// </summary>
        Minimum = long.MinValue
    }

    /// <summary>
    /// 表示使用 ulong 作为底层类型的测试枚举。
    /// </summary>
    private enum ULongEnum : ulong
    {
        /// <summary>
        /// 表示零值。
        /// </summary>
        Zero = 0,

        /// <summary>
        /// 表示 ulong 的最大值。
        /// </summary>
        Maximum = ulong.MaxValue
    }
}
