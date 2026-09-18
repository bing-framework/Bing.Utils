using BenchmarkDotNet.Attributes;
using Bing.Security.Cryptography;
using Bing.Security.Randomness;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 测量 BSS2 AES-GCM 认证流对大文件负载的加密性能。
/// </summary>
[MemoryDiagnoser]
public class AesGcmStreamBenchmarks
{
    /// <summary>
    /// 本轮认证流基准覆盖的输入长度，单位为字节。
    /// </summary>
    [Params(10 * 1024 * 1024, 100 * 1024 * 1024)]
    public int PayloadSize { get; set; }

    /// <summary>
    /// 固定的流输入数据。
    /// </summary>
    private byte[] _payload = null!;

    /// <summary>
    /// AES-256 主密钥。
    /// </summary>
    private byte[] _key = null!;

    /// <summary>
    /// 初始化流基准输入和密钥。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _payload = SecurityRandom.GetBytes(PayloadSize);
        _key = SecurityRandom.GetBytes(32);
    }

    /// <summary>
    /// 清除流基准阶段保留的敏感数据。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        Array.Clear(_payload, 0, _payload.Length);
        Array.Clear(_key, 0, _key.Length);
    }

    /// <summary>
    /// 测量使用默认 64 KiB 分块大小的 BSS2 流加密。
    /// </summary>
    /// <returns>表示认证流加密操作的任务。</returns>
    [Benchmark]
    public async Task EncryptBss2Async()
    {
        using var input = new MemoryStream(_payload, writable: false);
        using var output = new MemoryStream();
        await AesGcmStreamEncryption.EncryptAsync(input, output, _key).ConfigureAwait(false);
    }
}