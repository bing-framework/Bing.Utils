namespace Bing.Helpers;

/// <summary>
/// 字符串处理测试。
/// </summary>
public partial class StrTest 
{
    /// <summary>
    /// 验证集合字符串连接。
    /// </summary>
    [Fact]
    public void Test_Join()
    {
        Assert.Equal("1,2,3", Str.Join(new List<int> { 1, 2, 3 }));
        Assert.Equal("'1','2','3'", Str.Join(new List<int> { 1, 2, 3 }, "'"));
        Assert.Equal("123", Str.Join(new List<int> { 1, 2, 3 }, "", ""));
        Assert.Equal("\"1\",\"2\",\"3\"", Str.Join(new List<int> { 1, 2, 3 }, "\""));
        Assert.Equal("1 2 3", Str.Join(new List<int> { 1, 2, 3 }, "", " "));
        Assert.Equal("1;2;3", Str.Join(new List<int> { 1, 2, 3 }, "", ";"));
        Assert.Equal("1,2,3", Str.Join(new List<string> { "1", "2", "3" }));
        Assert.Equal("'1','2','3'", Str.Join(new List<string> { "1", "2", "3" }, "'"));

        var list = new List<Guid> {
            new( "83B0233C-A24F-49FD-8083-1337209EBC9A" ),
            new( "EAB523C6-2FE7-47BE-89D5-C6D440C3033A" )
        };
        Assert.Equal("83B0233C-A24F-49FD-8083-1337209EBC9A,EAB523C6-2FE7-47BE-89D5-C6D440C3033A".ToLower(), Str.Join(list));
        Assert.Equal("'83B0233C-A24F-49FD-8083-1337209EBC9A','EAB523C6-2FE7-47BE-89D5-C6D440C3033A'".ToLower(), Str.Join(list, "'"));
    }

    /// <summary>
    /// 验证拼音首字母转换。
    /// </summary>
    /// <param name="input">待转换文本。</param>
    /// <param name="result">预期转换结果。</param>
    /// <param name="input">待转换文本。</param>
    /// <param name="result">预期转换结果。</param>
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("中国", "zg")]
    [InlineData("a1宝藏b2", "a1bcb2")]
    [InlineData("饕餮", "tt")]
    [InlineData("爩", "y")]
    public void Test_PinYin(string input, string result)
    {
        Assert.Equal(result, Str.PinYin(input));
    }

    /// <summary>
    /// 验证汉字全拼转换。
    /// </summary>
    /// <param name="input">待转换文本。</param>
    /// <param name="result">预期转换结果。</param>
    /// <param name="input">待转换文本。</param>
    /// <param name="result">预期转换结果。</param>
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("中国", "ZhongGuo")]
    [InlineData("隔壁老王", "GeBiLaoWang")]
    [InlineData("A1中国!", "A1ZhongGuo!")]
    public void Test_FullPinYin(string input, string result)
    {
        Assert.Equal(result, Str.FullPinYin(input));
    }

    /// <summary>
    /// 验证首字母小写转换。
    /// </summary>
    /// <param name="value">待转换文本。</param>
    /// <param name="result">预期转换结果。</param>
    /// <param name="value">待转换文本。</param>
    /// <param name="result">预期转换结果。</param>
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("a", "a")]
    [InlineData("A", "a")]
    [InlineData("Ab", "ab")]
    [InlineData("AB", "aB")]
    [InlineData("Abc", "abc")]
    public void Test_FirstLowerCase(string value, string result)
    {
        Assert.Equal(result, Str.FirstLower(value));
    }

    /// <summary>
    /// 验证首字母大写转换。
    /// </summary>
    /// <param name="value">待转换文本。</param>
    /// <param name="result">预期转换结果。</param>
    /// <param name="value">待转换文本。</param>
    /// <param name="result">预期转换结果。</param>
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("a", "A")]
    [InlineData("A", "A")]
    [InlineData("ab", "Ab")]
    [InlineData("AB", "AB")]
    [InlineData("abC", "AbC")]
    public void Test_FirstUpperCase(string value, string result)
    {
        Assert.Equal(result, Str.FirstUpper(value));
    }

    /// <summary>
    /// 验证词组分隔。
    /// </summary>
    /// <param name="value">待分隔文本。</param>
    /// <param name="result">预期分隔结果。</param>
    /// <param name="value">待分隔文本。</param>
    /// <param name="result">预期分隔结果。</param>
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("AaA", "aa-a")]
    [InlineData("AA", "aa")]
    [InlineData("ABC", "abc")]
    [InlineData("NetCore", "net-core")]
    public void Test_SplitWordGroup(string value, string result)
    {
        Assert.Equal(result, Str.SplitWordGroup(value));
    }
}
