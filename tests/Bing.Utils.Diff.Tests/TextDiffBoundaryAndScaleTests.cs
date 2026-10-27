namespace Bing.Utils.Diff.Tests;

/// <summary>
/// 验证文本大小限制及稀疏差异的完整结果和实际开销。
/// </summary>
public class TextDiffBoundaryAndScaleTests
{
	/// <summary>
	/// 接收测试输出以记录大文本比较的耗时与分配量。
	/// </summary>
	/// <param name="output">xUnit 测试输出接收器。</param>
	public TextDiffBoundaryAndScaleTests(ITestOutputHelper output)
	{
		_output = output;
	}

	/// <summary>
	/// 保存大文本比较的测量输出目标。
	/// </summary>
	private readonly ITestOutputHelper _output;

	/// <summary>
	/// 验证 10 万行追加及分散的百处插入、删除、修改均完整且在默认预算内。
	/// </summary>
	[Fact]
	public void Compare_WhenLargeSparseChangesAreProvided_ShouldCompleteAndReportMeasurements()
	{
		var oldLines = Enumerable.Range(0, 100_000).Select(index => $"line-{index:D6}").ToArray();
		var oldText = string.Join("\n", oldLines);

		var appendedLines = new List<string>(oldLines);
		for (var index = 0; index < 1_000; index++)
			appendedLines.Add($"append-{index:D4}");
		var appended = Measure("append-1000", oldText, string.Join("\n", appendedLines));
		appended.NewLines.Count.ShouldBe(101_000);
		appended.Blocks.Count.ShouldBe(1);
		appended.Blocks[0].Kind.ShouldBe(TextDiffKind.Inserted);
		appended.Blocks[0].OldStartLine.ShouldBe(100_000);
		appended.Blocks[0].OldLineCount.ShouldBe(0);
		appended.Blocks[0].NewStartLine.ShouldBe(100_000);
		appended.Blocks[0].NewLineCount.ShouldBe(1_000);
		AssertReconstructsNewLines(appended);

		var insertedLines = new List<string>(100_100);
		for (var index = 0; index < oldLines.Length; index++)
		{
			insertedLines.Add(oldLines[index]);
			if ((index + 1) % 1_000 == 0)
				insertedLines.Add($"insert-{index:D6}");
		}
		var inserted = Measure("distributed-insert-100", oldText, string.Join("\n", insertedLines));
		inserted.NewLines.Count.ShouldBe(100_100);
		AssertBlockKinds(inserted, TextDiffKind.Inserted, 100);
		AssertReconstructsNewLines(inserted);

		var deletedLines = new List<string>(99_900);
		var changedLines = new List<string>(oldLines.Length);
		for (var index = 0; index < oldLines.Length; index++)
		{
			if (index % 1_000 == 500)
			{
				changedLines.Add($"changed-{index:D6}");
				continue;
			}

			deletedLines.Add(oldLines[index]);
			changedLines.Add(oldLines[index]);
		}
		var deleted = Measure("distributed-delete-100", oldText, string.Join("\n", deletedLines));
		deleted.NewLines.Count.ShouldBe(99_900);
		AssertBlockKinds(deleted, TextDiffKind.Deleted, 100);
		AssertReconstructsNewLines(deleted);

		var modified = Measure("distributed-replace-100", oldText, string.Join("\n", changedLines));
		AssertBlockKinds(modified, TextDiffKind.Replaced, 100);
		AssertReconstructsNewLines(modified);
		Assert.Equal(changedLines, modified.NewLines);

		var mixedLines = new List<string>();
		for (var index = 0; index < oldLines.Length; index++)
		{
			if (index % 1_000 == 100) mixedLines.Add($"mixed-insert-{index}");
			if (index % 1_000 == 500) continue;
			mixedLines.Add(index % 1_000 == 700 ? $"mixed-change-{index}" : oldLines[index]);
		}
		var mixed = Measure("mixed-100-each", oldText, string.Join("\n", mixedLines));
		mixed.Blocks.Count.ShouldBe(300);
		AssertReconstructsNewLines(mixed);
	}

	/// <summary>
	/// 调整后的上限分别约束新旧输入，UTF-16 代理对按两个代码单元计数。
	/// </summary>
	[Fact]
	public void CustomLimits_AreAppliedToEachInput()
	{
		var options = new TextDiffOptions { MaxCharsPerInput = 2, MaxLinesPerInput = 1 };
		TextDiff.Compare("😀", "xy", options).Blocks.ShouldNotBeEmpty();
		Should.Throw<TextDiffLimitExceededException>(() => TextDiff.Compare("x", "😀x", options)).Code
			.ShouldBe(TextDiffLimitExceededException.CharacterLimitCode);
		Should.Throw<TextDiffLimitExceededException>(() => TextDiff.Compare("x", "x\n", options)).Code
			.ShouldBe(TextDiffLimitExceededException.LineLimitCode);
	}

