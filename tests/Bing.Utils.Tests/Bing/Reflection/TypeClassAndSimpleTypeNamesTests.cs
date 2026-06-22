using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 测试 <see cref="TypeClass"/>
/// 验证所有静态类型属性返回正确的 <see cref="Type"/> 实例
/// </summary>
[Trait("Bing.Reflection", "TypeClass")]
public class TypeClassTests
{
    // ──────────────────────────────────────────
    //  基础类型 (void / object)
    // ──────────────────────────────────────────

    [Fact] public void VoidClazz_ReturnsTypeOfVoid() => TypeClass.VoidClazz.ShouldBe(typeof(void));

    [Fact] public void ObjectClazz_ReturnsTypeOfObject() => TypeClass.ObjectClazz.ShouldBe(typeof(object));

    [Fact] public void ObjectArrayClazz_ReturnsTypeOfObjectArray() => TypeClass.ObjectArrayClazz.ShouldBe(typeof(object[]));

    // ──────────────────────────────────────────
    //  byte 系列
    // ──────────────────────────────────────────

    [Fact] public void ByteClazz_ReturnsTypeOfByte() => TypeClass.ByteClazz.ShouldBe(typeof(byte));

    [Fact] public void ByteNullableClazz_ReturnsTypeOfNullableByte() => TypeClass.ByteNullableClazz.ShouldBe(typeof(byte?));

    [Fact] public void ByteArrayClazz_ReturnsTypeOfByteArray() => TypeClass.ByteArrayClazz.ShouldBe(typeof(byte[]));

    [Fact] public void SByteClazz_ReturnsTypeOfSByte() => TypeClass.SByteClazz.ShouldBe(typeof(sbyte));

    [Fact] public void SByteNullableClazz_ReturnsTypeOfNullableSByte() => TypeClass.SByteNullableClazz.ShouldBe(typeof(sbyte?));

    // ──────────────────────────────────────────
    //  short / ushort (Int16 / UInt16)
    // ──────────────────────────────────────────

    [Fact] public void Int16Clazz_ReturnsTypeOfShort() => TypeClass.Int16Clazz.ShouldBe(typeof(short));

    [Fact] public void Int16NullableClazz_ReturnsTypeOfNullableShort() => TypeClass.Int16NullableClazz.ShouldBe(typeof(short?));

    [Fact] public void UInt16Clazz_ReturnsTypeOfUShort() => TypeClass.UInt16Clazz.ShouldBe(typeof(ushort));

    [Fact] public void UInt16NullableClazz_ReturnsTypeOfNullableUShort() => TypeClass.UInt16NullableClazz.ShouldBe(typeof(ushort?));

    /// <summary>ShortClazz 是 Int16Clazz 的别名</summary>
    [Fact] public void ShortClazz_IsSameAsInt16Clazz() => TypeClass.ShortClazz.ShouldBe(TypeClass.Int16Clazz);

    [Fact] public void ShortNullableClazz_IsSameAsInt16NullableClazz() => TypeClass.ShortNullableClazz.ShouldBe(TypeClass.Int16NullableClazz);

    [Fact] public void UShortClazz_IsSameAsUInt16Clazz() => TypeClass.UShortClazz.ShouldBe(TypeClass.UInt16Clazz);

    [Fact] public void UShortNullableClazz_IsSameAsUInt16NullableClazz() => TypeClass.UShortNullableClazz.ShouldBe(TypeClass.UInt16NullableClazz);

    // ──────────────────────────────────────────
    //  int / uint (Int32 / UInt32)
    // ──────────────────────────────────────────

    [Fact] public void Int32Clazz_ReturnsTypeOfInt() => TypeClass.Int32Clazz.ShouldBe(typeof(int));

    [Fact] public void Int32NullableClazz_ReturnsTypeOfNullableInt() => TypeClass.Int32NullableClazz.ShouldBe(typeof(int?));

    [Fact] public void UInt32Clazz_ReturnsTypeOfUInt() => TypeClass.UInt32Clazz.ShouldBe(typeof(uint));

    [Fact] public void UInt32NullableClazz_ReturnsTypeOfNullableUInt() => TypeClass.UInt32NullableClazz.ShouldBe(typeof(uint?));

    /// <summary>IntClazz 是 Int32Clazz 的别名</summary>
    [Fact] public void IntClazz_IsSameAsInt32Clazz() => TypeClass.IntClazz.ShouldBe(TypeClass.Int32Clazz);

