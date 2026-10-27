namespace Bing.Text.Diff.Internal;

/// <summary>
/// 引用原始行的规范化区间，缓存可分块计算的哈希值。
/// </summary>
internal readonly struct LineKey
{
    /// <summary>
    /// 原始行。
    /// </summary>
    internal readonly string Text;
    /// <summary>
    /// 参与比较的起点。
    /// </summary>
    internal readonly int Start;
    /// <summary>
    /// 参与比较的代码单元数。
    /// </summary>
    internal readonly int Length;
    /// <summary>
    /// 预计算的哈希值。
    /// </summary>
    internal readonly int Hash;
    /// <summary>
    /// 创建规范化区间。
    /// </summary>
    /// <param name="text">原始行。</param>
    /// <param name="start">起点。</param>
    /// <param name="length">长度。</param>
    /// <param name="hash">哈希值。</param>
    internal LineKey(string text, int start, int length, int hash) { Text = text; Start = start; Length = length; Hash = hash; }
}

/// <summary>
/// 分块执行序号哈希和比较，避免超长单行绕过取消检查。
/// </summary>
internal sealed class LineKeyComparer : IEqualityComparer<LineKey>
{
    /// <summary>
    /// 块内字符串比较器。
    /// </summary>
    private readonly StringComparer _comparer;
    /// <summary>
    /// 块内序号比较模式。
    /// </summary>
    private readonly StringComparison _comparison;
    /// <summary>
    /// 本次比较预算。
    /// </summary>
    private readonly DiffBudget _budget;
    /// <summary>
    /// 初始化比较策略。
    /// </summary>
    /// <param name="ignoreCase">是否忽略大小写。</param>
    /// <param name="budget">协作预算。</param>
    internal LineKeyComparer(bool ignoreCase, DiffBudget budget)
    {
        _comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        _comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        _budget = budget;
    }

    /// <summary>
    /// 扫描首尾空白并按最多 4096 个代码单元计算块哈希。
    /// </summary>
    /// <param name="text">原始行。</param>
    /// <param name="trim">是否忽略首尾空白。</param>
    /// <returns>规范化行键。</returns>
    internal LineKey Create(string text, bool trim)
    {
        var start = 0;
        var end = text.Length;
        if (trim)
        {
            while (start < end && char.IsWhiteSpace(text[start])) { _budget.Character(); start++; }
            while (end > start && char.IsWhiteSpace(text[end - 1])) { _budget.Character(); end--; }
        }
        var hash = 17;
        for (var offset = start; offset < end;)
        {
            _budget.Check();
            var count = ChunkLength(text, offset, end);
            var piece = offset == 0 && count == text.Length ? text : text.Substring(offset, count);
            hash = unchecked(hash * 31 + _comparer.GetHashCode(piece));
            offset += count;
        }
        return new LineKey(text, start, end - start, hash);
    }

    /// <summary>
    /// 分块比较规范化区间，代理对不跨块拆分。
    /// </summary>
    /// <param name="x">第一个行键。</param>
    /// <param name="y">第二个行键。</param>
    /// <returns>语义是否相同。</returns>
    public bool Equals(LineKey x, LineKey y)
    {
        if (x.Length != y.Length) return false;
        for (var offset = 0; offset < x.Length;)
        {
            _budget.Check();
            var count = ChunkLength(x.Text, x.Start + offset, x.Start + x.Length);
            if (string.Compare(x.Text, x.Start + offset, y.Text, y.Start + offset, count, _comparison) != 0) return false;
            offset += count;
        }
        return true;
    }

    /// <summary>
    /// 获取已计算的哈希值。
    /// </summary>
    /// <param name="obj">行键。</param>
    /// <returns>哈希值。</returns>
    public int GetHashCode(LineKey obj) => obj.Hash;

    /// <summary>
    /// 计算不拆分 UTF-16 代理对的块长。
    /// </summary>
    /// <param name="text">文本。</param>
    /// <param name="start">块起点。</param>
    /// <param name="end">区间终点。</param>
    /// <returns>最多 4096 的块长。</returns>
    private static int ChunkLength(string text, int start, int end)
    {
        var count = Math.Min(4096, end - start);
        if (start + count < end && char.IsHighSurrogate(text[start + count - 1]) && char.IsLowSurrogate(text[start + count])) count--;
        return count;
    }
}
