using BenchmarkDotNet.Attributes;
using Bing.Security.Cryptography;
using Bing.Security.Keys;
using Bing.Security.Signatures;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>测量包含 PEM 导入成本的 RSA-OAEP/PSS 公共 API。</summary>
[MemoryDiagnoser]
public class RsaSecurityBenchmarks
{
    private RsaKeyPair _keyPair = null!;
    private byte[] _data = null!;
    private byte[] _ciphertext = null!;
    private byte[] _signature = null!;

    /// <summary>在计时外准备密钥和消息。</summary>
    [GlobalSetup]
    public void Setup()
    {
        _keyPair = RsaKeyGenerator.Generate(2048);
        _data = new byte[32];
        _ciphertext = RsaEncryption.Encrypt(_data, _keyPair.PublicKeyPem);
        _signature = RsaSignature.Sign(_data, _keyPair.PrivateKeyPem);
    }

    /// <summary>RSA-OAEP-SHA256 加密。</summary>
    [Benchmark]
    public byte[] Encrypt() => RsaEncryption.Encrypt(_data, _keyPair.PublicKeyPem);
    /// <summary>RSA-OAEP-SHA256 解密。</summary>
    [Benchmark]
    public byte[] Decrypt() => RsaEncryption.Decrypt(_ciphertext, _keyPair.PrivateKeyPem);
    /// <summary>RSA-PSS-SHA256 签名。</summary>
    [Benchmark]
    public byte[] Sign() => RsaSignature.Sign(_data, _keyPair.PrivateKeyPem);
    /// <summary>RSA-PSS-SHA256 验签。</summary>
    [Benchmark]
    public bool Verify() => RsaSignature.Verify(_data, _signature, _keyPair.PublicKeyPem);
}
