using System.Collections.ObjectModel;

namespace Bing.Text.Diff;

/// <summary>
/// 差异块的变化类型。
/// </summary>
public enum TextDiffKind
{
    /// <summary>
    /// 新文本中新增行，旧侧长度为零。
    /// </summary>
    Inserted,
    /// <summary>
    /// 旧文本中删除行，新侧长度为零。
    /// </summary>
    Deleted,
    /// <summary>
    /// 旧文本的一段行被新文本的一段行替换。
    /// </summary>
    Replaced
}

/// <summary>
/// 不可变的连续差异块，行索引从零开始。
/// </summary>
public sealed class TextDiffBlock
{
    /// <summary>
    /// 初始化内部算法生成的差异块。
    /// </summary>
    /// <param name="oldStart">旧侧起始索引。</param>
    /// <param name="oldCount">旧侧行数。</param>
    /// <param name="newStart">新侧起始索引。</param>
    /// <param name="newCount">新侧行数。</param>
    internal TextDiffBlock(int oldStart, int oldCount, int newStart, int newCount)
    {
        OldStartLine = oldStart;
        OldLineCount = oldCount;
        NewStartLine = newStart;
        NewLineCount = newCount;
        Kind = oldCount == 0 ? TextDiffKind.Inserted : newCount == 0 ? TextDiffKind.Deleted : TextDiffKind.Replaced;
    }

    /// <summary>
    /// 变化类型。
    /// </summary>
    public TextDiffKind Kind { get; }
    /// <summary>
    /// 旧文本起始行索引，插入点可以位于旧文本末尾之后。
    /// </summary>
    public int OldStartLine { get; }
    /// <summary>
    /// 旧文本受影响的行数，插入时为零。
    /// </summary>
    public int OldLineCount { get; }
    /// <summary>
    /// 新文本起始行索引，删除点可以位于新文本末尾之后。
    /// </summary>
    public int NewStartLine { get; }
    /// <summary>
    /// 新文本受影响的行数，删除时为零。
    /// </summary>
    public int NewLineCount { get; }
}

/// <summary>
/// 原始行集合及不可变差异块；不为未变化行创建差异块。
/// </summary>
public sealed class TextDiffResult
{
    /// <summary>
    /// 接管本次调用独占的行集合和差异块集合。
    /// </summary>
    /// <param name="oldLines">原始旧行。</param>
    /// <param name="newLines">原始新行。</param>
    /// <param name="blocks">有序差异块。</param>
    internal TextDiffResult(List<string> oldLines, List<string> newLines, List<TextDiffBlock> blocks)
    {
        OldLines = oldLines.AsReadOnly();
        NewLines = newLines.AsReadOnly();
        Blocks = blocks.AsReadOnly();
    }

    /// <summary>
    /// 旧文本原始行，不含换行分隔符；空文本为零行，末尾换行保留空行。
    /// </summary>
    public ReadOnlyCollection<string> OldLines { get; }
    /// <summary>
    /// 新文本原始行，不含换行分隔符，内容不受比较选项修改。
    /// </summary>
    public ReadOnlyCollection<string> NewLines { get; }
    /// <summary>
    /// 按文本顺序排列的不可变差异块。
    /// </summary>
    public ReadOnlyCollection<TextDiffBlock> Blocks { get; }
}
