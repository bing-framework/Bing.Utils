using RuntimeCycleFixture.A;

namespace RuntimeCycleFixture.B;

/// <summary>
/// B 程序集中的类型，并通过字段引用 A 程序集。
/// </summary>
public sealed class BMarker
{
    /// <summary>
    /// 指向 A 程序集类型的字段，用于生成 B 到 A 的 AssemblyRef。
    /// </summary>
    public AMarker? Peer;
}
