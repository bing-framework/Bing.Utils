using System;
using System.Reflection;
using Shouldly;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 测试 <see cref="PropertyInfoExtensions"/>
/// 覆盖 GetValueGetter / GetValueSetter / IsStatic
/// </summary>
[Trait("Bing.Reflection", "PropertyInfoExtensions")]
public class PropertyInfoExtensionsTests
{
    // ──────────────────────────────────────────
    //  测试模型
    // ──────────────────────────────────────────

    private class PersonModel
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string ReadOnlyProp { get; } = "readonly_value";
        private string _writeOnlyBacking;
        public string WriteOnlyProp { set => _writeOnlyBacking = value; }
        public static string ClassName { get; } = "PersonModel";
        public static int StaticCount { get; set; } = 0;

        /// <summary>备份字段，用于验证 write-only 写入成功</summary>
        public string GetWriteOnlyBacking() => _writeOnlyBacking;
    }

    private struct PointStruct
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    // ──────────────────────────────────────────
    //  GetValueGetter<T>  — 泛型版本
    // ──────────────────────────────────────────

    [Fact]
    public void GetValueGetter_Generic_ReadableProp_ReturnsGetter()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Name))!;
        var getter = prop.GetValueGetter<PersonModel>();
        getter.ShouldNotBeNull();
        var person = new PersonModel { Name = "Alice" };
        getter(person).ShouldBe("Alice");
    }

    [Fact]
    public void GetValueGetter_Generic_IntProp_ReturnsBoxedValue()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Age))!;
        var getter = prop.GetValueGetter<PersonModel>();
        getter.ShouldNotBeNull();
        var person = new PersonModel { Age = 30 };
        getter(person).ShouldBe(30);
    }

    [Fact]
    public void GetValueGetter_Generic_ReadOnlyProp_ReturnsGetter()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.ReadOnlyProp))!;
        var getter = prop.GetValueGetter<PersonModel>();
        getter.ShouldNotBeNull();
        getter(new PersonModel()).ShouldBe("readonly_value");
    }

    [Fact]
    public void GetValueGetter_Generic_WriteOnlyProp_ReturnsNull()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.WriteOnlyProp))!;
        var getter = prop.GetValueGetter<PersonModel>();
        getter.ShouldBeNull();
    }

    [Fact]
    public void GetValueGetter_Generic_StaticProp_ReturnsNull()
    {
        // 静态属性被跳过，返回 null
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.ClassName))!;
        var getter = prop.GetValueGetter<PersonModel>();
        getter.ShouldBeNull();
    }

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

    [Fact]
    public void GetValueGetter_NonGeneric_ReadableProp_ReturnsGetter()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Name))!;
        var getter = prop.GetValueGetter();
        getter.ShouldNotBeNull();
        var person = new PersonModel { Name = "Bob" };
        getter(person).ShouldBe("Bob");
    }

    [Fact]
    public void GetValueGetter_NonGeneric_IntProp_ReturnsBoxedInt()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Age))!;
        var getter = prop.GetValueGetter();
        getter.ShouldNotBeNull();
        getter(new PersonModel { Age = 25 }).ShouldBe(25);
    }

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

    [Fact]
    public void GetValueGetter_NonGeneric_WriteOnlyProp_ReturnsNull()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.WriteOnlyProp))!;
        var getter = prop.GetValueGetter();
        getter.ShouldBeNull();
    }

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

    [Fact]
    public void GetValueSetter_Generic_ReadOnlyProp_ReturnsNull()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.ReadOnlyProp))!;
        var setter = prop.GetValueSetter<PersonModel>();
        setter.ShouldBeNull();
    }

    [Fact]
    public void GetValueSetter_Generic_StaticProp_ReturnsNull()
    {
        // 泛型版本跳过静态属性
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.StaticCount))!;
        var setter = prop.GetValueSetter<PersonModel>();
        setter.ShouldBeNull();
    }

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

    [Fact]
    public void GetValueSetter_NonGeneric_ReadOnlyProp_ReturnsNull()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.ReadOnlyProp))!;
        var setter = prop.GetValueSetter();
        setter.ShouldBeNull();
    }

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

    [Fact]
    public void IsStatic_InstanceProp_ReturnsFalse()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.Name))!;
        prop.IsStatic().ShouldBeFalse();
    }

    [Fact]
    public void IsStatic_StaticReadOnlyProp_ReturnsTrue()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.ClassName))!;
        prop.IsStatic().ShouldBeTrue();
    }

    [Fact]
    public void IsStatic_StaticReadWriteProp_ReturnsTrue()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.StaticCount))!;
        prop.IsStatic().ShouldBeTrue();
    }

    [Fact]
    public void IsStatic_WriteOnlyInstanceProp_ReturnsFalse()
    {
        var prop = typeof(PersonModel).GetProperty(nameof(PersonModel.WriteOnlyProp))!;
        prop.IsStatic().ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  综合场景：读后写验证一致性
    // ──────────────────────────────────────────

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
