using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Bing.Extensions;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// 测试 <see cref="BingExtensions"/> Type 扩展方法
/// (IsNullableType / IsNullableEnum / HasAttribute / GetAttributes / GetAttribute /
///  IsCustomType / IsAnonymousType / IsBaseType / CanUseForDb /
///  IsDeriveClassFrom / IsBaseOn / IsIntegerType / IsCollectionType / IsValueType)
/// </summary>
public class BingExtensionsTypeTests
{
    // ──────────────────────────────────────────
    //  Helper types
    // ──────────────────────────────────────────

    [Description("I am described")]
    private class DescribedClass { }

    private class BaseClass { }
    private class DerivedClass : BaseClass { }
    private interface IFoo { }
    private class FooImpl : IFoo { }

    // ──────────────────────────────────────────
    //  IsNullableType
    // ──────────────────────────────────────────

    [Fact]
    public void IsNullableType_NullableInt_ReturnsTrue()
    {
        typeof(int?).IsNullableType().ShouldBeTrue();
    }

    [Fact]
    public void IsNullableType_Int_ReturnsFalse()
    {
        typeof(int).IsNullableType().ShouldBeFalse();
    }

    [Fact]
    public void IsNullableType_NullableDateTime_ReturnsTrue()
    {
        typeof(DateTime?).IsNullableType().ShouldBeTrue();
    }

    [Fact]
    public void IsNullableType_WithGenericParam_Match_ReturnsTrue()
    {
        typeof(int?).IsNullableType(typeof(int)).ShouldBeTrue();
    }

