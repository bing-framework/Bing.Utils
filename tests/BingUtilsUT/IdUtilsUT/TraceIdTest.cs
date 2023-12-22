﻿using Bing.IdUtils;
using Xunit.Abstractions;

namespace BingUtilsUT.IdUtilsUT;

/// <summary>
/// 跟踪ID 测试
/// </summary>
[Trait("IdUtilsUT", "TraceIdAccessor")]
public class TraceIdTest
{
    protected ITestOutputHelper Output { get; }

    public TraceIdTest(ITestOutputHelper output)
    {
        Output = output;
    }

    /// <summary>
    /// 测试 - 跟踪ID生成
    /// </summary>
    [Fact]
    public void Test_TraceIdAccessor()
    {
        var accessor1 = new TraceIdAccessor(null);
        var accessor2 = new TraceIdAccessor(null);

        var id1 = accessor1.GetTraceId();
        var id2 = accessor2.GetTraceId();

        id1.ShouldNotBe(id2);

        Output.WriteLine(id1);
        Output.WriteLine(id2);
    }
}