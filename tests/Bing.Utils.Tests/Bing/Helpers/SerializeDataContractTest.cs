using System.Collections;
using System.Runtime.Serialization;

namespace Bing.Helpers;

/// <summary>
/// DataContract Binary XML 序列化测试类。
/// </summary>
[Trait("Bing.Helpers", "Serialize.DataContract")]
public class SerializeDataContractTest
{
    /// <summary>
    /// 测试目的：普通对象应能够在 DataContract Binary XML 字节中往返。
    /// </summary>
    [Fact]
    public void ToDataContractBytes_AndFromDataContractBytes_ObjectRoundTrip_Success()
    {
        // Arrange
        var value = new PlainReferenceSample { Name = "alpha", Count = 2 };

        // Act
        var bytes = Serialize.ToDataContractBytes(value);
        var result = Serialize.FromDataContractBytes<PlainReferenceSample>(bytes);

        // Assert
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
        result.Name.ShouldBe(value.Name);
        result.Count.ShouldBe(value.Count);
    }

    /// <summary>
    /// 测试目的：集合与嵌套对象应能够在 DataContract Binary XML 字节中往返。
    /// </summary>
    [Fact]
    public void ToDataContractBytes_AndFromDataContractBytes_CollectionAndNestedObjectRoundTrip_Success()
    {
        // Arrange
        var value = new ContractContainer
        {
            Name = "container",
            Items = new List<ContractSample>
            {
                new ContractSample { Name = "first", Count = 1 },
                new ContractSample { Name = "second", Count = 2 }
            }
        };

        // Act
        var bytes = Serialize.ToDataContractBytes(value);
        var result = Serialize.FromDataContractBytes<ContractContainer>(bytes);

        // Assert
        result.Name.ShouldBe(value.Name);
        result.Items.Count.ShouldBe(2);
        result.Items[0].Name.ShouldBe("first");
        result.Items[1].Count.ShouldBe(2);
    }

    /// <summary>
    /// 测试目的：显式注册派生类型后，多态对象应能够往返。
    /// </summary>
    [Fact]
    public void ToDataContractBytes_KnownDerivedType_RoundTripSucceeds()
    {
        // Arrange
        PolymorphicBase value = new PolymorphicChild { Name = "child", Number = 7 };
        var knownTypes = new[] { typeof(PolymorphicChild) };

        // Act
        var bytes = Serialize.ToDataContractBytes<PolymorphicBase>(value, knownTypes);
        var result = Serialize.FromDataContractBytes<PolymorphicBase>(bytes, knownTypes);

        // Assert
        result.ShouldBeOfType<PolymorphicChild>();
        ((PolymorphicChild)result).Number.ShouldBe(7);
    }

    /// <summary>
    /// 测试目的：未显式注册派生类型时，序列化应拒绝多态对象。
    /// </summary>
    [Fact]
    public void ToDataContractBytes_UnknownDerivedType_ThrowsSerializationException()
    {
        // Arrange
        PolymorphicBase value = new PolymorphicChild { Name = "child", Number = 7 };

        // Act and Assert
        Should.Throw<SerializationException>(() => Serialize.ToDataContractBytes<PolymorphicBase>(value));
    }

    /// <summary>
    /// 测试目的：未使用相同已知类型集合读取多态字节时，应拒绝派生类型。
    /// </summary>
    [Fact]
    public void FromDataContractBytes_UnknownDerivedType_ThrowsSerializationException()
    {
        // Arrange
        PolymorphicBase value = new PolymorphicChild { Name = "child", Number = 7 };
        var knownTypes = new[] { typeof(PolymorphicChild) };
        var bytes = Serialize.ToDataContractBytes<PolymorphicBase>(value, knownTypes);

        // Act and Assert
        Should.Throw<SerializationException>(() => Serialize.FromDataContractBytes<PolymorphicBase>(bytes));
    }

    /// <summary>
    /// 测试目的：启用引用保持后，循环对象图应能够往返且保持引用关系。
    /// </summary>
    [Fact]
    public void ToDataContractBytes_CircularReference_PreservesObjectReference()
    {
        // Arrange
        var value = new CircularNode { Name = "root" };
        value.Next = value;

        // Act
        var bytes = Serialize.ToDataContractBytes(value);
        var result = Serialize.FromDataContractBytes<CircularNode>(bytes);

        // Assert
        result.Name.ShouldBe("root");
        ReferenceEquals(result, result.Next).ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：空对象输入应被明确拒绝。
    /// </summary>
    [Fact]
    public void ToDataContractBytes_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Should.Throw<ArgumentNullException>(() => Serialize.ToDataContractBytes<ContractSample>(null)).ParamName.ShouldBe("data");
    }