	/// <summary>
	/// 验证默认 20 万逻辑行上限包含边界并拒绝多一行输入。
	/// </summary>
	[Fact]
	public void Compare_WhenLineCountIsAtAndAboveLimit_ShouldAcceptBoundaryAndRejectOverflow()
	{
		var exactLimit = string.Join("\n", Enumerable.Repeat("x", 200_000));
		var accepted = TextDiff.Compare(exactLimit, exactLimit);
		accepted.OldLines.Count.ShouldBe(200_000);
		accepted.Blocks.ShouldBeEmpty();

		var overLimit = string.Concat(exactLimit, "\nx");
		var exception = Should.Throw<TextDiffLimitExceededException>(() => TextDiff.Compare(overLimit, overLimit));
		exception.Code.ShouldBe(TextDiffLimitExceededException.LineLimitCode);
	}

	/// <summary>
	/// 验证默认 1600 万 UTF-16 代码单元上限包含边界并拒绝多一单元输入。
	/// </summary>
	[Fact]
	public void Compare_WhenCharacterCountIsAtAndAboveLimit_ShouldAcceptBoundaryAndRejectOverflow()
	{
		var exactLimit = new string('x', 16_000_000);
		var accepted = TextDiff.Compare(exactLimit, exactLimit);
		accepted.OldLines.Count.ShouldBe(1);
		accepted.Blocks.ShouldBeEmpty();

		var overLimit = new string('x', 16_000_001);
		var exception = Should.Throw<TextDiffLimitExceededException>(() => TextDiff.Compare(overLimit, overLimit));
		exception.Code.ShouldBe(TextDiffLimitExceededException.CharacterLimitCode);
	}

	/// <summary>
	/// 验证行数、字符数和时间预算必须为正值并拒绝空输入引用。
	/// </summary>
	[Fact]
	public void Compare_WhenOptionsOrInputsAreInvalid_ShouldThrowArgumentExceptions()
	{
		var invalidOptions = new[]
		{
			new TextDiffOptions { MaxLinesPerInput = 0 },
			new TextDiffOptions { MaxCharsPerInput = 0 },
			new TextDiffOptions { MaxDuration = TimeSpan.Zero },
			new TextDiffOptions { MaxDuration = TimeSpan.FromTicks(-1) }
		};
		foreach (var options in invalidOptions)
			Should.Throw<ArgumentOutOfRangeException>(() => TextDiff.Compare("old", "new", options));

		Should.Throw<ArgumentNullException>(() => TextDiff.Compare(null!, "new"));
		Should.Throw<ArgumentNullException>(() => TextDiff.Compare("old", null!));
	}

	/// <summary>
	/// 执行一次默认预算比较并记录实际耗时与当前线程分配量。
	/// </summary>
	/// <param name="name">输出中的场景名称。</param>
	/// <param name="oldText">旧文本。</param>
	/// <param name="newText">新文本。</param>
	/// <returns>完整比较结果。</returns>
	private TextDiffResult Measure(string name, string oldText, string newText)
	{
		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		var stopwatch = Stopwatch.StartNew();
		var result = TextDiff.Compare(oldText, newText);
		stopwatch.Stop();
		var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
		_output.WriteLine("{0}: {1:F1} ms, {2:N0} bytes allocated", name, stopwatch.Elapsed.TotalMilliseconds, allocated);
		return result;
	}

	/// <summary>
	/// 验证结果中的差异块数量及每块的变化类型。
	/// </summary>
	/// <param name="result">比较结果。</param>
	/// <param name="kind">预期变化类型。</param>
	/// <param name="count">预期差异块数。</param>
	private static void AssertBlockKinds(TextDiffResult result, TextDiffKind kind, int count)
	{
		result.Blocks.Count.ShouldBe(count);
		Assert.All(result.Blocks, block => Assert.Equal(kind, block.Kind));
	}

	/// <summary>
	/// 使用差异块从旧行集合重建并核对完整新行集合。
	/// </summary>
	/// <param name="result">比较结果。</param>
	private static void AssertReconstructsNewLines(TextDiffResult result)
	{
		var reconstructed = new List<string>();
		var oldPosition = 0;
		foreach (var block in result.Blocks)
		{
			while (oldPosition < block.OldStartLine)
				reconstructed.Add(result.OldLines[oldPosition++]);
			oldPosition += block.OldLineCount;
			for (var i = 0; i < block.NewLineCount; i++)
				reconstructed.Add(result.NewLines[block.NewStartLine + i]);
		}
		while (oldPosition < result.OldLines.Count)
			reconstructed.Add(result.OldLines[oldPosition++]);

		Assert.Equal(result.NewLines, reconstructed);
	}
}
