using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "AttributeDefined")]
public class AttributeDefinedTest
{
    public Type AttributeOne = typeof(ModelOneAttribute);
    public Type AttributeTwo = typeof(ModelTwoAttribute);
    public Type AttributeThree = typeof(ModelThreeAttribute);

    public Type ModelWithAttr;
    public ConstructorInfo ConstructorForModelWithAttrWithoutParam;
    public ConstructorInfo ConstructorForModelWithAttrWithOneParam;
    public PropertyInfo PropertyForModelWithAttr;
    public FieldInfo FieldForModelWithAttr;
    public MethodInfo MethodForModelWithAttr;
    public ParameterInfo ParameterOfConstructorForModelWithAttrWithOneParam;

    public Type ModelWithoutAttr;
    public ConstructorInfo ConstructorForModelWithoutAttrWithoutParam;
    public ConstructorInfo ConstructorForModelWithoutAttrWithOneParam;
    public PropertyInfo PropertyForModelWithoutAttr;
    public FieldInfo FieldForModelWithoutAttr;
    public MethodInfo MethodForModelWithoutAttr;
    public ParameterInfo ParameterOfConstructorForModelWithoutAttrWithOneParam;

    public Type ModelWrapper;
    public ConstructorInfo ConstructorForModelWrapperWithoutParam;
    public ConstructorInfo ConstructorForModelWrapperWithOneParam;
    public PropertyInfo PropertyForModelWrapper;
    public FieldInfo FieldForModelWrapper;
    public MethodInfo MethodForModelWrapper;
    public ParameterInfo ParameterOfConstructorForModelWrapperWithOneParam;

    /// <summary>
    /// 测试初始化
    /// </summary>
    public AttributeDefinedTest()
    {
        ModelWithAttr = typeof(NormalWithAttrClass);
        ModelWithoutAttr = typeof(NormalWithoutAttrClass);
        ModelWrapper = typeof(NormalWithAttrClassWrapper);

        var ctors = ModelWithAttr.GetConstructors();
        ConstructorForModelWithAttrWithoutParam = ctors.Single(x => !x.GetParameters().Any());
        ConstructorForModelWithAttrWithOneParam = ctors.Single(x => x.GetParameters().Length == 1);
        var members = ModelWithAttr.GetMembers();
        PropertyForModelWithAttr = members.Single(x => x.Name == nameof(NormalWithAttrClass.Nice)) as PropertyInfo;
        FieldForModelWithAttr = members.Single(x => x.Name == nameof(NormalWithAttrClass.Good)) as FieldInfo;
        MethodForModelWithAttr = members.Single(x => x.Name == nameof(NormalWithAttrClass.GetAwesome)) as MethodInfo;
        ParameterOfConstructorForModelWithAttrWithOneParam = ConstructorForModelWithAttrWithOneParam.GetParameters()[0];

        ctors = ModelWithoutAttr.GetConstructors();
        ConstructorForModelWithoutAttrWithoutParam = ctors.Single(x => !x.GetParameters().Any());
        ConstructorForModelWithoutAttrWithOneParam = ctors.Single(x => x.GetParameters().Length == 1);
        members = ModelWithoutAttr.GetMembers();
        PropertyForModelWithoutAttr = members.Single(x => x.Name == nameof(NormalWithoutAttrClass.Nice)) as PropertyInfo;
        FieldForModelWithoutAttr = members.Single(x => x.Name == nameof(NormalWithoutAttrClass.Good)) as FieldInfo;
        MethodForModelWithoutAttr = members.Single(x => x.Name == nameof(NormalWithoutAttrClass.GetAwesome)) as MethodInfo;
        ParameterOfConstructorForModelWithoutAttrWithOneParam = ConstructorForModelWithoutAttrWithOneParam.GetParameters()[0];

        ctors = ModelWrapper.GetConstructors();
        ConstructorForModelWrapperWithoutParam = ctors.Single(x => !x.GetParameters().Any());
        ConstructorForModelWrapperWithOneParam = ctors.Single(x => x.GetParameters().Length == 1);
        members = ModelWrapper.GetMembers();
        PropertyForModelWrapper = members.Single(x => x.Name == nameof(NormalWithAttrClassWrapper.Nice)) as PropertyInfo;
        FieldForModelWrapper = members.Single(x => x.Name == nameof(NormalWithAttrClassWrapper.Good)) as FieldInfo;
        MethodForModelWrapper = members.Single(x => x.Name == nameof(NormalWithAttrClassWrapper.GetAwesome)) as MethodInfo;
        ParameterOfConstructorForModelWrapperWithOneParam = ConstructorForModelWrapperWithOneParam.GetParameters()[0];
    }

