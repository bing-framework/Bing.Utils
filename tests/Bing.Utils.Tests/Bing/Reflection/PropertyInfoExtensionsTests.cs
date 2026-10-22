using System;
using System.Reflection;
using Shouldly;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 验证属性访问委托的生成、缓存及静态属性识别。
/// </summary>
[Trait("Bing.Reflection", "PropertyInfoExtensions")]
public class PropertyInfoExtensionsTests
{
    // ──────────────────────────────────────────
    //  测试模型
    // ──────────────────────────────────────────

    /// <summary>
    /// 提供实例、静态及不同访问器形状的属性样例。
    /// </summary>
    private class PersonModel
    {
        /// <summary>
        /// 获取或设置测试姓名。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置测试年龄。
        /// </summary>
        public int Age { get; set; }
        /// <summary>
        /// 获取固定的只读测试文本。
        /// </summary>
        public string ReadOnlyProp { get; } = "readonly_value";
        /// <summary>
        /// 保存只写属性接收的测试文本。
        /// </summary>
        private string _writeOnlyBacking;
        /// <summary>
        /// 设置只写属性的测试文本。
        /// </summary>
        public string WriteOnlyProp { set => _writeOnlyBacking = value; }
        /// <summary>
        /// 获取用于验证静态属性读取的固定类型名称。
        /// </summary>
        public static string ClassName { get; } = "PersonModel";
        /// <summary>
        /// 获取或设置用于验证静态属性写入的计数。
        /// </summary>
        public static int StaticCount { get; set; } = 0;

        /// <summary>
        /// 读取只写属性保存的测试文本。
        /// </summary>
        /// <returns>最近设置的测试文本；尚未设置时为空。</returns>
        public string GetWriteOnlyBacking() => _writeOnlyBacking;
    }

    /// <summary>
    /// 提供具有两个可读写坐标属性的值类型样例。
    /// </summary>
    private struct PointStruct
    {
        /// <summary>
        /// 获取或设置测试横坐标。
        /// </summary>
        public int X { get; set; }
        /// <summary>
        /// 获取或设置测试纵坐标。
        /// </summary>
        public int Y { get; set; }
    }

