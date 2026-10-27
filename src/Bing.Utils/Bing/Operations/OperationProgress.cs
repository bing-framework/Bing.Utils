namespace Bing.Operations;

/// <summary>
/// 表示一次流式操作在某个阶段的不可变进度快照。
/// </summary>
/// <remarks>
/// <see cref="Total"/> 是数据源提供的总量快照，可能不准确，因此实际完成数可能超过它。
/// <see cref="IsFinal"/> 表示当前阶段结束，不表示结果已提交到最终目标。
/// </remarks>
public sealed class OperationProgress
{
    /// <summary>
    /// 初始化进度快照。
    /// </summary>
    /// <param name="stage">当前处理阶段。</param>
    /// <param name="unit">已完成数量的计量单位。</param>
    /// <param name="completed">已完成的数量。</param>
    /// <param name="total">总数量；无法确定时为 null。</param>
    /// <param name="attempt">当前尝试序号，从 1 开始。</param>
    /// <param name="sequence">当前操作内的单调事件序号，从 0 开始。</param>
    /// <param name="isFinal">当前阶段是否已发出最终进度事件。</param>
    public OperationProgress(OperationStage stage, ProgressUnit unit, long completed, long? total = null,
        int attempt = 1, long sequence = 0, bool isFinal = false)
    {
        if (!Enum.IsDefined(typeof(OperationStage), stage)) throw new ArgumentOutOfRangeException(nameof(stage));
        if (!Enum.IsDefined(typeof(ProgressUnit), unit)) throw new ArgumentOutOfRangeException(nameof(unit));
        if (completed < 0) throw new ArgumentOutOfRangeException(nameof(completed));
        if (total < 0) throw new ArgumentOutOfRangeException(nameof(total));
        if (attempt < 1) throw new ArgumentOutOfRangeException(nameof(attempt));
        if (sequence < 0) throw new ArgumentOutOfRangeException(nameof(sequence));

        Stage = stage;
        Unit = unit;
        Completed = completed;
        Total = total;
        Attempt = attempt;
        Sequence = sequence;
        IsFinal = isFinal;
    }

    /// <summary>
    /// 获取当前处理阶段。
    /// </summary>
    public OperationStage Stage { get; }

    /// <summary>
    /// 获取进度计量单位。
    /// </summary>
    public ProgressUnit Unit { get; }

    /// <summary>
    /// 获取已完成数量。
    /// </summary>
    public long Completed { get; }

    /// <summary>
    /// 获取总数量；无法确定时为 null。
    /// </summary>
    public long? Total { get; }

    /// <summary>
    /// 获取当前尝试序号，从 1 开始。
    /// </summary>
    public int Attempt { get; }

    /// <summary>
    /// 获取当前操作内的事件序号，从 0 开始。
    /// </summary>
    public long Sequence { get; }

    /// <summary>
    /// 获取当前阶段是否已经完成。
    /// </summary>
    public bool IsFinal { get; }
}
