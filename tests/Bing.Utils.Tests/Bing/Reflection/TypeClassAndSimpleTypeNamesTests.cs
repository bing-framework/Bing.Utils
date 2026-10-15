using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 验证常用类型属性及别名返回预期类型。
/// </summary>
[Trait("Bing.Reflection", "TypeClass")]
public class TypeClassTests
{
    // ──────────────────────────────────────────
    //  基础类型 (void / object)
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>VoidClazz</c> 返回 <c>typeof(void)</c>。
    /// </summary>
    [Fact] public void VoidClazz_ReturnsTypeOfVoid() => TypeClass.VoidClazz.ShouldBe(typeof(void));

    /// <summary>
    /// 验证 <c>ObjectClazz</c> 返回 <c>typeof(object)</c>。
    /// </summary>
    [Fact] public void ObjectClazz_ReturnsTypeOfObject() => TypeClass.ObjectClazz.ShouldBe(typeof(object));

    /// <summary>
    /// 验证 <c>ObjectArrayClazz</c> 返回 <c>typeof(object[])</c>。
    /// </summary>
    [Fact] public void ObjectArrayClazz_ReturnsTypeOfObjectArray() => TypeClass.ObjectArrayClazz.ShouldBe(typeof(object[]));

    // ──────────────────────────────────────────
    //  byte 系列
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>ByteClazz</c> 返回 <c>typeof(byte)</c>。
    /// </summary>
    [Fact] public void ByteClazz_ReturnsTypeOfByte() => TypeClass.ByteClazz.ShouldBe(typeof(byte));

    /// <summary>
    /// 验证 <c>ByteNullableClazz</c> 返回 <c>typeof(byte?)</c>。
    /// </summary>
    [Fact] public void ByteNullableClazz_ReturnsTypeOfNullableByte() => TypeClass.ByteNullableClazz.ShouldBe(typeof(byte?));

    /// <summary>
    /// 验证 <c>ByteArrayClazz</c> 返回 <c>typeof(byte[])</c>。
    /// </summary>
    [Fact] public void ByteArrayClazz_ReturnsTypeOfByteArray() => TypeClass.ByteArrayClazz.ShouldBe(typeof(byte[]));

    /// <summary>
    /// 验证 <c>SByteClazz</c> 返回 <c>typeof(sbyte)</c>。
    /// </summary>
    [Fact] public void SByteClazz_ReturnsTypeOfSByte() => TypeClass.SByteClazz.ShouldBe(typeof(sbyte));

    /// <summary>
    /// 验证 <c>SByteNullableClazz</c> 返回 <c>typeof(sbyte?)</c>。
    /// </summary>
    [Fact] public void SByteNullableClazz_ReturnsTypeOfNullableSByte() => TypeClass.SByteNullableClazz.ShouldBe(typeof(sbyte?));

    // ──────────────────────────────────────────
    //  short / ushort (Int16 / UInt16)
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>Int16Clazz</c> 返回 <c>typeof(short)</c>。
    /// </summary>
    [Fact] public void Int16Clazz_ReturnsTypeOfShort() => TypeClass.Int16Clazz.ShouldBe(typeof(short));

    /// <summary>
    /// 验证 <c>Int16NullableClazz</c> 返回 <c>typeof(short?)</c>。
    /// </summary>
    [Fact] public void Int16NullableClazz_ReturnsTypeOfNullableShort() => TypeClass.Int16NullableClazz.ShouldBe(typeof(short?));

    /// <summary>
    /// 验证 <c>UInt16Clazz</c> 返回 <c>typeof(ushort)</c>。
    /// </summary>
    [Fact] public void UInt16Clazz_ReturnsTypeOfUShort() => TypeClass.UInt16Clazz.ShouldBe(typeof(ushort));