    [Fact] public void IntNullableClazz_IsSameAsInt32NullableClazz() => TypeClass.IntNullableClazz.ShouldBe(TypeClass.Int32NullableClazz);

    [Fact] public void UIntClazz_IsSameAsUInt32Clazz() => TypeClass.UIntClazz.ShouldBe(TypeClass.UInt32Clazz);

    [Fact] public void UIntNullableClazz_IsSameAsUInt32NullableClazz() => TypeClass.UIntNullableClazz.ShouldBe(TypeClass.UInt32NullableClazz);

    // ──────────────────────────────────────────
    //  long / ulong (Int64 / UInt64)
    // ──────────────────────────────────────────

    [Fact] public void Int64Clazz_ReturnsTypeOfLong() => TypeClass.Int64Clazz.ShouldBe(typeof(long));

    [Fact] public void Int64NullableClazz_ReturnsTypeOfNullableLong() => TypeClass.Int64NullableClazz.ShouldBe(typeof(long?));

    [Fact] public void UInt64Clazz_ReturnsTypeOfULong() => TypeClass.UInt64Clazz.ShouldBe(typeof(ulong));

    [Fact] public void UInt64NullableClazz_ReturnsTypeOfNullableULong() => TypeClass.UInt64NullableClazz.ShouldBe(typeof(ulong?));

    [Fact] public void LongClazz_IsSameAsInt64Clazz() => TypeClass.LongClazz.ShouldBe(TypeClass.Int64Clazz);

    [Fact] public void LongNullableClazz_IsSameAsInt64NullableClazz() => TypeClass.LongNullableClazz.ShouldBe(TypeClass.Int64NullableClazz);

    [Fact] public void ULongClazz_IsSameAsUInt64Clazz() => TypeClass.ULongClazz.ShouldBe(TypeClass.UInt64Clazz);

    [Fact] public void ULongNullableClazz_IsSameAsUInt64NullableClazz() => TypeClass.ULongNullableClazz.ShouldBe(TypeClass.UInt64NullableClazz);

    // ──────────────────────────────────────────
    //  float / Single
    // ──────────────────────────────────────────

    [Fact] public void FloatClazz_ReturnsTypeOfFloat() => TypeClass.FloatClazz.ShouldBe(typeof(float));

    [Fact] public void FloatNullableClazz_ReturnsTypeOfNullableFloat() => TypeClass.FloatNullableClazz.ShouldBe(typeof(float?));

    [Fact] public void SingleClazz_ReturnsTypeOfFloat() => TypeClass.SingleClazz.ShouldBe(typeof(float));

    [Fact] public void SingleNullableClazz_ReturnsTypeOfNullableFloat() => TypeClass.SingleNullableClazz.ShouldBe(typeof(float?));

    // ──────────────────────────────────────────
    //  double / decimal
    // ──────────────────────────────────────────

    [Fact] public void DoubleClazz_ReturnsTypeOfDouble() => TypeClass.DoubleClazz.ShouldBe(typeof(double));

    [Fact] public void DoubleNullableClazz_ReturnsTypeOfNullableDouble() => TypeClass.DoubleNullableClazz.ShouldBe(typeof(double?));

    [Fact] public void DecimalClazz_ReturnsTypeOfDecimal() => TypeClass.DecimalClazz.ShouldBe(typeof(decimal));

    [Fact] public void DecimalNullableClazz_ReturnsTypeOfNullableDecimal() => TypeClass.DecimalNullableClazz.ShouldBe(typeof(decimal?));

    // ──────────────────────────────────────────
    //  string / bool / char
    // ──────────────────────────────────────────

    [Fact] public void StringClazz_ReturnsTypeOfString() => TypeClass.StringClazz.ShouldBe(typeof(string));

    [Fact] public void BooleanClazz_ReturnsTypeOfBool() => TypeClass.BooleanClazz.ShouldBe(typeof(bool));

    [Fact] public void BooleanNullableClazz_ReturnsTypeOfNullableBool() => TypeClass.BooleanNullableClazz.ShouldBe(typeof(bool?));

    [Fact] public void CharClazz_ReturnsTypeOfChar() => TypeClass.CharClazz.ShouldBe(typeof(char));

    [Fact] public void CharNullableClazz_ReturnsTypeOfNullableChar() => TypeClass.CharNullableClazz.ShouldBe(typeof(char?));

    // ──────────────────────────────────────────
    //  DateTime / DateTimeOffset / TimeSpan / Guid
    // ──────────────────────────────────────────

