namespace Bing.Text.Similarity;

/// <summary>
/// 字符串相似度计算
/// </summary>
public static class StringSimilarity
{
    /// <summary>
    /// 最大字符差异数。
    /// </summary>
    private const int MAX_DIF_TOLERADAS = 2;

    /// <summary>
    /// 评估字符串相似度并返回定量结果。
    /// </summary>
    /// <param name="text">要比较的第一个字符串。</param>
    /// <param name="comparisonText">要比较的第二个字符串。</param>
    /// <param name="similarityMinimal">最小相似度阈值。</param>
    /// <returns>相似度评分（范围：0.0 到 1.0）。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double EvaluateSimilarity(string text, string comparisonText, double similarityMinimal)
    {
        const int diffFound = 0;
        return EvaluateSimilarity(text, comparisonText, similarityMinimal, diffFound);
    }

    /// <summary>
    /// 评估字符串相似度并返回定量结果。
    /// </summary>
    /// <param name="text">要比较的第一个字符串。</param>
    /// <param name="comparisonText">要比较的第二个字符串。</param>
    /// <param name="similarityMinimal">最小相似度阈值。</param>
    /// <param name="diffFound">已发现的差异数量。</param>
    /// <returns>相似度评分（范围：0.0 到 1.0）。</returns>
    public static double EvaluateSimilarity(string text, string comparisonText, double similarityMinimal, int diffFound)
    {
        // 如果已发现的差异数量超过最大容忍差异数，返回最低相似度
        if (diffFound >= MAX_DIF_TOLERADAS)
            return 0.0;

        // 移除字符串中的空白字符
        text = Strings.RemoveWhiteSpace(text);
        comparisonText = Strings.RemoveWhiteSpace(comparisonText);

        // 判断是否完全相同
        if (text.EqualsIgnoreCase(comparisonText))
            return 1;

        // 处理不同长度的字符串
        var portionText = text;
        var portionToCheck = comparisonText;
        if (text.Length != comparisonText.Length)
        {
            if (text.Length > comparisonText.Length)
                portionText = text.Substring(0, comparisonText.Length);
            else if (comparisonText.Length > text.Length)
                portionToCheck = comparisonText.Substring(0, text.Length);
            // 判断部分字符串是否相同
            if (portionText.EqualsIgnoreCase(portionToCheck))
                return 0.75;
        }

        // 统计差异数量并记录第一个差异的位置
        var totalDifferences = 0;
        var posDifference = -1;
        for (var i = 0; i < text.Length; i++)
        {
            if (i >= comparisonText.Length || text.ToCharArray()[i] != comparisonText.ToCharArray()[i])
                totalDifferences++;
            if (posDifference < 0 && totalDifferences == 1)
                posDifference = i;
        }

        // 计算相似度评分
        var res = Convert.ToDouble(text.Length - totalDifferences) / Convert.ToDouble(text.Length);
        // 如果差异数量在容忍范围内，返回评分
        if (totalDifferences <= MAX_DIF_TOLERADAS)
            return res;

        var differences = diffFound + 1;
        // 递归计算移除一个字符后的相似度
        var resConCarAwayInText2 = EvaluateSimilarity(text.Substring(posDifference + 1), comparisonText.Substring(posDifference), similarityMinimal, differences);
        var resConCarAwayInText1 = EvaluateSimilarity(text.Substring(posDifference), comparisonText.Substring(posDifference + 1), similarityMinimal, differences);
        var resConCarCharacter = EvaluateSimilarity(text.Substring(posDifference + 1), comparisonText.Substring(posDifference + 1), similarityMinimal, differences);

        // 返回三个递归结果的平均值
        // simParte1 + max(simParte2) - 0.1 / 2
        if (resConCarAwayInText2 > similarityMinimal || resConCarAwayInText1 > similarityMinimal || resConCarCharacter > similarityMinimal)
            return (1.0 + Math.Max(resConCarAwayInText2, Math.Max(resConCarAwayInText1, resConCarCharacter)) - 0.10) / 2.0;
        return res;
    }

    /// <summary>
    /// 评估字符串相似度并返回定量结果。
    /// </summary>
    /// <param name="text">要比较的第一个字符串。</param>
    /// <param name="comparisonText">要比较的第二个字符串。</param>
    /// <returns>相似度类型枚举。</returns>
    public static StringSimilarityTypes EvaluateSimilarity(string text, string comparisonText)
    {
        // 移除字符串中的空白字符
        text = Strings.RemoveWhiteSpace(text);
        comparisonText = Strings.RemoveWhiteSpace(comparisonText);

        // 判断是否完全相同
        if (text.EqualsIgnoreCase(comparisonText))
            return StringSimilarityTypes.Same;

        // 处理不同长度的字符串
        var portionText = text;
        var portionToCheck = comparisonText;
        if (text.Length != comparisonText.Length)
        {
            if (text.Length > comparisonText.Length)
                portionText = text.Substring(0, comparisonText.Length);
            else if (comparisonText.Length > text.Length)
                portionToCheck = comparisonText.Substring(0, text.Length);
            if (portionText.EqualsIgnoreCase(portionToCheck))
                return text.Length > comparisonText.Length
                    ? StringSimilarityTypes.MayorLong
                    : StringSimilarityTypes.MinorLong;
        }

        return StringSimilarityTypes.Any;
    }
}