    /// <summary>
    /// 验证 <c>UInt16NullableClazz</c> 返回 <c>typeof(ushort?)</c>。
    /// </summary>
    [Fact] public void UInt16NullableClazz_ReturnsTypeOfNullableUShort() => TypeClass.UInt16NullableClazz.ShouldBe(typeof(ushort?));

    /// <summary>
    /// 验证 <c>ShortClazz</c> 返回 <c>TypeClass.Int16Clazz</c>。
    /// </summary>
    [Fact] public void ShortClazz_IsSameAsInt16Clazz() => TypeClass.ShortClazz.ShouldBe(TypeClass.Int16Clazz);

    /// <summary>
    /// 验证 <c>ShortNullableClazz</c> 返回 <c>TypeClass.Int16NullableClazz</c>。
    /// </summary>
    [Fact] public void ShortNullableClazz_IsSameAsInt16NullableClazz() => TypeClass.ShortNullableClazz.ShouldBe(TypeClass.Int16NullableClazz);

    /// <summary>
    /// 验证 <c>UShortClazz</c> 返回 <c>TypeClass.UInt16Clazz</c>。
    /// </summary>
    [Fact] public void UShortClazz_IsSameAsUInt16Clazz() => TypeClass.UShortClazz.ShouldBe(TypeClass.UInt16Clazz);

    /// <summary>
    /// 验证 <c>UShortNullableClazz</c> 返回 <c>TypeClass.UInt16NullableClazz</c>。
    /// </summary>
    [Fact] public void UShortNullableClazz_IsSameAsUInt16NullableClazz() => TypeClass.UShortNullableClazz.ShouldBe(TypeClass.UInt16NullableClazz);

    // ──────────────────────────────────────────
    //  int / uint (Int32 / UInt32)
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>Int32Clazz</c> 返回 <c>typeof(int)</c>。
    /// </summary>
    [Fact] public void Int32Clazz_ReturnsTypeOfInt() => TypeClass.Int32Clazz.ShouldBe(typeof(int));

    /// <summary>
    /// 验证 <c>Int32NullableClazz</c> 返回 <c>typeof(int?)</c>。
    /// </summary>
    [Fact] public void Int32NullableClazz_ReturnsTypeOfNullableInt() => TypeClass.Int32NullableClazz.ShouldBe(typeof(int?));

    /// <summary>
    /// 验证 <c>UInt32Clazz</c> 返回 <c>typeof(uint)</c>。
    /// </summary>
    [Fact] public void UInt32Clazz_ReturnsTypeOfUInt() => TypeClass.UInt32Clazz.ShouldBe(typeof(uint));

    /// <summary>
    /// 验证 <c>UInt32NullableClazz</c> 返回 <c>typeof(uint?)</c>。
    /// </summary>
    [Fact] public void UInt32NullableClazz_ReturnsTypeOfNullableUInt() => TypeClass.UInt32NullableClazz.ShouldBe(typeof(uint?));

    /// <summary>
    /// 验证 <c>IntClazz</c> 返回 <c>TypeClass.Int32Clazz</c>。
    /// </summary>
    [Fact] public void IntClazz_IsSameAsInt32Clazz() => TypeClass.IntClazz.ShouldBe(TypeClass.Int32Clazz);

    /// <summary>
    /// 验证 <c>IntNullableClazz</c> 返回 <c>TypeClass.Int32NullableClazz</c>。
    /// </summary>
    [Fact] public void IntNullableClazz_IsSameAsInt32NullableClazz() => TypeClass.IntNullableClazz.ShouldBe(TypeClass.Int32NullableClazz);

    /// <summary>
    /// 验证 <c>UIntClazz</c> 返回 <c>TypeClass.UInt32Clazz</c>。
    /// </summary>
    [Fact] public void UIntClazz_IsSameAsUInt32Clazz() => TypeClass.UIntClazz.ShouldBe(TypeClass.UInt32Clazz);

