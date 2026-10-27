namespace Bing.Utils.Diff.Tests;

/// <summary>
/// 验证文本差异块、行语义与比较选项。
/// </summary>
public class TextDiffTests
{
	/// <summary>
	/// 超长单行中的代理对跨过分块边界时仍使用完整序号大小写语义。
	/// </summary>
	[Fact]
	public void LongLines_WithSurrogateAtChunkBoundary_PreserveCaseSemantics()
	{
		var prefix = new string('x', 4095);
		var oldText = " \t" + prefix + "\U00010400😀ABC" + new string('z', 5000) + "\u3000";
		var newText = prefix + "\U00010428😀abc" + new string('Z', 5000);
		StringComparer.OrdinalIgnoreCase.Equals(oldText.Trim(), newText).ShouldBeTrue();
		var result = TextDiff.Compare(oldText, newText, new TextDiffOptions { IgnoreCase = true, IgnoreLeadingTrailingWhitespace = true });
		result.Blocks.ShouldBeEmpty();
		result.OldLines[0].ShouldBe(oldText);
		result.NewLines[0].ShouldBe(newText);
		TextDiff.Compare("a\nb", "").Blocks.Single().OldLineCount.ShouldBe(2);
	}

	/// <summary>
	/// 验证小型语料的块坐标与 DiffPlex 1.7.0 一致且能重建新文本。
	/// </summary>
	[Fact]
	public void Compare_WhenSmallCorpusIsCompared_ShouldMatchDiffPlexCoordinatesAndReconstruct()
	{
		var cases = new[]
		{
			("alpha\nbeta\ngamma", "alpha\nBETA\ngamma"),
			("alpha\ngamma", "alpha\nbeta\ngamma"),
			("alpha\nbeta\ngamma", "alpha\ngamma"),
			("alpha\nbeta", "alpha\nchanged"),
			("same\ntext", "same\ntext"),
			("repeat\nanchor\nrepeat", "repeat\nrepeat\nanchor"),
			("中文\n😀\n旧行", "中文\n😀\n新行")
		};

		foreach (var (oldText, newText) in cases)
			AssertMatchesDiffPlex(oldText, newText);
	}

	/// <summary>
	/// 验证固定种子随机语料与 DiffPlex 块坐标一致并能重建新文本。
	/// </summary>
	[Fact]
	public void Compare_WhenFixedSeedRandomInputsAreCompared_ShouldMatchDiffPlexCoordinates()
	{
		var random = new Random(20261027);
		var lines = new[] { "alpha", "beta", "repeat", "中文", "😀", "tail" };

		for (var sample = 0; sample < 256; sample++)
		{
			var oldText = CreateRandomText(random, lines);
			var newText = CreateRandomText(random, lines);
			AssertMatchesDiffPlex(oldText, newText);
		}
	}

	/// <summary>
	/// 验证 CRLF、LF、CR、emoji 与末尾空行按原文行集合保留。
	/// </summary>
	[Fact]
	public void Compare_WhenSeparatorsAndTrailingNewlineDiffer_ShouldPreserveLogicalLines()
	{
		var equivalent = TextDiff.Compare("首行\r\n😀\r结束\r", "首行\n😀\n结束\n");
		equivalent.OldLines.ShouldBe(new[] { "首行", "😀", "结束", "" });
		equivalent.NewLines.ShouldBe(new[] { "首行", "😀", "结束", "" });
		equivalent.Blocks.ShouldBeEmpty();

		var trailingLineRemoved = TextDiff.Compare("末尾\n", "末尾");
		trailingLineRemoved.OldLines.ShouldBe(new[] { "末尾", "" });
		trailingLineRemoved.NewLines.ShouldBe(new[] { "末尾" });
		trailingLineRemoved.Blocks.Count.ShouldBe(1);
		trailingLineRemoved.Blocks[0].Kind.ShouldBe(TextDiffKind.Deleted);
		trailingLineRemoved.Blocks[0].OldStartLine.ShouldBe(1);
		trailingLineRemoved.Blocks[0].OldLineCount.ShouldBe(1);
		trailingLineRemoved.Blocks[0].NewStartLine.ShouldBe(1);
		trailingLineRemoved.Blocks[0].NewLineCount.ShouldBe(0);
	}

