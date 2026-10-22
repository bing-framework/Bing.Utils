using System.ComponentModel;
using System.Threading.Tasks;

namespace ReflectionTrimProbe.UnrootedModels;

/// <summary>
/// 未 root 边界模型的公共基类，提供继承属性和基类常量。
/// </summary>
[Description("Unrooted boundary base type")]
public abstract class UnrootedBoundaryBase
{
    /// <summary>
    /// 用于观察公共继承属性元数据。
    /// </summary>
    public virtual int InheritedProperty { get; set; } = 89;

    /// <summary>
    /// 用于验证未 root 抽象属性的基类元数据。
    /// </summary>
    public abstract int AbstractBoundaryProperty { get; }

    /// <summary>
    /// 用于观察基类公共常量元数据。
    /// </summary>
    public const string BasePublicConstant = "Unrooted boundary base constant";

    /// <summary>
    /// 用于验证泛型字段访问按声明类型解析，而不是按运行时派生类型解析。
    /// </summary>
    public int HiddenField = 41;

    /// <summary>
    /// 用于验证未 root 覆盖方法的基类声明元数据。
    /// </summary>
    public virtual int ComputeBaseValue(int value) => value + 5;
}

/// <summary>
/// 用于验证未 root 类型继承特性和显式继承选项的模型。
/// </summary>
public sealed class UnrootedInheritanceModel : UnrootedBoundaryBase
{
    /// <summary>
    /// 满足基类抽象属性契约；该类型仍只用于继承特性边界验证。
    /// </summary>
    public override int AbstractBoundaryProperty => 131;
}

/// <summary>
/// 用于验证未 root 泛型继承链和原始类型参数解析的泛型基类。
/// </summary>
public class UnrootedGenericBase<T>
{
}

/// <summary>
/// 用于验证未 root 闭合泛型继承元数据的模型。
/// </summary>
public sealed class UnrootedGenericModel : UnrootedGenericBase<int>
{
}

/// <summary>
/// 不加入 linker root 的独立程序集边界模型，仅通过程序集和类型名称访问。
/// </summary>
[Description("Unrooted boundary type")]
public sealed class UnrootedBoundaryModel : UnrootedBoundaryBase, IUnrootedBoundaryContract
{
    private int _reflectedValue = 73;

    /// <summary>
    /// 用于观察当前类型的公共常量元数据。
    /// </summary>
    public const string PublicConstant = "Unrooted boundary constant";

    /// <summary>
    /// 用于观察未 root 模型的公共字段元数据。
    /// </summary>
    [Description("Unrooted boundary field")]
    public int PublicField = 79;

    /// <summary>
    /// 用于验证派生类型隐藏同名字段时读写声明保持一致。
    /// </summary>
    public new int HiddenField = 43;

    /// <summary>
    /// 用于观察字段布尔类型判断元数据。
    /// </summary>
    public bool BoolField = true;

    /// <summary>
    /// 用于观察字段枚举类型判断元数据。
    /// </summary>
    public DayOfWeek EnumField = DayOfWeek.Monday;

    /// <summary>
    /// 用于观察字段可空枚举类型判断元数据。
    /// </summary>
    public DayOfWeek? NullableEnumField = DayOfWeek.Tuesday;

    /// <summary>
    /// 用于观察字段数值类型判断元数据。
    /// </summary>
    public double NumberField = 2.5d;

    /// <summary>
    /// 用于观察元组成员的类型分类元数据。
    /// </summary>
    public (int Value, string Text) TupleField = (11, "tuple");

    /// <summary>
    /// 用于观察未 root 集合字段的元素类型和集合分类元数据。
    /// </summary>
    public List<int> CollectionField = new() { 13, 17 };

    /// <summary>
    /// 用于观察结构成员的类型分类元数据。
    /// </summary>
    public DateTime StructField = new(2024, 1, 1);

    /// <summary>
    /// 用于观察未 root 模型的公共属性元数据。
    /// </summary>
    [Description("Unrooted boundary property")]
    [DisplayName("Unrooted boundary display property")]
    public int PublicProperty { get; set; } = 83;

    /// <summary>
    /// 用于验证未 root 插件的属性筛选不会把 init-only setter 当作常规可写 setter。
    /// </summary>
    public int InitOnlyProperty { get; init; } = 91;

    /// <summary>
    /// 用于验证未 root 派生类型对抽象属性的实现元数据。
    /// </summary>
    public override int AbstractBoundaryProperty => 127;

    /// <summary>
    /// 用于验证未 root 覆盖方法可以解析到基类声明。
    /// </summary>
    public override int ComputeBaseValue(int value) => base.ComputeBaseValue(value) + 1;

    /// <summary>
    /// 用于观察实例属性扫描是否排除静态属性。
    /// </summary>
    public static int StaticProperty { get; set; } = 109;

    /// <summary>
    /// 用于观察仅包含公共 getter 的属性元数据。
    /// </summary>
    public int GetterOnlyProperty => 101;

    /// <summary>
    /// 用于观察仅包含公共 setter 的属性元数据；setter 不读取不存在的 getter。
    /// </summary>
    public int SetterOnlyProperty
    {
        set { }
    }

    /// <summary>
    /// 用于观察属性扫描是否排除索引器。
    /// </summary>
    public int this[int index]
    {
        get => index;
        set { }
    }

    /// <summary>
    /// 创建未显式保留的边界模型。
    /// </summary>
    public UnrootedBoundaryModel()
    {
    }

    /// <summary>
    /// 创建带字符串参数的未显式保留边界模型；参数仅用于观察构造函数签名。
    /// </summary>
    [Description("Unrooted boundary constructor")]
    public UnrootedBoundaryModel([Description("Unrooted boundary constructor parameter")] string ignored)
    {
    }

    /// <summary>
    /// 仅用于观察裁剪后非公开元数据是否仍然存在。
    /// </summary>
    private int ReflectedValue
    {
        get => _reflectedValue;
        set => _reflectedValue = value;
    }

    /// <summary>
    /// 用于观察未 root 模型的方法签名元数据。
    /// </summary>
    [Description("Unrooted boundary method")]
    public int Execute([Description("Unrooted boundary parameter")] int value) => value + PublicField;

    /// <summary>
    /// 用于观察泛型方法签名元数据和闭合调用。
    /// </summary>
    [Description("Unrooted boundary generic method")]
    public T Echo<T>(T value) => value;

    /// <summary>
    /// 用于观察未 root 的无结果 Task 返回类型。
    /// </summary>
    public Task GetTask() => Task.CompletedTask;

    /// <summary>
    /// 用于观察未 root 的带结果 Task 返回类型。
    /// </summary>
    public Task<int> GetTaskResult() => Task.FromResult(PublicField);

    /// <summary>
    /// 用于观察未 root 的无结果 ValueTask 返回类型。
    /// </summary>
    public ValueTask GetValueTask() => ValueTask.CompletedTask;

    /// <summary>
    /// 用于观察未 root 的带结果 ValueTask 返回类型。
    /// </summary>
    public ValueTask<int> GetValueTaskResult() => new(PublicField);
}

/// <summary>
/// 用于验证未 root 程序集接口图和实现类型扫描的公开契约。
/// </summary>
public interface IUnrootedBoundaryContract : IUnrootedBoundaryBaseContract
{
    int Execute(int value);
}

/// <summary>
/// 用于验证直接接口查询会排除基接口的公开契约。
/// </summary>
public interface IUnrootedBoundaryBaseContract
{
}
