namespace Bing.Tests.Samples
{
    /// <summary>
    /// 用于验证不同访问级别的属性和字段反射的样例。
    /// </summary>
    public class ReflectionsSample
    {
        /// <summary>
        /// 获取或设置第一个公开属性的样例文本。
        /// </summary>
        public string A { get; set; }
        /// <summary>
        /// 获取或设置第二个公开属性的样例文本。
        /// </summary>
        public string B { get; set; }
        /// <summary>
        /// 获取或设置用于验证私有属性发现的样例文本。
        /// </summary>
        private string E { get; set; }
        /// <summary>
        /// 获取或设置用于验证受保护属性发现的样例文本。
        /// </summary>
        protected string F { get; set; }
        /// <summary>
        /// 用于验证私有字段发现的样例文本。
        /// </summary>
        private string _c;
        /// <summary>
        /// 用于验证受保护字段发现的样例文本。
        /// </summary>
        protected string _d;
    }
}
