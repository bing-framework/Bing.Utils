using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Text;
using TextEncoding = System.Text.Encoding;
using Bing.Security.Encoding;
using Shouldly;

namespace Bing.Security.Gm;

/// <summary>SM3、HMAC-SM3 与 SM4-GCM 互操作测试。</summary>
public class GmHashAndInteroperabilityTests
{
    [Fact]
    public async Task Sm3AndHmacSm3_StreamApis_ShouldMatchOneShotApis()
    {
        var data = TextEncoding.UTF8.GetBytes("streaming-sm3-data");
        var key = TextEncoding.UTF8.GetBytes("high-entropy-test-key");
        using var sm3Stream = new MemoryStream(data);
        using var hmacStream = new MemoryStream(data);

        var sm3 = await Sm3.ComputeAsync(sm3Stream);
        var hmac = await HmacSm3.ComputeAsync(key, hmacStream);

        sm3.ShouldBe(Sm3.Compute(data));
        hmac.ShouldBe(HmacSm3.Compute(key, data));
        HmacSm3.Verify(key, data, hmac).ShouldBeTrue();
        HmacSm3.Verify(key, TextEncoding.UTF8.GetBytes("changed"), hmac).ShouldBeFalse();
        Sm3.ComputeHex(data).ShouldBe(HexEncoding.Encode(sm3));
        HmacSm3.ComputeBase64(key, data).ShouldBe(Convert.ToBase64String(hmac));
    }

    [Fact]
    public void HmacSm3_WhenIetfVectorIsProvided_ShouldMatch()
    {
        // draft-guo-ipsecme-ikev2-using-shangmi-03, Appendix A.5.
        var key = HexEncoding.Decode("00112233445566778899AABBCCDDEEFF");
        var data = TextEncoding.ASCII.GetBytes("abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq");

        HmacSm3.ComputeHex(key, data).ShouldBe("dc813339153491ad81477754eb3df00dbb3cc3e6a69f9cacce737db7e61342ff");
    }

    [Fact]
    public void Sm4Gcm_WhenRfc8998VectorIsProvided_ShouldDecrypt()
    {
        // RFC 8998 Appendix A.1; all values are network-order hexadecimal.
        var nonce = HexEncoding.Decode("00001234567800000000ABCD");
        var key = HexEncoding.Decode("0123456789ABCDEFFEDCBA9876543210");
        var plaintext = HexEncoding.Decode("AAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBCCCCCCCCCCCCCCCCDDDDDDDDDDDDDDDDEEEEEEEEEEEEEEEEFFFFFFFFFFFFFFFFEEEEEEEEEEEEEEEEAAAAAAAAAAAAAAAA");
        var aad = HexEncoding.Decode("FEEDFACEDEADBEEFFEEDFACEDEADBEEFABADDAD2");
        var ciphertext = HexEncoding.Decode("17F399F08C67D5EE19D0DC9969C4BB7D5FD46FD3756489069157B282BB200735D82710CA5C22F0CCFA7CBF93D496AC15A56834CBCF98C397B4024A2691233B8D");
        var tag = HexEncoding.Decode("83DE3541E4C2B58177E065A9BF7B62EC");
        var payload = new Sm4GcmPayload(nonce, ciphertext, tag);

        Sm4GcmEncryption.Decrypt(payload, key, aad).ShouldBe(plaintext);
    }

    [Fact]
    public async Task Sm3AndHmacSm3_WhenCancelled_ShouldStop()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var first = new MemoryStream(new byte[1]);
        using var second = new MemoryStream(new byte[1]);

        await Should.ThrowAsync<OperationCanceledException>(() => Sm3.ComputeAsync(first, cancellation.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => HmacSm3.ComputeAsync(new byte[] { 1 }, second, cancellation.Token));
    }
}