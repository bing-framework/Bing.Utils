namespace Bing.Helpers;

/// <summary>
/// 测试集合：环境变量相关测试串行执行，避免进程级环境变量互相污染。
/// </summary>
[CollectionDefinition("EnvSerial", DisableParallelization = true)]
public sealed class EnvTestCollection;