    /// <summary>
    /// 验证 <c>UIntNullableClazz</c> 返回 <c>TypeClass.UInt32NullableClazz</c>。
    /// </summary>
    [Fact] public void UIntNullableClazz_IsSameAsUInt32NullableClazz() => TypeClass.UIntNullableClazz.ShouldBe(TypeClass.UInt32NullableClazz);

    // ──────────────────────────────────────────
    //  long / ulong (Int64 / UInt64)
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>Int64Clazz</c> 返回 <c>typeof(long)</c>。
    /// </summary>
    [Fact] public void Int64Clazz_ReturnsTypeOfLong() => TypeClass.Int64Clazz.ShouldBe(typeof(long));

    /// <summary>
    /// 验证 <c>Int64NullableClazz</c> 返回 <c>typeof(long?)</c>。
    /// </summary>
    [Fact] public void Int64NullableClazz_ReturnsTypeOfNullableLong() => TypeClass.Int64NullableClazz.ShouldBe(typeof(long?));

    /// <summary>
    /// 验证 <c>UInt64Clazz</c> 返回 <c>typeof(ulong)</c>。
    /// </summary>
    [Fact] public void UInt64Clazz_ReturnsTypeOfULong() => TypeClass.UInt64Clazz.ShouldBe(typeof(ulong));

    /// <summary>
    /// 验证 <c>UInt64NullableClazz</c> 返回 <c>typeof(ulong?)</c>。
    /// </summary>
    [Fact] public void UInt64NullableClazz_ReturnsTypeOfNullableULong() => TypeClass.UInt64NullableClazz.ShouldBe(typeof(ulong?));

    /// <summary>
    /// 验证 <c>LongClazz</c> 返回 <c>TypeClass.Int64Clazz</c>。
    /// </summary>
    [Fact] public void LongClazz_IsSameAsInt64Clazz() => TypeClass.LongClazz.ShouldBe(TypeClass.Int64Clazz);

    /// <summary>
    /// 验证 <c>LongNullableClazz</c> 返回 <c>TypeClass.Int64NullableClazz</c>。
    /// </summary>
    [Fact] public void LongNullableClazz_IsSameAsInt64NullableClazz() => TypeClass.LongNullableClazz.ShouldBe(TypeClass.Int64NullableClazz);

    /// <summary>
    /// 验证 <c>ULongClazz</c> 返回 <c>TypeClass.UInt64Clazz</c>。
    /// </summary>
    [Fact] public void ULongClazz_IsSameAsUInt64Clazz() => TypeClass.ULongClazz.ShouldBe(TypeClass.UInt64Clazz);

    /// <summary>
    /// 验证 <c>ULongNullableClazz</c> 返回 <c>TypeClass.UInt64NullableClazz</c>。
    /// </summary>
    [Fact] public void ULongNullableClazz_IsSameAsUInt64NullableClazz() => TypeClass.ULongNullableClazz.ShouldBe(TypeClass.UInt64NullableClazz);

    // ──────────────────────────────────────────
    //  float / Single
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>FloatClazz</c> 返回 <c>typeof(float)</c>。
    /// </summary>
    [Fact] public void FloatClazz_ReturnsTypeOfFloat() => TypeClass.FloatClazz.ShouldBe(typeof(float));

    /// <summary>
    /// 验证 <c>FloatNullableClazz</c> 返回 <c>typeof(float?)</c>。
    /// </summary>
    [Fact] public void FloatNullableClazz_ReturnsTypeOfNullableFloat() => TypeClass.FloatNullableClazz.ShouldBe(typeof(float?));

    /// <summary>
    /// 验证 <c>SingleClazz</c> 返回 <c>typeof(float)</c>。
    /// </summary>
    [Fact] public void SingleClazz_ReturnsTypeOfFloat() => TypeClass.SingleClazz.ShouldBe(typeof(float));

    /// <summary>
    /// 验证 <c>SingleNullableClazz</c> 返回 <c>typeof(float?)</c>。
    /// </summary>
    [Fact] public void SingleNullableClazz_ReturnsTypeOfNullableFloat() => TypeClass.SingleNullableClazz.ShouldBe(typeof(float?));

