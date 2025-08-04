using Shouldly;
using System.Collections.Concurrent;

namespace Bing.IdUtils;

/// <summary>
/// ObjectId 测试类
/// </summary>
[Trait("Bing.IdUtils", "ObjectId")]
public class ObjectIdTest
{
    #region 构造函数测试

    /// <summary>
    /// 测试 - 默认构造函数 - 创建空ObjectId
    /// </summary>
    [Fact]
    public void DefaultConstructor_CreatesEmptyObjectId()
    {
        // Act
        var objectId = new ObjectId();

        // Assert
        objectId.Timestamp.ShouldBe(0);
        objectId.Machine.ShouldBe(0);
        objectId.Pid.ShouldBe((short)0);
        objectId.Increment.ShouldBe(0);
        objectId.ShouldBe(ObjectId.Empty);
    }

    /// <summary>
    /// 测试 - 字节数组构造函数 - 正确解析字节数组
    /// </summary>
    [Fact]
    public void ByteArrayConstructor_ValidBytes_ParsesCorrectly()
    {
        // Arrange
        var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C };

        // Act
        var objectId = new ObjectId(bytes);

        // Assert
        objectId.Timestamp.ShouldBe(0x01020304);
        objectId.Machine.ShouldBe(0x050607);
        objectId.Pid.ShouldBe((short)0x0809);
        objectId.Increment.ShouldBe(0x0A0B0C);
    }

    /// <summary>
    /// 测试 - 字节数组构造函数 - null字节数组抛出异常
    /// </summary>
    [Fact]
    public void ByteArrayConstructor_NullBytes_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ObjectId((byte[])null))
            .ParamName.ShouldBe("bytes");
    }

    /// <summary>
    /// 测试 - 字节数组构造函数 - 无效长度抛出异常
    /// </summary>
    [Theory]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(0)]
    [InlineData(24)]
    public void ByteArrayConstructor_InvalidLength_ThrowsArgumentOutOfRangeException(int length)
    {
        // Arrange
        var bytes = new byte[length];

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new ObjectId(bytes))
            .ParamName.ShouldBe("bytes");
    }

    /// <summary>
    /// 测试 - DateTime构造函数 - 正确设置时间戳
    /// </summary>
    [Fact]
    public void DateTimeConstructor_ValidParameters_SetsCorrectTimestamp()
    {
        // Arrange
        var dateTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var machine = 0x123456;
        var pid = (short)0x789A;
        var increment = 0xBCDEF0;

        // Act
        var objectId = new ObjectId(dateTime, machine, pid, increment);

        // Assert
        objectId.Machine.ShouldBe(machine);
        objectId.Pid.ShouldBe(pid);
        objectId.Increment.ShouldBe(increment);
        objectId.CreationTime.ShouldBe(dateTime, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// 测试 - 数值构造函数 - 正确设置所有字段
    /// </summary>
    [Fact]
    public void NumericConstructor_ValidParameters_SetsAllFields()
    {
        // Arrange
        var timestamp = 0x12345678;
        var machine = 0x123456;
        var pid = (short)0x789A;
        var increment = 0xBCDEF0;

        // Act
        var objectId = new ObjectId(timestamp, machine, pid, increment);

        // Assert
        objectId.Timestamp.ShouldBe(timestamp);
        objectId.Machine.ShouldBe(machine);
        objectId.Pid.ShouldBe(pid);
        objectId.Increment.ShouldBe(increment);
    }

    /// <summary>
    /// 测试 - 数值构造函数 - 机器标识符超出范围抛出异常
    /// </summary>
    [Fact]
    public void NumericConstructor_MachineOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidMachine = 0x01000000; // 超出3字节范围

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new ObjectId(0, invalidMachine, 0, 0))
            .ParamName.ShouldBe("machine");
    }

    /// <summary>
    /// 测试 - 数值构造函数 - 递增计数器超出范围抛出异常
    /// </summary>
    [Fact]
    public void NumericConstructor_IncrementOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidIncrement = 0x01000000; // 超出3字节范围

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new ObjectId(0, 0, 0, invalidIncrement))
            .ParamName.ShouldBe("increment");
    }

    /// <summary>
    /// 测试 - 字符串构造函数 - 正确解析有效字符串
    /// </summary>
    [Fact]
    public void StringConstructor_ValidString_ParsesCorrectly()
    {
        // Arrange
        var validString = "507f1f77bcf86cd799439011";

        // Act
        var objectId = new ObjectId(validString);

        // Assert
        objectId.ToString().ShouldBe(validString);
    }

    /// <summary>
    /// 测试 - 字符串构造函数 - null字符串抛出异常
    /// </summary>
    [Fact]
    public void StringConstructor_NullString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ObjectId((string)null))
            .ParamName.ShouldBe("value");
    }

    /// <summary>
    /// 测试 - 字符串构造函数 - 无效长度抛出异常
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("507f1f77bcf86cd799439011abc")]
    public void StringConstructor_InvalidLength_ThrowsArgumentOutOfRangeException(string invalidString)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new ObjectId(invalidString))
            .ParamName.ShouldBe("value");
    }

    #endregion

    #region 静态方法测试

    /// <summary>
    /// 测试 - GenerateNewId - 生成唯一ObjectId
    /// </summary>
    [Fact]
    public void GenerateNewId_ReturnsUniqueObjectIds()
    {
        // Act
        var id1 = ObjectId.GenerateNewId();
        var id2 = ObjectId.GenerateNewId();

        // Assert
        id1.ShouldNotBe(id2);
        id1.ShouldNotBe(ObjectId.Empty);
        id2.ShouldNotBe(ObjectId.Empty);
    }

    /// <summary>
    /// 测试 - GenerateNewId - 大量生成验证唯一性
    /// </summary>
    [Fact]
    public void GenerateNewId_MassGeneration_AllUnique()
    {
        // Arrange
        const int count = 10000;
        var ids = new HashSet<ObjectId>();

        // Act
        for (int i = 0; i < count; i++)
        {
            ids.Add(ObjectId.GenerateNewId());
        }

        // Assert
        ids.Count.ShouldBe(count);
    }

    /// <summary>
    /// 测试 - GenerateNewId - 带DateTime参数生成ObjectId
    /// </summary>
    [Fact]
    public void GenerateNewId_WithDateTime_UsesSpecifiedTimestamp()
    {
        // Arrange
        var dateTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var objectId = ObjectId.GenerateNewId(dateTime);

        // Assert
        objectId.CreationTime.ShouldBe(dateTime, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// 测试 - GenerateNewStringId - 生成有效字符串
    /// </summary>
    [Fact]
    public void GenerateNewStringId_ReturnsValidString()
    {
        // Act
        var stringId = ObjectId.GenerateNewStringId();

        // Assert
        stringId.ShouldNotBeNull();
        stringId.Length.ShouldBe(24);
        stringId.ShouldMatch(@"^[0-9a-f]{24}$");
    }

    /// <summary>
    /// 测试 - Parse - 正确解析有效字符串
    /// </summary>
    [Fact]
    public void Parse_ValidString_ReturnsCorrectObjectId()
    {
        // Arrange
        var validString = "507f1f77bcf86cd799439011";

        // Act
        var objectId = ObjectId.Parse(validString);

        // Assert
        objectId.ToString().ShouldBe(validString);
    }

    /// <summary>
    /// 测试 - Parse - null字符串抛出异常
    /// </summary>
    [Fact]
    public void Parse_NullString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ObjectId.Parse(null))
            .ParamName.ShouldBe("s");
    }

    /// <summary>
    /// 测试 - Parse - 无效长度抛出异常
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("507f1f77bcf86cd799439011abc")]
    public void Parse_InvalidLength_ThrowsArgumentOutOfRangeException(string invalidString)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => ObjectId.Parse(invalidString))
            .ParamName.ShouldBe("s");
    }

    /// <summary>
    /// 测试 - TryParse - 有效字符串解析成功
    /// </summary>
    [Fact]
    public void TryParse_ValidString_ReturnsTrue()
    {
        // Arrange
        var validString = "507f1f77bcf86cd799439011";

        // Act
        var result = ObjectId.TryParse(validString, out var objectId);

        // Assert
        result.ShouldBeTrue();
        objectId.ToString().ShouldBe(validString);
    }

    /// <summary>
    /// 测试 - TryParse - 无效字符串解析失败
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("507f1f77bcf86cd799439011xyz")]
    public void TryParse_InvalidString_ReturnsFalse(string invalidString)
    {
        // Act
        var result = ObjectId.TryParse(invalidString, out var objectId);

        // Assert
        result.ShouldBeFalse();
        objectId.ShouldBe(ObjectId.Empty);
    }

    /// <summary>
    /// 测试 - Pack - 正确打包组件
    /// </summary>
    [Fact]
    public void Pack_ValidComponents_ReturnsCorrectBytes()
    {
        // Arrange
        var timestamp = 0x12345678;
        var machine = 0x123456;
        var pid = (short)0x789A;
        var increment = 0xBCDEF0;

        // Act
        var bytes = ObjectId.Pack(timestamp, machine, pid, increment);

        // Assert
        bytes.Length.ShouldBe(12);
        bytes[0].ShouldBe((byte)0x12);
        bytes[1].ShouldBe((byte)0x34);
        bytes[2].ShouldBe((byte)0x56);
        bytes[3].ShouldBe((byte)0x78);
        bytes[4].ShouldBe((byte)0x12);
        bytes[5].ShouldBe((byte)0x34);
        bytes[6].ShouldBe((byte)0x56);
        bytes[7].ShouldBe((byte)0x78);
        bytes[8].ShouldBe((byte)0x9A);
        bytes[9].ShouldBe((byte)0xBC);
        bytes[10].ShouldBe((byte)0xDE);
        bytes[11].ShouldBe((byte)0xF0);
    }

    /// <summary>
    /// 测试 - Unpack - 正确解包字节数组
    /// </summary>
    [Fact]
    public void Unpack_ValidBytes_ReturnsCorrectComponents()
    {
        // Arrange
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };

        // Act
        ObjectId.Unpack(bytes, out var timestamp, out var machine, out var pid, out var increment);

        // Assert
        timestamp.ShouldBe(0x12345678);
        machine.ShouldBe(0x123456);
        pid.ShouldBe((short)0x789A);
        increment.ShouldBe(0xBCDEF0);
    }

    #endregion

    #region 实例方法测试

    /// <summary>
    /// 测试 - CompareTo - 正确比较ObjectId
    /// </summary>
    [Fact]
    public void CompareTo_DifferentObjectIds_ReturnsCorrectComparison()
    {
        // Arrange
        var objectId1 = new ObjectId(1, 1, 1, 1);
        var objectId2 = new ObjectId(2, 1, 1, 1);
        var objectId3 = new ObjectId(1, 1, 1, 1);

        // Act & Assert
        objectId1.CompareTo(objectId2).ShouldBeLessThan(0);
        objectId2.CompareTo(objectId1).ShouldBeGreaterThan(0);
        objectId1.CompareTo(objectId3).ShouldBe(0);
    }

    /// <summary>
    /// 测试 - Equals - 相同ObjectId返回true
    /// </summary>
    [Fact]
    public void Equals_SameObjectIds_ReturnsTrue()
    {
        // Arrange
        var objectId1 = new ObjectId(1, 2, 3, 4);
        var objectId2 = new ObjectId(1, 2, 3, 4);

        // Act & Assert
        objectId1.Equals(objectId2).ShouldBeTrue();
        objectId1.Equals((object)objectId2).ShouldBeTrue();
        (objectId1 == objectId2).ShouldBeTrue();
        (objectId1 != objectId2).ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - Equals - 不同ObjectId返回false
    /// </summary>
    [Fact]
    public void Equals_DifferentObjectIds_ReturnsFalse()
    {
        // Arrange
        var objectId1 = new ObjectId(1, 2, 3, 4);
        var objectId2 = new ObjectId(1, 2, 3, 5);

        // Act & Assert
        objectId1.Equals(objectId2).ShouldBeFalse();
        objectId1.Equals((object)objectId2).ShouldBeFalse();
        (objectId1 == objectId2).ShouldBeFalse();
        (objectId1 != objectId2).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - Equals - 与非ObjectId对象比较返回false
    /// </summary>
    [Fact]
    public void Equals_WithNonObjectId_ReturnsFalse()
    {
        // Arrange
        var objectId = new ObjectId(1, 2, 3, 4);

        // Act & Assert
        objectId.Equals("string").ShouldBeFalse();
        objectId.Equals(null).ShouldBeFalse();
        objectId.Equals(123).ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - GetHashCode - 相同ObjectId产生相同哈希码
    /// </summary>
    [Fact]
    public void GetHashCode_SameObjectIds_ReturnsSameHashCode()
    {
        // Arrange
        var objectId1 = new ObjectId(1, 2, 3, 4);
        var objectId2 = new ObjectId(1, 2, 3, 4);

        // Act & Assert
        objectId1.GetHashCode().ShouldBe(objectId2.GetHashCode());
    }

    /// <summary>
    /// 测试 - GetHashCode - 不同ObjectId产生不同哈希码
    /// </summary>
    [Fact]
    public void GetHashCode_DifferentObjectIds_ReturnsDifferentHashCodes()
    {
        // Arrange
        var objectId1 = new ObjectId(1, 2, 3, 4);
        var objectId2 = new ObjectId(1, 2, 3, 5);

        // Act & Assert
        objectId1.GetHashCode().ShouldNotBe(objectId2.GetHashCode());
    }

    /// <summary>
    /// 测试 - ToByteArray - 返回正确的字节数组
    /// </summary>
    [Fact]
    public void ToByteArray_ReturnsCorrectBytes()
    {
        // Arrange
        var objectId = new ObjectId(0x12345678, 0x123456, 0x789A, 0xBCDEF0);

        // Act
        var bytes = objectId.ToByteArray();

        // Assert
        bytes.Length.ShouldBe(12);
        bytes.ShouldBe(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 });
    }

    /// <summary>
    /// 测试 - ToString - 返回正确的字符串表示
    /// </summary>
    [Fact]
    public void ToString_ReturnsCorrectHexString()
    {
        // Arrange
        var objectId = new ObjectId("507f1f77bcf86cd799439011");

        // Act
        var result = objectId.ToString();

        // Assert
        result.ShouldBe("507f1f77bcf86cd799439011");
    }

    #endregion

    #region 运算符测试

    /// <summary>
    /// 测试 - 比较运算符 - 正确比较ObjectId大小
    /// </summary>
    [Fact]
    public void ComparisonOperators_WorkCorrectly()
    {
        // Arrange
        var objectId1 = new ObjectId(1, 1, 1, 1);
        var objectId2 = new ObjectId(2, 1, 1, 1);
        var objectId3 = new ObjectId(1, 1, 1, 1);

        // Act & Assert
        (objectId1 < objectId2).ShouldBeTrue();
        (objectId1 <= objectId2).ShouldBeTrue();
        (objectId1 <= objectId3).ShouldBeTrue();
        (objectId2 > objectId1).ShouldBeTrue();
        (objectId2 >= objectId1).ShouldBeTrue();
        (objectId1 >= objectId3).ShouldBeTrue();
    }

    #endregion

    #region 属性测试

    /// <summary>
    /// 测试 - CreationTime - 正确计算创建时间
    /// </summary>
    [Fact]
    public void CreationTime_ReturnsCorrectDateTime()
    {
        // Arrange
        var expectedTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var objectId = ObjectId.GenerateNewId(expectedTime);

        // Act
        var creationTime = objectId.CreationTime;

        // Assert
        creationTime.ShouldBe(expectedTime, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// 测试 - Empty - 返回空ObjectId
    /// </summary>
    [Fact]
    public void Empty_ReturnsEmptyObjectId()
    {
        // Act
        var empty = ObjectId.Empty;

        // Assert
        empty.Timestamp.ShouldBe(0);
        empty.Machine.ShouldBe(0);
        empty.Pid.ShouldBe((short)0);
        empty.Increment.ShouldBe(0);
    }

    #endregion

    #region 工具方法测试

    /// <summary>
    /// 测试 - ParseHexString - 正确解析16进制字符串
    /// </summary>
    [Fact]
    public void ParseHexString_ValidHexString_ReturnsCorrectBytes()
    {
        // Arrange
        var hexString = "1234567890abcdef";

        // Act
        var bytes = ObjectId.ParseHexString(hexString);

        // Assert
        bytes.ShouldBe(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xab, 0xcd, 0xef });
    }

    /// <summary>
    /// 测试 - ParseHexString - null字符串抛出异常
    /// </summary>
    [Fact]
    public void ParseHexString_NullString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ObjectId.ParseHexString(null))
            .ParamName.ShouldBe("s");
    }

    /// <summary>
    /// 测试 - ParseHexString - 奇数长度字符串抛出异常
    /// </summary>
    [Fact]
    public void ParseHexString_OddLengthString_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => ObjectId.ParseHexString("123"))
            .ParamName.ShouldBe("s");
    }

    /// <summary>
    /// 测试 - ToHexString - 正确转换字节数组
    /// </summary>
    [Fact]
    public void ToHexString_ValidBytes_ReturnsCorrectHexString()
    {
        // Arrange
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xab, 0xcd, 0xef };

        // Act
        var hexString = ObjectId.ToHexString(bytes);

        // Assert
        hexString.ShouldBe("1234567890abcdef");
    }

    /// <summary>
    /// 测试 - ToHexString - null字节数组抛出异常
    /// </summary>
    [Fact]
    public void ToHexString_NullBytes_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ObjectId.ToHexString(null))
            .ParamName.ShouldBe("bytes");
    }

    /// <summary>
    /// 测试 - ToMillisecondsSinceEpoch - 正确转换时间
    /// </summary>
    [Fact]
    public void ToMillisecondsSinceEpoch_ValidDateTime_ReturnsCorrectMilliseconds()
    {
        // Arrange
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 1, DateTimeKind.Utc);

        // Act
        var milliseconds = ObjectId.ToMillisecondsSinceEpoch(dateTime);

        // Assert
        milliseconds.ShouldBe(1000);
    }

    /// <summary>
    /// 测试 - ToUniversalTime - 正确处理特殊值
    /// </summary>
    [Fact]
    public void ToUniversalTime_SpecialValues_HandlesCorrectly()
    {
        // Act & Assert
        ObjectId.ToUniversalTime(DateTime.MinValue).Kind.ShouldBe(DateTimeKind.Utc);
        ObjectId.ToUniversalTime(DateTime.MaxValue).Kind.ShouldBe(DateTimeKind.Utc);
        ObjectId.ToUniversalTime(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Local)).Kind.ShouldBe(DateTimeKind.Utc);
    }

    #endregion

    #region 并发测试

    /// <summary>
    /// 测试 - 并发生成ObjectId - 确保唯一性
    /// </summary>
    [Fact]
    public void ConcurrentGeneration_ProducesUniqueObjectIds()
    {
        // Arrange
        const int threadCount = 10;
        const int idsPerThread = 1000;
        var allIds = new ConcurrentBag<ObjectId>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < idsPerThread; j++)
                {
                    allIds.Add(ObjectId.GenerateNewId());
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var uniqueIds = new HashSet<ObjectId>(allIds);
        allIds.Count.ShouldBe(threadCount * idsPerThread);
        uniqueIds.Count.ShouldBe(threadCount * idsPerThread);
    }

    #endregion

    #region 性能测试

    /// <summary>
    /// 测试 - 生成性能 - 快速生成大量ObjectId
    /// </summary>
    [Fact]
    public void GenerationPerformance_GeneratesQuickly()
    {
        // Arrange
        const int count = 100000;

        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < count; i++)
            {
                ObjectId.GenerateNewId();
            }
        }, TimeSpan.FromSeconds(5), $"生成{count}个ObjectId应该在5秒内完成");
    }

    #endregion

    #region 集成测试

    /// <summary>
    /// 测试 - 完整往返 - 字符串到ObjectId再到字符串
    /// </summary>
    [Fact]
    public void RoundTrip_StringToObjectIdToString_Preserves()
    {
        // Arrange
        var originalString = "507f1f77bcf86cd799439011";

        // Act
        var objectId = ObjectId.Parse(originalString);
        var resultString = objectId.ToString();

        // Assert
        resultString.ShouldBe(originalString);
    }

    /// <summary>
    /// 测试 - 完整往返 - 字节数组到ObjectId再到字节数组
    /// </summary>
    [Fact]
    public void RoundTrip_BytesToObjectIdToBytes_Preserves()
    {
        // Arrange
        var originalBytes = new byte[] { 0x50, 0x7f, 0x1f, 0x77, 0xbc, 0xf8, 0x6c, 0xd7, 0x99, 0x43, 0x90, 0x11 };

        // Act
        var objectId = new ObjectId(originalBytes);
        var resultBytes = objectId.ToByteArray();

        // Assert
        resultBytes.ShouldBe(originalBytes);
    }

    /// <summary>
    /// 测试 - 时序性 - 后生成的ObjectId应该更大
    /// </summary>
    [Fact]
    public void SequentialGeneration_LaterObjectIdsAreGreater()
    {
        // Arrange
        var ids = new List<ObjectId>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            ids.Add(ObjectId.GenerateNewId());
            System.Threading.Thread.Sleep(1); // 确保时间差异
        }

        // Assert
        for (int i = 1; i < ids.Count; i++)
        {
            ids[i].ShouldBeGreaterThanOrEqualTo(ids[i - 1]);
        }
    }

    #endregion
}