    [Fact] public void DateTimeClazz_ReturnsTypeOfDateTime() => TypeClass.DateTimeClazz.ShouldBe(typeof(DateTime));

    [Fact] public void DateTimeNullableClazz_ReturnsTypeOfNullableDateTime() => TypeClass.DateTimeNullableClazz.ShouldBe(typeof(DateTime?));

    [Fact] public void DateTimeOffsetClazz_ReturnsTypeOfDateTimeOffset() => TypeClass.DateTimeOffsetClazz.ShouldBe(typeof(DateTimeOffset));

    [Fact] public void DateTimeOffsetNullableClazz_ReturnsTypeOfNullableDateTimeOffset() => TypeClass.DateTimeOffsetNullableClazz.ShouldBe(typeof(DateTimeOffset?));

    [Fact] public void TimeSpanClazz_ReturnsTypeOfTimeSpan() => TypeClass.TimeSpanClazz.ShouldBe(typeof(TimeSpan));

    [Fact] public void TimeSpanNullableClazz_ReturnsTypeOfNullableTimeSpan() => TypeClass.TimeSpanNullableClazz.ShouldBe(typeof(TimeSpan?));

    [Fact] public void GuidClazz_ReturnsTypeOfGuid() => TypeClass.GuidClazz.ShouldBe(typeof(Guid));

    [Fact] public void GuidNullableClazz_ReturnsTypeOfNullableGuid() => TypeClass.GuidNullableClazz.ShouldBe(typeof(Guid?));

    // ──────────────────────────────────────────
    //  Enum / Tuple / Task / ValueTask
    // ──────────────────────────────────────────

    [Fact] public void EnumClazz_ReturnsTypeOfEnum() => TypeClass.EnumClazz.ShouldBe(typeof(Enum));

    [Fact] public void ValueTupleClazz_ReturnsTypeOfValueTuple() => TypeClass.ValueTupleClazz.ShouldBe(typeof(ValueTuple));

    [Fact] public void TaskClazz_ReturnsTypeOfTask() => TypeClass.TaskClazz.ShouldBe(typeof(Task));

    [Fact] public void GenericTaskClazz_ReturnsOpenGenericTask() => TypeClass.GenericTaskClazz.ShouldBe(typeof(Task<>));

    [Fact] public void ValueTaskClazz_ReturnsTypeOfValueTask() => TypeClass.ValueTaskClazz.ShouldBe(typeof(ValueTask));

    [Fact] public void GenericValueTaskClazz_ReturnsOpenGenericValueTask() => TypeClass.GenericValueTaskClazz.ShouldBe(typeof(ValueTask<>));

    // ──────────────────────────────────────────
    //  集合 / Nullable
    // ──────────────────────────────────────────

    [Fact] public void GenericListClazz_ReturnsOpenGenericList() => TypeClass.GenericListClazz.ShouldBe(typeof(List<>));

    [Fact] public void NullableClazz_ReturnsTypeOfNullable() => TypeClass.NullableClazz.ShouldBe(typeof(Nullable));

    [Fact] public void GenericNullableClazz_ReturnsOpenGenericNullable() => TypeClass.GenericNullableClazz.ShouldBe(typeof(Nullable<>));

    // ──────────────────────────────────────────
    //  格式化接口
    // ──────────────────────────────────────────

    [Fact] public void FormattableClazz_ReturnsTypeOfIFormattable() => TypeClass.FormattableClazz.ShouldBe(typeof(IFormattable));

    [Fact] public void FormatProviderClazz_ReturnsTypeOfIFormatProvider() => TypeClass.FormatProviderClazz.ShouldBe(typeof(IFormatProvider));

    // ──────────────────────────────────────────
    //  属性值唯一性与非空验证
    // ──────────────────────────────────────────

    [Fact]
    public void AllClazzProperties_AreNotNull()
    {
        TypeClass.VoidClazz.ShouldNotBeNull();
        TypeClass.ObjectClazz.ShouldNotBeNull();
        TypeClass.StringClazz.ShouldNotBeNull();
        TypeClass.Int32Clazz.ShouldNotBeNull();
        TypeClass.BooleanClazz.ShouldNotBeNull();
        TypeClass.TaskClazz.ShouldNotBeNull();
        TypeClass.GenericListClazz.ShouldNotBeNull();
    }

