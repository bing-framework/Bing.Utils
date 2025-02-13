using Str = Bing.Helpers.Str;

namespace Bing.Utils.Tests.Helpers;

/// <summary>
/// 字符串操作测试
/// </summary>
public partial class StrTest 
{
    /// <summary>
    /// 测试将集合连接为带分隔符的字符串
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
            new Guid( "83B0233C-A24F-49FD-8083-1337209EBC9A" ),
            new Guid( "EAB523C6-2FE7-47BE-89D5-C6D440C3033A" )
        };
        Assert.Equal("83B0233C-A24F-49FD-8083-1337209EBC9A,EAB523C6-2FE7-47BE-89D5-C6D440C3033A".ToLower(), Str.Join(list));
        Assert.Equal("'83B0233C-A24F-49FD-8083-1337209EBC9A','EAB523C6-2FE7-47BE-89D5-C6D440C3033A'".ToLower(), Str.Join(list, "'"));
    }

    /// <summary>
    /// 测试获取拼音简码
    /// </summary>
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
    /// 测试获取汉字的全拼
    /// </summary>
    [Fact]
    public void Test_FullPinYin()
    {
        Output.WriteLine(Str.FullPinYin("隔壁老王"));
    }

    /// <summary>
    /// 首字母小写
    /// </summary>
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
    /// 首字母大写
    /// </summary>
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
    /// 分隔词组
    /// </summary>
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