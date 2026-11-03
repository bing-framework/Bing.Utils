namespace Bing.Helpers;

/// <summary>
/// 串行执行访问共享 Id 静态状态的测试，避免测试之间互相污染。
/// </summary>
[CollectionDefinition("Id static state", DisableParallelization = true)]
public sealed class IdStaticStateCollection
{
}
