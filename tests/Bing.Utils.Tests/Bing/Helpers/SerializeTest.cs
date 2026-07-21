using System.Runtime.InteropServices;
namespace Bing.Helpers;
/// <summary>
/// 序列化操作 测试类
/// </summary>
[Trait("Bing.Helpers", "Serialize")]
public class SerializeTest : IDisposable
{
    /// <summary>
    /// 测试目录
    /// </summary>
    private readonly string _testDirectory;
    /// <summary>
    /// 测试初始化
    /// </summary>
    public SerializeTest()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"SerializeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }
    #region 结构体序列化测试
    [StructLayout(LayoutKind.Sequential)]
    public struct TestStruct
    {
        public int IntValue;
        public double DoubleValue;
        public bool BoolValue;
    }
    /// <summary>
    /// 测试目的：不包含托管引用的结构体应能够按当前内存布局往返。
    /// </summary>
    [Fact]
    public void StructToBytes_AndBytesToStruct_StructRoundTrip_Success()
    {
        // Arrange
        var original = new TestStruct
        {
            IntValue = 42,
            DoubleValue = 3.14159,
            BoolValue = true
        };
        // Act
        var bytes = Serialize.StructToBytes(original);
        var restored = Serialize.BytesToStruct<TestStruct>(bytes);
        // Assert
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
        restored.IntValue.ShouldBe(original.IntValue);
        restored.DoubleValue.ShouldBe(original.DoubleValue);
        restored.BoolValue.ShouldBe(original.BoolValue);
    }
    /// <summary>
    /// 测试目的：长度不匹配的字节数组应被拒绝。
    /// </summary>
    [Fact]
    public void BytesToStruct_InvalidByteArrayLength_ThrowsArgumentException()
    {
        // Arrange
        var invalidBytes = new byte[5]; // TestStruct需要更多字节
        // Act & Assert
        Should.Throw<ArgumentException>(() => Serialize.BytesToStruct<TestStruct>(invalidBytes))
            .Message.ShouldContain("字节数组长度");
    }
    /// <summary>
    /// 测试目的：null 字节数组应被明确拒绝。
    /// </summary>
    [Fact]
    public void BytesToStruct_NullByteArray_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Serialize.BytesToStruct<TestStruct>(null))
            .ParamName.ShouldBe("bytes");
    }
    /// <summary>
    /// 测试目的：默认值和全零结构体是合法的内存布局输入。
    /// </summary>
    [Fact]
    public void StructToBytes_DefaultValues_RoundTripSucceeds()
    {
        // Arrange
        var zero = 0;
        var boolean = false;
        var guid = default(Guid);
        var dateTime = default(DateTime);
        var structValue = default(TestStruct);

        // Act and Assert
        Serialize.BytesToStruct<int>(Serialize.StructToBytes(zero)).ShouldBe(0);
        Serialize.BytesToStruct<bool>(Serialize.StructToBytes(boolean)).ShouldBeFalse();
        Serialize.BytesToStruct<Guid>(Serialize.StructToBytes(guid)).ShouldBe(guid);
        Serialize.BytesToStruct<DateTime>(Serialize.StructToBytes(dateTime)).ShouldBe(dateTime);
        var result = Serialize.BytesToStruct<TestStruct>(Serialize.StructToBytes(structValue));
        result.IntValue.ShouldBe(0);
        result.DoubleValue.ShouldBe(0);
        result.BoolValue.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：空字节数组和包含托管引用的结构体应被明确拒绝。
    /// </summary>
    [Fact]
    public void BytesToStruct_EmptyBytesOrManagedReferenceStruct_ThrowsArgumentException()
    {
        // Act and Assert
        Should.Throw<ArgumentException>(() => Serialize.BytesToStruct<int>(Array.Empty<byte>())).ParamName.ShouldBe("bytes");
        Should.Throw<ArgumentException>(() => Serialize.StructToBytes(new ManagedReferenceStruct { Name = "invalid" }));
    }

    /// <summary>
    /// 测试目的：过时的结构体 API 应转发到名称明确的新 API。
    /// </summary>
    [Fact]
    public void ToBytes_AndFromBytes_ObsoleteApis_ForwardToStructApis()
    {
        // Arrange
        var value = new TestStruct { IntValue = 42, DoubleValue = 1.5, BoolValue = true };

        // Act
#pragma warning disable CS0618
        var bytes = Serialize.ToBytes(value);
        var result = Serialize.FromBytes<TestStruct>(bytes);
#pragma warning restore CS0618

        // Assert
        result.IntValue.ShouldBe(42);
        typeof(Serialize).GetMethod(nameof(Serialize.ToBytes)).GetCustomAttributes(typeof(ObsoleteAttribute), false).Length.ShouldBe(1);
        typeof(Serialize).GetMethod(nameof(Serialize.FromBytes)).GetCustomAttributes(typeof(ObsoleteAttribute), false).Length.ShouldBe(1);
    }

    /// <summary>
    /// 包含托管引用字段的无效结构体。
    /// </summary>
    public struct ManagedReferenceStruct
    {
        /// <summary>
        /// 托管字符串字段。
        /// </summary>
        public string Name;
    }
    #endregion
    #region 二进制序列化测试
#pragma warning disable CS0618
    [Serializable]
    public class TestSerializableClass
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public DateTime Date { get; set; }
    }
    /// <summary>
    /// 仅在已启用 BinaryFormatter 兼容开关的 net8.0 测试宿主中执行历史兼容测试。
    /// </summary>
