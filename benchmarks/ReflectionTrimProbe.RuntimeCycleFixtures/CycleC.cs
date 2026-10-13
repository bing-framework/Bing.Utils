using RuntimeCycleFixture.B;

namespace RuntimeCycleFixture.C;

/// <summary>
/// C 程序集中的类型，并通过字段引用 B 程序集。
/// </summary>
public sealed class CMarker
{
    /// <summary>
    /// 指向 B 程序集类型的字段，用于生成 C 到 B 的真实 AssemblyRef。
    /// </summary>
    public BMarker? Peer;
}