    // ──────────────────────────────────────────
    //  double / decimal
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>DoubleClazz</c> 返回 <c>typeof(double)</c>。
    /// </summary>
    [Fact] public void DoubleClazz_ReturnsTypeOfDouble() => TypeClass.DoubleClazz.ShouldBe(typeof(double));

    /// <summary>
    /// 验证 <c>DoubleNullableClazz</c> 返回 <c>typeof(double?)</c>。
    /// </summary>
    [Fact] public void DoubleNullableClazz_ReturnsTypeOfNullableDouble() => TypeClass.DoubleNullableClazz.ShouldBe(typeof(double?));

    /// <summary>
    /// 验证 <c>DecimalClazz</c> 返回 <c>typeof(decimal)</c>。
    /// </summary>
    [Fact] public void DecimalClazz_ReturnsTypeOfDecimal() => TypeClass.DecimalClazz.ShouldBe(typeof(decimal));

    /// <summary>
    /// 验证 <c>DecimalNullableClazz</c> 返回 <c>typeof(decimal?)</c>。
    /// </summary>
    [Fact] public void DecimalNullableClazz_ReturnsTypeOfNullableDecimal() => TypeClass.DecimalNullableClazz.ShouldBe(typeof(decimal?));

    // ──────────────────────────────────────────
    //  string / bool / char
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>StringClazz</c> 返回 <c>typeof(string)</c>。
    /// </summary>
    [Fact] public void StringClazz_ReturnsTypeOfString() => TypeClass.StringClazz.ShouldBe(typeof(string));

    /// <summary>
    /// 验证 <c>BooleanClazz</c> 返回 <c>typeof(bool)</c>。
    /// </summary>
    [Fact] public void BooleanClazz_ReturnsTypeOfBool() => TypeClass.BooleanClazz.ShouldBe(typeof(bool));

    /// <summary>
    /// 验证 <c>BooleanNullableClazz</c> 返回 <c>typeof(bool?)</c>。
    /// </summary>
    [Fact] public void BooleanNullableClazz_ReturnsTypeOfNullableBool() => TypeClass.BooleanNullableClazz.ShouldBe(typeof(bool?));

    /// <summary>
    /// 验证 <c>CharClazz</c> 返回 <c>typeof(char)</c>。
    /// </summary>
    [Fact] public void CharClazz_ReturnsTypeOfChar() => TypeClass.CharClazz.ShouldBe(typeof(char));

    /// <summary>
    /// 验证 <c>CharNullableClazz</c> 返回 <c>typeof(char?)</c>。
    /// </summary>
    [Fact] public void CharNullableClazz_ReturnsTypeOfNullableChar() => TypeClass.CharNullableClazz.ShouldBe(typeof(char?));

    // ──────────────────────────────────────────
    //  DateTime / DateTimeOffset / TimeSpan / Guid
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>DateTimeClazz</c> 返回 <c>typeof(DateTime)</c>。
    /// </summary>
    [Fact] public void DateTimeClazz_ReturnsTypeOfDateTime() => TypeClass.DateTimeClazz.ShouldBe(typeof(DateTime));

    /// <summary>
    /// 验证 <c>DateTimeNullableClazz</c> 返回 <c>typeof(DateTime?)</c>。
    /// </summary>
    [Fact] public void DateTimeNullableClazz_ReturnsTypeOfNullableDateTime() => TypeClass.DateTimeNullableClazz.ShouldBe(typeof(DateTime?));

    /// <summary>
    /// 验证 <c>DateTimeOffsetClazz</c> 返回 <c>typeof(DateTimeOffset)</c>。
    /// </summary>
    [Fact] public void DateTimeOffsetClazz_ReturnsTypeOfDateTimeOffset() => TypeClass.DateTimeOffsetClazz.ShouldBe(typeof(DateTimeOffset));

