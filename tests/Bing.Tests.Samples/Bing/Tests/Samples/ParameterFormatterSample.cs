using Bing.Utils.Parameters.Formats;

namespace Bing.Tests.Samples
{
    /// <summary>
    /// 使用固定分隔符验证参数格式化和连接的样例。
    /// </summary>
    public class ParameterFormatterSample : ParameterFormatBase
    {
        /// <inheritdoc />
        /// <remarks>此样例使用冒号分隔参数名和值。</remarks>
        protected override string FormatSeparator => ":";

        /// <inheritdoc />
        /// <remarks>此样例使用竖线连接参数。</remarks>
        protected override string JoinSeparator => "|";
    }
}
