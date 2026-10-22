using System.Reflection;
using System.Reflection.Emit;

namespace Bing.Utils.Comments.Tests;

/// <summary>
/// 验证注释读取器的参数守卫、成员分发和枚举回退行为。
/// </summary>
public class CsCommentReaderTest
{
    #region Create(MemberInfo) — 参数守卫

    /// <summary>
    /// 验证类型参数为 null 时抛出参数异常。
    /// </summary>
    [Fact]
    public void Create_Type_NullArgument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CsCommentReader.Create((Type)null));
    }

    /// <summary>
    /// 验证方法参数为 null 时抛出参数异常。
    /// </summary>
    [Fact]
    public void Create_Method_NullArgument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CsCommentReader.Create((MethodInfo)null));
    }

    /// <summary>
    /// 验证属性参数为 null 时抛出参数异常。
    /// </summary>
    [Fact]
    public void Create_Property_NullArgument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CsCommentReader.Create((PropertyInfo)null));
    }

    /// <summary>
    /// 验证字段参数为 null 时抛出参数异常。
    /// </summary>
    [Fact]
    public void Create_Field_NullArgument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CsCommentReader.Create((FieldInfo)null));
    }

    /// <summary>
    /// 验证成员参数为 null 时抛出参数异常。
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
    /// 验证空注释对象的文本属性为空且集合属性已初始化。
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
    /// 验证空注释对象由静态属性复用同一实例。
    /// </summary>
    [Fact]
    public void CsComments_Empty_ReturnsSameInstance()
    {
        // Assert
        Assert.Same(CsComments.Empty, CsComments.Empty);
    }

    /// <summary>
    /// 测试目的：验证修改空注释的参数集合不会污染后续访问。
    /// </summary>
    [Fact]
    public void CsComments_Empty_ParamMutation_DoesNotLeakAcrossAccesses()
    {
        // Arrange
        var empty = CsComments.Empty;
        empty.Param["value"] = CsCommentsParam.Empty;
        empty.TypeParam["T"] = CsCommentsParam.Empty;

        // Act
        var nextParam = empty.Param["value"];
        var nextTypeParam = empty.TypeParam["T"];

        // Assert
        Assert.Null(nextParam);
        Assert.Null(nextTypeParam);
    }

    #endregion

    #region CsCommentsParam.Empty 静态对象

    /// <summary>
    /// 验证空参数注释的文本格式为空字符串。
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
    /// 验证参数集合使用 null 或空键查询时返回 null。
    /// </summary>
    /// <param name="key">要查询的参数名称。</param>
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
    /// 验证参数集合查询时会移除参数名中的 `@` 前缀。
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
    /// 验证参数集合查询不存在的名称时返回 null。
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
    /// 验证非枚举类型不能传入枚举注释读取方法。
    /// </summary>
    [Fact]
    public void CreateEnumComment_NonEnumType_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CsCommentReader.CreateEnumComment(typeof(string)));
    }

    /// <summary>
    /// 验证枚举类型参数为 null 时抛出参数异常。
    /// </summary>
    [Fact]
    public void CreateEnumComment_NullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            CsCommentReader.CreateEnumComment(null));
    }

    /// <summary>
    /// 验证枚举注释读取结果包含每个枚举成员。
    /// </summary>
    [Fact]
    public void CreateEnumComment_ValidEnum_ReturnsDictionaryWithAllValues()
    {
        // Act
        var dict = CsCommentReader.CreateEnumComment(typeof(SampleColor));

        // Assert
        Assert.NotNull(dict);
        Assert.True(dict.ContainsKey(SampleColor.Red));
        Assert.True(dict.ContainsKey(SampleColor.Green));
        Assert.True(dict.ContainsKey(SampleColor.Blue));
    }

    /// <summary>
    /// 验证枚举成员存在 XML 摘要时优先使用 XML 文案。
    /// </summary>
    [Fact]
    public void CreateEnumComment_FieldWithXmlSummary_UsesXmlSummary()
    {
        // Act — SampleColor.Green 有 [Description("绿")]
        var dict = CsCommentReader.CreateEnumComment(typeof(SampleColor));

        // Assert
        Assert.Equal("绿色", dict[SampleColor.Green]);
    }

    /// <summary>
    /// 验证枚举成员没有 XML 摘要时使用 <see cref="DescriptionAttribute"/> 文案。
    /// </summary>
    [Fact]
    public void CreateEnumComment_FieldWithDescription_UsesDescriptionAttributeValue()
    {
        // Act
        var dict = CsCommentReader.CreateEnumComment(typeof(FallbackColor));

        // Assert
        Assert.Equal("绿", dict[FallbackColor.Green]);
    }

    /// <summary>
    /// 验证枚举成员缺少 XML 摘要和描述特性时回退到成员名称。
    /// </summary>
    [Fact]
    public void CreateEnumComment_FieldWithoutDescription_FallsBackToName()
    {
        // Act
        var dict = CsCommentReader.CreateEnumComment(typeof(FallbackColor));

        // Assert
        Assert.Equal("Red", dict[FallbackColor.Red]);
    }

    #endregion

    #region Create(Type) — XML 注释读取

    /// <summary>
    /// 验证读取器可取得类型的 XML 摘要。
    /// </summary>
    [Fact]
    public void Create_Type_WithXmlDoc_ReturnsSummaryNotNull()
    {
        // Arrange — CsCommentReader 自身有 XML 文档
        var type = typeof(CsCommentReader);

        // Act
        var comments = CsCommentReader.Create(type);

        // Assert
        Assert.NotNull(comments);
        Assert.False(string.IsNullOrWhiteSpace(comments.Summary));
    }

    /// <summary>
    /// 验证方法信息通过成员重载分发后可正常读取。
    /// </summary>
    [Fact]
    public void Create_MemberInfo_Method_DispatchesCorrectly()
    {
        // Arrange
        MemberInfo member = typeof(CsCommentReader).GetMethod(
            nameof(CsCommentReader.Create),
            new[] { typeof(Type) });

        // Act
        var comments = CsCommentReader.Create(member);

        // Assert
        Assert.NotNull(comments);
        Assert.Equal("读取指定类型的文档注释。", comments.Summary);
    }

    /// <summary>
    /// 验证嵌套泛型类型可通过 <see cref="MemberInfo"/> 重载读取 XML 注释。
    /// </summary>
    [Fact]
    public void Create_NestedGenericType_WithXmlDocs_ReturnsComments()
    {
        // Arrange
        var nestedType = typeof(GenericCommentFixture<>).GetNestedType("Nested`1");

        // Act
        var comments = CsCommentReader.Create((MemberInfo)nestedType);

        // Assert
        Assert.NotNull(comments);
        Assert.Equal("测试嵌套泛型类型。", comments.Summary);
    }

    /// <summary>
    /// 测试目的：验证类型、字段、事件、属性、索引器和构造函数均能命中 XML 成员。
    /// </summary>
    [Fact]
    public void Create_MemberKinds_WithXmlDocs_ReturnsExpectedComments()
    {
        // Arrange
        var type = typeof(CommentFixture);
        var constructor = type.GetConstructor(new[] { typeof(string) });
        var staticConstructor = type.TypeInitializer;
        var eventInfo = type.GetEvent(nameof(CommentFixture.Changed));
        var property = type.GetProperty(nameof(CommentFixture.Name));
        var indexer = type.GetProperty("Item", new[] { typeof(int) });
        var field = type.GetField(nameof(CommentFixture.Field));

        // Act
        var typeComments = CsCommentReader.Create(type);
        var constructorComments = CsCommentReader.Create(constructor);
        var staticConstructorComments = CsCommentReader.Create(staticConstructor);
        var eventComments = CsCommentReader.Create(eventInfo);
        var propertyComments = CsCommentReader.Create(property);
        var indexerComments = CsCommentReader.Create(indexer);
        var fieldComments = CsCommentReader.Create(field);
        var basePropertyComments = CsCommentReader.Create(type.GetProperty(nameof(CommentFixture.Label)));
        var baseEventComments = CsCommentReader.Create(type.GetEvent(nameof(CommentFixture.BaseChanged)));

        // Assert
        Assert.Equal("注释读取测试夹具。", typeComments.Summary);
        Assert.Equal("带参数构造函数。", constructorComments.Summary);
        Assert.Equal("静态构造函数。", staticConstructorComments.Summary);
        Assert.Equal("测试事件。", eventComments.Summary);
        Assert.Equal("测试属性。", propertyComments.Summary);
        Assert.Equal("索引器值。", indexerComments.Summary);
        Assert.Equal("实例字段。", fieldComments.Summary);
        Assert.Equal("基类属性。", basePropertyComments.Summary);
        Assert.Equal("基类事件。", baseEventComments.Summary);
    }

    /// <summary>
    /// 测试目的：验证结构化标签、属性、嵌套 XML 和原始成员内容均被保留。
    /// </summary>
    [Fact]
    public void Create_RenderMethod_WithStructuredXml_PreservesElements()
    {
        // Arrange
        var method = typeof(CommentFixture).GetMethod(nameof(CommentFixture.Render));

        // Act
        var comments = CsCommentReader.Create(method);

        // Assert
        Assert.Contains("结构化文档样例。", comments.Summary);
        Assert.Contains("这是第二段内容。", comments.Summary);
        Assert.Contains("第一项。", comments.Summary);
        Assert.Contains("第二项。", comments.Summary);
        Assert.Contains("<example>", comments.RawXml);
        Assert.Contains("<paramref", comments.Param["value"].RawXml);
        Assert.Contains("<see", comments.RawXml);
        Assert.Contains("<list", comments.RawXml);
        Assert.Equal(new[] { "summary", "remarks", "param", "returns", "seealso" },
            comments.Elements.Select(element => element.Name).ToArray());
        Assert.Single(comments.GetElements("seealso"));
    }

    /// <summary>
    /// 测试目的：验证泛型参数、数组、引用参数、指针和转换运算符的 member ID 均能命中。
    /// </summary>
    [Fact]
    public void Create_ComplexMemberIds_WithCompilerXml_ReturnsComments()
    {
        // Arrange
        var type = typeof(CommentFixture);
        var generic = type.GetMethod(nameof(CommentFixture.Generic));
        var complex = type.GetMethod(nameof(CommentFixture.Complex));
        var pointer = type.GetMethod(nameof(CommentFixture.Pointer));
        var arrays = type.GetMethod(nameof(CommentFixture.Arrays));
        var implicitOperator = type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static);
        var explicitOperator = type.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.Static);
        var checkedExplicitOperator = type.GetMethod("op_CheckedExplicit", BindingFlags.Public | BindingFlags.Static);

        // Act
        var genericComments = CsCommentReader.Create(generic);
        var complexComments = CsCommentReader.Create(complex);
        var pointerComments = CsCommentReader.Create(pointer);
        var arrayComments = CsCommentReader.Create(arrays);
        var implicitComments = CsCommentReader.Create(implicitOperator);
        var explicitComments = CsCommentReader.Create(explicitOperator);
        var checkedExplicitComments = CsCommentReader.Create(checkedExplicitOperator);

        // Assert
        Assert.Equal("泛型参数样例。", genericComments.Summary);
        Assert.Equal("复杂参数样例。", complexComments.Summary);
        Assert.Equal("指针参数样例。", pointerComments.Summary);
        Assert.Equal("数组参数样例。", arrayComments.Summary);
        Assert.Equal("隐式转换样例。", implicitComments.Summary);
        Assert.Equal("显式转换样例。", explicitComments.Summary);
        Assert.Equal("checked 显式转换样例。", checkedExplicitComments.Summary);
    }

    /// <summary>
    /// 测试目的：验证构造泛型参数使用编译器生成的 XML member ID。
    /// </summary>
    [Fact]
    public void Create_ConstructedGenericParameter_WithCompilerXml_ReturnsComments()
    {
        // Arrange
        var method = typeof(CommentFixture).GetMethod(nameof(CommentFixture.ConstructedGenericParameter));

        // Act
        var comments = CsCommentReader.Create(method);

        // Assert
        Assert.Equal("构造泛型参数样例。", comments.Summary);
    }

    /// <summary>
    /// 测试目的：验证构造嵌套泛型参数使用编译器生成的 XML member ID。
    /// </summary>
    [Fact]
    public void Create_NestedConstructedGenericParameter_WithCompilerXml_ReturnsComments()
    {
        // Arrange
        var method = typeof(CommentFixture).GetMethod(nameof(CommentFixture.NestedConstructedGenericParameter));

        // Act
        var comments = CsCommentReader.Create(method);

        // Assert
        Assert.Equal("构造嵌套泛型参数样例。", comments.Summary);
    }

    /// <summary>
    /// 测试目的：验证反射创建的非 SZ 一维数组使用 XML 文档 ID 的星号表示。
    /// </summary>
    [Fact]
    public void CreateTypeName_NonSzArray_UsesStarArraySyntax()
    {
        // Arrange
        var arrayType = typeof(int).MakeArrayType(1);

        // Act
        var typeName = CsCommentId.GetTypeName(arrayType);

        // Assert
        Assert.Equal("System.Int32[*]", typeName);
    }

    /// <summary>
    /// 测试目的：验证动态构造泛型成员在无法依赖 MetadataToken 时仍能回到定义签名。
    /// </summary>
    [Fact]
    public void Create_DynamicConstructedGenericMember_UsesDefinitionSignature()
    {
        // Arrange
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("Bing.Utils.Comments.DynamicFixture"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Main");
        var typeBuilder = module.DefineType("DynamicGeneric`1", TypeAttributes.Public);
        var genericParameters = typeBuilder.DefineGenericParameters("T");
        var methodBuilder = typeBuilder.DefineMethod("Read", MethodAttributes.Public, typeof(void), new[] { genericParameters[0] });
        methodBuilder.GetILGenerator().Emit(OpCodes.Ret);
        var constructedType = typeBuilder.CreateType().MakeGenericType(typeof(string));
        var method = constructedType.GetMethod("Read");

        // Act
        var memberId = CsCommentId.Create(method);

        // Assert
        Assert.Equal("M:DynamicGeneric`1.Read(`0)", memberId);
    }

    /// <summary>
    /// 测试目的：验证基类、接口和显式接口实现的 inheritdoc 能补齐本地注释。
    /// </summary>
    [Fact]
    public void Create_InheritedMembers_WithInheritdoc_UsesSourceComments()
    {
        // Arrange
        var type = typeof(CommentFixture);
        var overrideMethod = type.GetMethod(nameof(CommentFixture.Convert));
        var map = type.GetInterfaceMap(typeof(ICommentContract<int>));
        var explicitMethod = map.TargetMethods.Single(method => method.Name.Contains("ICommentContract") &&
                                     method.Name.EndsWith(".Transform"));
        var explicitProperty = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(property => property.Name.Contains("ICommentContract") && property.Name.EndsWith(".Label"));
        var explicitEvent = type.GetEvents(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(@event => @event.Name.Contains("ICommentContract") && @event.Name.EndsWith(".ChangedContract"));

        // Act
        var overrideComments = CsCommentReader.Create(overrideMethod);
        var explicitComments = CsCommentReader.Create(explicitMethod);
        var explicitPropertyComments = CsCommentReader.Create(explicitProperty);
        var explicitEventComments = CsCommentReader.Create(explicitEvent);

        // Assert
        Assert.Equal("基类操作。", overrideComments.Summary);
        Assert.Equal("基类输入。", overrideComments.Param["value"].Text);
        Assert.Equal("接口转换操作。", explicitComments.Summary);
        Assert.Equal("接口属性。", explicitPropertyComments.Summary);
        Assert.Equal("接口事件。", explicitEventComments.Summary);
    }

    /// <summary>
    /// 测试目的：验证嵌套泛型类型和方法参数使用编译器的类型参数编号。
    /// </summary>
    [Fact]
    public void Create_NestedGenericMember_WithCompilerXml_ReturnsComments()
    {
        // Arrange
        var nestedType = typeof(GenericCommentFixture<>).GetNestedType("Nested`1");
        var method = nestedType.GetMethod(nameof(GenericCommentFixture<int>.Nested<string>.Join));

        // Act
        var typeComments = CsCommentReader.Create(nestedType);
        var methodComments = CsCommentReader.Create(method);

        // Assert
        Assert.Equal("测试嵌套泛型类型。", typeComments.Summary);
        Assert.Equal("返回外层和内层类型参数。", methodComments.Summary);
    }

    /// <summary>
    /// 测试目的：验证 inheritdoc 的显式来源和 path 过滤能够只继承指定元素。
    /// </summary>
    [Fact]
    public void Create_ExplicitInheritdoc_WithPath_UsesFilteredSource()
    {
        // Arrange
        var method = typeof(ExplicitInheritCommentFixture).GetMethod(nameof(ExplicitInheritCommentFixture.Convert));

        // Act
        var comments = CsCommentReader.Create(method);

        // Assert
        Assert.Equal("基类操作。", comments.Summary);
        Assert.Null(comments.Param["value"]);
        Assert.Null(comments.Returns);
    }

    /// <summary>
    /// 测试目的：验证 inheritdoc path 支持同时选择多个来源元素。
    /// </summary>
    [Fact]
    public void Create_ExplicitInheritdoc_WithUnionPath_UsesAllSelectedElements()
    {
        // Arrange
        var method = typeof(ExplicitInheritPathCommentFixture).GetMethod(nameof(ExplicitInheritPathCommentFixture.Convert));

        // Act
        var comments = CsCommentReader.Create(method);

        // Assert
        Assert.Equal("基类操作。", comments.Summary);
        Assert.Equal("基类输出。", comments.Returns);
    }

    /// <summary>
    /// 测试目的：验证非法 inheritdoc path 会保留原节点且不伪造摘要。
    /// </summary>
    [Fact]
    public void Create_ExplicitInheritdoc_WithInvalidPath_PreservesNode()
    {
        // Arrange
        var method = typeof(InvalidInheritPathCommentFixture).GetMethod(nameof(InvalidInheritPathCommentFixture.Convert));

        // Act
        var comments = CsCommentReader.Create(method);

        // Assert
        Assert.NotNull(comments);
        Assert.Null(comments.Summary);
        Assert.Contains(comments.Elements, element => element.Name == "inheritdoc");
        Assert.Single(comments.UnresolvedInheritdoc);
        Assert.Equal("[", comments.UnresolvedInheritdoc[0].Path);
    }

    /// <summary>
    /// 测试目的：验证 path 选中文本节点时不抛异常且不伪造成员摘要。
    /// </summary>
    [Fact]
    public void Create_ExplicitInheritdoc_WithTextPath_PreservesNode()
    {
        // Arrange
        var method = typeof(TextInheritPathCommentFixture).GetMethod(nameof(TextInheritPathCommentFixture.Convert));

        // Act
        var comments = CsCommentReader.Create(method);

        // Assert
        Assert.NotNull(comments);
        Assert.Null(comments.Summary);
        Assert.Contains(comments.Elements, element => element.Name == "inheritdoc");
        Assert.NotEmpty(comments.UnresolvedInheritdoc);
    }

    /// <summary>
    /// 测试目的：验证类型级 inheritdoc 能作为成员缺失注释时的来源。
    /// </summary>
    [Fact]
    public void Create_TypeInheritdoc_PropagatesToMember()
    {
        // Arrange
        var inheritedProperty = typeof(TypeInheritCommentFixture).GetProperty(nameof(TypeInheritCommentFixture.Name));
        var missingProperty = typeof(TypeInheritCommentFixture).GetProperty(nameof(TypeInheritCommentFixture.Local));

        // Act
        var inheritedComments = CsCommentReader.Create(inheritedProperty);
        var missingComments = CsCommentReader.Create(missingProperty);

        // Assert
        Assert.Equal("类型级继承内容。", inheritedComments.Summary);
        Assert.NotNull(missingComments);
        Assert.Null(missingComments.Summary);
        Assert.Contains(missingComments.Elements, element => element.Name == "inheritdoc");
    }

    /// <summary>
    /// 测试目的：验证 summary 内的 inheritdoc 只替换为来源元素的内容节点。
    /// </summary>
    [Fact]
    public void Create_InlineInheritdoc_MergesSourceContent()
    {
        // Arrange
        var method = typeof(InlineCommentFixture).GetMethod(nameof(InlineCommentFixture.Describe));

        // Act
        var comments = CsCommentReader.Create(method);

        // Assert
        Assert.Contains("前缀。", comments.Summary);
        Assert.Contains("基类内联内容。", comments.Summary);
        Assert.Contains("后缀。", comments.Summary);
        Assert.DoesNotContain("inheritdoc", comments.RawXml);
    }

    /// <summary>
    /// 测试目的：验证显式 inheritdoc 循环会终止并保留未解析节点。
    /// </summary>
    [Fact]
    public void Create_InheritdocCycle_DoesNotRecurseForever()
    {
        // Arrange
        var method = typeof(InheritCycleFixture).GetMethod(nameof(InheritCycleFixture.First));

        // Act
        var comments = CsCommentReader.Create(method);

        // Assert
        Assert.NotNull(comments);
        Assert.Null(comments.Summary);
        Assert.Contains(comments.Elements, element => element.Name == "inheritdoc");
    }

    /// <summary>
    /// 测试目的：验证基类和接口来源按优先级合并，基类单值优先且接口缺失项补齐。
    /// </summary>
    [Fact]
    public void Create_MultipleInheritdocSources_MergesWithStablePriority()
    {
        // Arrange
        var method = typeof(MultiSourceCommentFixture).GetMethod(nameof(MultiSourceCommentFixture.Merge));

        // Act
        var comments = CsCommentReader.Create(method);

        // Assert
        Assert.Equal("基类摘要。", comments.Summary);
        Assert.Equal("接口补充备注。", comments.Remarks);
        Assert.Equal("基类返回值。", comments.Returns);
        Assert.Equal("基类参数。", comments.Param["value"].Text);
        Assert.Contains(comments.Exception, exception => exception.Cref == "System.InvalidOperationException");
    }

    /// <summary>
    /// 测试目的：验证缺少成员注释时返回 null，且不会污染后续读取状态。
    /// </summary>
    [Fact]
    public void Create_MissingMember_DoesNotPoisonFollowingRead()
    {
        // Act
        var missing = CsCommentReader.Create(typeof(FallbackColor));
        var existing = CsCommentReader.Create(typeof(CommentFixture));

        // Assert
        Assert.Null(missing);
        Assert.NotNull(existing);
    }

    #endregion
}
