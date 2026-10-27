namespace Bing.Utils.Diff.Tests;

/// <summary>
/// 验证比较各阶段的取消及协作时间预算检查点。
/// </summary>
public class TextDiffCheckpointTests
{
	/// <summary>
	/// 验证扫描、哈希、差异计算和结果汇总中的检查点均能中途取消。
	/// </summary>
	[Fact]
	public void CompareCore_WhenCancelledAtStageCheckpoints_ShouldStopWithinEachStage()
	{
		foreach (var stage in Enum.GetValues(typeof(DiffStage)).Cast<DiffStage>())
		{
			var (oldText, newText) = CreateStageInputs(stage);
			var targetCheckpoint = GetTargetCheckpoint(stage);
			var stageCheckpoints = 0;
			using var cancellation = new CancellationTokenSource();
			Action<DiffStage> checkpoint = current =>
			{
				if (current != stage)
					return;
				stageCheckpoints++;
				if (stageCheckpoints == targetCheckpoint)
					cancellation.Cancel();
			};

			Should.Throw<OperationCanceledException>(() => TextDiff.CompareCore(
				oldText,
				newText,
				null,
				cancellation.Token,
				() => TimeSpan.Zero,
				checkpoint));
			stageCheckpoints.ShouldBe(targetCheckpoint);
		}
	}

	/// <summary>
	/// 验证注入时钟能在各阶段检查点耗尽预算并返回稳定错误代码。
	/// </summary>
	[Fact]
	public void CompareCore_WhenBudgetExpiresAtStageCheckpoints_ShouldReportDurationLimit()
	{
		foreach (var stage in Enum.GetValues(typeof(DiffStage)).Cast<DiffStage>())
		{
			var (oldText, newText) = CreateStageInputs(stage);
			var targetCheckpoint = GetTargetCheckpoint(stage);
			var duration = TimeSpan.FromSeconds(1);
			var stageCheckpoints = 0;
			Action<DiffStage> checkpoint = current =>
			{
				if (current == stage)
					stageCheckpoints++;
			};
			TimeSpan Elapsed() => stageCheckpoints >= targetCheckpoint ? duration : TimeSpan.Zero;

			var exception = Should.Throw<TextDiffLimitExceededException>(() => TextDiff.CompareCore(
				oldText,
				newText,
				new TextDiffOptions { MaxDuration = duration },
				CancellationToken.None,
				Elapsed,
				checkpoint));
			exception.Code.ShouldBe(TextDiffLimitExceededException.DurationLimitCode);
			stageCheckpoints.ShouldBe(targetCheckpoint);
		}
	}

	/// <summary>
	/// 验证已取消令牌会阻止公开比较返回结果。
	/// </summary>
	[Fact]
	public void Compare_WhenTokenIsCancelledBeforeCall_ShouldThrowOperationCanceledException()
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		Should.Throw<OperationCanceledException>(() =>
			TextDiff.Compare("old", "new", cancellationToken: cancellation.Token));
	}

	/// <summary>
	/// 获取进入指定阶段后触发的检查点序号。
	/// </summary>
	/// <param name="stage">被测阶段。</param>
	/// <returns>在入口检查之后触发的内部检查点序号。</returns>
	private static int GetTargetCheckpoint(DiffStage stage)
	{
		return stage switch
		{
			DiffStage.Scanning => 3,
			DiffStage.Hashing => 4,
			DiffStage.Comparing => 4,
			DiffStage.Results => 2,
			_ => throw new ArgumentOutOfRangeException(nameof(stage))
		};
	}

	/// <summary>
	/// 为每个阶段构造足以到达指定内部检查点的输入。
	/// </summary>
	/// <param name="stage">被测阶段。</param>
	/// <returns>旧文本和新文本。</returns>
	private static (string OldText, string NewText) CreateStageInputs(DiffStage stage)
	{
		if (stage == DiffStage.Scanning)
			return (new string('a', 12_000), new string('b', 12_000));
		if (stage == DiffStage.Hashing)
			return (new string('a', 12_000), new string('b', 12_000));
		if (stage == DiffStage.Comparing)
			return (string.Join("\n", Enumerable.Range(0, 1_000).Select(index => $"old-{index}")),
				string.Join("\n", Enumerable.Range(0, 1_000).Select(index => $"new-{index}")));
		if (stage == DiffStage.Results)
		{
			var oldLines = Enumerable.Range(0, 2_200)
				.Select(index => index % 2 == 0 ? $"old-{index}" : $"anchor-{index}");
			var newLines = Enumerable.Range(0, 2_200)
				.Select(index => index % 2 == 0 ? $"new-{index}" : $"anchor-{index}");
			return (string.Join("\n", oldLines), string.Join("\n", newLines));
		}

		throw new ArgumentOutOfRangeException(nameof(stage));
	}
}
