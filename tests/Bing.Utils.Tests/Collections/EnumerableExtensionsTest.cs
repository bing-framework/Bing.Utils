using Bing.Collections;

namespace Bing.Utils.Tests.Collections;

/// <summary>
/// 可枚举类型(<see cref="IEnumerable{T}"/>) 扩展 - ChunkBy 方法测试
/// </summary>
[Trait("CollUT", "Colls")]
public class EnumerableExtensionsTest: TestBase
{
    /// <inheritdoc />
    public EnumerableExtensionsTest(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// 测试 - 将列表分块为多个小列表
    /// </summary>
    [Fact]
    public void Test_ChunkBy_List()
    {
        // Arrange
        var source = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var chunkSize = 3;

        // Act
        var result = source.ChunkBy(chunkSize).ToList();

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal(new List<int> { 1, 2, 3 }, result[0]);
        Assert.Equal(new List<int> { 4, 5, 6 }, result[1]);
        Assert.Equal(new List<int> { 7, 8, 9 }, result[2]);
        Assert.Equal(new List<int> { 10 }, result[3]);

        Output.WriteLine($"分块结果: {string.Join(", ", result.Select(chunk => $"[{string.Join(", ", chunk)}]"))}");
    }

    /// <summary>
    /// 测试 - 将数组分块为多个小列表
    /// </summary>
    [Fact]
    public void Test_ChunkBy_Array()
    {
        // Arrange
        var source = new[] { "A", "B", "C", "D", "E" };
        var chunkSize = 2;

        // Act
        var result = source.ChunkBy(chunkSize).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(new List<string> { "A", "B" }, result[0]);
        Assert.Equal(new List<string> { "C", "D" }, result[1]);
        Assert.Equal(new List<string> { "E" }, result[2]);

        Output.WriteLine($"分块结果: {string.Join(", ", result.Select(chunk => $"[{string.Join(", ", chunk)}]"))}");
    }

    /// <summary>
    /// 测试 - 当源集合为空时返回空结果
    /// </summary>
    [Fact]
    public void Test_ChunkBy_EmptySource()
    {
        // Arrange
        var source = new List<int>();
        var chunkSize = 3;

        // Act
        var result = source.ChunkBy(chunkSize).ToList();

        // Assert
        Assert.Empty(result);
        Output.WriteLine("空集合分块结果为空");
    }

    /// <summary>
    /// 测试 - 当分块大小大于等于集合大小时，返回单个块
    /// </summary>
    [Fact]
    public void Test_ChunkBy_ChunkSizeGreaterThanCollectionSize()
    {
        // Arrange
        var source = new List<int> { 1, 2, 3 };
        var chunkSize = 5;

        // Act
        var result = source.ChunkBy(chunkSize).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(new List<int> { 1, 2, 3 }, result[0]);

        Output.WriteLine($"分块大小({chunkSize})大于集合大小({source.Count})时，返回单个块: [{string.Join(", ", result[0])}]");
    }

    /// <summary>
    /// 测试 - 当分块大小等于1时，每个元素单独一个块
    /// </summary>
    [Fact]
    public void Test_ChunkBy_ChunkSizeOne()
    {
        // Arrange
        var source = new List<int> { 1, 2, 3 };
        var chunkSize = 1;

        // Act
        var result = source.ChunkBy(chunkSize).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(new List<int> { 1 }, result[0]);
        Assert.Equal(new List<int> { 2 }, result[1]);
        Assert.Equal(new List<int> { 3 }, result[2]);

        Output.WriteLine("分块大小为1时，每个元素单独一个块");
    }

    /// <summary>
    /// 测试 - 对于非集合类型的可枚举对象
    /// </summary>
    [Fact]
    public void Test_ChunkBy_Enumerable()
    {
        // Arrange
        IEnumerable<int> source = Enumerable.Range(1, 7);
        var chunkSize = 3;

        // Act
        var result = source.ChunkBy(chunkSize).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(new List<int> { 1, 2, 3 }, result[0]);
        Assert.Equal(new List<int> { 4, 5, 6 }, result[1]);
        Assert.Equal(new List<int> { 7 }, result[2]);

        Output.WriteLine($"可枚举对象分块结果: {string.Join(", ", result.Select(chunk => $"[{string.Join(", ", chunk)}]"))}");
    }

    /// <summary>
    /// 测试 - 延迟执行特性
    /// </summary>
    [Fact]
    public void Test_ChunkBy_LazyExecution()
    {
        // Arrange
        var counter = 0;
        IEnumerable<int> GetNumbers()
        {
            for (int i = 1; i <= 5; i++)
            {
                counter++;
                yield return i;
            }
        }

        // Act
        var chunks = GetNumbers().ChunkBy(2);

        // Assert - 此时应该没有执行任何迭代
        Assert.Equal(0, counter);

        // 仅访问第一个块
        var firstChunk = chunks.First();

        // 应该只执行了生成第一个块所需的迭代
        Assert.Equal(2, counter);
        Assert.Equal(new List<int> { 1, 2 }, firstChunk);

        Output.WriteLine("验证延迟执行特性成功");
    }

    /// <summary>
    /// 测试 - 源集合为空引用时应抛出异常
    /// </summary>
    [Fact]
    public void Test_ChunkBy_NullSource_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<int> source = null;
        var chunkSize = 3;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => source.ChunkBy(chunkSize).ToList());
        Assert.Equal("source", exception.ParamName);

        Output.WriteLine($"当源集合为null时抛出异常: {exception.Message}");
    }

    /// <summary>
    /// 测试 - 分块大小小于等于0时应抛出异常
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Test_ChunkBy_InvalidChunkSize_ThrowsArgumentOutOfRangeException(int chunkSize)
    {
        // Arrange
        var source = new List<int> { 1, 2, 3 };

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => source.ChunkBy(chunkSize).ToList());
        Assert.Equal("chunkSize", exception.ParamName);

        Output.WriteLine($"当分块大小({chunkSize})小于等于0时抛出异常: {exception.Message}");
    }

    /// <summary>
    /// 测试 - 复杂对象分块
    /// </summary>
    [Fact]
    public void Test_ChunkBy_ComplexObjects()
    {
        // Arrange
        var source = new List<TestItem>
            {
                new TestItem { Id = 1, Name = "Item 1" },
                new TestItem { Id = 2, Name = "Item 2" },
                new TestItem { Id = 3, Name = "Item 3" },
                new TestItem { Id = 4, Name = "Item 4" },
                new TestItem { Id = 5, Name = "Item 5" }
            };
        var chunkSize = 2;

        // Act
        var result = source.ChunkBy(chunkSize).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].Count);
        Assert.Equal(2, result[1].Count);
        Assert.Single(result[2]);
        Assert.Equal(1, result[0][0].Id);
        Assert.Equal(2, result[0][1].Id);
        Assert.Equal(3, result[1][0].Id);
        Assert.Equal(4, result[1][1].Id);
        Assert.Equal(5, result[2][0].Id);

        Output.WriteLine("复杂对象分块成功");
    }

    /// <summary>
    /// 测试项
    /// </summary>
    private class TestItem
    {
        /// <summary>
        /// 标识
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
    }
}