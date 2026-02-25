using System.Reflection;

namespace Bing.IdUtils;

/// <summary>
/// 测试类：覆盖 RandomIdGenerator、RandomNonceStrGenerator、TimestampId 与 GuidExtensions 的核心契约。
/// </summary>
[Trait("IdUtilsUT", "RandomAndTimestamp")]
public class RandomAndTimestampTest
{
    /// <summary>
    /// 测试用例：Create 在指定长度与字典时，返回长度正确且字符全部来自字典。
    /// </summary>
    [Theory]
    [InlineData(1, RandomIdGenerator.AllNumbers)]
    [InlineData(8, RandomIdGenerator.SimpleWords)]
    [InlineData(16, RandomIdGenerator.NanoWords)]
    public void Create_LengthAndDict_ReturnsExpectedLengthAndCharacters(int length, string dict)
    {
        var id = RandomIdGenerator.Create(length, dict);
        id.Length.ShouldBe(length);
        id.All(dict.Contains).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：Create 在格式化模板输入时，返回符合模板的结果。
    /// </summary>
    [Fact]
    public void Create_FormatInput_ReturnsFormattedResult()
    {
        var id = RandomIdGenerator.Create("ID-{0}{0}-END", RandomIdGenerator.AllNumbers);
        id.Length.ShouldBe(9);
        id.ShouldMatch(@"^ID-\d\d-END$");
    }

    /// <summary>
    /// 测试用例：Create 在空字典输入时，抛出 IndexOutOfRangeException。
    /// </summary>
    [Fact]
    public void Create_EmptyDictionary_ThrowsIndexOutOfRangeException()
    {
        Should.Throw<IndexOutOfRangeException>(() => RandomIdGenerator.Create(4, string.Empty));
    }

    /// <summary>
    /// 测试用例：CreateNonce 在长度小于 16 时，自动提升到最小长度 16。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    public void CreateNonce_LengthLessThan16_ReturnsMinimumLength16(int inputLength)
    {
        var nonce = RandomNonceStrGenerator.Create(inputLength);
        nonce.Length.ShouldBe(16);
        nonce.ShouldMatch("^[a-zA-Z0-9]+$");
    }

    /// <summary>
    /// 测试用例：CreateNonce 在强制避免重复场景下，返回字母数字串。
    /// </summary>
    [Fact]
    public void CreateNonce_ForceAvoidRepetitionTrue_ReturnsAlphaNumeric()
    {
        var nonce = RandomNonceStrGenerator.Create(20, true);
        nonce.Length.ShouldBe(20);
        nonce.ShouldMatch("^[a-zA-Z0-9]+$");
    }

    /// <summary>
    /// 测试用例：GuidExtensions.IsNullOrEmpty 对 null、Guid.Empty 与正常 Guid 返回预期结果。
    /// </summary>
    [Fact]
    public void IsNullOrEmpty_GuidExtensions_ReturnsExpectedResult()
    {
        Guid? nullableGuid = null;
        nullableGuid.IsNullOrEmpty().ShouldBeTrue();
        Guid.Empty.IsNullOrEmpty().ShouldBeTrue();
        Guid.NewGuid().IsNullOrEmpty().ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：TimestampId.GetInstance 多次调用返回同一单例实例。
    /// </summary>
    [Fact]
    public void GetInstance_MultipleCalls_ReturnsSameSingleton()
    {
        var first = TimestampId.GetInstance();
        var second = TimestampId.GetInstance(DateTime.UtcNow.AddDays(-1));
        first.ShouldBeSameAs(second);
    }

    /// <summary>
    /// 测试用例：GetId 在有效初始时间下，返回纯数字字符串。
    /// </summary>
    [Fact]
    public void GetId_ValidInitialDate_ReturnsNumericString()
    {
        var instance = CreateTimestampIdByReflection(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var id = instance.GetId();

        id.ShouldNotBeNullOrWhiteSpace();
        id.ShouldMatch(@"^\d+$");
    }

    /// <summary>
    /// 测试用例：GetId 在初始时间大于当前时间时，抛出异常并包含业务错误信息。
    /// </summary>
    [Fact]
    public void GetId_FutureInitialDate_ThrowsException()
    {
        var future = DateTime.Now.AddMinutes(5);
        var instance = CreateTimestampIdByReflection(future);
        var ex = Should.Throw<Exception>(() => instance.GetId());

        ex.Message.ShouldContain("初始化时间比当前时间还大");
    }

    /// <summary>
    /// 测试辅助：通过反射调用 TimestampId 非公开构造函数，创建可控初始时间的实例。
    /// </summary>
    private static TimestampId CreateTimestampIdByReflection(DateTime initialDate)
    {
        var ctor = typeof(TimestampId).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(DateTime?) },
            modifiers: null);

        ctor.ShouldNotBeNull();
        return (TimestampId)ctor.Invoke(new object[] { (DateTime?)initialDate });
    }
}
