using RuntimeCycleFixture.B;
using RuntimeCycleFixture.D;

namespace RuntimeCycleFixture.E;

/// <summary>
/// E 程序集中的类型，并通过两个公开字段分别引用 D 和 B 程序集。
/// </summary>
public sealed class EMarker
{
    /// <summary>
    /// 指向 D 程序集类型的字段，用于生成 E 到 D 的真实 AssemblyRef。
    /// </summary>
    public DMarker? DeepPeer;

    /// <summary>
    /// 指向 B 程序集类型的字段，用于生成 E 到 B 的真实 AssemblyRef。
    /// </summary>
    public BMarker? CyclePeer;
}