#if NET8_0
    private const string LegacyBinarySkipReason = null;
#else
    private const string LegacyBinarySkipReason = "当前目标框架未启用 BinaryFormatter 历史兼容运行时开关。";
#endif

    /// <summary>
    /// 测试目的：可信历史对象应能够通过显式 Legacy API 往返。该测试不代表该格式安全。
    /// </summary>
    [Fact(Skip = LegacyBinarySkipReason)]
    public void ToLegacyBinary_AndFromLegacyBinary_TrustedObjectRoundTrip_Success()
    {
        // Arrange
        var original = new TestSerializableClass
        {
            Name = "Binary Test",
            Value = 123,
            Date = new DateTime(2023, 6, 15)
        };
        // Act
        var bytes = Serialize.ToLegacyBinary(original);
        var restored = Serialize.FromLegacyBinary<TestSerializableClass>(bytes);
        // Assert
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
        restored.ShouldNotBeNull();
        restored.Name.ShouldBe(original.Name);
        restored.Value.ShouldBe(original.Value);
        restored.Date.ShouldBe(original.Date);
    }
    /// <summary>
    /// 测试目的：Legacy BinaryFormatter 的空对象和空字节输入应被明确拒绝。该测试不代表该格式安全。
    /// </summary>
    [Fact(Skip = LegacyBinarySkipReason)]
    public void LegacyBinary_NullInputs_ThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Serialize.ToLegacyBinary(null))
            .ParamName.ShouldBe("data");
        Should.Throw<ArgumentNullException>(() => Serialize.FromLegacyBinary<object>(null))
            .ParamName.ShouldBe("bytes");
        Should.Throw<ArgumentException>(() => Serialize.FromLegacyBinary<object>(Array.Empty<byte>()))
            .ParamName.ShouldBe("bytes");
    }

    /// <summary>
    /// 测试目的：Legacy BinaryFormatter 的错误目标类型应抛出类型转换异常。该测试不代表该格式安全。
    /// </summary>
    [Fact(Skip = LegacyBinarySkipReason)]
    public void FromLegacyBinary_WrongTargetType_ThrowsInvalidCastException()
    {
        // Arrange
        var bytes = Serialize.ToLegacyBinary(new TestSerializableClass());

        // Act and Assert
        Should.Throw<InvalidCastException>(() => Serialize.FromLegacyBinary<int>(bytes));
    }
    /// <summary>
    /// 测试目的：可信历史文件应通过显式 Legacy API 往返。该测试不代表该格式安全。
    /// </summary>
    [Fact(Skip = LegacyBinarySkipReason)]
    public void ToLegacyBinaryFile_AndFromLegacyBinaryFile_TrustedObjectRoundTrip_Success()
    {
        // Arrange
        var original = new TestSerializableClass
        {
            Name = "Binary File Test",
            Value = 456,
            Date = DateTime.Today
        };
        var fileName = Path.Combine(_testDirectory, "test.bin");
        // Act
        Serialize.ToLegacyBinaryFile(fileName, original);
        var restored = Serialize.FromLegacyBinaryFile<TestSerializableClass>(fileName);
        // Assert
        File.Exists(fileName).ShouldBeTrue();
        restored.ShouldNotBeNull();
        restored.Name.ShouldBe(original.Name);
        restored.Value.ShouldBe(original.Value);
        restored.Date.ShouldBe(original.Date);
    }
    /// <summary>
    /// 测试目的：不存在的 Legacy 文件应抛出文件不存在异常。该测试不代表该格式安全。
    /// </summary>
    [Fact(Skip = LegacyBinarySkipReason)]
    public void FromLegacyBinaryFile_FileNotExists_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.bin");
        // Act & Assert
        Should.Throw<FileNotFoundException>(() => Serialize.FromLegacyBinaryFile<object>(nonExistentFile));
    }
    /// <summary>
    /// 测试目的：Legacy 文件 API 的无效路径应被明确拒绝。该测试不代表该格式安全。
    /// </summary>
    [Theory(Skip = LegacyBinarySkipReason)]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToLegacyBinaryFile_InvalidFileName_ThrowsArgumentException(string fileName)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Serialize.ToLegacyBinaryFile(fileName, new TestSerializableClass()))
            .ParamName.ShouldBe("fileName");
    }
    /// <summary>
    /// 测试目的：Legacy 文件 API 的空对象应被明确拒绝。该测试不代表该格式安全。
    /// </summary>
    [Fact(Skip = LegacyBinarySkipReason)]
    public void ToLegacyBinaryFile_NullData_ThrowsArgumentNullException()
    {
        var fileName = Path.Combine(_testDirectory, "test.bin");
        Should.Throw<ArgumentNullException>(() => Serialize.ToLegacyBinaryFile(fileName, null))
            .ParamName.ShouldBe("data");
    }
    /// <summary>
    /// 测试目的：Legacy 文件读取的无效路径应被明确拒绝。该测试不代表该格式安全。
    /// </summary>
    [Theory(Skip = LegacyBinarySkipReason)]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromLegacyBinaryFile_InvalidFileName_ThrowsArgumentException(string fileName)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Serialize.FromLegacyBinaryFile<object>(fileName))
            .ParamName.ShouldBe("fileName");
    }

    /// <summary>
    /// 测试目的：历史 API 应保留过时标记并转发到显式 Legacy API。该测试不代表该格式安全。
    /// </summary>
    [Fact(Skip = LegacyBinarySkipReason)]
    public void BinaryFormatter_ObsoleteApis_ForwardToLegacyApis()
    {
        // Arrange
        var value = new TestSerializableClass { Name = "legacy", Value = 1, Date = DateTime.UtcNow };

        // Act
        var bytes = Serialize.ToBinary(value);
        var result = Serialize.FromBinary<TestSerializableClass>(bytes);

        // Assert
        result.Name.ShouldBe("legacy");
        typeof(Serialize).GetMethod(nameof(Serialize.ToBinary)).GetCustomAttributes(typeof(ObsoleteAttribute), false).Length.ShouldBe(1);
        typeof(Serialize).GetMethod(nameof(Serialize.FromBinary)).GetCustomAttributes(typeof(ObsoleteAttribute), false).Length.ShouldBe(1);
        typeof(Serialize).GetMethod(nameof(Serialize.ToBinaryFile)).GetCustomAttributes(typeof(ObsoleteAttribute), false).Length.ShouldBe(1);
        typeof(Serialize).GetMethod(nameof(Serialize.FromBinaryFile)).GetCustomAttributes(typeof(ObsoleteAttribute), false).Length.ShouldBe(1);
        typeof(Serialize).GetMethod(nameof(Serialize.ToLegacyBinary)).GetCustomAttributes(typeof(ObsoleteAttribute), false).Length.ShouldBe(1);
        typeof(Serialize).GetMethod(nameof(Serialize.FromLegacyBinary)).GetCustomAttributes(typeof(ObsoleteAttribute), false).Length.ShouldBe(1);
        typeof(Serialize).GetMethod(nameof(Serialize.ToLegacyBinaryFile)).GetCustomAttributes(typeof(ObsoleteAttribute), false).Length.ShouldBe(1);
        typeof(Serialize).GetMethod(nameof(Serialize.FromLegacyBinaryFile)).GetCustomAttributes(typeof(ObsoleteAttribute), false).Length.ShouldBe(1);
    }
