namespace Bing.Helpers;

/// <summary>
/// 将受运行时负载影响的性能测试与其他测试隔离并串行执行。
/// </summary>
[CollectionDefinition("TimingSensitive", DisableParallelization = true)]
public sealed class TimingSensitiveTestCollection;
