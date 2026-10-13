namespace Bing.Helpers;

/// <summary>
/// 测试集合：结构体映射缓存计数断言串行执行，避免进程级计数受到其他映射测试干扰。
/// </summary>
[CollectionDefinition("MapperHelperCacheSerial", DisableParallelization = true)]
public sealed class MapperHelperCacheTestCollection;
