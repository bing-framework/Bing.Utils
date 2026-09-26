using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Bing.Extra.Emoji;
using Shouldly;
using Xunit;

namespace Bing.Utils.Extra.Tests.Bing.Extra.Emoji;

/// <summary>
/// Emoji 官方数据覆盖测试。
/// </summary>
/// <remarks>使用嵌入的 Unicode emoji-test 和 gemoji 清单独立验证内置索引。</remarks>
[Trait("Category", "Emoji.OfficialData")]
public sealed class EmojiOfficialDataTests
{
    /// <summary>
    /// Unicode emoji-test 清单的嵌入资源名称。
    /// </summary>
    private const string EmojiTestResourceName = "Bing.Utils.Extra.Tests.emoji-test.txt";

    /// <summary>
    /// gemoji 别名清单的嵌入资源名称。
    /// </summary>
    private const string GemojiResourceName = "Bing.Utils.Extra.Tests.gemoji.json";

    /// <summary>
    /// 验证 Unicode 清单中的三种 qualification 状态均可识别。
    /// </summary>
    [Fact]
    public void UnicodeEmojiTest_CoversAllSupportedQualificationStatuses()
    {
        var entries = ReadEmojiTestEntries();

        entries.Count.ShouldBeGreaterThan(3000);
        entries.Any(entry => entry.Status == "fully-qualified").ShouldBeTrue();
        entries.Any(entry => entry.Status == "minimally-qualified").ShouldBeTrue();
        entries.Any(entry => entry.Status == "unqualified").ShouldBeTrue();

        foreach (var entry in entries)
        {
            EmojiUtil.IsEmoji(entry.Value).ShouldBeTrue($"Unicode status {entry.Status}: {entry.Value}");
            EmojiUtil.Count(entry.Value).ShouldBe(1, $"Unicode status {entry.Status}: {entry.Value}");
            var match = EmojiUtil.FindAll(entry.Value).Single();
            match.Value.ShouldBe(entry.Value);
            match.Length.ShouldBe(entry.Value.Length);
            EmojiUtil.TryGetByUnicode(entry.Value, out var info).ShouldBeTrue(entry.Value);
            info.ShouldNotBeNull();
            EmojiUtil.RemoveAllEmojis(entry.Value).ShouldBe(string.Empty);
        }
    }

    /// <summary>
    /// 验证规范 Emoji 元数据与 Unicode 清单一致。
    /// </summary>
    [Fact]
    public void GetAll_ContainsCanonicalOfficialEntriesWithCompleteMetadata()
    {
        var official = ReadEmojiTestEntries()
            .Where(entry => entry.Status == "fully-qualified")
            .Select(entry => entry.Value)
            .ToHashSet(StringComparer.Ordinal);
        var all = EmojiUtil.GetAll();

        all.Count.ShouldBe(official.Count);
        all.Select(info => info.Unicode).Distinct(StringComparer.Ordinal).Count().ShouldBe(all.Count);

        foreach (var info in all)
        {
            official.Contains(info.Unicode).ShouldBeTrue(info.Unicode);
            info.Name.ShouldNotBeNullOrWhiteSpace();
            info.Group.ShouldNotBeNullOrWhiteSpace();
            info.Subgroup.ShouldNotBeNullOrWhiteSpace();
            info.Version.ShouldNotBeNullOrWhiteSpace();
            info.Aliases.ShouldNotBeNull();
            EmojiUtil.TryGetByUnicode(info.Unicode, out var indexed).ShouldBeTrue(info.Unicode);
            indexed.ShouldBeSameAs(info);
        }
    }

    /// <summary>
    /// 验证 gemoji 别名快照可完整查询和转换。
    /// </summary>
    [Fact]
    public void GemojiAliasSnapshot_ResolvesOfficialAliases()
    {
        var entries = ReadGemojiEntries();

        entries.Count.ShouldBeGreaterThan(1500);
        var grinning = entries.Single(entry => entry.Aliases.Contains("grinning", StringComparer.Ordinal));
        EmojiUtil.TryGetByAlias("grinning", out var grinningInfo).ShouldBeTrue();
        grinningInfo.ShouldNotBeNull();
        grinningInfo.Unicode.ShouldBe(grinning.Emoji);

        foreach (var entry in entries)
        {
            EmojiUtil.TryGetByUnicode(entry.Emoji, out var expected).ShouldBeTrue(entry.Emoji);
            foreach (var alias in entry.Aliases)
            {
                EmojiUtil.TryGetByAlias(alias, out var actual).ShouldBeTrue(alias);
                actual.ShouldBeSameAs(expected);
                EmojiUtil.ToUnicode(":" + alias + ":").ShouldBe(expected.Unicode);
            }
            EmojiUtil.ToAlias(entry.Emoji).ShouldBe(":" + entry.Aliases[0] + ":");
        }
    }