    // ──────────────────────────────────────────
    //  GetValueGetter<T>  — 泛型版本
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证泛型读取委托能够读取实例字符串属性。
    /// </summary>
    [Fact]
    public void GetValueGetter_Generic_ReadableProp_ReturnsGetter()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Name))!;
        var getter = prop.GetValueGetter<PersonModel>();
        getter.ShouldNotBeNull();
        var person = new PersonModel { Name = "Alice" };
        getter(person).ShouldBe("Alice");
    }

    /// <summary>
    /// 验证泛型读取委托将整数属性值装箱返回。
    /// </summary>
    [Fact]
    public void GetValueGetter_Generic_IntProp_ReturnsBoxedValue()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Age))!;
        var getter = prop.GetValueGetter<PersonModel>();
        getter.ShouldNotBeNull();
        var person = new PersonModel { Age = 30 };
        getter(person).ShouldBe(30);
    }

    /// <summary>
    /// 验证泛型读取委托支持只读属性。
    /// </summary>
    [Fact]
    public void GetValueGetter_Generic_ReadOnlyProp_ReturnsGetter()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.ReadOnlyProp))!;
        var getter = prop.GetValueGetter<PersonModel>();
        getter.ShouldNotBeNull();
        getter(new PersonModel()).ShouldBe("readonly_value");
    }

    /// <summary>
    /// 验证只写属性无法生成泛型读取委托。
    /// </summary>
    [Fact]
    public void GetValueGetter_Generic_WriteOnlyProp_ThrowsInvalidOperationException()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.WriteOnlyProp))!;
        Should.Throw<InvalidOperationException>(() => prop.GetValueGetter<PersonModel>());
    }

    /// <summary>
    /// 验证静态属性无法生成泛型读取委托。
    /// </summary>
    [Fact]
    public void GetValueGetter_Generic_StaticProp_ThrowsArgumentException()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.ClassName))!;
        Should.Throw<ArgumentException>(() => prop.GetValueGetter<PersonModel>());
    }

    /// <summary>
    /// 验证重复获取泛型读取委托复用同一缓存实例。
    /// </summary>
    [Fact]
    public void GetValueGetter_Generic_CalledTwice_ReturnsSameDelegate()
    {
        // 验证缓存 — 两次调用返回同一委托实例
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Name))!;
        var getter1 = prop.GetValueGetter<PersonModel>();
        var getter2 = prop.GetValueGetter<PersonModel>();
        getter1.ShouldBeSameAs(getter2);
    }

    // ──────────────────────────────────────────
    //  GetValueGetter  — 非泛型版本
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证非泛型读取委托能够读取实例字符串属性。
    /// </summary>
    [Fact]
    public void GetValueGetter_NonGeneric_ReadableProp_ReturnsGetter()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Name))!;
        var getter = prop.GetValueGetter();
        getter.ShouldNotBeNull();
        var person = new PersonModel { Name = "Bob" };
        getter(person).ShouldBe("Bob");
    }

    /// <summary>
    /// 验证非泛型读取委托将整数属性值装箱返回。
    /// </summary>
    [Fact]
    public void GetValueGetter_NonGeneric_IntProp_ReturnsBoxedInt()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Age))!;
        var getter = prop.GetValueGetter();
        getter.ShouldNotBeNull();
        getter(new PersonModel { Age = 25 }).ShouldBe(25);
    }

    /// <summary>
    /// 验证非泛型读取委托无需实例即可读取静态属性。
    /// </summary>
    [Fact]
    public void GetValueGetter_NonGeneric_StaticProp_ReturnsValue()
    {
        // 非泛型版本支持静态属性
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.ClassName))!;
        var getter = prop.GetValueGetter();
        getter.ShouldNotBeNull();
        // 静态属性不依赖实例，传 null 也可行
        getter(null).ShouldBe("PersonModel");
    }

    /// <summary>
    /// 验证只写属性无法生成非泛型读取委托。
    /// </summary>
    [Fact]
    public void GetValueGetter_NonGeneric_WriteOnlyProp_ThrowsInvalidOperationException()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.WriteOnlyProp))!;
        Should.Throw<InvalidOperationException>(() => prop.GetValueGetter());
    }

    /// <summary>
    /// 验证重复获取非泛型读取委托复用同一缓存实例。
    /// </summary>
    [Fact]
    public void GetValueGetter_NonGeneric_CalledTwice_ReturnsSameDelegate()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Age))!;
        var getter1 = prop.GetValueGetter();
        var getter2 = prop.GetValueGetter();
        getter1.ShouldBeSameAs(getter2);
    }

    // ──────────────────────────────────────────
    //  GetValueSetter<T>  — 泛型版本
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证泛型写入委托能够更新实例字符串属性。
    /// </summary>
    [Fact]
    public void GetValueSetter_Generic_WritableProp_ReturnsSetter()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Name))!;
        var setter = prop.GetValueSetter<PersonModel>();
        setter.ShouldNotBeNull();
        var person = new PersonModel();
        setter(person, "Charlie");
        person.Name.ShouldBe("Charlie");
    }

    /// <summary>
    /// 验证泛型写入委托将装箱值写入整数属性。
    /// </summary>
    [Fact]
    public void GetValueSetter_Generic_IntProp_SetsUnboxedValue()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Age))!;
        var setter = prop.GetValueSetter<PersonModel>();
        setter.ShouldNotBeNull();
        var person = new PersonModel();
        setter(person, 42);
        person.Age.ShouldBe(42);
    }

    /// <summary>
    /// 验证只读属性无法生成泛型写入委托。
    /// </summary>
    [Fact]
    public void GetValueSetter_Generic_ReadOnlyProp_ThrowsInvalidOperationException()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.ReadOnlyProp))!;
        Should.Throw<InvalidOperationException>(() => prop.GetValueSetter<PersonModel>());
    }

    /// <summary>
    /// 验证静态属性无法生成泛型写入委托。
    /// </summary>
    [Fact]
    public void GetValueSetter_Generic_StaticProp_ThrowsArgumentException()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.StaticCount))!;
        Should.Throw<ArgumentException>(() => prop.GetValueSetter<PersonModel>());
    }

    /// <summary>
    /// 验证重复获取泛型写入委托复用同一缓存实例。
    /// </summary>
    [Fact]
    public void GetValueSetter_Generic_CalledTwice_ReturnsSameDelegate()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Name))!;
        var setter1 = prop.GetValueSetter<PersonModel>();
        var setter2 = prop.GetValueSetter<PersonModel>();
        setter1.ShouldBeSameAs(setter2);
    }

    // ──────────────────────────────────────────
    //  GetValueSetter  — 非泛型版本
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证非泛型写入委托能够更新实例字符串属性。
    /// </summary>
    [Fact]
    public void GetValueSetter_NonGeneric_WritableProp_ReturnsSetter()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Name))!;
        var setter = prop.GetValueSetter();
        setter.ShouldNotBeNull();
        var person = new PersonModel();
        setter(person, "Dave");
        person.Name.ShouldBe("Dave");
    }

    /// <summary>
    /// 验证非泛型写入委托无需实例即可更新静态属性。
    /// </summary>
    [Fact]
    public void GetValueSetter_NonGeneric_StaticProp_SetterWorks()
    {
        // 非泛型版本支持静态属性
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.StaticCount))!;
        var setter = prop.GetValueSetter();
        setter.ShouldNotBeNull();
        setter(null, 99);
        PersonModel.StaticCount.ShouldBe(99);
        // 恢复
        PersonModel.StaticCount = 0;
    }

    /// <summary>
    /// 验证只读属性无法生成非泛型写入委托。
    /// </summary>
    [Fact]
    public void GetValueSetter_NonGeneric_ReadOnlyProp_ThrowsInvalidOperationException()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.ReadOnlyProp))!;
        Should.Throw<InvalidOperationException>(() => prop.GetValueSetter());
    }

    /// <summary>
    /// 验证重复获取非泛型写入委托复用同一缓存实例。
    /// </summary>
    [Fact]
    public void GetValueSetter_NonGeneric_CalledTwice_ReturnsSameDelegate()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Age))!;
        var setter1 = prop.GetValueSetter();
        var setter2 = prop.GetValueSetter();
        setter1.ShouldBeSameAs(setter2);
    }

    // ──────────────────────────────────────────
    //  IsStatic  — 静态属性判断
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证普通实例属性不被识别为静态属性。
    /// </summary>
    [Fact]
    public void IsStatic_InstanceProp_ReturnsFalse()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Name))!;
        prop.IsStatic().ShouldBeFalse();
    }

    /// <summary>
    /// 验证只读静态属性被识别为静态属性。
    /// </summary>
    [Fact]
    public void IsStatic_StaticReadOnlyProp_ReturnsTrue()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.ClassName))!;
        prop.IsStatic().ShouldBeTrue();
    }

    /// <summary>
    /// 验证可读写静态属性被识别为静态属性。
    /// </summary>
    [Fact]
    public void IsStatic_StaticReadWriteProp_ReturnsTrue()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.StaticCount))!;
        prop.IsStatic().ShouldBeTrue();
    }

    /// <summary>
    /// 验证只写实例属性不被识别为静态属性。
    /// </summary>
    [Fact]
    public void IsStatic_WriteOnlyInstanceProp_ReturnsFalse()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.WriteOnlyProp))!;
        prop.IsStatic().ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  综合场景：读后写验证一致性
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证泛型读写委托返回最近写入的属性值。
    /// </summary>
    [Fact]
    public void GetterAndSetter_RoundTrip_ReturnsSetValue()
    {
        var nameProp = typeof(PersonModel).GetProperty(nameof(PersonModel.Name))!;
        var getter = nameProp.GetValueGetter<PersonModel>();
        var setter = nameProp.GetValueSetter<PersonModel>();

        var person = new PersonModel();
        setter(person, "Eve");
        getter(person).ShouldBe("Eve");
    }

    /// <summary>
    /// 验证非泛型读写委托返回最近写入的属性值。
    /// </summary>
    [Fact]
    public void GetterAndSetter_NonGeneric_RoundTrip_ReturnsSetValue()
    {
        var ageProp = typeof(PersonModel).GetProperty(nameof(PersonModel.Age))!;
        var getter = ageProp.GetValueGetter();
        var setter = ageProp.GetValueSetter();

        var person = new PersonModel();
        setter(person, 55);
        getter(person).ShouldBe(55);
    }
}
