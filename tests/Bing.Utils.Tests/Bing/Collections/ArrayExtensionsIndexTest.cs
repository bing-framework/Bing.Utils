namespace Bing.Collections;

/// <summary>
/// 数组扩展方法 - 索引 测试
/// </summary>
[Trait("CollectionsUT", "ArrayExtensions.Index")]
public class ArrayExtensionsIndexTest : TestBase
{
    /// <inheritdoc />
    public ArrayExtensionsIndexTest(ITestOutputHelper output) : base(output)
    {
    }

    #region WithInIndex(一维数组)

    /// <summary>
    /// 测试 - WithInIndex - 有效索引返回true
    /// </summary>
    [Fact]
    public void WithInIndex_ValidIndex_ReturnsTrue()
    {
        // 准备
        var array = new[] { 1, 2, 3, 4, 5 };

        // 执行 & 验证
        array.WithInIndex(0).ShouldBeTrue("首个索引应在范围内");
        array.WithInIndex(2).ShouldBeTrue("中间索引应在范围内");
        array.WithInIndex(4).ShouldBeTrue("最后索引应在范围内");
    }

    /// <summary>
    /// 测试 - WithInIndex - 无效索引返回false
    /// </summary>
    [Fact]
    public void WithInIndex_InvalidIndex_ReturnsFalse()
    {
        // 准备
        var array = new[] { 1, 2, 3 };

        // 执行 & 验证
        array.WithInIndex(-1).ShouldBeFalse("负数索引应超出范围");
        array.WithInIndex(3).ShouldBeFalse("等于数组长度的索引应超出范围");
        array.WithInIndex(100).ShouldBeFalse("远大于数组长度的索引应超出范围");
    }

    /// <summary>
    /// 测试 - WithInIndex - 空数组返回false
    /// </summary>
    [Fact]
    public void WithInIndex_NullArray_ReturnsFalse()
    {
        // 准备
        Array nullArray = null;

        // 执行 & 验证
        nullArray.WithInIndex(0).ShouldBeFalse("空数组应返回false");
    }

    /// <summary>
    /// 测试 - WithInIndex - 空数组元素返回false
    /// </summary>
    [Fact]
    public void WithInIndex_EmptyArray_ReturnsFalse()
    {
        // 准备
        var emptyArray = Array.Empty<int>();

        // 执行 & 验证
        emptyArray.WithInIndex(0).ShouldBeFalse("空数组中任何索引应超出范围");
    }

    #endregion

    #region WithInIndex(多维数组)

    /// <summary>
    /// 测试 - WithInIndex - 二维数组有效索引返回true
    /// </summary>
    [Fact]
    public void WithInIndex_MultiDimension_ValidIndex_ReturnsTrue()
    {
        // 准备
        var array2D = new[,]
        {
            { 1, 2, 3 },
            { 4, 5, 6 }
        };

        // 执行 & 验证
        // 第一维度（行）
        array2D.WithInIndex(0, 0).ShouldBeTrue("第一维度首个索引应在范围内");
        array2D.WithInIndex(1, 0).ShouldBeTrue("第一维度最后索引应在范围内");

        // 第二维度（列）
        array2D.WithInIndex(0, 1).ShouldBeTrue("第二维度首个索引应在范围内");
        array2D.WithInIndex(1, 1).ShouldBeTrue("第二维度中间索引应在范围内");
        array2D.WithInIndex(2, 1).ShouldBeTrue("第二维度最后索引应在范围内");
    }

    /// <summary>
    /// 测试 - WithInIndex - 二维数组无效索引返回false
    /// </summary>
    [Fact]
    public void WithInIndex_MultiDimension_InvalidIndex_ReturnsFalse()
    {
        // 准备
        var array2D = new[,]
        {
            { 1, 2, 3 },
            { 4, 5, 6 }
        };

        // 执行 & 验证
        // 第一维度（行）
        array2D.WithInIndex(-1, 0).ShouldBeFalse("第一维度负数索引应超出范围");
        array2D.WithInIndex(2, 0).ShouldBeFalse("第一维度超出长度的索引应超出范围");

        // 第二维度（列）
        array2D.WithInIndex(-1, 1).ShouldBeFalse("第二维度负数索引应超出范围");
        array2D.WithInIndex(3, 1).ShouldBeFalse("第二维度超出长度的索引应超出范围");
    }

    /// <summary>
    /// 测试 - WithInIndex - 非0起始索引的数组
    /// </summary>
    [Fact]
    public void WithInIndex_NonZeroBasedArray_ChecksCorrectly()
    {
        // 准备 - 创建一个下标从5开始的数组
        Array nonZeroBasedArray = Array.CreateInstance(typeof(int), new[] { 3 }, new[] { 5 });

        // 验证数组下标确实从5开始
        nonZeroBasedArray.GetLowerBound(0).ShouldBe(5);
        nonZeroBasedArray.GetUpperBound(0).ShouldBe(7); // 5, 6, 7 共三个元素

        // 执行 & 验证
        nonZeroBasedArray.WithInIndex(5, 0).ShouldBeTrue("下标5应在范围内");
        nonZeroBasedArray.WithInIndex(6, 0).ShouldBeTrue("下标6应在范围内");
        nonZeroBasedArray.WithInIndex(7, 0).ShouldBeTrue("下标7应在范围内");
        nonZeroBasedArray.WithInIndex(4, 0).ShouldBeFalse("下标4应超出范围");
        nonZeroBasedArray.WithInIndex(8, 0).ShouldBeFalse("下标8应超出范围");
    }

    /// <summary>
    /// 测试 - WithInIndex - 维度参数异常
    /// </summary>
    [Fact]
    public void WithInIndex_InvalidDimension_ThrowsException()
    {
        // 准备
        var array = new[] { 1, 2, 3 };

        // 执行 & 验证 - 负数维度
        Should.Throw<ArgumentOutOfRangeException>(() => array.WithInIndex(0, -1))
            .ParamName.ShouldBe("dimension");

        // 执行 & 验证 - 超出维度数
        Should.Throw<ArgumentOutOfRangeException>(() => array.WithInIndex(0, 1))
            .ParamName.ShouldBe("dimension");
    }

    /// <summary>
    /// 测试 - WithInIndex - 多维数组空数组返回false
    /// </summary>
    [Fact]
    public void WithInIndex_MultiDimension_NullArray_ReturnsFalse()
    {
        // 准备
        Array nullArray = null;

        // 执行 & 验证
        nullArray.WithInIndex(0, 0).ShouldBeFalse("空数组应返回false");
    }

    #endregion
}