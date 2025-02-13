using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "TypeClass")]
public class TypeClassTest
{
    /// <summary>
    /// 测试 - Void
    /// </summary>
    [Fact]
    public void Test_VoidType()
    {
        TypeClass.VoidClazz.ShouldBe(typeof(void));
    }

    /// <summary>
    /// 测试 - 类型
    /// </summary>
    [Fact]
    public void Test_DirectType()
    {
        TypeClass.ObjectClazz.ShouldBe(typeof(object));
            
        TypeClass.ByteClazz.ShouldBe(typeof(byte));
        TypeClass.SByteClazz.ShouldBe(typeof(sbyte));
        TypeClass.Int16Clazz.ShouldBe(typeof(short));
        TypeClass.UInt16Clazz.ShouldBe(typeof(ushort));
        TypeClass.Int32Clazz.ShouldBe(typeof(int));
        TypeClass.UInt32Clazz.ShouldBe(typeof(uint));
        TypeClass.Int64Clazz.ShouldBe(typeof(long));
        TypeClass.UInt64Clazz.ShouldBe(typeof(ulong));
            
        TypeClass.ShortClazz.ShouldBe(typeof(short));
        TypeClass.UShortClazz.ShouldBe(typeof(ushort));
        TypeClass.IntClazz.ShouldBe(typeof(int));
        TypeClass.UIntClazz.ShouldBe(typeof(uint));
        TypeClass.LongClazz.ShouldBe(typeof(long));
        TypeClass.ULongClazz.ShouldBe(typeof(ulong));

        TypeClass.FloatClazz.ShouldBe(typeof(float));
        TypeClass.DoubleClazz.ShouldBe(typeof(double));
        TypeClass.DecimalClazz.ShouldBe(typeof(decimal));
            
        TypeClass.DateTimeClazz.ShouldBe(typeof(DateTime));
        TypeClass.DateTimeOffsetClazz.ShouldBe(typeof(DateTimeOffset));
        TypeClass.TimeSpanClazz.ShouldBe(typeof(TimeSpan));
            
        TypeClass.GuidClazz.ShouldBe(typeof(Guid));
        TypeClass.StringClazz.ShouldBe(typeof(string));
        TypeClass.BooleanClazz.ShouldBe(typeof(bool));
        TypeClass.CharClazz.ShouldBe(typeof(char));
        TypeClass.EnumClazz.ShouldBe(typeof(Enum));
        TypeClass.ValueTupleClazz.ShouldBe(typeof(ValueTuple));
            
        TypeClass.TaskClazz.ShouldBe(typeof(Task));
        TypeClass.GenericTaskClazz.ShouldBe(typeof(Task<>));
        TypeClass.ValueTaskClazz.ShouldBe(typeof(ValueTask));
        TypeClass.GenericValueTaskClazz.ShouldBe(typeof(ValueTask<>));
    }

    /// <summary>
    /// 测试 - 类型 - 数组
    /// </summary>
    [Fact]
    public void Test_DirectType_Array()
    {
        TypeClass.ObjectArrayClazz.ShouldBe(typeof(object[]));
        TypeClass.ByteArrayClazz.ShouldBe(typeof(byte[]));
            
        TypeClass.GenericListClazz.ShouldBe(typeof(List<>));
    }

    /// <summary>
    /// 测试 - 可空类型
    /// </summary>
    [Fact]
    public void Test_NullableType()
    {
        TypeClass.ByteNullableClazz.ShouldBe(typeof(byte?));
        TypeClass.SByteNullableClazz.ShouldBe(typeof(sbyte?));
        TypeClass.Int16NullableClazz.ShouldBe(typeof(short?));
        TypeClass.UInt16NullableClazz.ShouldBe(typeof(ushort?));
        TypeClass.Int32NullableClazz.ShouldBe(typeof(int?));
        TypeClass.UInt32NullableClazz.ShouldBe(typeof(uint?));
        TypeClass.Int64NullableClazz.ShouldBe(typeof(long?));
        TypeClass.UInt64NullableClazz.ShouldBe(typeof(ulong?));
            
        TypeClass.ShortNullableClazz.ShouldBe(typeof(short?));
        TypeClass.UShortNullableClazz.ShouldBe(typeof(ushort?));
        TypeClass.IntNullableClazz.ShouldBe(typeof(int?));
        TypeClass.UIntNullableClazz.ShouldBe(typeof(uint?));
        TypeClass.LongNullableClazz.ShouldBe(typeof(long?));
        TypeClass.ULongNullableClazz.ShouldBe(typeof(ulong?));

        TypeClass.FloatNullableClazz.ShouldBe(typeof(float?));
        TypeClass.DoubleNullableClazz.ShouldBe(typeof(double?));
        TypeClass.DecimalNullableClazz.ShouldBe(typeof(decimal?));
            
        TypeClass.DateTimeNullableClazz.ShouldBe(typeof(DateTime?));
        TypeClass.DateTimeOffsetNullableClazz.ShouldBe(typeof(DateTimeOffset?));
        TypeClass.TimeSpanNullableClazz.ShouldBe(typeof(TimeSpan?));
            
        TypeClass.GuidNullableClazz.ShouldBe(typeof(Guid?));
        TypeClass.BooleanNullableClazz.ShouldBe(typeof(bool?));
        TypeClass.CharNullableClazz.ShouldBe(typeof(char?));
            
        TypeClass.NullableClazz.ShouldBe(typeof(Nullable));
        TypeClass.GenericNullableClazz.ShouldBe(typeof(Nullable<>));
    }

    /// <summary>
    /// 测试 - 接口
    /// </summary>
    [Fact]
    public void Test_Interface()
    {
        TypeClass.FormattableClazz.ShouldBe(typeof(IFormattable));
        TypeClass.FormatProviderClazz.ShouldBe(typeof(IFormatProvider));
    }
}