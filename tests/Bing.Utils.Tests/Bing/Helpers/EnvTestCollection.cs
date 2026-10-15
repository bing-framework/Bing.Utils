namespace Bing.Helpers;

/// <summary>
/// 环境变量相关测试的串行集合。
/// </summary>
/// <remarks>串行执行避免进程级环境变量互相污染。</remarks>
[CollectionDefinition("EnvSerial", DisableParallelization = true)]
public sealed class EnvTestCollection;
