using Bing.Helpers;

namespace Bing.Tests.Samples.Json
{
    /// <summary>
    /// 用于验证循环引用序列化的首层样例。
    /// </summary>
    public class A
    {
        /// <summary>
        /// 获取或设置首层样例名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置循环引用链中的中间层样例。
        /// </summary>
        public B B { get; set; }
    }

    /// <summary>
    /// 用于验证循环引用序列化的中间层样例。
    /// </summary>
    public class B
    {
        /// <summary>
        /// 获取或设置中间层样例名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置循环引用链中的末层样例。
        /// </summary>
        public C C { get; set; }
    }

    /// <summary>
    /// 用于验证循环引用序列化并连接首层样例的类型。
    /// </summary>
    public class C
    {
        /// <summary>
        /// 获取或设置末层样例名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置循环引用链指向的首层样例。
        /// </summary>
        public A A { get; set; }
    }

    /// <summary>
    /// 用于验证属性命名及不同值类型序列化的 JSON 样例。
    /// </summary>
    public class JsonTestSample
    {
        /// <summary>
        /// 获取或设置用于验证大写首字母属性名的样例名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置用于验证非公开属性序列化行为的样例文本。
        /// </summary>
        private string A { get; set; }
        /// <summary>
        /// 获取或设置用于验证小写属性名的样例昵称。
        /// </summary>
        public string nickname { get; set; }
        /// <summary>
        /// 获取或设置用于验证空值序列化的样例值。
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// 获取或设置用于序列化的日期文本。
        /// </summary>
        public string Date { get; set; }
        /// <summary>
        /// 获取或设置用于验证整数序列化的样例年龄。
        /// </summary>
        public int Age { get; set; }
        /// <summary>
        /// 获取或设置用于验证布尔值序列化的显示标志。
        /// </summary>
        public bool isShow { get; set; }

        /// <summary>
        /// 创建包含固定测试数据的 JSON 样例。
        /// </summary>
        /// <returns>包含名称、日期、空值及数值属性的样例实例。</returns>
        public static JsonTestSample Create()
        {
            return new JsonTestSample()
            {
                Name = "a",
                A = "1",
                nickname = "b",
                Value = null,
                Date = Conv.ToDate("2012-1-1").ToString(),
                Age = 1,
                isShow = true
            };
        }
    }
}
