using System.Globalization;

namespace Bing.Exceptions;

/// <summary>
/// 在遍历和追加文本时执行预算限制的异常格式化器。
/// </summary>
internal static class ExceptionFormatter
{
    /// <summary>
    /// 生成有限长度的异常诊断信息。
    /// </summary>
    /// <param name="exception">根异常；为 null 时返回空字符串。</param>
    /// <param name="options">调用配置，不能为空。</param>
    /// <param name="includeRecordTime">是否添加分隔符和记录时间。</param>
    /// <returns>满足输出预算的文本。</returns>
    internal static string Format(Exception exception, ExceptionFormatOptions options, bool includeRecordTime)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        var settings = options.Snapshot();
        if (exception is null)
            return string.Empty;
        var output = new BoundedText(settings.MaxOutputLength);
        if (includeRecordTime)
        {
            output.Line(0, "***************************************", null);
            output.Line(0, "记录时间：", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
        }
        using var nodes = ExceptionTraversal.Enumerate(exception, settings.IncludeAggregateExceptions,
            settings.MaxDepth, settings.MaxExceptionCount).GetEnumerator();
        // 先检查输出预算，确保写满后不会为了进入循环而读取下一个分支。
        while (!output.IsFull && nodes.MoveNext())
        {
            var node = nodes.Current;
            if (node.CountLimited)
            {
                output.Line(0, "[已截断：异常节点数上限]", null);
                break;
            }
            var indent = Math.Min(node.Depth - 1, Math.Min(settings.MaxIndentLevel, settings.MaxOutputLength / 2)) * 2;
            if (node.IsRepeated)
            {
                output.Line(indent, "[已出现：相同异常实例]", null);
                continue;
            }
            if (node.Depth > 1)
                output.Line(indent, "内部异常：", null);
            var error = node.Exception;
            if (output.IsFull) break;
            // 聚合异常的 Message 会隐式拼接所有子消息，必须在逐项预算之外避免读取。
            var message = error is AggregateException aggregate
                ? $"聚合异常（{aggregate.InnerExceptions.Count} 个内部异常）"
                : error.Message;
            output.Line(indent, "异常消息：", message);
            if (output.IsFull) break;
            output.Line(indent, "异常类型：", error.GetType().FullName);
            if (output.IsFull) break;
            output.Line(indent, "HResult：", error.HResult.ToString(CultureInfo.InvariantCulture));
            if (output.IsFull) break;
            output.Line(indent, "异常方法：", error.TargetSite?.Name);
            if (output.IsFull) break;
            output.Line(indent, "异常源：", error.Source);
            if (output.IsFull) break;
            if (!settings.HideStackTrace)
            {
                var stack = error.StackTrace;
                if (stack != null)
                    output.Line(indent, "异常堆栈：", stack);
            }
            if (node.DepthLimited)
                output.Line(indent, "[已截断：深度上限]", null);
        }
        if (includeRecordTime && !output.IsFull)
            output.Line(0, "***************************************", null);
        return output.ToString();
    }

    /// <summary>
    /// 只追加预算内的文本，并为长度截断标记预留空间。
    /// </summary>
    private sealed class BoundedText
    {
        /// <summary>
        /// 超出输出长度预算时附加的固定标记。
        /// </summary>
        private const string LengthMarker = "[已截断：输出长度上限]";

        /// <summary>
        /// 本次输出的字符数上限。
        /// </summary>
        private readonly int _limit;

        /// <summary>
        /// 按需增长的文本缓冲，不按用户提供的最大预算预分配。
        /// </summary>
        private readonly StringBuilder _builder = new StringBuilder();

        /// <summary>
        /// 初始化输出预算。
        /// </summary>
        /// <param name="limit">已校验的字符数上限。</param>
        internal BoundedText(int limit) => _limit = limit;

        /// <summary>
        /// 获取是否已经输出长度截断标记。
        /// </summary>
        internal bool IsFull { get; private set; }

        /// <summary>
        /// 逐段追加一个诊断字段，避免先拼接整行。
        /// </summary>
        /// <param name="indent">已限制的缩进空格数。</param>
        /// <param name="label">字段标签。</param>
        /// <param name="value">可空字段值。</param>
        internal void Line(int indent, string label, string value)
        {
            if (IsFull) return;
            var available = _limit - LengthMarker.Length - _builder.Length;
            _builder.Append(' ', Math.Min(indent, available));
            if (indent > available)
            {
                Truncate();
                return;
            }
            Append(label);
            Append(value);
            Append(Environment.NewLine);
        }

        /// <summary>
        /// 在预算内追加字符串片段。
        /// </summary>
        /// <param name="value">需要追加的可空文本。</param>
        private void Append(string value)
        {
            if (IsFull || string.IsNullOrEmpty(value)) return;
            var available = _limit - LengthMarker.Length - _builder.Length;
            var length = Math.Min(value.Length, available);
            // 不在合法代理项对中间截断，避免生成新的无效 UTF-16 序列。
            if (length < value.Length && length > 0 && char.IsHighSurrogate(value[length - 1]) && char.IsLowSurrogate(value[length]))
                length--;
            _builder.Append(value, 0, length);
            if (length < value.Length)
                Truncate();
        }

        /// <summary>
        /// 追加长度截断标记并关闭后续追加。
        /// </summary>
        private void Truncate()
        {
            _builder.Append(LengthMarker);
            IsFull = true;
        }

        /// <inheritdoc />
        public override string ToString() => _builder.ToString();
    }
}
