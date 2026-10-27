using System.Diagnostics;

namespace Bing.Text.Diff.Internal;

/// <summary>
/// 计算阶段，用于内部可控检查点验证。
/// </summary>
internal enum DiffStage
{
    /// <summary>
    /// 输入扫描。
    /// </summary>
    Scanning,
    /// <summary>
    /// 行哈希及相等性比较。
    /// </summary>
    Hashing,
    /// <summary>
    /// 差异算法。
    /// </summary>
    Comparing,
    /// <summary>
    /// 结果汇总。
    /// </summary>
    Results
}

/// <summary>
/// 使用单调时钟约束全过程，并按字符和循环步数检查取消。
/// </summary>
internal sealed class DiffBudget
{
    /// <summary>
    /// 调用方令牌。
    /// </summary>
    private readonly CancellationToken _token;
    /// <summary>
    /// 从创建预算起的经过时间。
    /// </summary>
    private readonly Func<TimeSpan> _elapsed;
    /// <summary>
    /// 内部测试检查点，生产调用为空。
    /// </summary>
    private readonly Action<DiffStage>? _checkpoint;
    /// <summary>
    /// 最大协作时长。
    /// </summary>
    private readonly TimeSpan _duration;
    /// <summary>
    /// 当前阶段。
    /// </summary>
    private DiffStage _stage;
    /// <summary>
    /// 距上次循环检查的步数。
    /// </summary>
    private int _steps;
    /// <summary>
    /// 距上次字符检查的代码单元数。
    /// </summary>
    private int _characters;

    /// <summary>
    /// 在输入扫描前启动预算计时。
    /// </summary>
    /// <param name="duration">有限正预算。</param>
    /// <param name="token">取消令牌。</param>
    /// <param name="elapsed">测试时钟，为空时使用 Stopwatch。</param>
    /// <param name="checkpoint">测试阶段观察器。</param>
    internal DiffBudget(TimeSpan duration, CancellationToken token, Func<TimeSpan>? elapsed, Action<DiffStage>? checkpoint)
    {
        var watch = Stopwatch.StartNew();
        _elapsed = elapsed ?? (() => watch.Elapsed);
        _duration = duration;
        _token = token;
        _checkpoint = checkpoint;
    }

    /// <summary>
    /// 切换阶段并立即检查。
    /// </summary>
    /// <param name="stage">计算阶段。</param>
    internal void Enter(DiffStage stage) { _stage = stage; Check(); }

    /// <summary>
    /// 每最多 1024 次循环检查一次。
    /// </summary>
    internal void Step() { if (++_steps >= 1024) Check(); }

    /// <summary>
    /// 每最多 4096 个代码单元检查一次；块处理需先直接检查，再最多处理 4096 个单元。
    /// </summary>
    internal void Character() { if (++_characters >= 4096) Check(); }

    /// <summary>
    /// 执行检查点，取消优先于时间超限。
    /// </summary>
    internal void Check()
    {
        _steps = 0;
        _characters = 0;
        _checkpoint?.Invoke(_stage);
        _token.ThrowIfCancellationRequested();
        if (_elapsed() >= _duration)
            throw new TextDiffLimitExceededException(TextDiffLimitExceededException.DurationLimitCode, "文本比较已耗尽计算时间预算。");
    }
}
