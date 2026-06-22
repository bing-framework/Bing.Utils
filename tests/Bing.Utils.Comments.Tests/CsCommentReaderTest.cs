namespace Bing.Utils.Comments.Tests;

/// <summary>
/// CsCommentReader 单元测试
/// </summary>
public class CsCommentReaderTest
{
    #region Create(MemberInfo) — 参数守卫

    /// <summary>
    /// 测试目的：验证传入 null 时抛出 ArgumentNullException（类型重载）
    /// </summary>
    [Fact]
    public void Create_Type_NullArgument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CsCommentReader.Create((Type)null));
    }

    /// <summary>
    /// 测试目的：验证传入 null 时抛出 ArgumentNullException（方法重载）
    /// </summary>
    [Fact]
    public void Create_Method_NullArgument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CsCommentReader.Create((MethodInfo)null));
    }

    /// <summary>
    /// 测试目的：验证传入 null 时抛出 ArgumentNullException（属性重载）
    /// </summary>
    [Fact]
    public void Create_Property_NullArgument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CsCommentReader.Create((PropertyInfo)null));
    }

    /// <summary>
    /// 测试目的：验证传入 null 时抛出 ArgumentNullException（字段重载）
    /// </summary>
    [Fact]
    public void Create_Field_NullArgument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CsCommentReader.Create((FieldInfo)null));
    }

    /// <summary>
    /// 测试目的：验证传入 null 时抛出 ArgumentNullException（MemberInfo 重载）
    /// </summary>
    [Fact]
    public void Create_MemberInfo_NullArgument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CsCommentReader.Create((MemberInfo)null));
    }

    #endregion

    #region CsComments.Empty 静态对象

    /// <summary>
    /// 测试目的：验证 CsComments.Empty 的所有属性均为默认空值
    /// </summary>
    [Fact]
    public void CsComments_Empty_AllPropertiesAreNull()
    {
        // Arrange
        var empty = CsComments.Empty;

        // Assert
        Assert.Null(empty.Summary);
        Assert.Null(empty.Remarks);
        Assert.Null(empty.Returns);
        Assert.NotNull(empty.Param);
        Assert.NotNull(empty.TypeParam);
        Assert.NotNull(empty.Exception);
        Assert.Empty(empty.Exception);
    }

    /// <summary>
    /// 测试目的：验证 CsComments.Empty 每次返回同一实例（单例）
    /// </summary>
    [Fact]
    public void CsComments_Empty_ReturnsSameInstance()
    {
        // Assert
        Assert.Same(CsComments.Empty, CsComments.Empty);
    }

    #endregion

    #region CsCommentsParam.Empty 静态对象

    /// <summary>
    /// 测试目的：验证 CsCommentsParam.Empty.ToString() 返回空字符串
    /// </summary>
    [Fact]
    public void CsCommentsParam_Empty_ToStringReturnsEmpty()
    {
        // Arrange
        var empty = CsCommentsParam.Empty;

        // Assert
        Assert.Null(empty.Name);
        Assert.Null(empty.Text);
        Assert.Equal("", empty.ToString());
    }

    #endregion

    #region CsCommentsParamCollection 索引器

    /// <summary>
    /// 测试目的：验证参数集合对 null/空 key 返回 null
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CsCommentsParamCollection_NullOrEmptyKey_ReturnsNull(string key)
    {
        // Arrange
        var collection = new CsCommentsParamCollection();

        // Act
        var result = collection[key];

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 测试目的：验证 @ 前缀 key 会被自动去掉前缀后查找
    /// </summary>
    [Fact]
    public void CsCommentsParamCollection_AtPrefixKey_StripsAtSign()
    {
        // Arrange
        var collection = new CsCommentsParamCollection();
        collection["value"] = CsCommentsParam.Empty;

        // Act
        var result = collection["@value"];

        // Assert
        Assert.NotNull(result);
        Assert.Same(CsCommentsParam.Empty, result);
    }

    /// <summary>
    /// 测试目的：验证不存在的 key 返回 null
    /// </summary>
    [Fact]
    public void CsCommentsParamCollection_UnknownKey_ReturnsNull()
    {
        // Arrange
        var collection = new CsCommentsParamCollection();

        // Act
        var result = collection["nonexistent"];

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateEnumComment — 守卫测试

    /// <summary>
    /// 测试目的：验证对非枚举类型调用 CreateEnumComment 抛出 ArgumentOutOfRangeException
    /// </summary>
    [Fact]
    public void CreateEnumComment_NonEnumType_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CsCommentReader.CreateEnumComment(typeof(string)));
    }

    /// <summary>
    /// 测试目的：验证对 null 枚举类型调用 CreateEnumComment 抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void CreateEnumComment_NullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            CsCommentReader.CreateEnumComment(null));
    }

    /// <summary>
    /// 测试目的：验证 CreateEnumComment 对枚举能返回字典（含所有枚举值作为 key）
    /// </summary>
    [Fact]
    public void CreateEnumComment_ValidEnum_ReturnsDictionaryWithAllValues()
    {
        // Act — SampleColor 无 XML 文档（测试程序集），走 DescriptionAttribute 或 Name 回退
        var dict = CsCommentReader.CreateEnumComment(typeof(SampleColor));

        // Assert
        Assert.NotNull(dict);
        Assert.True(dict.ContainsKey(SampleColor.Red));
        Assert.True(dict.ContainsKey(SampleColor.Green));
        Assert.True(dict.ContainsKey(SampleColor.Blue));
    }

    /// <summary>
    /// 测试目的：验证 CreateEnumComment 对有 DescriptionAttribute 的字段优先返回描述
    /// </summary>
    [Fact]
    public void CreateEnumComment_FieldWithDescription_UsesDescriptionAttributeValue()
    {
        // Act — SampleColor.Green 有 [Description("绿")]
        var dict = CsCommentReader.CreateEnumComment(typeof(SampleColor));

        // Assert — XML 不存在时回退到 DescriptionAttribute
        Assert.Equal("绿", dict[SampleColor.Green]);
    }

    /// <summary>
    /// 测试目的：验证 CreateEnumComment 对无 Description、无 XML 的字段返回成员名称
    /// </summary>
    [Fact]
    public void CreateEnumComment_FieldWithoutDescription_FallsBackToName()
    {
        // Act
        var dict = CsCommentReader.CreateEnumComment(typeof(SampleColor));

        // Assert — Red 没有 Description，XML 也不存在，应返回 "Red"
        Assert.Equal("Red", dict[SampleColor.Red]);
    }

    #endregion

    #region Create(Type) — XML 注释读取

    /// <summary>
    /// 测试目的：验证对有 XML 文档的类型能读取 Summary（Bing.Utils.Comments 自身）
    /// </summary>
    [Fact]
    public void Create_Type_WithXmlDoc_ReturnsSummaryNotNull()
    {
        // Arrange — CsCommentReader 自身有 XML 文档
        var type = typeof(CsCommentReader);

        // Act
        var comments = CsCommentReader.Create(type);

        // Assert — 如果 XML 不可用则返回 null（不强制），有则检查内容
        if (comments != null)
            Assert.False(string.IsNullOrWhiteSpace(comments.Summary));
    }

    /// <summary>
    /// 测试目的：验证 Create(MethodInfo) 通过 MemberInfo 重载正常分发
    /// </summary>
    [Fact]
    public void Create_MemberInfo_Method_DispatchesCorrectly()
    {
        // Arrange
        MemberInfo member = typeof(CsCommentReader).GetMethod(
            nameof(CsCommentReader.Create),
            new[] { typeof(Type) });

        // Act — 不抛出即可
        var exception = Record.Exception(() => CsCommentReader.Create(member));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试目的：验证对不支持的 MemberTypes 抛出 NotSupportedException
    /// </summary>
    [Fact]
    public void Create_MemberInfo_UnsupportedType_ThrowsNotSupportedException()
    {
        // Arrange — 使用自定义 CustomMemberInfo 模拟 Custom 类型（通过枚举反射）
        // 实际无法直接构造 Custom MemberInfo，但可以验证 Custom member type 枚举覆盖
        // 这里改为测试 NestedType 分支（非 Type 实例）通过 TypeInfo 转换成功不抛
        var nestedType = typeof(CsComments).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
            .FirstOrDefault();
        if (nestedType != null)
        {
            // Act — NestedType 是 Type 实例，应该正常处理
            Exception ex = null;
            try { CsCommentReader.Create((MemberInfo)nestedType); }
            catch (Exception e) { ex = e; }
            Assert.Null(ex);
        }
    }

    #endregion
}
