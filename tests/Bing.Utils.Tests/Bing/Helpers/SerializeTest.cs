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
    /// 测试 - ToBytes/FromBytes - 结构体序列化往返
    /// </summary>
    [Fact]
    public void ToBytes_FromBytes_StructRoundTrip_Success()
    {
        // Arrange
        var original = new TestStruct
        {
            IntValue = 42,
            DoubleValue = 3.14159,
            BoolValue = true
        };

        // Act
        var bytes = Serialize.ToBytes(original);
        var restored = Serialize.FromBytes<TestStruct>(bytes);

        // Assert
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBe(Marshal.SizeOf<TestStruct>());
        restored.IntValue.ShouldBe(original.IntValue);
        restored.DoubleValue.ShouldBe(original.DoubleValue);
        restored.BoolValue.ShouldBe(original.BoolValue);
    }

    /// <summary>
    /// 测试 - FromBytes - 无效字节数组长度抛出异常
    /// </summary>
    [Fact]
    public void FromBytes_InvalidByteArrayLength_ThrowsArgumentException()
    {
        // Arrange
        var invalidBytes = new byte[5]; // TestStruct需要更多字节

        // Act & Assert
        Should.Throw<ArgumentException>(() => Serialize.FromBytes<TestStruct>(invalidBytes))
            .Message.ShouldContain("字节数组长度");
    }

    /// <summary>
    /// 测试 - FromBytes - null字节数组抛出异常
    /// </summary>
    [Fact]
    public void FromBytes_NullByteArray_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Serialize.FromBytes<TestStruct>(null))
            .ParamName.ShouldBe("bytes");
    }

    #endregion

    #region 二进制序列化测试

    [Serializable]
    public class TestSerializableClass
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// 测试 - ToBinary/FromBinary - 二进制序列化往返
    /// </summary>
    [Fact]
    public void ToBinary_FromBinary_ObjectRoundTrip_Success()
    {
        // Arrange
        var original = new TestSerializableClass
        {
            Name = "Binary Test",
            Value = 123,
            Date = new DateTime(2023, 6, 15)
        };

        // Act
        var bytes = Serialize.ToBinary(original);
        var restored = Serialize.FromBinary<TestSerializableClass>(bytes);

        // Assert
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
        restored.ShouldNotBeNull();
        restored.Name.ShouldBe(original.Name);
        restored.Value.ShouldBe(original.Value);
        restored.Date.ShouldBe(original.Date);
    }

    /// <summary>
    /// 测试 - ToBinary - null对象抛出异常
    /// </summary>
    [Fact]
    public void ToBinary_NullObject_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Serialize.ToBinary(null))
            .ParamName.ShouldBe("data");
    }

    /// <summary>
    /// 测试 - FromBinary - null字节数组抛出异常
    /// </summary>
    [Fact]
    public void FromBinary_NullByteArray_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Serialize.FromBinary<object>(null))
            .ParamName.ShouldBe("bytes");
    }

    /// <summary>
    /// 测试 - FromBinary - 空字节数组抛出异常
    /// </summary>
    [Fact]
    public void FromBinary_EmptyByteArray_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Serialize.FromBinary<object>(new byte[0]))
            .ParamName.ShouldBe("bytes");
    }

    /// <summary>
    /// 测试 - ToBinaryFile/FromBinaryFile - 二进制文件序列化往返
    /// </summary>
    [Fact]
    public void ToBinaryFile_FromBinaryFile_ObjectRoundTrip_Success()
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
        Serialize.ToBinaryFile(fileName, original);
        var restored = Serialize.FromBinaryFile<TestSerializableClass>(fileName);

        // Assert
        File.Exists(fileName).ShouldBeTrue();
        restored.ShouldNotBeNull();
        restored.Name.ShouldBe(original.Name);
        restored.Value.ShouldBe(original.Value);
        restored.Date.ShouldBe(original.Date);
    }

    /// <summary>
    /// 测试 - FromBinaryFile - 文件不存在抛出异常
    /// </summary>
    [Fact]
    public void FromBinaryFile_FileNotExists_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.bin");

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => Serialize.FromBinaryFile<object>(nonExistentFile));
    }

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

    #endregion

    #region 性能对比测试

    /// <summary>
    /// 测试 - 不同序列化方式的性能对比
    /// </summary>
    [Fact]
    public void SerializationPerformance_Comparison_AllComplete()
    {
        // Arrange
        var testData = new TestSerializableClass
        {
            Name = "Performance Test",
            Value = 12345,
            Date = DateTime.UtcNow
        };

        // Act & Assert - Binary序列化
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var bytes = Serialize.ToBinary(testData);
                var restored = Serialize.FromBinary<TestSerializableClass>(bytes);
            }
        }, TimeSpan.FromSeconds(2), "100次二进制序列化应该在2秒内完成");

        // Act & Assert - JSON序列化
        //Should.CompleteIn(() =>
        //{
        //    for (int i = 0; i < 100; i++)
        //    {
        //        var json = Serialize.ToJson(testData);
        //        var restored = Serialize.FromJson<TestSerializableClass>(json);
        //    }
        //}, TimeSpan.FromSeconds(2), "100次JSON序列化应该在2秒内完成");

        // Act & Assert - XML序列化
        var xmlData = new XmlTestClass
        {
            Name = testData.Name,
            Value = testData.Value,
            Date = testData.Date
        };

        Should.CompleteIn(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var xml = Serialize.ToXml(xmlData);
                var restored = Serialize.FromXml<XmlTestClass>(xml);
            }
        }, TimeSpan.FromSeconds(3), "100次XML序列化应该在3秒内完成");
    }

    #endregion

    #region 集成测试

    /// <summary>
    /// 测试 - 混合序列化场景
    /// </summary>
    [Fact]
    public void MixedSerialization_DifferentFormats_AllWork()
    {
        // Arrange
        var testData = new TestSerializableClass
        {
            Name = "Mixed Test",
            Value = 999,
            Date = new DateTime(2023, 12, 25)
        };

        // Act & Assert - Binary
        var binaryBytes = Serialize.ToBinary(testData);
        var fromBinary = Serialize.FromBinary<TestSerializableClass>(binaryBytes);
        fromBinary.Name.ShouldBe(testData.Name);

        // Act & Assert - JSON
        //var json = Serialize.ToJson(testData);
        //var fromJson = Serialize.FromJson<TestSerializableClass>(json);
        //fromJson.Name.ShouldBe(testData.Name);

        // Act & Assert - XML
        var xmlData = new XmlTestClass
        {
            Name = testData.Name,
            Value = testData.Value,
            Date = testData.Date
        };
        var xml = Serialize.ToXml(xmlData);
        var fromXml = Serialize.FromXml<XmlTestClass>(xml);
        fromXml.Name.ShouldBe(xmlData.Name);

        // Act & Assert - Base64JSON
        //var base64Json = Serialize.ToBase64Json(testData);
        //var fromBase64Json = Serialize.FromBase64Json<TestSerializableClass>(base64Json);
        //fromBase64Json.Name.ShouldBe(testData.Name);
    }

    /// <summary>
    /// 测试 - 文件操作完整流程
    /// </summary>
    [Fact]
    public void FileOperations_CompleteWorkflow_Success()
    {
        // Arrange
        var testData = new TestSerializableClass
        {
            Name = "File Workflow Test",
            Value = 888,
            Date = DateTime.UtcNow
        };

        var binaryFile = Path.Combine(_testDirectory, "workflow.bin");
        var jsonFile = Path.Combine(_testDirectory, "workflow.json");
        var xmlFile = Path.Combine(_testDirectory, "workflow.xml");

        // Act - 写入文件
        Serialize.ToBinaryFile(binaryFile, testData);
        //Serialize.ToJsonFile(jsonFile, testData);

        var xmlData = new XmlTestClass
        {
            Name = testData.Name,
            Value = testData.Value,
            Date = testData.Date
        };
        Serialize.ToXmlFile(xmlFile, xmlData);

        // Act - 从文件读取
        var fromBinaryFile = Serialize.FromBinaryFile<TestSerializableClass>(binaryFile);
        //var fromJsonFile = Serialize.FromJsonFile<TestSerializableClass>(jsonFile);
        var fromXmlFile = Serialize.FromXmlFile<XmlTestClass>(xmlFile);

        // Assert
        File.Exists(binaryFile).ShouldBeTrue();
        //File.Exists(jsonFile).ShouldBeTrue();
        File.Exists(xmlFile).ShouldBeTrue();

        fromBinaryFile.Name.ShouldBe(testData.Name);
        //fromJsonFile.Name.ShouldBe(testData.Name);
        fromXmlFile.Name.ShouldBe(testData.Name);
    }

    #endregion
}