	/// <summary>
	/// 验证大小写和首尾空白选项只影响比较键并保留两侧原文。
	/// </summary>
	[Fact]
	public void Compare_WhenCaseAndEdgeWhitespaceAreIgnored_ShouldRetainOriginalLines()
	{
		var oldText = "  Alpha \n\tBeta";
		var newText = "alpha\nbeta  ";
		var result = TextDiff.Compare(oldText, newText, new TextDiffOptions
		{
			IgnoreCase = true,
			IgnoreLeadingTrailingWhitespace = true
		});

		result.Blocks.ShouldBeEmpty();
		result.OldLines.ShouldBe(new[] { "  Alpha ", "\tBeta" });
		result.NewLines.ShouldBe(new[] { "alpha", "beta  " });
	}

	/// <summary>
	/// 验证默认比较区分大小写和行首尾空白。
	/// </summary>
	[Fact]
	public void Compare_WhenDefaultOptionsAreUsed_ShouldTreatCaseAndWhitespaceAsChanges()
	{
		var result = TextDiff.Compare(" Alpha", "alpha ");

		result.Blocks.Count.ShouldBe(1);
		result.Blocks[0].Kind.ShouldBe(TextDiffKind.Replaced);
	}

	/// <summary>
	/// 验证空输入、插入结果及公开集合均保持预期语义和只读状态。
	/// </summary>
	[Fact]
	public void Compare_WhenResultIsCreated_ShouldExposeReadOnlyOriginalLinesAndBlocks()
	{
		var empty = TextDiff.Compare("", "");
		empty.OldLines.ShouldBeEmpty();
		empty.NewLines.ShouldBeEmpty();
		empty.Blocks.ShouldBeEmpty();

		var inserted = TextDiff.Compare("", "added");
		inserted.OldLines.ShouldBeEmpty();
		inserted.NewLines.ShouldBe(new[] { "added" });
		inserted.Blocks.Count.ShouldBe(1);
		inserted.Blocks[0].Kind.ShouldBe(TextDiffKind.Inserted);
		((IList<string>)inserted.OldLines).IsReadOnly.ShouldBeTrue();
		((IList<string>)inserted.NewLines).IsReadOnly.ShouldBeTrue();
		((IList<TextDiffBlock>)inserted.Blocks).IsReadOnly.ShouldBeTrue();
		Should.Throw<NotSupportedException>(() => ((IList<string>)inserted.NewLines).Add("mutated"));
		Should.Throw<NotSupportedException>(() => ((IList<TextDiffBlock>)inserted.Blocks).Clear());
	}

	/// <summary>
	/// 使用 DiffPlex 原版行差异块核对坐标并验证差异可重建新行。
	/// </summary>
	/// <param name="oldText">旧文本。</param>
	/// <param name="newText">新文本。</param>
	private static void AssertMatchesDiffPlex(string oldText, string newText)
	{
		var expected = Differ.Instance.CreateLineDiffs(oldText, newText, ignoreWhitespace: false, ignoreCase: false)
			.DiffBlocks
			.Select(block => $"{block.DeleteStartA}:{block.DeleteCountA}:{block.InsertStartB}:{block.InsertCountB}")
			.ToArray();
		var result = TextDiff.Compare(oldText, newText);
		var actual = result.Blocks
			.Select(block => $"{block.OldStartLine}:{block.OldLineCount}:{block.NewStartLine}:{block.NewLineCount}")
			.ToArray();

		Assert.Equal(expected, actual);
		AssertReconstructsNewLines(result);
	}

	/// <summary>
	/// 从固定词表生成无尾部分隔符的随机行文本。
	/// </summary>
	/// <param name="random">固定种子随机数生成器。</param>
	/// <param name="lines">候选行词表。</param>
	/// <returns>至少包含一行的文本。</returns>
	private static string CreateRandomText(Random random, string[] lines)
	{
		var count = random.Next(1, 18);
		var selected = new string[count];
		for (var i = 0; i < selected.Length; i++)
			selected[i] = lines[random.Next(lines.Length)];
		return string.Join("\n", selected);
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
