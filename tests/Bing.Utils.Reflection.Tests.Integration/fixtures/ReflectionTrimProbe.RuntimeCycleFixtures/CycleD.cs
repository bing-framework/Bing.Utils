using RuntimeCycleFixture.A;
using RuntimeCycleFixture.C;

namespace RuntimeCycleFixture.D;

/// <summary>
/// D 程序集中的类型，并通过两个公开字段分别引用 A 和 C 程序集。
/// </summary>
public sealed class DMarker
{
    /// <summary>
    /// 指向 A 程序集类型的字段，用于生成 D 到 A 的真实 AssemblyRef。
    /// </summary>
    public AMarker? RootPeer;

    /// <summary>
    /// 指向 C 程序集类型的字段，用于生成 D 到 C 的真实 AssemblyRef。
    /// </summary>
    public CMarker? BranchPeer;
}