    /// <summary>
    /// 测试目的：null 与空字节输入应被明确拒绝。
    /// </summary>
    [Fact]
    public void FromDataContractBytes_NullOrEmptyInput_ThrowsArgumentException()
    {
        // Act and Assert
        Should.Throw<ArgumentNullException>(() => Serialize.FromDataContractBytes<ContractSample>(null)).ParamName.ShouldBe("bytes");
        Should.Throw<ArgumentException>(() => Serialize.FromDataContractBytes<ContractSample>(Array.Empty<byte>())).ParamName.ShouldBe("bytes");
    }

    /// <summary>
    /// 测试目的：错误的目标类型应导致 DataContract 读取失败。
    /// </summary>
    [Fact]
    public void FromDataContractBytes_WrongTargetType_ThrowsSerializationException()
    {
        // Arrange
        var bytes = Serialize.ToDataContractBytes(new ContractSample { Name = "alpha", Count = 2 });

        // Act and Assert
        Should.Throw<SerializationException>(() => Serialize.FromDataContractBytes<ContractContainer>(bytes));
    }

    /// <summary>
    /// 测试目的：非法已知类型集合应被拒绝，避免隐式信任任意输入类型。
    /// </summary>
    [Fact]
    public void ToDataContractBytes_InvalidKnownTypes_ThrowsArgumentException()
    {
        // Arrange
        IEnumerable knownTypes = new object[] { typeof(ContractSample), "invalid" };

        // Act and Assert
        Should.Throw<ArgumentException>(() => Serialize.ToDataContractBytes(new ContractSample(), knownTypes)).ParamName.ShouldBe("knownTypes");
    }

    /// <summary>
    /// 测试目的：超过读取字符串配额的 DataContract Binary XML 应被拒绝。
    /// </summary>
    [Fact]
    public void FromDataContractBytes_StringQuotaExceeded_ThrowsSerializationException()
    {
        // Arrange
        var bytes = Serialize.ToDataContractBytes(new ContractSample
        {
            Name = new string('a', 1_048_577),
            Count = 1
        });

        // Act and Assert
        Should.Throw<SerializationException>(() => Serialize.FromDataContractBytes<ContractSample>(bytes));
    }

    /// <summary>
    /// 测试目的：重复读取不同字节数组不应保留已释放读取器状态。
    /// </summary>
    [Fact]
    public void FromDataContractBytes_RepeatedCalls_DoNotRetainReaderState()
    {
        // Arrange
        var first = Serialize.ToDataContractBytes(new ContractSample { Name = "first", Count = 1 });
        var second = Serialize.ToDataContractBytes(new ContractSample { Name = "second", Count = 2 });

        // Act
        var firstResult = Serialize.FromDataContractBytes<ContractSample>(first);
        var secondResult = Serialize.FromDataContractBytes<ContractSample>(second);

        // Assert
        firstResult.Name.ShouldBe("first");
        secondResult.Name.ShouldBe("second");
    }

    /// <summary>
    /// 用于普通对象测试的 DataContract 类型。
    /// </summary>
    [DataContract]
    public class ContractSample
    {
        /// <summary>
        /// 名称。
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// 数量。
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// 用于验证普通引用类型默认契约的测试类型。
    /// </summary>
    public class PlainReferenceSample
    {
        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 数量。
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// 用于集合和嵌套对象测试的 DataContract 类型。
    /// </summary>
    [DataContract]
    public class ContractContainer
    {
        /// <summary>
        /// 名称。
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// 嵌套项目集合。
        /// </summary>
        [DataMember]
        public List<ContractSample> Items { get; set; }
    }

    /// <summary>
    /// 用于多态测试的基类。
    /// </summary>
    [DataContract]
    public class PolymorphicBase
    {
        /// <summary>
        /// 名称。
        /// </summary>
        [DataMember]
        public string Name { get; set; }
    }

    /// <summary>
    /// 用于多态测试的派生类。
    /// </summary>
    [DataContract]
    public class PolymorphicChild : PolymorphicBase
    {
        /// <summary>
        /// 数值。
        /// </summary>
        [DataMember]
        public int Number { get; set; }
    }

    /// <summary>
    /// 用于循环引用测试的节点类型。
    /// </summary>
    [DataContract]
    public class CircularNode
    {
        /// <summary>
        /// 节点名称。
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// 下一个节点。
        /// </summary>
        [DataMember]
        public CircularNode Next { get; set; }
    }
}