    /// <summary>
    /// 读取嵌入的 Unicode Emoji 测试清单。
    /// </summary>
    /// <returns>解析后的 Unicode Emoji 清单条目。</returns>
    private static IReadOnlyList<EmojiTestEntry> ReadEmojiTestEntries()
    {
        using var stream = OpenResource(EmojiTestResourceName);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var entries = new List<EmojiTestEntry>();
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            var semicolon = line.IndexOf(';');
            var comment = line.IndexOf('#');
            if (semicolon < 0 || comment < 0 || semicolon > comment)
                continue;

            var status = line.Substring(semicolon + 1, comment - semicolon - 1).Trim();
            if (status != "fully-qualified" && status != "minimally-qualified" && status != "unqualified")
                continue;

            var value = string.Concat(line.Substring(0, semicolon)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(codePoint => char.ConvertFromUtf32(int.Parse(codePoint, NumberStyles.HexNumber, CultureInfo.InvariantCulture))));
            entries.Add(new EmojiTestEntry(value, status));
        }

        return entries;
    }

    /// <summary>
    /// 读取嵌入的 gemoji 别名清单。
    /// </summary>
    /// <returns>解析后的 gemoji 别名条目。</returns>
    private static IReadOnlyList<GemojiEntry> ReadGemojiEntries()
    {
        using var stream = OpenResource(GemojiResourceName);
        using var document = JsonDocument.Parse(stream);
        var entries = new List<GemojiEntry>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var names = element.GetProperty("aliases")
                .EnumerateArray()
                .Select(name => name.GetString())
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!)
                .ToArray();
            entries.Add(new GemojiEntry(element.GetProperty("emoji").GetString()!, names));
        }

        return entries;
    }

    /// <summary>
    /// 打开指定的嵌入测试资源。
    /// </summary>
    /// <param name="name">嵌入资源名称。</param>
    /// <returns>可读取的资源流。</returns>
    /// <exception cref="InvalidOperationException">指定的嵌入资源不存在。</exception>
    /// <remarks>返回的资源流由调用方释放。</remarks>
    private static Stream OpenResource(string name)
    {
        return typeof(EmojiOfficialDataTests).GetTypeInfo().Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Missing embedded test resource: {name}");
    }

    /// <summary>
    /// Unicode Emoji 清单中的测试条目。
    /// </summary>
    private sealed class EmojiTestEntry
    {
        /// <summary>
        /// 初始化 <see cref="EmojiTestEntry" /> 类的新实例。
        /// </summary>
        /// <param name="value">Emoji 字符序列。</param>
        /// <param name="status">Emoji 的 qualification 状态。</param>
        internal EmojiTestEntry(string value, string status)
        {
            Value = value;
            Status = status;
        }

        /// <summary>
        /// 获取条目的 Emoji 字符序列。
        /// </summary>
        internal string Value { get; }

        /// <summary>
        /// 获取条目的 qualification 状态。
        /// </summary>
        internal string Status { get; }
    }

    /// <summary>
    /// gemoji 清单中的别名条目。
    /// </summary>
    private sealed class GemojiEntry
    {
        /// <summary>
        /// 初始化 <see cref="GemojiEntry" /> 类的新实例。
        /// </summary>
        /// <param name="emoji">条目的 Emoji 字符序列。</param>
        /// <param name="aliases">条目对应的不带冒号别名列表。</param>
        internal GemojiEntry(string emoji, IReadOnlyList<string> aliases)
        {
            Emoji = emoji;
            Aliases = aliases;
        }

        /// <summary>
        /// 获取条目的 Emoji 字符序列。
        /// </summary>
        internal string Emoji { get; }

        /// <summary>
        /// 获取条目对应的不带冒号别名列表。
        /// </summary>
        internal IReadOnlyList<string> Aliases { get; }
    }
}
