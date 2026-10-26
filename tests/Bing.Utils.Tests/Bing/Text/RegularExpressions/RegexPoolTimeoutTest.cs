using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Bing.Text.RegularExpressions;

[Collection("RegexPool")]
public sealed class RegexPoolTimeoutTest : IDisposable
{
    public RegexPoolTimeoutTest() => RegexPool.Clear();

    public void Dispose() => RegexPool.Clear();

    [Fact]
    public void GetOrCreate_UsesTimeoutAsPartOfTheCacheKey()
    {
        const string pattern = @"\d+";
        const RegexOptions options = RegexOptions.None;
        var firstTimeout = TimeSpan.FromMilliseconds(100);
        var secondTimeout = TimeSpan.FromMilliseconds(200);

        var first = RegexPool.GetOrCreate(pattern, options, firstTimeout);
        var same = RegexPool.GetOrCreate(pattern, options, firstTimeout);
        var second = RegexPool.GetOrCreate(pattern, options, secondTimeout);
        var explicitInfinite = RegexPool.GetOrCreate(pattern, options, Regex.InfiniteMatchTimeout);
        var legacy = RegexPool.GetOrCreate(pattern, options);

        Assert.Same(first, same);
        Assert.NotSame(first, second);
        Assert.NotSame(explicitInfinite, legacy);
        Assert.Equal(firstTimeout, first.MatchTimeout);
        Assert.Equal(secondTimeout, second.MatchTimeout);
        Assert.Equal(4, RegexPool.Count);
    }

    [Fact]
    public void ContainsAndRemove_AreScopedByTimeoutAndCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        const string pattern = "^i$";
        const RegexOptions options = RegexOptions.IgnoreCase;
        var timeout = TimeSpan.FromMilliseconds(100);
        var otherTimeout = TimeSpan.FromMilliseconds(200);

