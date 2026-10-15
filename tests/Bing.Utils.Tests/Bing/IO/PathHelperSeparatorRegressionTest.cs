namespace Bing.IO;

/// <summary>
/// 验证路径分隔符标准化的回归行为。
/// </summary>
[Trait("Category", "Regression")]
[Trait("Risk", "High")]
public class PathHelperSeparatorRegressionTest
{
    /// <summary>
    /// 验证NormalizePath 应按系统分隔符标准化路径
    /// </summary>
    [Fact]
    [Trait("DefectPattern", "IO.PathHelper.SeparatorNormalization")]
    public void NormalizePath_WithAltSeparator_ShouldNormalizeToSystemSeparator()
    {
        var alt = Path.AltDirectorySeparatorChar;
        var sep = Path.DirectorySeparatorChar;
        var rawPath = $"root{alt}folder{alt}file.txt";

        var normalized = PathHelper.NormalizePath(rawPath);

        normalized.ShouldBe($"root{sep}folder{sep}file.txt");
    }
}