    [Fact]
    public void NullableVariant_IsNullableWrappedPrimitive()
    {
        // 验证 Nullable<T> 包装正确
        TypeClass.Int32NullableClazz.ShouldBe(typeof(Nullable<int>));
        TypeClass.BooleanNullableClazz.ShouldBe(typeof(Nullable<bool>));
        TypeClass.GuidNullableClazz.ShouldBe(typeof(Nullable<Guid>));
    }

    [Fact]
    public void AliasProperties_AreSameAsCanonicalProperties()
    {
        // 别名属性指向相同的 Type 实例
        TypeClass.IntClazz.ShouldBeSameAs(TypeClass.Int32Clazz);
        TypeClass.LongClazz.ShouldBeSameAs(TypeClass.Int64Clazz);
        TypeClass.ShortClazz.ShouldBeSameAs(TypeClass.Int16Clazz);
        TypeClass.UIntClazz.ShouldBeSameAs(TypeClass.UInt32Clazz);
        TypeClass.ULongClazz.ShouldBeSameAs(TypeClass.UInt64Clazz);
        TypeClass.UShortClazz.ShouldBeSameAs(TypeClass.UInt16Clazz);
    }
}

/// <summary>
/// 测试 <see cref="SimpleTypeNames"/>
/// 验证所有常量值和集合内容的正确性
/// </summary>
[Trait("Bing.Reflection", "SimpleTypeNames")]
public class SimpleTypeNamesTests
{
    // ──────────────────────────────────────────
    //  常量值验证
    // ──────────────────────────────────────────

    [Fact] public void Byte_ConstValue_IsSystemByte() => SimpleTypeNames.Byte.ShouldBe("System.Byte");

    [Fact] public void SByte_ConstValue_IsSystemSByte() => SimpleTypeNames.SByte.ShouldBe("System.SByte");

    [Fact] public void Int16_ConstValue_IsSystemInt16() => SimpleTypeNames.Int16.ShouldBe("System.Int16");

    [Fact] public void UInt16_ConstValue_IsSystemUInt16() => SimpleTypeNames.UInt16.ShouldBe("System.UInt16");

    [Fact] public void Int32_ConstValue_IsSystemInt32() => SimpleTypeNames.Int32.ShouldBe("System.Int32");

    [Fact] public void UInt32_ConstValue_IsSystemUInt32() => SimpleTypeNames.UInt32.ShouldBe("System.UInt32");

    [Fact] public void Int64_ConstValue_IsSystemInt64() => SimpleTypeNames.Int64.ShouldBe("System.Int64");

    [Fact] public void UInt64_ConstValue_IsSystemUInt64() => SimpleTypeNames.UInt64.ShouldBe("System.UInt64");

    [Fact] public void Single_ConstValue_IsSystemSingle() => SimpleTypeNames.Single.ShouldBe("System.Single");

    [Fact] public void Double_ConstValue_IsSystemDouble() => SimpleTypeNames.Double.ShouldBe("System.Double");

    [Fact] public void Boolean_ConstValue_IsSystemBoolean() => SimpleTypeNames.Boolean.ShouldBe("System.Boolean");

    [Fact] public void Char_ConstValue_IsSystemChar() => SimpleTypeNames.Char.ShouldBe("System.Char");

    [Fact] public void IntPtr_ConstValue_IsSystemIntPtr() => SimpleTypeNames.IntPtr.ShouldBe("System.IntPtr");

    [Fact] public void UIntPtr_ConstValue_IsSystemUIntPtr() => SimpleTypeNames.UIntPtr.ShouldBe("System.UIntPtr");

    [Fact] public void Decimal_ConstValue_IsSystemDecimal() => SimpleTypeNames.Decimal.ShouldBe("System.Decimal");

    [Fact] public void TimeSpan_ConstValue_IsSystemTimeSpan() => SimpleTypeNames.TimeSpan.ShouldBe("System.TimeSpan");

    [Fact] public void DateTime_ConstValue_IsSystemDateTime() => SimpleTypeNames.DateTime.ShouldBe("System.DateTime");

    [Fact] public void DateTimeOffset_ConstValue_IsSystemDateTimeOffset() => SimpleTypeNames.DateTimeOffset.ShouldBe("System.DateTimeOffset");

    [Fact] public void Guid_ConstValue_IsSystemGuid() => SimpleTypeNames.Guid.ShouldBe("System.Guid");

    [Fact] public void String_ConstValue_IsSystemString() => SimpleTypeNames.String.ShouldBe("System.String");

    [Fact] public void Bytes_ConstValue_IsSystemByteArray() => SimpleTypeNames.Bytes.ShouldBe("System.Byte[]");

