using System.Text.Json.Serialization;

namespace BingUtilsUT.Samples;

/// <summary>
/// 表示包含名称和嵌套对象的循环引用样例节点。
/// </summary>
public class A
{
    /// <summary>
    /// 获取或设置节点名称。
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 获取或设置指向 <see cref="B"/> 节点的引用。
    /// </summary>
    public B B { get; set; }
}

/// <summary>
/// 表示循环引用样例中的第二种节点。
/// </summary>
public class B
{
    /// <summary>
    /// 获取或设置节点名称。
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 获取或设置指向 <see cref="C"/> 节点的引用。
    /// </summary>
    public C C { get; set; }
}

/// <summary>
/// 表示循环引用样例中的第三种节点。
/// </summary>
public class C
{
    /// <summary>
    /// 获取或设置节点名称。
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 获取或设置指向 <see cref="A"/> 节点的引用。
    /// </summary>
    public A A { get; set; }
}

/// <summary>
/// 定义用于验证 JSON 序列化属性行为的数据样例。
/// </summary>
public class JsonTestSample : IJsonTestSample
{
    /// <summary>
    /// 获取或设置 JSON 样例的名称。
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 获取或设置用于验证私有属性序列化行为的字符串。
    /// </summary>
    private string A { get; set; }

    /// <summary>
    /// 获取或设置用于验证小写属性名称序列化行为的昵称。
    /// </summary>
    public string nickname { get; set; }

    /// <summary>
    /// 获取或设置序列化名称由 <see cref="JsonPropertyNameAttribute"/> 指定的名字。
    /// </summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }

    /// <summary>
    /// 获取或设置用于验证空值序列化行为的对象。
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// 获取或设置用于验证本地日期时间序列化行为的可空日期。
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// 获取或设置用于验证 UTC 日期时间序列化行为的可空日期。
    /// </summary>
    public DateTime? UtcDate { get; set; }

    /// <summary>
    /// 获取或设置用于验证整数序列化行为的年龄值。
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// 获取或设置用于验证布尔值序列化行为的显示标志。
    /// </summary>
    public bool IsShow { get; set; }

    /// <summary>
    /// 获取或设置用于验证枚举序列化行为的值。
    /// </summary>
    public TestEnum Enum { get; set; }

    /// <summary>
    /// 获取或设置用于验证可空枚举序列化行为的值。
    /// </summary>
    public TestEnum? NullableEnum { get; set; }

    /// <summary>
    /// 创建填充了 JSON 序列化测试数据的样例。
    /// </summary>
    /// <returns>已填充测试数据的 <see cref="JsonTestSample"/> 实例。</returns>
    public static JsonTestSample Create() =>
        new()
        {
            Name = "a",
            A = "1",
            nickname = "b",
            FirstName = "c",
            Value = null,
            Date = new DateTime(2012, 12, 12, 20, 12, 12, DateTimeKind.Local),
            UtcDate = new DateTime(2012, 12, 12, 12, 12, 12, DateTimeKind.Utc),
            Age = 1,
            IsShow = true,
        };

    /// <summary>
    /// 创建并以接口类型返回 JSON 序列化测试样例。
    /// </summary>
    /// <returns>由 <see cref="Create"/> 创建的接口样例。</returns>
    public static IJsonTestSample CreateToInterface() => Create();
}

/// <summary>
/// 定义用于验证 JSON 序列化长整数行为的数据样例。
/// </summary>
public class JsonTestSample2
{
    /// <summary>
    /// 获取或设置用于验证长整数序列化行为的值。
    /// </summary>
    public long Long { get; set; }
    /// <summary>
    /// 获取或设置用于验证可空长整数序列化行为的值。
    /// </summary>
    public long? NullableLong { get; set; }
}

/// <summary>
/// 定义 JSON 序列化测试使用的样例契约。
/// </summary>
public interface IJsonTestSample
{
    /// <summary>
    /// 获取或设置 JSON 样例的名称。
    /// </summary>
    public string Name { get; set; }
}

/// <summary>
/// 定义 JSON 序列化测试使用的枚举值。
/// </summary>
public enum TestEnum
{
    /// <summary>
    /// 表示序列化测试值 1。
    /// </summary>
    Test1 = 1,

    /// <summary>
    /// 表示序列化测试值 2。
    /// </summary>
    Test2 = 2,
}