        try
        {
            RegexPool.Clear();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var englishRegex = RegexPool.GetOrCreate(pattern, options, timeout);
            RegexPool.GetOrCreate(pattern, options, otherTimeout);
            RegexPool.GetOrCreate(pattern, options);

            Assert.Matches(englishRegex, "I");
            Assert.True(RegexPool.Contains(pattern, options, timeout));
            Assert.True(RegexPool.Contains(pattern, options, otherTimeout));
            Assert.False(RegexPool.Contains(pattern, options, TimeSpan.FromMilliseconds(300)));
            Assert.True(RegexPool.Contains(pattern, options));

            Assert.True(RegexPool.Remove(pattern, options, timeout));
            Assert.False(RegexPool.Contains(pattern, options, timeout));
            Assert.True(RegexPool.Contains(pattern, options, otherTimeout));
            Assert.True(RegexPool.Contains(pattern, options));
            Assert.False(RegexPool.Remove(pattern, options, timeout));

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Assert.False(RegexPool.Contains(pattern, options, otherTimeout));
            Assert.False(RegexPool.Contains(pattern, options));
            Assert.False(RegexPool.Remove(pattern, options, otherTimeout));

            var turkishRegex = RegexPool.GetOrCreate(pattern, options, otherTimeout);
            Assert.NotSame(englishRegex, turkishRegex);
            Assert.DoesNotMatch(turkishRegex, "I");
            Assert.True(RegexPool.Remove(pattern, options, otherTimeout));

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.False(RegexPool.Contains(pattern, options, timeout));
            Assert.True(RegexPool.Contains(pattern, options, otherTimeout));
            Assert.True(RegexPool.Remove(pattern, options, otherTimeout));
            Assert.True(RegexPool.Contains(pattern, options));
            Assert.True(RegexPool.Remove(pattern, options));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            RegexPool.Clear();
        }
    }

    [Fact]
    public void CultureInvariantTimeoutCache_IsSharedAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        const string pattern = "^i$";
        const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        var timeout = TimeSpan.FromMilliseconds(100);

        try
        {
            RegexPool.Clear();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var englishRegex = RegexPool.GetOrCreate(pattern, options, timeout);

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var invariantRegex = RegexPool.GetOrCreate(pattern, options, timeout);

            Assert.Same(englishRegex, invariantRegex);
            Assert.Matches(invariantRegex, "I");
            Assert.True(RegexPool.Contains(pattern, options, timeout));
            Assert.Equal(1, RegexPool.Count);
            Assert.True(RegexPool.Remove(pattern, options, timeout));

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.False(RegexPool.Contains(pattern, options, timeout));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            RegexPool.Clear();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(int.MaxValue)]
    public void ExplicitTimeoutOutsideSupportedRange_ThrowsArgumentOutOfRangeException(int milliseconds)
    {
        const string pattern = "a+";
        const RegexOptions options = RegexOptions.None;
        var timeout = TimeSpan.FromMilliseconds(milliseconds);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => RegexPool.GetOrCreate(pattern, options, timeout));

        Assert.Equal("matchTimeout", exception.ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => RegexPool.Contains(pattern, options, timeout));
        Assert.Throws<ArgumentOutOfRangeException>(() => RegexPool.Remove(pattern, options, timeout));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RegexPool.IsMatch("aaa", pattern, options, timeout, useCache: false));
        Assert.Equal(0, RegexPool.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExplicitOperations_ReturnExpectedResultsWithOrWithoutCache(bool useCache)
    {
        const string input = "name=alice; id=42";
        const string pattern = @"(?<key>\w+)=(?<value>\w+)";
        const RegexOptions options = RegexOptions.None;
        var timeout = TimeSpan.FromSeconds(1);

        Assert.True(RegexPool.IsMatch(input, pattern, options, timeout, useCache));
        Assert.Equal("name=alice", RegexPool.Match(input, pattern, options, timeout, useCache).Value);
        Assert.Equal(
            new[] { "name=alice", "id=42" },
            RegexPool.Matches(input, pattern, options, timeout, useCache).Cast<Match>().Select(match => match.Value).ToArray());
        Assert.Equal(
            "name:alice; id:42",
            RegexPool.Replace(input, pattern, "${key}:${value}", options, timeout, useCache));
        Assert.Equal(
            new[] { "name=alice", "id=42" },
            RegexPool.Split(input, @"\s*;\s*", options, timeout, useCache));
        Assert.Equal("alice", RegexPool.GetValue(input, pattern, "${value}", options, timeout, useCache));

        var values = RegexPool.GetValues(input, pattern, new[] { "${key}", "${value}" }, options, timeout, useCache);
        Assert.Equal("name", values["${key}"]);
        Assert.Equal("alice", values["${value}"]);
        Assert.Equal(useCache ? 2 : 0, RegexPool.Count);
    }

    [Fact]
    public void ExplicitOperations_PreserveWhitespaceInputResults()
    {
        const string pattern = "a+";
        const RegexOptions options = RegexOptions.None;
        var timeout = TimeSpan.FromMilliseconds(100);
        var inputs = new[] { null!, string.Empty, " \t\r\n" };

        foreach (var input in inputs)
        {
            Assert.Equal(string.Empty, RegexPool.Replace(input, pattern, "x", options, timeout));
            Assert.Empty(RegexPool.Split(input, pattern, options, timeout));
            Assert.Equal(string.Empty, RegexPool.GetValue(input, pattern, string.Empty, options, timeout));
            Assert.Empty(RegexPool.GetValues(input, pattern, Array.Empty<string>(), options, timeout));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExplicitOperations_ThrowOnCatastrophicBacktracking(bool useCache)
    {
        const string pattern = @"^(a+)+$";
        const RegexOptions options = RegexOptions.None;
        var timeout = TimeSpan.FromMilliseconds(15);
        var input = new string('a', 10000) + "!";

        Assert.Throws<RegexMatchTimeoutException>(
            () => RegexPool.IsMatch(input, pattern, options, timeout, useCache));
        Assert.Throws<RegexMatchTimeoutException>(
            () => RegexPool.Match(input, pattern, options, timeout, useCache));

        var matches = RegexPool.Matches(input, pattern, options, timeout, useCache);
        Assert.Throws<RegexMatchTimeoutException>(() => matches.Cast<Match>().ToArray());

        Assert.Throws<RegexMatchTimeoutException>(
            () => RegexPool.Replace(input, pattern, "x", options, timeout, useCache));
        Assert.Throws<RegexMatchTimeoutException>(
            () => RegexPool.Split(input, pattern, options, timeout, useCache));
        Assert.Throws<RegexMatchTimeoutException>(
            () => RegexPool.GetValue(input, pattern, string.Empty, options, timeout, useCache));
        Assert.Throws<RegexMatchTimeoutException>(
            () => RegexPool.GetValues(input, pattern, new[] { "$0" }, options, timeout, useCache));
    }
}
