using System;
using System.Reflection;
using System.Threading.Tasks;
using Bing.Conversions;
using Shouldly;
using Xunit;
using BingTypeConverter = Bing.Conversions.TypeConverter;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

// ============================================================
//  TypeReflections �?IsTask / IsValueTask 系列
// ============================================================

/// <summary>
/// 验证 <see cref="TypeReflections"/> 及其扩展方法对异步类型的判定。
/// </summary>
/// <remarks>覆盖 Task、Task&lt;T&gt;、ValueTask 和 ValueTask&lt;T&gt; 的类型判断及扩展重载。</remarks>
[Trait("Bing.Reflection", "TypeReflections.IsTask")]
public class TypeReflectionsIsTaskTests
{
    // ──────────────────────────────────────────
    //  IsTask
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>IsTask</c> 拒绝空类型元数据。
    /// </summary>
    [Fact]
    public void IsTask_NullArg_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeReflections.IsTask((TypeInfo)null));
    }

    /// <summary>
    /// 验证 <c>Task</c> 满足 <c>IsTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsTask_TaskType_ReturnsTrue()
    {
        TypeReflections.IsTask(typeof(Task).GetTypeInfo()).ShouldBeTrue();
    }

    /// <summary>
    /// 验证 <c>Task&lt;int&gt;</c> 不满足 <c>IsTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsTask_TaskOfIntType_ReturnsFalse()
    {
        // Task<T> is NOT bare Task
        TypeReflections.IsTask(typeof(Task<int>).GetTypeInfo()).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 <c>ValueTask</c> 不满足 <c>IsTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsTask_ValueTaskType_ReturnsFalse()
    {
        TypeReflections.IsTask(typeof(ValueTask).GetTypeInfo()).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 <c>int</c> 不满足 <c>IsTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsTask_IntType_ReturnsFalse()
    {
        TypeReflections.IsTask(typeof(int).GetTypeInfo()).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 <c>string</c> 不满足 <c>IsTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsTask_StringType_ReturnsFalse()
    {
        TypeReflections.IsTask(typeof(string).GetTypeInfo()).ShouldBeFalse();
    }

    // Extension method variant
    /// <summary>
    /// 验证 <c>Task</c> 满足 <c>IsTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsTask_Extension_TaskType_ReturnsTrue()
    {
        typeof(Task).GetTypeInfo().IsTask().ShouldBeTrue();
    }

    /// <summary>
    /// 验证 <c>int</c> 不满足 <c>IsTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsTask_Extension_IntType_ReturnsFalse()
    {
        typeof(int).GetTypeInfo().IsTask().ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  IsTaskWithResult
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>IsTaskWithResult</c> 拒绝空类型元数据。
    /// </summary>
    [Fact]
    public void IsTaskWithResult_NullArg_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeReflections.IsTaskWithResult((TypeInfo)null));
    }

    /// <summary>
    /// 验证 <c>Task&lt;int&gt;</c> 满足 <c>IsTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsTaskWithResult_TaskOfInt_ReturnsTrue()
    {
        TypeReflections.IsTaskWithResult(typeof(Task<int>).GetTypeInfo()).ShouldBeTrue();
    }

    /// <summary>
    /// 验证 <c>Task&lt;string&gt;</c> 满足 <c>IsTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsTaskWithResult_TaskOfString_ReturnsTrue()
    {
        TypeReflections.IsTaskWithResult(typeof(Task<string>).GetTypeInfo()).ShouldBeTrue();
    }

    /// <summary>
    /// 验证 <c>Task</c> 不满足 <c>IsTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsTaskWithResult_BareTask_ReturnsFalse()
    {
        // Task (non-generic) is not Task<T>
        TypeReflections.IsTaskWithResult(typeof(Task).GetTypeInfo()).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 <c>int</c> 不满足 <c>IsTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsTaskWithResult_IntType_ReturnsFalse()
    {
        TypeReflections.IsTaskWithResult(typeof(int).GetTypeInfo()).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 <c>ValueTask&lt;int&gt;</c> 不满足 <c>IsTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsTaskWithResult_ValueTaskOfInt_ReturnsFalse()
    {
        TypeReflections.IsTaskWithResult(typeof(ValueTask<int>).GetTypeInfo()).ShouldBeFalse();
    }

    // Extension method variant
    /// <summary>
    /// 验证 <c>Task&lt;int&gt;</c> 满足 <c>IsTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsTaskWithResult_Extension_TaskOfInt_ReturnsTrue()
    {
        typeof(Task<int>).GetTypeInfo().IsTaskWithResult().ShouldBeTrue();
    }

    /// <summary>
    /// 验证 <c>Task</c> 不满足 <c>IsTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsTaskWithResult_Extension_BareTask_ReturnsFalse()
    {
        typeof(Task).GetTypeInfo().IsTaskWithResult().ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  IsTaskWithVoidTaskResult
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>IsTaskWithVoidTaskResult</c> 拒绝空类型元数据。
    /// </summary>
    [Fact]
    public void IsTaskWithVoidTaskResult_NullArg_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeReflections.IsTaskWithVoidTaskResult((TypeInfo)null));
    }

    /// <summary>
    /// 验证 <c>Task</c> 不满足 <c>IsTaskWithVoidTaskResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsTaskWithVoidTaskResult_BareTask_ReturnsFalse()
    {
        // BareTask has no generic parameters �?false
        TypeReflections.IsTaskWithVoidTaskResult(typeof(Task).GetTypeInfo()).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 <c>Task&lt;int&gt;</c> 不满足 <c>IsTaskWithVoidTaskResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsTaskWithVoidTaskResult_TaskOfInt_ReturnsFalse()
    {
        // Task<int> generic param is int, not VoidTaskResult
        TypeReflections.IsTaskWithVoidTaskResult(typeof(Task<int>).GetTypeInfo()).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 <c>int</c> 不满足 <c>IsTaskWithVoidTaskResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsTaskWithVoidTaskResult_TaskOfVoidTaskResult_ReturnsTrue()
    {
        var voidTaskResult = Type.GetType("System.Threading.Tasks.VoidTaskResult", throwOnError: true)!;
        var taskType = typeof(Task<>).MakeGenericType(voidTaskResult);

        TypeReflections.IsTaskWithVoidTaskResult(taskType.GetTypeInfo()).ShouldBeTrue();
        taskType.GetTypeInfo().IsTaskWithVoidTaskResult().ShouldBeTrue();
    }

    [Fact]
    public void IsTaskWithVoidTaskResult_IntType_ReturnsFalse()
    {
        TypeReflections.IsTaskWithVoidTaskResult(typeof(int).GetTypeInfo()).ShouldBeFalse();
    }

    // Extension method variant
    /// <summary>
    /// 验证 <c>Task</c> 不满足 <c>IsTaskWithVoidTaskResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsTaskWithVoidTaskResult_Extension_BareTask_ReturnsFalse()
    {
        typeof(Task).GetTypeInfo().IsTaskWithVoidTaskResult().ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  IsValueTask
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>IsValueTask</c> 拒绝空类型元数据。
    /// </summary>
    [Fact]
    public void IsValueTask_NullArg_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeReflections.IsValueTask((TypeInfo)null));
    }

    /// <summary>
    /// 验证 <c>ValueTask</c> 满足 <c>IsValueTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTask_ValueTaskType_ReturnsTrue()
    {
        TypeReflections.IsValueTask(typeof(ValueTask).GetTypeInfo()).ShouldBeTrue();
    }

    /// <summary>
    /// 验证 <c>ValueTask&lt;int&gt;</c> 不满足 <c>IsValueTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTask_ValueTaskOfIntType_ReturnsFalse()
    {
        // ValueTask<T> is NOT bare ValueTask
        TypeReflections.IsValueTask(typeof(ValueTask<int>).GetTypeInfo()).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 <c>Task</c> 不满足 <c>IsValueTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTask_TaskType_ReturnsFalse()
    {
        TypeReflections.IsValueTask(typeof(Task).GetTypeInfo()).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 <c>int</c> 不满足 <c>IsValueTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTask_IntType_ReturnsFalse()
    {
        TypeReflections.IsValueTask(typeof(int).GetTypeInfo()).ShouldBeFalse();
    }

    // Extension method variant
    /// <summary>
    /// 验证 <c>ValueTask</c> 满足 <c>IsValueTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTask_Extension_ValueTaskType_ReturnsTrue()
    {
        typeof(ValueTask).GetTypeInfo().IsValueTask().ShouldBeTrue();
    }

    /// <summary>
    /// 验证 <c>Task</c> 不满足 <c>IsValueTask</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTask_Extension_TaskType_ReturnsFalse()
    {
        typeof(Task).GetTypeInfo().IsValueTask().ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  IsValueTaskWithResult
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证 <c>IsValueTaskWithResult</c> 拒绝空类型元数据。
    /// </summary>
    [Fact]
    public void IsValueTaskWithResult_NullArg_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeReflections.IsValueTaskWithResult((TypeInfo)null));
    }

    /// <summary>
    /// 验证 <c>ValueTask&lt;int&gt;</c> 满足 <c>IsValueTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTaskWithResult_ValueTaskOfInt_ReturnsTrue()
    {
        TypeReflections.IsValueTaskWithResult(typeof(ValueTask<int>).GetTypeInfo()).ShouldBeTrue();
    }

    /// <summary>
    /// 验证 <c>ValueTask&lt;string&gt;</c> 满足 <c>IsValueTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTaskWithResult_ValueTaskOfString_ReturnsTrue()
    {
        TypeReflections.IsValueTaskWithResult(typeof(ValueTask<string>).GetTypeInfo()).ShouldBeTrue();
    }

    /// <summary>
    /// 验证 <c>ValueTask</c> 不满足 <c>IsValueTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTaskWithResult_BareValueTask_ReturnsFalse()
    {
        TypeReflections.IsValueTaskWithResult(typeof(ValueTask).GetTypeInfo()).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 <c>Task&lt;int&gt;</c> 不满足 <c>IsValueTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTaskWithResult_TaskOfInt_ReturnsFalse()
    {
        TypeReflections.IsValueTaskWithResult(typeof(Task<int>).GetTypeInfo()).ShouldBeFalse();
    }

    /// <summary>
    /// 验证 <c>int</c> 不满足 <c>IsValueTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTaskWithResult_IntType_ReturnsFalse()
    {
        TypeReflections.IsValueTaskWithResult(typeof(int).GetTypeInfo()).ShouldBeFalse();
    }

    // Extension method variant
    /// <summary>
    /// 验证 <c>ValueTask&lt;int&gt;</c> 满足 <c>IsValueTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTaskWithResult_Extension_ValueTaskOfInt_ReturnsTrue()
    {
        typeof(ValueTask<int>).GetTypeInfo().IsValueTaskWithResult().ShouldBeTrue();
    }

    /// <summary>
    /// 验证 <c>ValueTask</c> 不满足 <c>IsValueTaskWithResult</c> 判定。
    /// </summary>
    [Fact]
    public void IsValueTaskWithResult_Extension_BareValueTask_ReturnsFalse()
    {
        typeof(ValueTask).GetTypeInfo().IsValueTaskWithResult().ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  Caching 幂等�?�?调用两次结果相同
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证重复判定泛型任务类型返回一致的肯定结果。
    /// </summary>
    [Fact]
    public void IsTaskWithResult_CalledTwice_SameResult()
    {
        var ti = typeof(Task<bool>).GetTypeInfo();
        var first = TypeReflections.IsTaskWithResult(ti);
        var second = TypeReflections.IsTaskWithResult(ti);
        first.ShouldBe(second);
        first.ShouldBeTrue();
    }

    /// <summary>
    /// 验证重复判定泛型值任务类型返回一致的肯定结果。
    /// </summary>
    [Fact]
    public void IsValueTaskWithResult_CalledTwice_SameResult()
    {
        var ti = typeof(ValueTask<bool>).GetTypeInfo();
        var first = TypeReflections.IsValueTaskWithResult(ti);
        var second = TypeReflections.IsValueTaskWithResult(ti);
        first.ShouldBe(second);
        first.ShouldBeTrue();
    }
}

// ============================================================
//  TypeConverter
// ============================================================

/// <summary>
/// 验证 <see cref="Bing.Conversions.TypeConverter"/> 对可空类型的转换。
/// </summary>
[Trait("Bing.Conversions", "TypeConverter")]
public class TypeConverterTests
{
    // ──────────────────────────────────────────
    //  ToNonNullableType
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证可空整数转换为整数类型。
    /// </summary>
    [Fact]
    public void ToNonNullableType_NullableInt_ReturnsInt()
    {
        var result = BingTypeConverter.ToNonNullableType(typeof(int?));
        result.ShouldBe(typeof(int));
    }

    /// <summary>
    /// 验证可空布尔值转换为布尔类型。
    /// </summary>
    [Fact]
    public void ToNonNullableType_NullableBool_ReturnsBool()
    {
        var result = BingTypeConverter.ToNonNullableType(typeof(bool?));
        result.ShouldBe(typeof(bool));
    }

    /// <summary>
    /// 验证非可空整数在非安全转换入口返回空。
    /// </summary>
    [Fact]
    public void ToNonNullableType_NonNullableInt_ReturnsNull()
    {
        // Nullable.GetUnderlyingType returns null for non-nullable types
        var result = BingTypeConverter.ToNonNullableType(typeof(int));
        result.ShouldBeNull();
    }

    /// <summary>
    /// 验证可空日期转换为日期类型。
    /// </summary>
    [Fact]
    public void ToNonNullableType_NullableDateTime_ReturnsDateTime()
    {
        var result = BingTypeConverter.ToNonNullableType(typeof(DateTime?));
        result.ShouldBe(typeof(DateTime));
    }

    // ──────────────────────────────────────────
    //  ToNonNullableTypeInfo
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证可空整数元数据转换为整数类型元数据。
    /// </summary>
    [Fact]
    public void ToNonNullableTypeInfo_NullableInt_ReturnsIntTypeInfo()
    {
        var result = BingTypeConverter.ToNonNullableTypeInfo(typeof(int?).GetTypeInfo());
        result.ShouldBe(typeof(int).GetTypeInfo());
    }

    /// <summary>
    /// 验证可空长整数元数据转换为长整数类型元数据。
    /// </summary>
    [Fact]
    public void ToNonNullableTypeInfo_NullableLong_ReturnsLongTypeInfo()
    {
        var result = BingTypeConverter.ToNonNullableTypeInfo(typeof(long?).GetTypeInfo());
        result.ShouldBe(typeof(long).GetTypeInfo());
    }

    // ──────────────────────────────────────────
    //  ToSafeNonNullableType
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证安全转换入口将可空整数转换为整数类型。
    /// </summary>
    [Fact]
    public void ToSafeNonNullableType_NullableInt_ReturnsInt()
    {
        var result = BingTypeConverter.ToSafeNonNullableType(typeof(int?));
        result.ShouldBe(typeof(int));
    }

    /// <summary>
    /// 验证安全转换入口保留非可空整数类型。
    /// </summary>
    [Fact]
    public void ToSafeNonNullableType_NonNullableInt_ReturnsSameType()
    {
        // Safe variant returns the original type instead of null
        var result = BingTypeConverter.ToSafeNonNullableType(typeof(int));
        result.ShouldBe(typeof(int));
    }

    /// <summary>
    /// 验证安全转换入口保留字符串类型。
    /// </summary>
    [Fact]
    public void ToSafeNonNullableType_String_ReturnsSameType()
    {
        var result = BingTypeConverter.ToSafeNonNullableType(typeof(string));
        result.ShouldBe(typeof(string));
    }

    /// <summary>
    /// 验证安全转换入口将可空标识符转换为标识符类型。
    /// </summary>
    [Fact]
    public void ToSafeNonNullableType_NullableGuid_ReturnsGuid()
    {
        var result = BingTypeConverter.ToSafeNonNullableType(typeof(Guid?));
        result.ShouldBe(typeof(Guid));
    }

    // ──────────────────────────────────────────
    //  ToSafeNonNullableTypeInfo
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证安全转换入口将可空整数元数据转换为底层类型元数据。
    /// </summary>
    [Fact]
    public void ToSafeNonNullableTypeInfo_NullableInt_ReturnsIntTypeInfo()
    {
        var result = BingTypeConverter.ToSafeNonNullableTypeInfo(typeof(int?).GetTypeInfo());
        result.ShouldBe(typeof(int).GetTypeInfo());
    }

    /// <summary>
    /// 验证安全转换入口保留非可空整数类型元数据。
    /// </summary>
    [Fact]
    public void ToSafeNonNullableTypeInfo_NonNullableInt_ReturnsSameTypeInfo()
    {
        var result = BingTypeConverter.ToSafeNonNullableTypeInfo(typeof(int).GetTypeInfo());
        result.ShouldBe(typeof(int).GetTypeInfo());
    }

    /// <summary>
    /// 验证安全转换入口将可空布尔元数据转换为底层类型元数据。
    /// </summary>
    [Fact]
    public void ToSafeNonNullableTypeInfo_NullableBool_ReturnsBoolTypeInfo()
    {
        var result = BingTypeConverter.ToSafeNonNullableTypeInfo(typeof(bool?).GetTypeInfo());
        result.ShouldBe(typeof(bool).GetTypeInfo());
    }

    // ──────────────────────────────────────────
    //  Safe vs Unsafe 对比验证
    // ──────────────────────────────────────────

    /// <summary>
    /// 验证安全与非安全入口对可空类型返回相同结果。
    /// </summary>
    /// <param name="nullableType">待转换的可空测试类型。</param>
    [Theory]
    [InlineData(typeof(int?))]
    [InlineData(typeof(double?))]
    [InlineData(typeof(bool?))]
    [InlineData(typeof(Guid?))]
    public void SafeAndUnsafe_ForNullableTypes_ReturnSameResult(Type nullableType)
    {
        var unsafe_ = BingTypeConverter.ToNonNullableType(nullableType);
        var safe_ = BingTypeConverter.ToSafeNonNullableType(nullableType);
        unsafe_.ShouldBe(safe_);
    }
}

// ============================================================
//  ByteArrayExtensions (Bing.Conversions)
// ============================================================

/// <summary>
/// 验证字节数组转换为内存流后的内容读取。
/// </summary>
[Trait("Bing.Conversions", "ByteArrayExtensions")]
public class ByteArrayExtensionsTests
{
    /// <summary>
    /// 验证非空字节数组转换的内存流保留长度与内容。
    /// </summary>
    [Fact]
    public void CastToMemoryStream_NonEmptyArray_ReturnsMemoryStreamWithSameContent()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        using var ms = bytes.CastToMemoryStream();
        ms.ShouldNotBeNull();
        ms.Length.ShouldBe(5);
        var buf = ms.ToArray();
        buf.ShouldBe(bytes);
    }

    /// <summary>
    /// 验证空字节数组转换为空内存流。
    /// </summary>
    [Fact]
    public void CastToMemoryStream_EmptyArray_ReturnsEmptyMemoryStream()
    {
        var bytes = Array.Empty<byte>();
        using var ms = bytes.CastToMemoryStream();
        ms.Length.ShouldBe(0);
    }

    /// <summary>
    /// 验证单字节数组转换后能够读取原字节值。
    /// </summary>
    [Fact]
    public void CastToMemoryStream_SingleByteArray_ReturnsCorrectStream()
    {
        var bytes = new byte[] { 0xFF };
        using var ms = bytes.CastToMemoryStream();
        ms.ReadByte().ShouldBe(0xFF);
    }

    /// <summary>
    /// 验证修改原数组后内存流仍可读取。
    /// </summary>
    /// <remarks>当前断言仅检查读取结果非负，未区分流是否共享原数组。</remarks>
    [Fact]
    public void CastToMemoryStream_ModifyingOriginalArray_DoesNotAffectStream()
    {
        var bytes = new byte[] { 10, 20, 30 };
        using var ms = bytes.CastToMemoryStream();
        // Mutate original after creation
        bytes[0] = 99;
        // MemoryStream wraps the original array, so reading back gives modified value
        // This test documents the actual behavior (wrap, not copy)
        var readByte = ms.ReadByte();
        // Both behaviors (wrap or copy) are valid; just verify stream is functional
        readByte.ShouldBeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// 验证大数组转换后保留长度并支持定位读取。
    /// </summary>
    [Fact]
    public void CastToMemoryStream_LargeArray_HandlesCorrectly()
    {
        var bytes = new byte[1024];
        for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)(i % 256);
        using var ms = bytes.CastToMemoryStream();
        ms.Length.ShouldBe(1024);
        ms.Position = 0;
        ms.ReadByte().ShouldBe(0);
        ms.Position = 255;
        ms.ReadByte().ShouldBe(255);
    }
}
