using System;
using System.Collections.Generic;
using Bing.IO;
using Xunit;

namespace Bing.Utils.Tests.Bing.IO;

/// <summary>验证规则冲突、掩码及部分前缀的组合语义，不依赖内置规则排列。</summary>
public class FileContentDetectionRuleTests
{
    /// <summary>不同格式具有相同签名时保持歧义，不因规则顺序不同选择不同结果。</summary>
    [Fact]
    public void Match_ConflictingFormats_RemainsAmbiguousInEitherOrder()
    {
        var first = Rule("first", 0x41, 0x42);
        var second = Rule("second", 0x41, 0x42);
        foreach (var rules in new[] { new[] { first, second }, new[] { second, first } })
        {
            var result = Match(new byte[] { 0x41, 0x42 }, rules);
            Assert.Equal(FileDetectionStatus.Ambiguous, result.Status);
            Assert.Null(result.Format);
            Assert.Equal(2, result.Candidates.Count);
            Assert.Equal(FileExtensionMatch.Indeterminate, FileContentDetector.CompareExtension(result, "jpg"));
        }
    }

    /// <summary>同一格式的多个变体不产生歧义，候选快照不可变。</summary>
    [Fact]
    public void Match_SameFormatVariants_ReturnsOneImmutableCandidate()
    {
        var result = Match(new byte[] { 0x41, 0x42 }, Rule("same", 0x41), Rule("same", 0x41, 0x42));
        Assert.Equal(FileDetectionStatus.Matched, result.Status);
        Assert.Single(result.Candidates);
        Assert.Throws<NotSupportedException>(() => ((IList<FileContentFormat>)result.Candidates).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)result.Format.Extensions).Clear());
    }

    /// <summary>完整短签名不能掩盖仍可能匹配另一格式的长签名。</summary>
    [Fact]
    public void Match_UnresolvedLongerRule_ReturnsIncompleteUntilDisambiguated()
    {
        var rules = new[] { Rule("short", 0x41), Rule("long", 0x41, 0x42) };
        var result = Match(new byte[] { 0x41 }, rules);
        Assert.Equal(FileDetectionStatus.Incomplete, result.Status);
        Assert.Equal(FileDetectionReason.Truncated, result.ReasonCode);
        Assert.Null(result.Format);
        Assert.Single(result.Candidates);
        Assert.Equal("short", Match(new byte[] { 0x41, 0x43 }, rules).Format.Id);
    }

    /// <summary>固定偏移、多个片段与掩码必须同时生效；规则复制输入字节。</summary>
    [Fact]
    public void Match_MaskedOffsetSegments_RequiresAllConstrainedBits()
    {
        var pattern = new byte[] { 0xa0 };
        var mask = new byte[] { 0xf0 };
        var rule = new FileSignatureRule(new FileContentFormat("masked", "application/test", false, "mask"),
            new FileSignatureSegment(0, new byte[] { 0x41 }), new FileSignatureSegment(3, pattern, mask));
        pattern[0] = 0;
        mask[0] = 0;
        Assert.Equal(FileDetectionStatus.Matched, Match(new byte[] { 0x41, 0xff, 0xff, 0xaf }, rule).Status);
        Assert.Equal(FileDetectionStatus.Unknown, Match(new byte[] { 0x41, 0xff, 0xff, 0xbf }, rule).Status);
        Assert.Equal(FileDetectionStatus.Unknown, Match(new byte[] { 0x42, 0xff, 0xff, 0xaf }, rule).Status);
        Assert.Equal(FileDetectionStatus.Incomplete, Match(new byte[] { 0x41, 0xff }, rule).Status);
    }

    /// <summary>创建独立测试规则。</summary>
    private static FileSignatureRule Rule(string id, params byte[] bytes)
        => new(new FileContentFormat(id, "application/test", false, id), new FileSignatureSegment(0, bytes));

    /// <summary>通过内部匹配入口验证没有内置样本可表达的歧义。</summary>
    private static FileDetectionResult Match(byte[] input, params FileSignatureRule[] rules)
        => FileContentSignatures.Match(input, rules, FileDetectionReason.Truncated);
}
