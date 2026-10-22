using RuntimeCycleFixture.B;

namespace RuntimeCycleFixture.A;

/// <summary>
/// A 程序集中的类型，并通过字段引用 B 程序集。
/// </summary>
public sealed class AMarker
{
    /// <summary>
    /// 指向 B 程序集类型的字段，用于生成 A 到 B 的 AssemblyRef。
    /// </summary>
    public BMarker? Peer;
}