#pragma warning restore CS0618
    #endregion
    #region XML序列化测试
    [Serializable]
    public class XmlTestClass
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public DateTime Date { get; set; }
    }
    /// <summary>
    /// 测试 - ToXml/FromXml - XML序列化往返
    /// </summary>
    [Fact]
    public void ToXml_FromXml_ObjectRoundTrip_Success()
    {
        // Arrange
        var original = new XmlTestClass
        {
            Name = "XML Test",
            Value = 100,
            Date = new DateTime(2023, 6, 15)
        };
        // Act
        var xml = Serialize.ToXml(original);
        var restored = Serialize.FromXml<XmlTestClass>(xml);
        // Assert
        xml.ShouldNotBeNullOrEmpty();
        xml.ShouldContain("XML Test");
        xml.ShouldContain("<XmlTestClass");
        restored.ShouldNotBeNull();
        restored.Name.ShouldBe(original.Name);
        restored.Value.ShouldBe(original.Value);
        restored.Date.ShouldBe(original.Date);
    }
    /// <summary>
    /// 测试 - ToXmlFile/FromXmlFile - XML文件序列化往返
    /// </summary>
    [Fact]
    public void ToXmlFile_FromXmlFile_ObjectRoundTrip_Success()
    {
        // Arrange
        var original = new XmlTestClass
        {
            Name = "XML File Test",
            Value = 200,
            Date = DateTime.Today
        };
        var fileName = Path.Combine(_testDirectory, "test.xml");
        // Act
        Serialize.ToXmlFile(fileName, original);
        var restored = Serialize.FromXmlFile<XmlTestClass>(fileName);
        // Assert
        File.Exists(fileName).ShouldBeTrue();
        var xmlContent = File.ReadAllText(fileName);
        xmlContent.ShouldContain("XML File Test");
        restored.ShouldNotBeNull();
        restored.Name.ShouldBe(original.Name);
        restored.Value.ShouldBe(original.Value);
        restored.Date.ShouldBe(original.Date);
    }
    /// <summary>
    /// 测试 - ToXml - null对象抛出异常
    /// </summary>
    [Fact]
    public void ToXml_NullObject_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Serialize.ToXml(null))
            .ParamName.ShouldBe("data");
    }
    /// <summary>
    /// 测试 - FromXml - 无效XML抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromXml_InvalidXml_ThrowsArgumentException(string invalidXml)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Serialize.FromXml<XmlTestClass>(invalidXml))
            .ParamName.ShouldBe("xml");
    }
    /// <summary>
    /// 测试 - ToXmlFile - 文件名无效抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToXmlFile_InvalidFileName_ThrowsArgumentException(string fileName)
    {
        // Arrange
        var data = new XmlTestClass { Name = "xml", Value = 1, Date = DateTime.Today };
        // Act & Assert
        Should.Throw<ArgumentException>(() => Serialize.ToXmlFile(fileName, data))
            .ParamName.ShouldBe("fileName");
    }
    /// <summary>
    /// 测试 - ToXmlFile - 空对象抛出异常
    /// </summary>
    [Fact]
    public void ToXmlFile_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var fileName = Path.Combine(_testDirectory, "test.xml");
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Serialize.ToXmlFile(fileName, null))
            .ParamName.ShouldBe("data");
    }
    /// <summary>
    /// 测试 - FromXmlFile - 文件名无效抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromXmlFile_InvalidFileName_ThrowsArgumentException(string fileName)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Serialize.FromXmlFile<XmlTestClass>(fileName))
            .ParamName.ShouldBe("fileName");
    }
    /// <summary>
    /// 测试 - FromXmlFile - 文件不存在抛出异常
    /// </summary>
    [Fact]
    public void FromXmlFile_FileNotExists_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.xml");
        // Act & Assert
        Should.Throw<FileNotFoundException>(() => Serialize.FromXmlFile<XmlTestClass>(nonExistentFile));
    }
    #endregion
}
