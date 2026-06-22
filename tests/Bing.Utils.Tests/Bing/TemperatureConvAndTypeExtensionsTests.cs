using System.Collections;
using Bing.Utils.Maths;
using Bing.Utils.Parameters.Formats;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Bing;

// ─────────────────────────────────────────────────────────────────────────────
//  TemperatureConv Tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="TemperatureConv"/> 温度转换方法
/// </summary>
public class TemperatureConvTests
{
    // ── 摄氏度 → 华氏度 ───────────────────────────────────────────────────────

    [Fact]
    public void DegreesCelsiusToFahrenheit_Zero_Returns32()
    {
        TemperatureConv.DegreesCelsiusToFahrenheit(0).ShouldBe(32m);
    }

    [Fact]
    public void DegreesCelsiusToFahrenheit_HundredDegrees_Returns212()
    {
        TemperatureConv.DegreesCelsiusToFahrenheit(100).ShouldBe(212m);
    }

    [Fact]
    public void DegreesCelsiusToFahrenheit_Negative40_ReturnsMinus40()
    {
        // -40°C = -40°F (the only crossover point)
        TemperatureConv.DegreesCelsiusToFahrenheit(-40).ShouldBe(-40m);
    }

    // ── 摄氏度 → 开氏度 ───────────────────────────────────────────────────────

    [Fact]
    public void DegreesCelsiusToThermodynamicTemperature_Zero_Returns273Point16()
    {
        TemperatureConv.DegreesCelsiusToThermodynamicTemperature(0).ShouldBe(273.16m);
    }

    [Fact]
    public void DegreesCelsiusToThermodynamicTemperature_100_Returns373Point16()
    {
        TemperatureConv.DegreesCelsiusToThermodynamicTemperature(100).ShouldBe(373.16m);
    }

    // ── 华氏度 → 摄氏度 ───────────────────────────────────────────────────────

    [Fact]
    public void FahrenheitToDegreesCelsius_32_ReturnsZero()
    {
        TemperatureConv.FahrenheitToDegreesCelsius(32).ShouldBe(0m);
    }

    [Fact]
    public void FahrenheitToDegreesCelsius_212_Returns100()
    {
        TemperatureConv.FahrenheitToDegreesCelsius(212).ShouldBe(100m);
    }

    // ── 华氏度 → 开氏度 ───────────────────────────────────────────────────────

    [Fact]
    public void FahrenheitToThermodynamicTemperature_32_Returns273Point16()
    {
        TemperatureConv.FahrenheitToThermodynamicTemperature(32).ShouldBe(273.16m);
    }

    // ── 开氏度 → 摄氏度 ───────────────────────────────────────────────────────

    [Fact]
    public void ThermodynamicTemperatureToDegreesCelsius_273Point16_ReturnsZero()
    {
        TemperatureConv.ThermodynamicTemperatureToDegreesCelsius(273.16m).ShouldBe(0m);
    }

    [Fact]
    public void ThermodynamicTemperatureToDegreesCelsius_373Point16_Returns100()
    {
        TemperatureConv.ThermodynamicTemperatureToDegreesCelsius(373.16m).ShouldBe(100m);
    }

    // ── 开氏度 → 华氏度 ───────────────────────────────────────────────────────

    [Fact]
    public void ThermodynamicTemperatureToFahrenheit_273Point16_Returns32()
    {
        TemperatureConv.ThermodynamicTemperatureToFahrenheit(273.16m).ShouldBe(32m);
    }

    // ── 往返转换验证 ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(100)]
    [InlineData(-10)]
    public void CelsiusToFahrenheit_AndBack_RoundTrip(double celsius)
    {
        var fahrenheit = TemperatureConv.DegreesCelsiusToFahrenheit((decimal)celsius);
        var backCelsius = TemperatureConv.FahrenheitToDegreesCelsius(fahrenheit);
        backCelsius.ShouldBe((decimal)celsius, $"Round-trip failed for {celsius}°C");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(-50)]
    public void CelsiusToThermodynamic_AndBack_RoundTrip(double celsius)
    {
        var kelvin = TemperatureConv.DegreesCelsiusToThermodynamicTemperature((decimal)celsius);
        var backCelsius = TemperatureConv.ThermodynamicTemperatureToDegreesCelsius(kelvin);
        backCelsius.ShouldBe((decimal)celsius, $"Round-trip failed for {celsius}°C");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  TypeExtensions Tests  (namespace Bing)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="TypeExtensions"/> 扩展方法：
/// GetFullNameWithAssemblyName / IsAssignableTo / GetBaseClasses
/// </summary>
public class TypeExtensionsTests
{
    private class BaseA { }
    private class DerivedB : BaseA { }
    private class DerivedC : DerivedB { }

    // ── GetFullNameWithAssemblyName ────────────────────────────────────────────

    [Fact]
    public void GetFullNameWithAssemblyName_String_ContainsTypeName()
    {
        var result = typeof(string).GetFullNameWithAssemblyName();
        result.ShouldContain("System.String");
    }

    [Fact]
    public void GetFullNameWithAssemblyName_ContainsAssemblyName()
    {
        var result = typeof(string).GetFullNameWithAssemblyName();
        result.ShouldContain(",");  // "System.String, mscorlib" or "System.Private.CoreLib" etc.
    }

    [Fact]
    public void GetFullNameWithAssemblyName_NullType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeExtensions.GetFullNameWithAssemblyName(null!));
    }

    // ── IsAssignableTo<T> ─────────────────────────────────────────────────────