    /// <summary>
    /// 验证 <c>DateTimeOffsetNullableClazz</c> 返回 <c>typeof(DateTimeOffset?)</c>。
    /// </summary>
    [Fact] public void DateTimeOffsetNullableClazz_ReturnsTypeOfNullableDateTimeOffset() => TypeClass.DateTimeOffsetNullableClazz.ShouldBe(typeof(DateTimeOffset?));

    /// <summary>
    /// 验证 <c>TimeSpanClazz</c> 返回 <c>typeof(TimeSpan)</c>。
    /// </summary>
    [Fact] public void TimeSpanClazz_ReturnsTypeOfTimeSpan() => TypeClass.TimeSpanClazz.ShouldBe(typeof(TimeSpan));

    /// <summary>
    /// 验证 <c>TimeSpanNullableClazz</c> 返回 <c>typeof(TimeSpan?)</c>。
    /// </summary>
    [Fact] public void TimeSpanNullableClazz_ReturnsTypeOfNullableTimeSpan() => TypeClass.TimeSpanNullableClazz.ShouldBe(typeof(TimeSpan?));

    /// <summary>
    /// 验证 <c>GuidClazz</c> 返回 <c>typeof(Guid)</c>。
    /// </summary>
    [Fact] public void GuidClazz_ReturnsTypeOfGuid() => TypeClass.GuidClazz.ShouldBe(typeof(Guid));

    /// <summary>
    /// 验证 <c>GuidNullableClazz</c> 返回 <c>typeof(Guid?)</c>。
    /// </summary>
    [Fact] public void GuidNullableClazz_ReturnsTypeOfNullableGuid() => TypeClass.GuidNullableClazz.ShouldBe(typeof(Guid?));

    // ──────────────────────────────────────────
    //  Enum / Tuple / Task / ValueTask
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>EnumClazz</c> 返回 <c>typeof(Enum)</c>。
    /// </summary>
    [Fact] public void EnumClazz_ReturnsTypeOfEnum() => TypeClass.EnumClazz.ShouldBe(typeof(Enum));

    /// <summary>
    /// 验证 <c>ValueTupleClazz</c> 返回 <c>typeof(ValueTuple)</c>。
    /// </summary>
    [Fact] public void ValueTupleClazz_ReturnsTypeOfValueTuple() => TypeClass.ValueTupleClazz.ShouldBe(typeof(ValueTuple));

    /// <summary>
    /// 验证 <c>TaskClazz</c> 返回 <c>typeof(Task)</c>。
    /// </summary>
    [Fact] public void TaskClazz_ReturnsTypeOfTask() => TypeClass.TaskClazz.ShouldBe(typeof(Task));

    /// <summary>
    /// 验证 <c>GenericTaskClazz</c> 返回 <c>typeof(Task&lt;&gt;)</c>。
    /// </summary>
    [Fact] public void GenericTaskClazz_ReturnsOpenGenericTask() => TypeClass.GenericTaskClazz.ShouldBe(typeof(Task<>));

    /// <summary>
    /// 验证 <c>ValueTaskClazz</c> 返回 <c>typeof(ValueTask)</c>。
    /// </summary>
    [Fact] public void ValueTaskClazz_ReturnsTypeOfValueTask() => TypeClass.ValueTaskClazz.ShouldBe(typeof(ValueTask));

    /// <summary>
    /// 验证 <c>GenericValueTaskClazz</c> 返回 <c>typeof(ValueTask&lt;&gt;)</c>。
    /// </summary>
    [Fact] public void GenericValueTaskClazz_ReturnsOpenGenericValueTask() => TypeClass.GenericValueTaskClazz.ShouldBe(typeof(ValueTask<>));