    // ──────────────────────────────────────────
    //  PrimitiveTypes 集合验证
    // ──────────────────────────────────────────

    [Fact]
    public void PrimitiveTypes_ContainsAllExpectedTypes()
    {
        var expected = new[]
        {
            "System.Byte", "System.SByte", "System.Int16", "System.UInt16",
            "System.Int32", "System.UInt32", "System.Int64", "System.UInt64",
            "System.Single", "System.Double", "System.Boolean", "System.Char",
            "System.IntPtr", "System.UIntPtr"
        };
        foreach (var name in expected)
            SimpleTypeNames.PrimitiveTypes.ShouldContain(name);
    }

    [Fact]
    public void PrimitiveTypes_HasExactly14Members()
    {
        SimpleTypeNames.PrimitiveTypes.Count.ShouldBe(14);
    }

    [Fact]
    public void PrimitiveTypes_DoesNotContainDecimalOrString()
    {
        SimpleTypeNames.PrimitiveTypes.ShouldNotContain("System.Decimal");
        SimpleTypeNames.PrimitiveTypes.ShouldNotContain("System.String");
    }

    [Fact]
    public void PrimitiveTypes_Lookup_IsCaseSensitive()
    {
        SimpleTypeNames.PrimitiveTypes.Contains("system.int32").ShouldBeFalse();
        SimpleTypeNames.PrimitiveTypes.Contains("System.Int32").ShouldBeTrue();
    }

    // ──────────────────────────────────────────
    //  SimpleTypes 数组验证
    // ──────────────────────────────────────────

    [Fact]
    public void SimpleTypes_ContainsAllExpectedTypes()
    {
        var expected = new[]
        {
            "System.Decimal", "System.TimeSpan", "System.DateTime",
            "System.DateTimeOffset", "System.Guid", "System.String", "System.Byte[]"
        };
        foreach (var name in expected)
            SimpleTypeNames.SimpleTypes.ShouldContain(name);
    }

    [Fact]
    public void SimpleTypes_HasExactly7Members()
    {
        SimpleTypeNames.SimpleTypes.Length.ShouldBe(7);
    }

    [Fact]
    public void SimpleTypes_DoesNotContainInt32()
    {
        // int 是 PrimitiveType，不在 SimpleTypes 中
        SimpleTypeNames.SimpleTypes.ShouldNotContain("System.Int32");
    }

    // ──────────────────────────────────────────
    //  常量与 typeof() 匹配验证
    // ──────────────────────────────────────────

    [Fact]
    public void ConstValues_MatchRealTypeFullNames()
    {
        // 验证常量字符串与实际类型的 FullName 一致
        SimpleTypeNames.Int32.ShouldBe(typeof(int).FullName);
        SimpleTypeNames.String.ShouldBe(typeof(string).FullName);
        SimpleTypeNames.Boolean.ShouldBe(typeof(bool).FullName);
        SimpleTypeNames.Double.ShouldBe(typeof(double).FullName);
        SimpleTypeNames.Decimal.ShouldBe(typeof(decimal).FullName);
        SimpleTypeNames.Guid.ShouldBe(typeof(Guid).FullName);
        SimpleTypeNames.DateTime.ShouldBe(typeof(DateTime).FullName);
    }
}

/// <summary>
/// 测试 <see cref="InvokeHelper"/>
/// 验证静态异常处理委托的默认值与可替换性
/// </summary>
[Trait("Bing.Reflection", "InvokeHelper")]
public class InvokeHelperTests
{
    [Fact]
    public void OnInvokeException_DefaultValue_IsNotNull()
    {
        InvokeHelper.OnInvokeException.ShouldNotBeNull();
    }

    [Fact]
    public void OnInvokeException_CanBeReplaced()
    {
        var original = InvokeHelper.OnInvokeException;
        try
        {
            Exception captured = null;
            InvokeHelper.OnInvokeException = ex => captured = ex;
            var testException = new InvalidOperationException("test");
            InvokeHelper.OnInvokeException(testException);
            captured.ShouldBeSameAs(testException);
        }
        finally
        {
            // 恢复原始处理器
            InvokeHelper.OnInvokeException = original;
        }
    }

    [Fact]
    public void OnInvokeException_DefaultHandler_DoesNotThrow()
    {
        // 默认处理器输出到 Console，不应抛出异常
        Should.NotThrow(() => InvokeHelper.OnInvokeException(new Exception("test")));
    }
}