    [Fact]
    public void IsAssignableTo_Generic_StringToIEnumerable_ReturnsTrue()
    {
        typeof(string).IsAssignableTo<IEnumerable>().ShouldBeTrue();
    }

    [Fact]
    public void IsAssignableTo_Generic_IntToIEnumerable_ReturnsFalse()
    {
        typeof(int).IsAssignableTo<IEnumerable>().ShouldBeFalse();
    }

    [Fact]
    public void IsAssignableTo_Generic_DerivedToBase_ReturnsTrue()
    {
        typeof(DerivedB).IsAssignableTo<BaseA>().ShouldBeTrue();
    }

    [Fact]
    public void IsAssignableTo_Generic_SameType_ReturnsTrue()
    {
        typeof(BaseA).IsAssignableTo<BaseA>().ShouldBeTrue();
    }

    // ── IsAssignableTo(Type) ──────────────────────────────────────────────────

    [Fact]
    public void IsAssignableTo_Type_DerivedToBase_ReturnsTrue()
    {
        typeof(DerivedC).IsAssignableTo(typeof(BaseA)).ShouldBeTrue();
    }

    [Fact]
    public void IsAssignableTo_Type_UnrelatedTypes_ReturnsFalse()
    {
        typeof(string).IsAssignableTo(typeof(int)).ShouldBeFalse();
    }

    // ── GetBaseClasses(includeObject) ─────────────────────────────────────────

    [Fact]
    public void GetBaseClasses_IncludeObject_ContainsObject()
    {
        var bases = typeof(DerivedB).GetBaseClasses(includeObject: true);
        bases.ShouldContain(typeof(object));
    }

    [Fact]
    public void GetBaseClasses_ExcludeObject_DoesNotContainObject()
    {
        var bases = typeof(DerivedB).GetBaseClasses(includeObject: false);
        bases.ShouldNotContain(typeof(object));
    }

    [Fact]
    public void GetBaseClasses_DerivedC_ContainsChain()
    {
        var bases = typeof(DerivedC).GetBaseClasses(includeObject: false);
        bases.ShouldContain(typeof(DerivedB));
        bases.ShouldContain(typeof(BaseA));
    }

    // ── GetBaseClasses(stoppingType) ──────────────────────────────────────────

    [Fact]
    public void GetBaseClasses_WithStoppingType_StopsBeforeStopping()
    {
        // Starting from DerivedC, stop at DerivedB → should NOT include BaseA
        var bases = typeof(DerivedC).GetBaseClasses(typeof(DerivedB), includeObject: false);
        bases.ShouldNotContain(typeof(BaseA));
    }

    [Fact]
    public void GetBaseClasses_WithStoppingType_IncludesIntermediateTypes()
    {
        // GetBaseClasses starts from type.BaseType, so DerivedC itself is NOT included.
        // Stop at BaseA (exclusive): DerivedC.BaseType = DerivedB → DerivedB is included.
        var bases = typeof(DerivedC).GetBaseClasses(typeof(BaseA), includeObject: false);
        bases.ShouldContain(typeof(DerivedB));
        bases.ShouldNotContain(typeof(DerivedC));  // self is never included
        bases.ShouldNotContain(typeof(BaseA));      // stopping type is excluded
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  UrlParameterFormat / ParameterFormatBase Tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 测试 <see cref="UrlParameterFormat"/>（及基类 <see cref="ParameterFormatBase"/>）的 Format / Join 方法
/// </summary>
public class UrlParameterFormatTests
{
    private readonly IParameterFormat _fmt = UrlParameterFormat.Instance;

    // ── Format ────────────────────────────────────────────────────────────────

    [Fact]
    public void Format_Key_Value_ReturnKeyEqualsValue()
    {
        _fmt.Format("name", "Alice").ShouldBe("name=Alice");
    }

    [Fact]
    public void Format_NumericValue_ConvertedToString()
    {
        _fmt.Format("age", 30).ShouldBe("age=30");
    }

    [Fact]
    public void Format_EmptyKey_ReturnsEqualsWithValue()
    {
        _fmt.Format("", "v").ShouldBe("=v");
    }

    // ── Join ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Join_TwoParameters_ConnectedWithAmpersand()
    {
        _fmt.Join("a=1", "b=2").ShouldBe("a=1&b=2");
    }

    [Fact]
    public void Join_LeftEmpty_ReturnsRight()
    {
        _fmt.Join("", "b=2").ShouldBe("b=2");
    }

    [Fact]
    public void Join_RightEmpty_ReturnsLeft()
    {
        _fmt.Join("a=1", "").ShouldBe("a=1");
    }

    [Fact]
    public void Join_LeftNull_ReturnsRight()
    {
        _fmt.Join(null, "b=2").ShouldBe("b=2");
    }

    [Fact]
    public void Join_RightNull_ReturnsLeft()
    {
        _fmt.Join("a=1", null).ShouldBe("a=1");
    }

    [Fact]
    public void Join_BothEmpty_ReturnsEmpty()
    {
        // Both empty/null: left is empty → returns right (also empty)
        _fmt.Join("", "").ShouldBe("");
    }

    [Fact]
    public void Join_ThreeChained_BuildsQueryString()
    {
        var result = _fmt.Join(_fmt.Join("a=1", "b=2"), "c=3");
        result.ShouldBe("a=1&b=2&c=3");
    }

    // ── Instance is singleton ─────────────────────────────────────────────────

    [Fact]
    public void Instance_IsSameReference()
    {
        ReferenceEquals(UrlParameterFormat.Instance, UrlParameterFormat.Instance).ShouldBeTrue();
    }
}
