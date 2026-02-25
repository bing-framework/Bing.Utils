using System.Collections.ObjectModel;
namespace Bing.Collections;
/// <summary>
/// 集合扩展测试类
/// </summary>
public class CollectionExtensionsTest
{
    #region ToObservableCollection
    /// <summary>
    /// 测试 - ToObservableCollection - 转换到新的可观察集合
    /// </summary>
    [Fact]
    public void ToObservableCollection_ConvertToNew_ReturnNewObservableCollection()
    {
        // Arrange
        var source = new List<string> { "Item1", "Item2", "Item3" };
        // Act
        var result = source.ToObservableCollection();
        // Assert
        Assert.IsType<ObservableCollection<string>>(result);
        Assert.Equal(source.Count, result.Count);
        for (int i = 0; i < source.Count; i++)
        {
            Assert.Equal(source[i], result[i]);
        }
    }
    /// <summary>
    /// 测试 - ToObservableCollection - 源集合为空创建空的可观察集合
    /// </summary>
    [Fact]
    public void ToObservableCollection_EmptySource_ReturnEmptyObservableCollection()
    {
        // Arrange
        var source = new List<int>();
        // Act
        var result = source.ToObservableCollection();
        // Assert
        Assert.IsType<ObservableCollection<int>>(result);
        Assert.Empty(result);
    }
    /// <summary>
    /// 测试 - ToObservableCollection - 源集合为null抛出异常
    /// </summary>
    [Fact]
    public void ToObservableCollection_NullSource_ThrowArgumentNullException()
    {
        // Arrange
        ICollection<string> source = null;
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => source.ToObservableCollection());
    }
    /// <summary>
    /// 测试 - ToObservableCollection - 转换到指定的可观察集合
    /// </summary>
    [Fact]
    public void ToObservableCollection_ConvertToExisting_FillExistingObservableCollection()
    {
        // Arrange
        var source = new List<string> { "Item1", "Item2", "Item3" };
        var target = new ObservableCollection<string> { "OldItem" };
        // Act
        var result = source.ToObservableCollection(target);
        // Assert
        Assert.Same(target, result); // 返回同一个实例
        Assert.Equal(source.Count, result.Count);
        for (int i = 0; i < source.Count; i++)
        {
            Assert.Equal(source[i], result[i]);
        }
    }
    /// <summary>
    /// 测试 - ToObservableCollection - 转换空集合到指定的可观察集合后清空目标集合
    /// </summary>
    [Fact]
    public void ToObservableCollection_EmptySourceToExisting_ClearExistingObservableCollection()
    {
        // Arrange
        var source = new List<string>();
        var target = new ObservableCollection<string> { "Item1", "Item2" };
        // Act
        var result = source.ToObservableCollection(target);
        // Assert
        Assert.Same(target, result); // 返回同一个实例
        Assert.Empty(result);
    }
    /// <summary>
    /// 测试 - ToObservableCollection - 源集合为null抛出异常
    /// </summary>
    [Fact]
    public void ToObservableCollection_NullSourceWithExistingTarget_ThrowArgumentNullException()
    {
        // Arrange
        ICollection<string> source = null;
        var target = new ObservableCollection<string>();
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => source.ToObservableCollection(target));
    }
    /// <summary>
    /// 测试 - ToObservableCollection - 目标集合为null抛出异常
    /// </summary>
    [Fact]
    public void ToObservableCollection_NullTarget_ThrowArgumentNullException()
    {
        // Arrange
        var source = new List<string> { "Item1", "Item2" };
        ObservableCollection<string> target = null;
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => source.ToObservableCollection(target));
    }
    #endregion
}