    // ──────────────────────────────────────────
    //  集合 / Nullable
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>GenericListClazz</c> 返回 <c>typeof(List&lt;&gt;)</c>。
    /// </summary>
    [Fact] public void GenericListClazz_ReturnsOpenGenericList() => TypeClass.GenericListClazz.ShouldBe(typeof(List<>));

    /// <summary>
    /// 验证 <c>NullableClazz</c> 返回 <c>typeof(Nullable)</c>。
    /// </summary>
    [Fact] public void NullableClazz_ReturnsTypeOfNullable() => TypeClass.NullableClazz.ShouldBe(typeof(Nullable));

    /// <summary>
    /// 验证 <c>GenericNullableClazz</c> 返回 <c>typeof(Nullable&lt;&gt;)</c>。
    /// </summary>
    [Fact] public void GenericNullableClazz_ReturnsOpenGenericNullable() => TypeClass.GenericNullableClazz.ShouldBe(typeof(Nullable<>));

    // ──────────────────────────────────────────
    //  格式化接口
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>FormattableClazz</c> 返回 <c>typeof(IFormattable)</c>。
    /// </summary>
    [Fact] public void FormattableClazz_ReturnsTypeOfIFormattable() => TypeClass.FormattableClazz.ShouldBe(typeof(IFormattable));

    /// <summary>
    /// 验证 <c>FormatProviderClazz</c> 返回 <c>typeof(IFormatProvider)</c>。
    /// </summary>
    [Fact] public void FormatProviderClazz_ReturnsTypeOfIFormatProvider() => TypeClass.FormatProviderClazz.ShouldBe(typeof(IFormatProvider));

    // ──────────────────────────────────────────
    //  属性值唯一性与非空验证
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证常用类型属性均已初始化。
    /// </summary>
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

    /// <summary>
    /// 验证整数、布尔值与标识符的可空属性返回相应包装类型。
    /// </summary>
    [Fact]
    public void NullableVariant_IsNullableWrappedPrimitive()
    {
        // 验证 Nullable<T> 包装正确
        TypeClass.Int32NullableClazz.ShouldBe(typeof(Nullable<int>));
        TypeClass.BooleanNullableClazz.ShouldBe(typeof(Nullable<bool>));
        TypeClass.GuidNullableClazz.ShouldBe(typeof(Nullable<Guid>));
    }

    /// <summary>
    /// 验证整数类型别名与规范名称引用同一类型实例。
    /// </summary>
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
/// 验证简单类型名称常量及类型名称集合。
/// </summary>
[Trait("Bing.Reflection", "SimpleTypeNames")]
public class SimpleTypeNamesTests
{
    // ──────────────────────────────────────────
    //  常量值验证
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>Byte</c> 常量保存类型名称 <c>System.Byte</c>。
    /// </summary>
    [Fact] public void Byte_ConstValue_IsSystemByte() => SimpleTypeNames.Byte.ShouldBe("System.Byte");

    /// <summary>
    /// 验证 <c>SByte</c> 常量保存类型名称 <c>System.SByte</c>。
    /// </summary>
    [Fact] public void SByte_ConstValue_IsSystemSByte() => SimpleTypeNames.SByte.ShouldBe("System.SByte");

    /// <summary>
    /// 验证 <c>Int16</c> 常量保存类型名称 <c>System.Int16</c>。
    /// </summary>
    [Fact] public void Int16_ConstValue_IsSystemInt16() => SimpleTypeNames.Int16.ShouldBe("System.Int16");

    /// <summary>
    /// 验证 <c>UInt16</c> 常量保存类型名称 <c>System.UInt16</c>。
    /// </summary>
    [Fact] public void UInt16_ConstValue_IsSystemUInt16() => SimpleTypeNames.UInt16.ShouldBe("System.UInt16");

    /// <summary>
    /// 验证 <c>Int32</c> 常量保存类型名称 <c>System.Int32</c>。
    /// </summary>
    [Fact] public void Int32_ConstValue_IsSystemInt32() => SimpleTypeNames.Int32.ShouldBe("System.Int32");