    [Fact]
    public void IsNullableType_WithGenericParam_NoMatch_ReturnsFalse()
    {
        typeof(int?).IsNullableType(typeof(string)).ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  IsNullableEnum
    // ──────────────────────────────────────────

    [Fact]
    public void IsNullableEnum_NullableDayOfWeek_ReturnsTrue()
    {
        typeof(DayOfWeek?).IsNullableEnum().ShouldBeTrue();
    }

    [Fact]
    public void IsNullableEnum_DayOfWeek_ReturnsFalse()
    {
        typeof(DayOfWeek).IsNullableEnum().ShouldBeFalse();
    }

    [Fact]
    public void IsNullableEnum_NullableInt_ReturnsFalse()
    {
        typeof(int?).IsNullableEnum().ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  HasAttribute / GetAttributes / GetAttribute
    // ──────────────────────────────────────────

    [Fact]
    public void HasAttribute_HasDescriptionAttribute_ReturnsTrue()
    {
        typeof(DescribedClass).HasAttribute<DescriptionAttribute>().ShouldBeTrue();
    }

    [Fact]
    public void HasAttribute_NoAttribute_ReturnsFalse()
    {
        typeof(BaseClass).HasAttribute<DescriptionAttribute>().ShouldBeFalse();
    }

    [Fact]
    public void GetAttribute_Present_ReturnsAttribute()
    {
        var attr = typeof(DescribedClass).GetAttribute<DescriptionAttribute>();
        attr.ShouldNotBeNull();
        attr!.Description.ShouldBe("I am described");
    }

    [Fact]
    public void GetAttribute_NotPresent_ReturnsNull()
    {
        typeof(BaseClass).GetAttribute<DescriptionAttribute>().ShouldBeNull();
    }

    [Fact]
    public void GetAttributes_Present_ReturnsCollection()
    {
        var attrs = typeof(DescribedClass).GetAttributes<DescriptionAttribute>();
        attrs.ShouldNotBeEmpty();
    }

    // ──────────────────────────────────────────
    //  IsCustomType
    // ──────────────────────────────────────────

    [Fact]
    public void IsCustomType_PlainClass_ReturnsTrue()
    {
        typeof(BaseClass).IsCustomType().ShouldBeTrue();
    }

    [Fact]
    public void IsCustomType_Primitive_ReturnsFalse()
    {
        typeof(int).IsCustomType().ShouldBeFalse();
    }

    [Fact]
    public void IsCustomType_String_ReturnsFalse()
    {
        typeof(string).IsCustomType().ShouldBeFalse(); // TypeCode.String
    }

    [Fact]
    public void IsCustomType_Guid_ReturnsFalse()
    {
        typeof(Guid).IsCustomType().ShouldBeFalse(); // explicitly excluded
    }

    [Fact]
    public void IsCustomType_Object_ReturnsFalse()
    {
        typeof(object).IsCustomType().ShouldBeFalse(); // explicitly excluded
    }

    [Fact]
    public void IsCustomType_GenericList_ReturnsFalse()
    {
        typeof(List<int>).IsCustomType().ShouldBeFalse(); // IsGenericType
    }

    [Fact]
    public void IsCustomType_PrimitiveArray_ReturnsFalse()
    {
        typeof(int[]).IsCustomType().ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  IsAnonymousType
    // ──────────────────────────────────────────

    [Fact]
    public void IsAnonymousType_AnonymousObject_ReturnsTrue()
    {
        var anon = new { Name = "test", Age = 1 };
        anon.GetType().IsAnonymousType().ShouldBeTrue();
    }

    [Fact]
    public void IsAnonymousType_PlainClass_ReturnsFalse()
    {
        typeof(BaseClass).IsAnonymousType().ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  IsBaseType
    // ──────────────────────────────────────────

    [Fact]
    public void IsBaseType_SameType_ReturnsTrue()
    {
        typeof(DerivedClass).IsBaseType(typeof(DerivedClass)).ShouldBeTrue();
    }

    [Fact]
    public void IsBaseType_DirectBase_ReturnsTrue()
    {
        typeof(DerivedClass).IsBaseType(typeof(BaseClass)).ShouldBeTrue();
    }

    [Fact]
    public void IsBaseType_UnrelatedType_ReturnsFalse()
    {
        typeof(string).IsBaseType(typeof(int)).ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  CanUseForDb
    // ──────────────────────────────────────────

    [Fact]
    public void CanUseForDb_String_ReturnsTrue() => typeof(string).CanUseForDb().ShouldBeTrue();

    [Fact]
    public void CanUseForDb_Int_ReturnsTrue() => typeof(int).CanUseForDb().ShouldBeTrue();

    [Fact]
    public void CanUseForDb_Guid_ReturnsTrue() => typeof(Guid).CanUseForDb().ShouldBeTrue();

    [Fact]
    public void CanUseForDb_DateTime_ReturnsTrue() => typeof(DateTime).CanUseForDb().ShouldBeTrue();

    [Fact]
    public void CanUseForDb_Enum_ReturnsTrue() => typeof(DayOfWeek).CanUseForDb().ShouldBeTrue();

    [Fact]
    public void CanUseForDb_NullableInt_ReturnsTrue() => typeof(int?).CanUseForDb().ShouldBeTrue();

    [Fact]
    public void CanUseForDb_List_ReturnsFalse() => typeof(List<int>).CanUseForDb().ShouldBeFalse();

    [Fact]
    public void CanUseForDb_PlainClass_ReturnsFalse() => typeof(BaseClass).CanUseForDb().ShouldBeFalse();

    // ──────────────────────────────────────────
    //  IsDeriveClassFrom / IsBaseOn
    // ──────────────────────────────────────────

    [Fact]
    public void IsDeriveClassFrom_DerivedFromBase_ReturnsTrue()
    {
        typeof(DerivedClass).IsDeriveClassFrom<BaseClass>().ShouldBeTrue();
    }

    [Fact]
    public void IsDeriveClassFrom_Unrelated_ReturnsFalse()
    {
        typeof(string).IsDeriveClassFrom<BaseClass>().ShouldBeFalse();
    }

    [Fact]
    public void IsBaseOn_InterfaceImpl_ReturnsTrue()
    {
        typeof(FooImpl).IsBaseOn<IFoo>().ShouldBeTrue();
    }

    [Fact]
    public void IsBaseOn_NotImplementing_ReturnsFalse()
    {
        typeof(BaseClass).IsBaseOn<IFoo>().ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  IsIntegerType
    // ──────────────────────────────────────────

    [Fact]
    public void IsIntegerType_Int_ReturnsTrue() => typeof(int).IsIntegerType().ShouldBeTrue();

    [Fact]
    public void IsIntegerType_Long_ReturnsTrue() => typeof(long).IsIntegerType().ShouldBeTrue();

    [Fact]
    public void IsIntegerType_Byte_ReturnsTrue() => typeof(byte).IsIntegerType().ShouldBeTrue();

    [Fact]
    public void IsIntegerType_Double_ReturnsFalse() => typeof(double).IsIntegerType().ShouldBeFalse();

    [Fact]
    public void IsIntegerType_String_ReturnsFalse() => typeof(string).IsIntegerType().ShouldBeFalse();

    // ──────────────────────────────────────────
    //  IsCollectionType (BingExtensions version)
    // ──────────────────────────────────────────

    [Fact]
    public void IsCollectionType_ListOfInt_ReturnsTrue() => typeof(List<int>).IsCollectionType().ShouldBeTrue();

    [Fact]
    public void IsCollectionType_Array_ReturnsTrue() => typeof(int[]).IsCollectionType().ShouldBeTrue();

    [Fact]
    public void IsCollectionType_String_ReturnsTrue()
    {
        // string implements IEnumerable<char>
        typeof(string).IsCollectionType().ShouldBeTrue();
    }

    [Fact]
    public void IsCollectionType_Int_ReturnsFalse() => typeof(int).IsCollectionType().ShouldBeFalse();

    [Fact]
    public void IsCollectionType_PlainClass_ReturnsFalse() => typeof(BaseClass).IsCollectionType().ShouldBeFalse();

    // ──────────────────────────────────────────
    //  IsValueType (BingExtensions custom version)
    // ──────────────────────────────────────────

    [Fact]
    public void BingIsValueType_Int_ReturnsTrue() => typeof(int).IsValueType().ShouldBeTrue();

    [Fact]
    public void BingIsValueType_String_ReturnsTrue() => typeof(string).IsValueType().ShouldBeTrue();

    [Fact]
    public void BingIsValueType_Boolean_ReturnsTrue() => typeof(bool).IsValueType().ShouldBeTrue();

    [Fact]
    public void BingIsValueType_Double_ReturnsTrue() => typeof(double).IsValueType().ShouldBeTrue();

    [Fact]
    public void BingIsValueType_List_ReturnsFalse() => typeof(List<int>).IsValueType().ShouldBeFalse();

    [Fact]
    public void BingIsValueType_PlainClass_ReturnsFalse() => typeof(BaseClass).IsValueType().ShouldBeFalse();
}

/// <summary>
/// 测试 <see cref="BingExtensions"/> Reflection / DateTime 扩展
/// (GetPropertyValue / Description(TimeSpan))
/// </summary>
public class BingExtensionsReflectionAndDateTimeTests
{
    private class Sample
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    // ──────────────────────────────────────────
    //  GetPropertyValue
    // ──────────────────────────────────────────

    [Fact]
    public void GetPropertyValue_ExistingProperty_ReturnsValue()
    {
        var member = typeof(Sample).GetProperty("Name");
        var obj = new Sample { Name = "Alice" };
        member.GetPropertyValue(obj).ShouldBe("Alice");
    }

    [Fact]
    public void GetPropertyValue_IntProperty_ReturnsBoxedValue()
    {
        var member = typeof(Sample).GetProperty("Age");
        var obj = new Sample { Age = 42 };
        member.GetPropertyValue(obj).ShouldBe(42);
    }

    [Fact]
    public void GetPropertyValue_NullMember_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            BingExtensions.GetPropertyValue(null!, new Sample()));
    }

    [Fact]
    public void GetPropertyValue_NullInstance_ThrowsArgumentNullException()
    {
        var member = typeof(Sample).GetProperty("Name");
        Should.Throw<ArgumentNullException>(() => member.GetPropertyValue(null!));
    }

    // ──────────────────────────────────────────
    //  Description(TimeSpan)
    // ──────────────────────────────────────────

    [Fact]
    public void Description_TimeSpan_DaysOnly_ContainsDays()
    {
        var span = TimeSpan.FromDays(2);
        span.Description().ShouldBe("2天");
    }

    [Fact]
    public void Description_TimeSpan_HoursAndMinutes_ContainsBoth()
    {
        var span = new TimeSpan(0, 3, 15, 0);
        var result = span.Description();
        result.ShouldContain("3小时");
        result.ShouldContain("15分");
    }

    [Fact]
    public void Description_TimeSpan_Seconds_ContainsSeconds()
    {
        var span = TimeSpan.FromSeconds(45);
        span.Description().ShouldContain("45秒");
    }

    [Fact]
    public void Description_TimeSpan_Milliseconds_ContainsMilliseconds()
    {
        var span = TimeSpan.FromMilliseconds(250);
        span.Description().ShouldContain("250毫秒");
    }

    [Fact]
    public void Description_TimeSpan_Zero_ReturnsTotalMilliseconds()
    {
        var span = TimeSpan.Zero;
        span.Description().ShouldBe("0毫秒");
    }
}
