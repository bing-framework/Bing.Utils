using Shouldly;

namespace Bing.Reflection;

/// <summary>成员路径资源边界及拒绝写入的回归测试。</summary>
public class ReflectionsMemberPathResourceTests
{
    [Fact]
    public void MemberPath_TooLong_RejectsBeforeReadingOrWriting()
    {
        var sample = new Node();
        var path = new string('X', 4097);
        Reflections.TryGetMemberValueByPath(sample, path, out _).ShouldBeFalse();
        Reflections.TrySetMemberValueByPath(sample, path, 42).ShouldBeFalse();
        Should.Throw<ArgumentException>(() => Reflections.GetMemberValueByPath(sample, path)).ParamName.ShouldBe("memberPath");
        sample.Value.ShouldBe(0);
    }

    [Fact]
    public void MemberPath_DepthBoundary_Allows64AndRejects65()
    {
        var sample = new Node();
        var accepted = string.Join(".", Enumerable.Repeat(nameof(Node.Next), 63).Append(nameof(Node.Value)));
        var rejected = nameof(Node.Next) + "." + accepted;
        Reflections.TrySetMemberValueByPath(sample, accepted, 7).ShouldBeTrue();
        Reflections.TryGetMemberValueByPath(sample, accepted, out var value).ShouldBeTrue();
        value.ShouldBe(7);
        Reflections.TrySetMemberValueByPath(sample, rejected, 42).ShouldBeFalse();
        Reflections.TryGetMemberValueByPath(sample, rejected, out _).ShouldBeFalse();
        Should.Throw<ArgumentException>(() => Reflections.SetMemberValueByPath(sample, rejected, 42)).ParamName.ShouldBe("memberPath");
        sample.Value.ShouldBe(7);
    }

    [Fact]
    public void MemberPath_DbNull_WhenConversionDisabled_PreservesAssignableValue()
    {
        var sample = new DbNullTarget { Value = "before" };

        Reflections.TrySetMemberValueByPath(sample, nameof(DbNullTarget.Value), DBNull.Value,
            new MemberPathOptions { ConvertAssignedValue = false }).ShouldBeTrue();
        sample.Value.ShouldBeSameAs(DBNull.Value);

        Reflections.TrySetMemberValueByPath(sample, nameof(DbNullTarget.Value), DBNull.Value,
            new MemberPathOptions { ConvertAssignedValue = true }).ShouldBeTrue();
        sample.Value.ShouldBeNull();
    }

    private sealed class Node
    {
        public Node Next => this;
        public int Value { get; set; }
    }

    [Fact]
    public void MemberPath_DbNull_WhenConversionDisabled_RejectsIncompatibleReferenceAndNullable()
    {
        var sample = new DbNullTarget { Text = "before", Number = 7 };
        var options = new MemberPathOptions { ConvertAssignedValue = false };
        Reflections.TrySetMemberValueByPath(sample, nameof(DbNullTarget.Text), DBNull.Value, options).ShouldBeFalse();
        Reflections.TrySetMemberValueByPath(sample, nameof(DbNullTarget.Number), DBNull.Value, options).ShouldBeFalse();
        sample.Text.ShouldBe("before");
        sample.Number.ShouldBe(7);

        options.ConvertAssignedValue = true;
        Reflections.TrySetMemberValueByPath(sample, nameof(DbNullTarget.Text), DBNull.Value, options).ShouldBeTrue();
        Reflections.TrySetMemberValueByPath(sample, nameof(DbNullTarget.Number), DBNull.Value, options).ShouldBeTrue();
        sample.Text.ShouldBeNull();
        sample.Number.ShouldBeNull();
    }

    private sealed class DbNullTarget
    {
        public object Value { get; set; }
        public string Text { get; set; }
        public int? Number { get; set; }
    }
}
