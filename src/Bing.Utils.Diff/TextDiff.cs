using Bing.Text.Diff.Internal;

namespace Bing.Text.Diff;

/// <summary>
/// 支持实际计算取消与协作预算的同步行级文本比较。
/// </summary>
public static class TextDiff
{
    /// <summary>
    /// 比较两份文本，返回原始行及连续差异块。
    /// </summary>
    /// <param name="oldText">旧文本，空字符串视为零行。</param>
    /// <param name="newText">新文本。</param>
    /// <param name="options">比较选项，null 使用默认设置；调用期间请勿并发修改。</param>
    /// <param name="cancellationToken">覆盖扫描、哈希、算法及结果构建的取消令牌。</param>
    /// <returns>完整、只读的比较结果，失败时不返回部分结果。</returns>
    /// <exception cref="ArgumentNullException">输入为 null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">数量上限或预算不是正值。</exception>
    /// <exception cref="OperationCanceledException">比较被取消。</exception>
    /// <exception cref="TextDiffLimitExceededException">字符、行数或计算时间超限。</exception>
    /// <remarks>
    /// CRLF、LF、CR 均作为分隔符，末尾换行保留空行。换行风格不参与比较。
    /// 预算是协作检查而非硬实时中断。方法不创建后台任务；调用方可自行安排执行线程。
    /// 忽略大小写或首尾空白时，语义相同的行不会产生差异块，但原始行仍保留各自内容。
    /// </remarks>
    public static TextDiffResult Compare(string oldText, string newText, TextDiffOptions? options = null,
        CancellationToken cancellationToken = default) => CompareCore(oldText, newText, options, cancellationToken);

    /// <summary>
    /// 执行比较，并允许测试注入单调时钟及计算检查点。
    /// </summary>
    /// <param name="oldText">旧文本。</param>
    /// <param name="newText">新文本。</param>
    /// <param name="options">选项。</param>
    /// <param name="token">取消令牌。</param>
    /// <param name="elapsed">测试经过时间。</param>
    /// <param name="checkpoint">测试检查点观察器。</param>
    /// <returns>完整结果。</returns>
    internal static TextDiffResult CompareCore(string oldText, string newText, TextDiffOptions? options,
        CancellationToken token, Func<TimeSpan>? elapsed = null, Action<DiffStage>? checkpoint = null)
    {
        if (oldText == null) throw new ArgumentNullException(nameof(oldText));
        if (newText == null) throw new ArgumentNullException(nameof(newText));
        options ??= new TextDiffOptions();
        var maxLines = options.MaxLinesPerInput;
        var maxChars = options.MaxCharsPerInput;
        var duration = options.MaxDuration;
        var ignoreCase = options.IgnoreCase;
        var trim = options.IgnoreLeadingTrailingWhitespace;
        if (maxLines <= 0) throw new ArgumentOutOfRangeException(nameof(options), "每份输入的行数上限必须为正数。");
        if (maxChars <= 0) throw new ArgumentOutOfRangeException(nameof(options), "每份输入的字符上限必须为正数。");
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options), "计算预算必须为有限正值。");

        var budget = new DiffBudget(duration, token, elapsed, checkpoint);
        budget.Enter(DiffStage.Scanning);
        var oldLines = Scan(oldText, maxChars, maxLines, nameof(oldText), budget);
        var newLines = Scan(newText, maxChars, maxLines, nameof(newText), budget);
        budget.Enter(DiffStage.Hashing);
        var comparer = new LineKeyComparer(ignoreCase, budget);
        var hashes = new Dictionary<LineKey, int>(comparer);
        var oldIds = BuildIds(oldLines, hashes, comparer, trim, budget);
        var newIds = BuildIds(newLines, hashes, comparer, trim, budget);
        budget.Enter(DiffStage.Comparing);
        var oldModified = new bool[oldIds.Length];
        var newModified = new bool[newIds.Length];
        Internal.DiffPlex.LineDiffer.MarkChanges(oldIds, newIds, oldModified, newModified, budget);
        budget.Enter(DiffStage.Results);
        var blocks = new List<TextDiffBlock>();
        var a = 0;
        var b = 0;
        while (a < oldIds.Length || b < newIds.Length)
        {
            budget.Step();
            while (a < oldIds.Length && b < newIds.Length && !oldModified[a] && !newModified[b])
            {
                budget.Step();
                a++;
                b++;
            }
            var beginA = a;
            var beginB = b;
            while (a < oldIds.Length && oldModified[a]) { budget.Step(); a++; }
            while (b < newIds.Length && newModified[b]) { budget.Step(); b++; }
            if (a != beginA || b != beginB) blocks.Add(new TextDiffBlock(beginA, a - beginA, beginB, b - beginB));
        }
        var result = new TextDiffResult(oldLines, newLines, blocks);
        budget.Check();
        return result;
    }

    /// <summary>
    /// 扫描逻辑行并保留末尾空行，字符检查间隔不超过 4096。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <param name="maxChars">代码单元上限。</param>
    /// <param name="maxLines">行数上限。</param>
    /// <param name="inputName">诊断输入名。</param>
    /// <param name="budget">协作预算。</param>
    /// <returns>原始行集合。</returns>
    private static List<string> Scan(string text, int maxChars, int maxLines, string inputName, DiffBudget budget)
    {
        budget.Check();
        if (text.Length > maxChars)
            throw new TextDiffLimitExceededException(TextDiffLimitExceededException.CharacterLimitCode, $"{inputName} 的 UTF-16 代码单元数超过上限 {maxChars}。");
        var lines = new List<string>();
        if (text.Length == 0) return lines;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            budget.Character();
            if (text[i] != '\r' && text[i] != '\n') continue;
            AddLine(i);
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n') { budget.Character(); i++; }
            start = i + 1;
        }
        AddLine(text.Length);
        budget.Check();
        return lines;

        void AddLine(int end)
        {
            if (lines.Count == maxLines)
                throw new TextDiffLimitExceededException(TextDiffLimitExceededException.LineLimitCode, $"{inputName} 的逻辑行数超过上限 {maxLines}。");
            lines.Add(text.Substring(start, end - start));
        }
    }

    /// <summary>
    /// 使用共享字典为语义相同的行分配相同整数标识。
    /// </summary>
    /// <param name="lines">原始行。</param>
    /// <param name="hashes">两份输入共享的行字典。</param>
    /// <param name="comparer">可取消的比较器。</param>
    /// <param name="trim">是否忽略首尾空白。</param>
    /// <param name="budget">协作预算。</param>
    /// <returns>行标识数组。</returns>
    private static int[] BuildIds(List<string> lines, Dictionary<LineKey, int> hashes, LineKeyComparer comparer, bool trim, DiffBudget budget)
    {
        budget.Check();
        var ids = new int[lines.Count];
        for (var i = 0; i < lines.Count; i++)
        {
            budget.Step();
            var key = comparer.Create(lines[i], trim);
            if (!hashes.TryGetValue(key, out var id)) { id = hashes.Count; hashes.Add(key, id); }
            ids[i] = id;
        }
        budget.Check();
        return ids;
    }
}
