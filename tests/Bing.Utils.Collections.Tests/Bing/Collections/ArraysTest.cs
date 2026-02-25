using System.Collections.Generic;
using System.Linq;
namespace Bing.Collections;
/// <summary>
/// 数组操作 测试类
/// </summary>
[Trait("CollectionsUT", "Arrays")]
public class ArraysTest
{
    #region Empty
    /// <summary>
    /// 测试 - Empty - 获取空数组
    /// </summary>
    [Fact]
    public void Empty_ReturnEmptyArray()
    {
        // Act
        var emptyIntArray = Arrays.Empty<int>();
        var emptyStringArray = Arrays.Empty<string>();
        // Assert
        Assert.NotNull(emptyIntArray);
        Assert.NotNull(emptyStringArray);
        Assert.Empty(emptyIntArray);
        Assert.Empty(emptyStringArray);
    }
    /// <summary>
    /// 测试 - Empty - 多次调用返回同一个实例
    /// </summary>
    [Fact]
    public void Empty_ReturnSameInstance()
    {
        // Act
        var array1 = Arrays.Empty<int>();
        var array2 = Arrays.Empty<int>();
        // Assert
        Assert.Same(array1, array2);
    }
    #endregion
    #region ToArraySafety - IEnumerable<T> with count
    /// <summary>
    /// 测试 - ToArraySafety - 从可枚举集合获取指定数量的元素
    /// </summary>
    [Fact]
    public void ToArraySafety_FromEnumerableWithCount_ReturnArrayWithSpecifiedCount()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5 };
        // Act
        var result = Arrays.ToArraySafety(list, 3);
        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(new[] { 1, 2, 3 }, result);
    }
    /// <summary>
    /// 测试 - ToArraySafety - 请求元素数量大于集合大小
    /// </summary>
    [Fact]
    public void ToArraySafety_RequestMoreElementsThanAvailable_ReturnArrayWithDefaultValues()
    {
        // Arrange
        var list = new List<string> { "A", "B" };
        // Act
        var result = Arrays.ToArraySafety(list, 4);
        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal(new[] { "A", "B", null, null }, result);
    }
    /// <summary>
    /// 测试 - ToArraySafety - 请求元素数量为零或负数
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ToArraySafety_RequestZeroOrNegativeCount_ReturnEmptyArray(int count)
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        // Act
        var result = Arrays.ToArraySafety(list, count);
        // Assert
        Assert.Empty(result);
    }
    /// <summary>
    /// 测试 - ToArraySafety - 源集合为 null
    /// </summary>
    [Fact]
    public void ToArraySafety_SourceIsNull_ReturnArrayWithDefaultValues()
    {
        // Arrange
        IEnumerable<int> list = null;
        // Act
        var result = Arrays.ToArraySafety(list, 3);
        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(new[] { 0, 0, 0 }, result);
    }
    #endregion
    #region ToArraySafety - IEnumerable<T>
    /// <summary>
    /// 测试 - ToArraySafety - 从可枚举集合创建数组
    /// </summary>
    [Fact]
    public void ToArraySafety_FromEnumerable_ReturnArrayWithAllElements()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        // Act
        var result = Arrays.ToArraySafety(list);
        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(new[] { 1, 2, 3 }, result);
    }
    /// <summary>
    /// 测试 - ToArraySafety - 传入已经是数组的集合
    /// </summary>
    [Fact]
    public void ToArraySafety_SourceIsAlreadyArray_ReturnSameArray()
    {
        // Arrange
        var original = new[] { 1, 2, 3 };
        // Act
        var result = Arrays.ToArraySafety(original);
        // Assert
        Assert.Same(original, result);
    }
    /// <summary>
    /// 测试 - ToArraySafety - 源集合为 null
    /// </summary>
    [Fact]
    public void ToArraySafety_EnumerableIsNull_ReturnEmptyArray()
    {
        // Arrange
        IEnumerable<string> list = null;
        // Act
        var result = Arrays.ToArraySafety(list);
        // Assert
        Assert.Empty(result);
    }
    #endregion
    #region ToArraySafety - Array with count
    /// <summary>
    /// 测试 - ToArraySafety - 从非泛型数组转换指定数量元素到类型化数组
    /// </summary>
    [Fact]
    public void ToArraySafety_FromArrayWithCount_ReturnTypedArrayWithSpecifiedCount()
    {
        // Arrange
        Array array = new object[] { "A", "B", "C", "D" };
        // Act
        var result = Arrays.ToArraySafety<string>(array, 2);
        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal(new[] { "A", "B" }, result);
    }
    /// <summary>
    /// 测试 - ToArraySafety - 请求元素数量大于数组大小
    /// </summary>
    [Fact]
    public void ToArraySafety_RequestMoreElementsThanArraySize_ReturnArrayWithDefaultValues()
    {
        // Arrange
        Array array = new object[] { 1, 2 };
        // Act
        var result = Arrays.ToArraySafety<int>(array, 4);
        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal(new[] { 1, 2, 0, 0 }, result);
    }
    /// <summary>
    /// 测试 - ToArraySafety - 类型转换失败
    /// </summary>
    [Fact]
    public void ToArraySafety_InvalidCastFromArrayWithCount_ThrowInvalidCastException()
    {
        // Arrange
        Array array = new object[] { "not a number" };
        // Act & Assert
        Assert.Throws<InvalidCastException>(() => Arrays.ToArraySafety<int>(array, 1));
    }
    /// <summary>
    /// 测试 - ToArraySafety - 请求元素数量为零或负数
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ToArraySafety_RequestZeroOrNegativeCountFromArray_ReturnEmptyArray(int count)
    {
        // Arrange
        Array array = new object[] { 1, 2, 3 };
        // Act
        var result = Arrays.ToArraySafety<int>(array, count);
        // Assert
        Assert.Empty(result);
    }
    /// <summary>
    /// 测试 - ToArraySafety - 源数组为 null
    /// </summary>
    [Fact]
    public void ToArraySafety_ArraySourceIsNullWithCount_ReturnArrayWithDefaultValues()
    {
        // Arrange
        Array array = null;
        // Act
        var result = Arrays.ToArraySafety<string>(array, 2);
        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal(new string[] { null, null }, result);
    }
    #endregion
    #region ToArraySafety - Array
    /// <summary>
    /// 测试 - ToArraySafety - 从非泛型数组转换所有元素到类型化数组
    /// </summary>
    [Fact]
    public void ToArraySafety_FromArray_ReturnTypedArrayWithAllElements()
    {
        // Arrange
        Array array = new object[] { 10, 20, 30 };
        // Act
        var result = Arrays.ToArraySafety<int>(array);
        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(new[] { 10, 20, 30 }, result);
    }
    /// <summary>
    /// 测试 - ToArraySafety - 类型转换失败
    /// </summary>
    [Fact]
    public void ToArraySafety_InvalidCastFromArray_ThrowInvalidCastException()
    {
        // Arrange
        Array array = new object[] { 1, "not a number", 3 };
        // Act & Assert
        Assert.Throws<InvalidCastException>(() => Arrays.ToArraySafety<int>(array));
    }
    /// <summary>
    /// 测试 - ToArraySafety - 源数组为 null
    /// </summary>
    [Fact]
    public void ToArraySafety_ArraySourceIsNull_ReturnEmptyArray()
    {
        // Arrange
        Array array = null;
        // Act
        var result = Arrays.ToArraySafety<double>(array);
        // Assert
        Assert.Empty(result);
    }
    #endregion
    #region GetLength
    /// <summary>
    /// 测试 - GetLength - 获取数组长度
    /// </summary>
    [Fact]
    public void GetLength_WithValidArray_ReturnCorrectLength()
    {
        // Arrange
        int[] array = { 1, 2, 3, 4, 5 };
        // Act
        var result = Arrays.GetLength(array);
        // Assert
        Assert.Equal(5, result);
    }
    /// <summary>
    /// 测试 - GetLength - 空数组长度为零
    /// </summary>
    [Fact]
    public void GetLength_WithEmptyArray_ReturnZero()
    {
        // Arrange
        var array = Array.Empty<string>();
        // Act
        var result = Arrays.GetLength(array);
        // Assert
        Assert.Equal(0, result);
    }
    /// <summary>
    /// 测试 - GetLength - 数组为 null 时抛出异常
    /// </summary>
    [Fact]
    public void GetLength_WithNullArray_ThrowArgumentNullException()
    {
        // Arrange
        Array array = null;
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Arrays.GetLength(array));
    }
    #endregion
    #region AreEqual
    /// <summary>
    /// 测试 - AreEqual - 两个 null 数组被视为相等
    /// </summary>
    [Fact]
    public void AreEqual_BothNull_ReturnTrue()
    {
        // Act
        var result = Arrays.AreEqual(null, null);
        // Assert
        Assert.True(result);
    }
    /// <summary>
    /// 测试 - AreEqual - 一个数组为 null，另一个不为 null
    /// </summary>
    [Fact]
    public void AreEqual_OneNullOneNonNull_ReturnFalse()
    {
        // Arrange
        int[] array = { 1, 2, 3 };
        // Act
        var result1 = Arrays.AreEqual(array, null);
        var result2 = Arrays.AreEqual(null, array);
        // Assert
        Assert.False(result1);
        Assert.False(result2);
    }
    /// <summary>
    /// 测试 - AreEqual - 长度不同的数组
    /// </summary>
    [Fact]
    public void AreEqual_DifferentLength_ReturnFalse()
    {
        // Arrange
        int[] array1 = { 1, 2, 3 };
        int[] array2 = { 1, 2, 3, 4 };
        // Act
        var result = Arrays.AreEqual(array1, array2);
        // Assert
        Assert.False(result);
    }
    /// <summary>
    /// 测试 - AreEqual - 值相同的数组
    /// </summary>
    [Fact]
    public void AreEqual_SameValues_ReturnTrue()
    {
        // Arrange
        int[] array1 = { 1, 2, 3 };
        int[] array2 = { 1, 2, 3 };
        // Act
        var result = Arrays.AreEqual(array1, array2);
        // Assert
        Assert.True(result);
    }
    /// <summary>
    /// 测试 - AreEqual - 值不同的数组
    /// </summary>
    [Fact]
    public void AreEqual_DifferentValues_ReturnFalse()
    {
        // Arrange
        int[] array1 = { 1, 2, 3 };
        int[] array2 = { 1, 2, 4 };
        // Act
        var result = Arrays.AreEqual(array1, array2);
        // Assert
        Assert.False(result);
    }
    /// <summary>
    /// 测试 - AreEqual - 空数组相等比较
    /// </summary>
    [Fact]
    public void AreEqual_EmptyArrays_ReturnTrue()
    {
        // Arrange
        int[] array1 = Array.Empty<int>();
        int[] array2 = Array.Empty<int>();
        // Act
        var result = Arrays.AreEqual(array1, array2);
        // Assert
        Assert.True(result);
    }
    /// <summary>
    /// 测试 - AreEqual - 自定义对象数组比较
    /// </summary>
    [Fact]
    public void AreEqual_CustomObjectArrays_UseEqualsMethod()
    {
        // Arrange
        var obj1 = new TestObject { Id = 1, Name = "Test" };
        var obj2 = new TestObject { Id = 1, Name = "Test" };
        var obj3 = new TestObject { Id = 2, Name = "Different" };
        var array1 = new[] { obj1 };
        var array2 = new[] { obj2 };  // 值相等
        var array3 = new[] { obj3 };  // 值不相等
        // Act
        var result1 = Arrays.AreEqual(array1, array2);
        var result2 = Arrays.AreEqual(array1, array3);
        // Assert
        Assert.True(result1);
        Assert.False(result2);
    }
    #endregion
    #region GetEnumerator
    /// <summary>
    /// 测试 - GetEnumerator - 枚举正常数组的元素
    /// </summary>
    [Fact]
    public void GetEnumerator_WithValidArray_EnumerateAllElements()
    {
        // Arrange
        string[] array = { "One", "Two", "Three" };
        var expected = new List<string> { "One", "Two", "Three" };
        // Act
        var result = new List<string>();
        var enumerator = Arrays.GetEnumerator(array);
        while (enumerator.MoveNext())
        {
            result.Add(enumerator.Current);
        }
        // Assert
        Assert.Equal(expected, result);
    }
    /// <summary>
    /// 测试 - GetEnumerator - 枚举 null 数组
    /// </summary>
    [Fact]
    public void GetEnumerator_WithNullArray_ReturnEmptyEnumerator()
    {
        // Arrange
        string[] array = null;
        // Act
        var enumerator = Arrays.GetEnumerator(array);
        // Assert
        Assert.False(enumerator.MoveNext());
    }
    #endregion
    #region Enumerate
    /// <summary>
    /// 测试 - Enumerate - 枚举正常数组的元素
    /// </summary>
    [Fact]
    public void Enumerate_WithValidArray_ReturnAllElements()
    {
        // Arrange
        string[] array = { "One", "Two", "Three" };
        var expected = new List<string> { "One", "Two", "Three" };
        // Act
        var result = Arrays.Enumerate(array).ToList();
        // Assert
        Assert.Equal(expected, result);
    }
    /// <summary>
    /// 测试 - Enumerate - 枚举 null 数组
    /// </summary>
    [Fact]
    public void Enumerate_WithNullArray_ReturnEmptyEnumerable()
    {
        // Arrange
        string[] array = null;
        // Act
        var result = Arrays.Enumerate(array).ToList();
        // Assert
        Assert.Empty(result);
    }
    /// <summary>
    /// 测试 - Enumerate - 与 LINQ 结合使用
    /// </summary>
    [Fact]
    public void Enumerate_WithLinq_ProcessQueryCorrectly()
    {
        // Arrange
        string[] array = { "Apple", "Banana", "Cherry", "Date" };
        // Act
        var result = Arrays.Enumerate(array)
                          .Where(s => s.Length <= 5)
                          .Select(s => s.ToUpper())
                          .ToList();
        // Assert
        Assert.Equal(new[] { "APPLE", "DATE" }, result);
    }
    #endregion
    #region ReverseEnumerate
    /// <summary>
    /// 测试 - ReverseEnumerate - 逆序枚举正常数组的元素
    /// </summary>
    [Fact]
    public void ReverseEnumerate_WithValidArray_ReturnElementsInReverseOrder()
    {
        // Arrange
        int[] array = { 1, 2, 3, 4, 5 };
        int[] expected = { 5, 4, 3, 2, 1 };
        // Act
        var result = Arrays.ReverseEnumerate(array).ToArray();
        // Assert
        Assert.Equal(expected, result);
    }
    /// <summary>
    /// 测试 - ReverseEnumerate - 逆序枚举空数组
    /// </summary>
    [Fact]
    public void ReverseEnumerate_WithEmptyArray_ReturnEmptyEnumerable()
    {
        // Arrange
        int[] array = Array.Empty<int>();
        // Act
        var result = Arrays.ReverseEnumerate(array).ToArray();
        // Assert
        Assert.Empty(result);
    }
    /// <summary>
    /// 测试 - ReverseEnumerate - 逆序枚举 null 数组
    /// </summary>
    [Fact]
    public void ReverseEnumerate_WithNullArray_ReturnEmptyEnumerable()
    {
        // Arrange
        int[] array = null;
        // Act
        var result = Arrays.ReverseEnumerate(array).ToArray();
        // Assert
        Assert.Empty(result);
    }
    /// <summary>
    /// 测试 - ReverseEnumerate - 逆序枚举单元素数组
    /// </summary>
    [Fact]
    public void ReverseEnumerate_WithSingleElementArray_ReturnThatElement()
    {
        // Arrange
        string[] array = { "Single" };
        // Act
        var result = Arrays.ReverseEnumerate(array).ToArray();
        // Assert
        Assert.Single(result);
        Assert.Equal("Single", result[0]);
    }
    /// <summary>
    /// 测试 - ReverseEnumerate - 与 LINQ 结合使用
    /// </summary>
    [Fact]
    public void ReverseEnumerate_WithLinq_ProcessQueryCorrectly()
    {
        // Arrange
        string[] array = { "Apple", "Banana", "Cherry", "Date" };
        // Act
        var result = Arrays.ReverseEnumerate(array)
                          .Where(s => s.Length > 5)
                          .Select(s => s.ToLower())
                          .ToList();
        // Assert
        Assert.Equal(new[] { "cherry", "banana" }, result);
    }
    ///// <summary>
    ///// 测试 - ReverseEnumerate - 与 LINQ Reverse 方法比较
    ///// </summary>
    //[Fact]
    //public void ReverseEnumerate_ComparedToLinqReverse_ReturnSameResult()
    //{
    //    // Arrange
    //    int[] array = { 1, 2, 3, 4, 5 };
    //    // Act
    //    var result1 = Arrays.ReverseEnumerate(array).ToArray();
    //    var result2 = array.Reverse().ToArray();
    //    // Assert
    //    Assert.Equal(result2, result1);
    //}
    #endregion
}
/// <summary>
/// 测试用自定义对象
/// </summary>
public class TestObject
{
    /// <summary>
    /// 标识符
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// 判断对象是否相等
    /// </summary>
    /// <param name="obj">要比较的对象</param>
    /// <returns>如果对象相等则返回 true，否则返回 false</returns>
    public override bool Equals(object obj)
    {
        if (obj is not TestObject other)
            return false;
        return Id == other.Id && Name == other.Name;
    }
    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }
}