    /// <summary>
    /// 测试 - 类型 - 定义指定特性
    /// </summary>
    [Fact]
    public void Test_DirectType_AttributeDefined()
    {
        TypeReflections.IsAttributeDefined(ModelWithAttr, AttributeOne).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(ModelWithAttr, AttributeTwo).ShouldBeFalse();

        TypeReflections.IsAttributeDefined(PropertyForModelWithAttr, AttributeOne).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(PropertyForModelWithAttr, AttributeTwo).ShouldBeTrue();

        TypeReflections.IsAttributeDefined(FieldForModelWithAttr, AttributeOne).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(FieldForModelWithAttr, AttributeTwo).ShouldBeTrue();

        TypeReflections.IsAttributeDefined(MethodForModelWithAttr, AttributeOne).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(MethodForModelWithAttr, AttributeTwo).ShouldBeTrue();

        TypeReflections.IsAttributeDefined(ConstructorForModelWithAttrWithoutParam, AttributeOne).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(ConstructorForModelWithAttrWithoutParam, AttributeTwo).ShouldBeTrue();

        TypeReflections.IsAttributeDefined(ConstructorForModelWithAttrWithOneParam, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ConstructorForModelWithAttrWithOneParam, AttributeTwo).ShouldBeFalse();

        TypeReflections.IsAttributeDefined(ParameterOfConstructorForModelWithAttrWithOneParam, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ParameterOfConstructorForModelWithAttrWithOneParam, AttributeTwo).ShouldBeTrue();

        Types.IsAttributeDefined(ModelWithAttr, AttributeOne).ShouldBeTrue();
        Types.IsAttributeDefined(ModelWithAttr, AttributeTwo).ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 泛型类型 - 定义指定特性
    /// </summary>
    [Fact]
    public void Test_GenericType_AttributeDefined()
    {
        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ModelWithAttr).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ModelWithAttr).ShouldBeFalse();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(PropertyForModelWithAttr).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(PropertyForModelWithAttr).ShouldBeTrue();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(FieldForModelWithAttr).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(FieldForModelWithAttr).ShouldBeTrue();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(MethodForModelWithAttr).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(MethodForModelWithAttr).ShouldBeTrue();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ConstructorForModelWithAttrWithoutParam).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ConstructorForModelWithAttrWithoutParam).ShouldBeTrue();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ConstructorForModelWithAttrWithOneParam).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ConstructorForModelWithAttrWithOneParam).ShouldBeFalse();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ParameterOfConstructorForModelWithAttrWithOneParam).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ParameterOfConstructorForModelWithAttrWithOneParam).ShouldBeTrue();

        Types.IsAttributeDefined<ModelOneAttribute>(ModelWithAttr).ShouldBeTrue();
        Types.IsAttributeDefined<ModelTwoAttribute>(ModelWithAttr).ShouldBeFalse();

        Types.IsAttributeDefined<NormalWithAttrClass, ModelOneAttribute>().ShouldBeTrue();
        Types.IsAttributeDefined<NormalWithAttrClass, ModelTwoAttribute>().ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 类型 - 未定义指定特性
    /// </summary>
    [Fact]
    public void Test_DirectType_AttributeNotDefined()
    {
        TypeReflections.IsAttributeDefined(ModelWithoutAttr, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ModelWithoutAttr, AttributeTwo).ShouldBeFalse();

        TypeReflections.IsAttributeDefined(PropertyForModelWithoutAttr, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(PropertyForModelWithoutAttr, AttributeTwo).ShouldBeFalse();

        TypeReflections.IsAttributeDefined(FieldForModelWithoutAttr, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(FieldForModelWithoutAttr, AttributeTwo).ShouldBeFalse();

        TypeReflections.IsAttributeDefined(MethodForModelWithoutAttr, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(MethodForModelWithoutAttr, AttributeTwo).ShouldBeFalse();

        TypeReflections.IsAttributeDefined(ConstructorForModelWithoutAttrWithoutParam, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ConstructorForModelWithoutAttrWithoutParam, AttributeTwo).ShouldBeFalse();

        TypeReflections.IsAttributeDefined(ConstructorForModelWithoutAttrWithOneParam, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ConstructorForModelWithoutAttrWithOneParam, AttributeTwo).ShouldBeFalse();

        TypeReflections.IsAttributeDefined(ParameterOfConstructorForModelWithoutAttrWithOneParam, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ParameterOfConstructorForModelWithoutAttrWithOneParam, AttributeTwo).ShouldBeFalse();

        Types.IsAttributeDefined(ModelWithoutAttr, AttributeOne).ShouldBeFalse();
        Types.IsAttributeDefined(ModelWithoutAttr, AttributeTwo).ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 泛型类型 - 未定义指定特性
    /// </summary>
    [Fact]
    public void Test_GenericType_AttributeNotDefined()
    {
        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ModelWithoutAttr).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ModelWithoutAttr).ShouldBeFalse();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(PropertyForModelWithoutAttr).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(PropertyForModelWithoutAttr).ShouldBeFalse();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(FieldForModelWithoutAttr).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(FieldForModelWithoutAttr).ShouldBeFalse();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(MethodForModelWithoutAttr).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(MethodForModelWithoutAttr).ShouldBeFalse();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ConstructorForModelWithoutAttrWithoutParam).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ConstructorForModelWithoutAttrWithoutParam).ShouldBeFalse();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ConstructorForModelWithoutAttrWithOneParam).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ConstructorForModelWithoutAttrWithOneParam).ShouldBeFalse();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ParameterOfConstructorForModelWithoutAttrWithOneParam).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ParameterOfConstructorForModelWithoutAttrWithOneParam).ShouldBeFalse();

        Types.IsAttributeDefined<ModelOneAttribute>(ModelWithoutAttr).ShouldBeFalse();
        Types.IsAttributeDefined<ModelTwoAttribute>(ModelWithoutAttr).ShouldBeFalse();

        Types.IsAttributeDefined<NormalWithoutAttrClass, ModelOneAttribute>().ShouldBeFalse();
        Types.IsAttributeDefined<NormalWithoutAttrClass, ModelTwoAttribute>().ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 类型 - 定义指定特性 - 继承选项
    /// </summary>
    [Fact]
    public void Test_DirectType_AttributeDefined_Inherit()
    {
        TypeReflections.IsAttributeDefined(ModelWrapper, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ModelWrapper, AttributeTwo).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ModelWrapper, AttributeThree).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ModelWrapper, AttributeOne, ReflectionOptions.Inherit).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(ModelWrapper, AttributeTwo, ReflectionOptions.Inherit).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ModelWrapper, AttributeThree, ReflectionOptions.Inherit).ShouldBeTrue();

        TypeReflections.IsAttributeDefined(PropertyForModelWrapper, AttributeOne).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(PropertyForModelWrapper, AttributeTwo).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(PropertyForModelWrapper, AttributeOne, ReflectionOptions.Inherit).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(PropertyForModelWrapper, AttributeTwo, ReflectionOptions.Inherit).ShouldBeTrue();

        TypeReflections.IsAttributeDefined(FieldForModelWrapper, AttributeOne).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(FieldForModelWrapper, AttributeTwo).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(FieldForModelWrapper, AttributeOne, ReflectionOptions.Inherit).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(FieldForModelWrapper, AttributeTwo, ReflectionOptions.Inherit).ShouldBeTrue();

        TypeReflections.IsAttributeDefined(MethodForModelWrapper, AttributeOne).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(MethodForModelWrapper, AttributeTwo).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(MethodForModelWrapper, AttributeOne, ReflectionOptions.Inherit).ShouldBeTrue();
        TypeReflections.IsAttributeDefined(MethodForModelWrapper, AttributeTwo, ReflectionOptions.Inherit).ShouldBeTrue();

        TypeReflections.IsAttributeDefined(ConstructorForModelWrapperWithoutParam, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ConstructorForModelWrapperWithoutParam, AttributeTwo).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ConstructorForModelWrapperWithoutParam, AttributeOne, ReflectionOptions.Inherit).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ConstructorForModelWrapperWithoutParam, AttributeTwo, ReflectionOptions.Inherit).ShouldBeFalse();

        TypeReflections.IsAttributeDefined(ConstructorForModelWrapperWithOneParam, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ConstructorForModelWrapperWithOneParam, AttributeTwo).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ConstructorForModelWrapperWithOneParam, AttributeOne, ReflectionOptions.Inherit).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ConstructorForModelWrapperWithOneParam, AttributeTwo, ReflectionOptions.Inherit).ShouldBeFalse();

        TypeReflections.IsAttributeDefined(ParameterOfConstructorForModelWrapperWithOneParam, AttributeOne).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ParameterOfConstructorForModelWrapperWithOneParam, AttributeTwo).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ParameterOfConstructorForModelWrapperWithOneParam, AttributeThree).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ParameterOfConstructorForModelWrapperWithOneParam, AttributeOne, ReflectionOptions.Inherit).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ParameterOfConstructorForModelWrapperWithOneParam, AttributeTwo, ReflectionOptions.Inherit).ShouldBeFalse();
        TypeReflections.IsAttributeDefined(ParameterOfConstructorForModelWrapperWithOneParam, AttributeThree, ReflectionOptions.Inherit).ShouldBeFalse();

        Types.IsAttributeDefined(ModelWrapper, AttributeOne).ShouldBeFalse();
        Types.IsAttributeDefined(ModelWrapper, AttributeTwo).ShouldBeFalse();
        Types.IsAttributeDefined(ModelWrapper, AttributeThree).ShouldBeFalse();
        Types.IsAttributeDefined(ModelWrapper, AttributeOne, ReflectionOptions.Inherit).ShouldBeTrue();
        Types.IsAttributeDefined(ModelWrapper, AttributeTwo, ReflectionOptions.Inherit).ShouldBeFalse();
        Types.IsAttributeDefined(ModelWrapper, AttributeThree, ReflectionOptions.Inherit).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 泛型类型 - 定义指定特性 - 继承选项
    /// </summary>
    [Fact]
    public void Test_GenericType_AttributeDefined_Inherit()
    {
        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ModelWrapper).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ModelWrapper).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelThreeAttribute>(ModelWrapper).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ModelWrapper, ReflectionOptions.Inherit).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ModelWrapper, ReflectionOptions.Inherit).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelThreeAttribute>(ModelWrapper, ReflectionOptions.Inherit).ShouldBeTrue();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(PropertyForModelWrapper).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(PropertyForModelWrapper).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelOneAttribute>(PropertyForModelWrapper, ReflectionOptions.Inherit).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(PropertyForModelWrapper, ReflectionOptions.Inherit).ShouldBeTrue();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(FieldForModelWrapper).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(FieldForModelWrapper).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelOneAttribute>(FieldForModelWrapper, ReflectionOptions.Inherit).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(FieldForModelWrapper, ReflectionOptions.Inherit).ShouldBeTrue();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(MethodForModelWrapper).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(MethodForModelWrapper).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelOneAttribute>(MethodForModelWrapper, ReflectionOptions.Inherit).ShouldBeTrue();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(MethodForModelWrapper, ReflectionOptions.Inherit).ShouldBeTrue();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ConstructorForModelWrapperWithoutParam).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ConstructorForModelWrapperWithoutParam).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ConstructorForModelWrapperWithoutParam, ReflectionOptions.Inherit).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ConstructorForModelWrapperWithoutParam, ReflectionOptions.Inherit).ShouldBeFalse();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ConstructorForModelWrapperWithOneParam).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ConstructorForModelWrapperWithOneParam).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ConstructorForModelWrapperWithOneParam, ReflectionOptions.Inherit).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ConstructorForModelWrapperWithOneParam, ReflectionOptions.Inherit).ShouldBeFalse();

        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ParameterOfConstructorForModelWrapperWithOneParam).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ParameterOfConstructorForModelWrapperWithOneParam).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelThreeAttribute>(ParameterOfConstructorForModelWrapperWithOneParam).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelOneAttribute>(ParameterOfConstructorForModelWrapperWithOneParam, ReflectionOptions.Inherit).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelTwoAttribute>(ParameterOfConstructorForModelWrapperWithOneParam, ReflectionOptions.Inherit).ShouldBeFalse();
        TypeReflections.IsAttributeDefined<ModelThreeAttribute>(ParameterOfConstructorForModelWrapperWithOneParam, ReflectionOptions.Inherit).ShouldBeFalse();

        Types.IsAttributeDefined<ModelOneAttribute>(ModelWrapper).ShouldBeFalse();
        Types.IsAttributeDefined<ModelTwoAttribute>(ModelWrapper).ShouldBeFalse();
        Types.IsAttributeDefined<ModelThreeAttribute>(ModelWrapper).ShouldBeFalse();
        Types.IsAttributeDefined<ModelOneAttribute>(ModelWrapper, ReflectionOptions.Inherit).ShouldBeTrue();
        Types.IsAttributeDefined<ModelTwoAttribute>(ModelWrapper, ReflectionOptions.Inherit).ShouldBeFalse();
        Types.IsAttributeDefined<ModelThreeAttribute>(ModelWrapper, ReflectionOptions.Inherit).ShouldBeTrue();

        Types.IsAttributeDefined<NormalWithAttrClassWrapper, ModelOneAttribute>().ShouldBeFalse();
        Types.IsAttributeDefined<NormalWithAttrClassWrapper, ModelTwoAttribute>().ShouldBeFalse();
        Types.IsAttributeDefined<NormalWithAttrClassWrapper, ModelThreeAttribute>().ShouldBeFalse();
        Types.IsAttributeDefined<NormalWithAttrClassWrapper, ModelOneAttribute>(ReflectionOptions.Inherit).ShouldBeTrue();
        Types.IsAttributeDefined<NormalWithAttrClassWrapper, ModelTwoAttribute>(ReflectionOptions.Inherit).ShouldBeFalse();
        Types.IsAttributeDefined<NormalWithAttrClassWrapper, ModelThreeAttribute>(ReflectionOptions.Inherit).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 扩展方法 - 定义指定特性
    /// </summary>
    [Fact]
    public void Test_ExtensionMethod_AttributeDefined()
    {
        ModelWithAttr.IsAttributeDefined<ModelOneAttribute>().ShouldBeTrue();
        ModelWithAttr.IsAttributeDefined<ModelTwoAttribute>().ShouldBeFalse();

        ModelWithoutAttr.IsAttributeDefined<ModelOneAttribute>().ShouldBeFalse();
        ModelWithoutAttr.IsAttributeDefined<ModelTwoAttribute>().ShouldBeFalse();

        ModelWithAttr.IsAttributeNotDefined<ModelOneAttribute>().ShouldBeFalse();
        ModelWithAttr.IsAttributeNotDefined<ModelTwoAttribute>().ShouldBeTrue();

        ModelWithoutAttr.IsAttributeNotDefined<ModelOneAttribute>().ShouldBeTrue();
        ModelWithoutAttr.IsAttributeNotDefined<ModelTwoAttribute>().ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 对象 - 定义指定特性
    /// </summary>
    [Fact]
    public void Test_Object_AttributeDefined()
    {
        var objectWithAttr = new NormalWithAttrClass();
        var objectWithoutAttr = new NormalWithoutAttrClass();

        Types.IsAttributeDefined<NormalWithAttrClass, ModelOneAttribute>(objectWithAttr).ShouldBeTrue();
        Types.IsAttributeDefined<NormalWithAttrClass, ModelTwoAttribute>(objectWithAttr).ShouldBeFalse();

        Types.IsAttributeDefined<NormalWithoutAttrClass, ModelOneAttribute>(objectWithoutAttr).ShouldBeFalse();
        Types.IsAttributeDefined<NormalWithoutAttrClass, ModelTwoAttribute>(objectWithoutAttr).ShouldBeFalse();

        objectWithAttr = null;
        objectWithoutAttr = null;

        Types.IsAttributeDefined<NormalWithAttrClass, ModelOneAttribute>(objectWithAttr).ShouldBeFalse();
        Types.IsAttributeDefined<NormalWithAttrClass, ModelTwoAttribute>(objectWithAttr).ShouldBeFalse();

        Types.IsAttributeDefined<NormalWithoutAttrClass, ModelOneAttribute>(objectWithoutAttr).ShouldBeFalse();
        Types.IsAttributeDefined<NormalWithoutAttrClass, ModelTwoAttribute>(objectWithoutAttr).ShouldBeFalse();

        Types.IsAttributeDefined<NormalWithAttrClass, ModelOneAttribute>(objectWithAttr, isOptions: TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsAttributeDefined<NormalWithAttrClass, ModelTwoAttribute>(objectWithAttr, isOptions: TypeIsOptions.IgnoreNullable).ShouldBeFalse();

        Types.IsAttributeDefined<NormalWithoutAttrClass, ModelOneAttribute>(objectWithoutAttr, isOptions: TypeIsOptions.IgnoreNullable).ShouldBeFalse();
        Types.IsAttributeDefined<NormalWithoutAttrClass, ModelTwoAttribute>(objectWithoutAttr, isOptions: TypeIsOptions.IgnoreNullable).ShouldBeFalse();
    }
}