    /// <summary>
    /// 验证 <c>UInt32</c> 常量保存类型名称 <c>System.UInt32</c>。
    /// </summary>
    [Fact] public void UInt32_ConstValue_IsSystemUInt32() => SimpleTypeNames.UInt32.ShouldBe("System.UInt32");

    /// <summary>
    /// 验证 <c>Int64</c> 常量保存类型名称 <c>System.Int64</c>。
    /// </summary>
    [Fact] public void Int64_ConstValue_IsSystemInt64() => SimpleTypeNames.Int64.ShouldBe("System.Int64");

    /// <summary>
    /// 验证 <c>UInt64</c> 常量保存类型名称 <c>System.UInt64</c>。
    /// </summary>
    [Fact] public void UInt64_ConstValue_IsSystemUInt64() => SimpleTypeNames.UInt64.ShouldBe("System.UInt64");

    /// <summary>
    /// 验证 <c>Single</c> 常量保存类型名称 <c>System.Single</c>。
    /// </summary>
    [Fact] public void Single_ConstValue_IsSystemSingle() => SimpleTypeNames.Single.ShouldBe("System.Single");

    /// <summary>
    /// 验证 <c>Double</c> 常量保存类型名称 <c>System.Double</c>。
    /// </summary>
    [Fact] public void Double_ConstValue_IsSystemDouble() => SimpleTypeNames.Double.ShouldBe("System.Double");

    /// <summary>
    /// 验证 <c>Boolean</c> 常量保存类型名称 <c>System.Boolean</c>。
    /// </summary>
    [Fact] public void Boolean_ConstValue_IsSystemBoolean() => SimpleTypeNames.Boolean.ShouldBe("System.Boolean");

    /// <summary>
    /// 验证 <c>Char</c> 常量保存类型名称 <c>System.Char</c>。
    /// </summary>
    [Fact] public void Char_ConstValue_IsSystemChar() => SimpleTypeNames.Char.ShouldBe("System.Char");

    /// <summary>
    /// 验证 <c>IntPtr</c> 常量保存类型名称 <c>System.IntPtr</c>。
    /// </summary>
    [Fact] public void IntPtr_ConstValue_IsSystemIntPtr() => SimpleTypeNames.IntPtr.ShouldBe("System.IntPtr");

    /// <summary>
    /// 验证 <c>UIntPtr</c> 常量保存类型名称 <c>System.UIntPtr</c>。
    /// </summary>
    [Fact] public void UIntPtr_ConstValue_IsSystemUIntPtr() => SimpleTypeNames.UIntPtr.ShouldBe("System.UIntPtr");

    /// <summary>
    /// 验证 <c>Decimal</c> 常量保存类型名称 <c>System.Decimal</c>。
    /// </summary>
    [Fact] public void Decimal_ConstValue_IsSystemDecimal() => SimpleTypeNames.Decimal.ShouldBe("System.Decimal");

    /// <summary>
    /// 验证 <c>TimeSpan</c> 常量保存类型名称 <c>System.TimeSpan</c>。
    /// </summary>
    [Fact] public void TimeSpan_ConstValue_IsSystemTimeSpan() => SimpleTypeNames.TimeSpan.ShouldBe("System.TimeSpan");

    /// <summary>
    /// 验证 <c>DateTime</c> 常量保存类型名称 <c>System.DateTime</c>。
    /// </summary>
    [Fact] public void DateTime_ConstValue_IsSystemDateTime() => SimpleTypeNames.DateTime.ShouldBe("System.DateTime");

    /// <summary>
    /// 验证 <c>DateTimeOffset</c> 常量保存类型名称 <c>System.DateTimeOffset</c>。
    /// </summary>
    [Fact] public void DateTimeOffset_ConstValue_IsSystemDateTimeOffset() => SimpleTypeNames.DateTimeOffset.ShouldBe("System.DateTimeOffset");

    /// <summary>
    /// 验证 <c>Guid</c> 常量保存类型名称 <c>System.Guid</c>。
    /// </summary>
    [Fact] public void Guid_ConstValue_IsSystemGuid() => SimpleTypeNames.Guid.ShouldBe("System.Guid");

    /// <summary>
    /// 验证 <c>String</c> 常量保存类型名称 <c>System.String</c>。
    /// </summary>
    [Fact] public void String_ConstValue_IsSystemString() => SimpleTypeNames.String.ShouldBe("System.String");

    /// <summary>
    /// 验证 <c>Bytes</c> 常量保存类型名称 <c>System.Byte[]</c>。
    /// </summary>
    [Fact] public void Bytes_ConstValue_IsSystemByteArray() => SimpleTypeNames.Bytes.ShouldBe("System.Byte[]");

    // ──────────────────────────────────────────
    //  PrimitiveTypes 集合验证
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证基元类型名称集合包含全部预期类型。
    /// </summary>
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

    /// <summary>
    /// 验证基元类型名称集合包含十四个成员。
    /// </summary>
    [Fact]
    public void PrimitiveTypes_HasExactly14Members()
    {
        SimpleTypeNames.PrimitiveTypes.Count.ShouldBe(14);
    }

    /// <summary>
    /// 验证基元类型名称集合排除十进制数和字符串。
    /// </summary>
    [Fact]
    public void PrimitiveTypes_DoesNotContainDecimalOrString()
    {
        SimpleTypeNames.PrimitiveTypes.ShouldNotContain("System.Decimal");
        SimpleTypeNames.PrimitiveTypes.ShouldNotContain("System.String");
    }

    /// <summary>
    /// 验证基元类型名称查询区分大小写。
    /// </summary>
    [Fact]
    public void PrimitiveTypes_Lookup_IsCaseSensitive()
    {
        SimpleTypeNames.PrimitiveTypes.Contains("system.int32").ShouldBeFalse();
        SimpleTypeNames.PrimitiveTypes.Contains("System.Int32").ShouldBeTrue();
    }

    // ──────────────────────────────────────────
    //  SimpleTypes 数组验证
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证简单类型名称数组包含全部预期类型。
    /// </summary>
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

    /// <summary>
    /// 验证简单类型名称数组包含七个成员。
    /// </summary>
    [Fact]
    public void SimpleTypes_HasExactly7Members()
    {
        SimpleTypeNames.SimpleTypes.Length.ShouldBe(7);
    }

    /// <summary>
    /// 验证简单类型名称数组排除整数类型。
    /// </summary>
    [Fact]
    public void SimpleTypes_DoesNotContainInt32()
    {
        // int 是 PrimitiveType，不在 SimpleTypes 中
        SimpleTypeNames.SimpleTypes.ShouldNotContain("System.Int32");
    }

    // ──────────────────────────────────────────
    //  常量与 typeof() 匹配验证
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证类型名称常量与运行时类型完整名称一致。
    /// </summary>
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
/// 验证调用异常处理委托的默认行为与替换能力。
/// </summary>
[Trait("Bing.Reflection", "InvokeHelper")]
public class InvokeHelperTests
{
    /// <summary>
    /// 验证默认调用异常处理委托已初始化。
    /// </summary>
    [Fact]
    public void OnInvokeException_DefaultValue_IsNotNull()
    {
        InvokeHelper.OnInvokeException.ShouldNotBeNull();
    }

    /// <summary>
    /// 验证替换后的调用异常处理委托接收原异常实例。
    /// </summary>
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

    /// <summary>
    /// 验证默认调用异常处理委托处理异常时不会抛出异常。
    /// </summary>
    [Fact]
    public void OnInvokeException_DefaultHandler_DoesNotThrow()
    {
        // 默认处理器输出到 Console，不应抛出异常
        Should.NotThrow(() => InvokeHelper.OnInvokeException(new Exception("test")));
    }
}
