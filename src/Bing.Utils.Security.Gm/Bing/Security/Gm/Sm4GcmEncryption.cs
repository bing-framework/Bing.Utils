using System;
using System.Security.Cryptography;
using Bing.Security.Randomness;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Engines;

namespace Bing.Security.Gm;

/// <summary>
/// 提供 SM4-GCM 认证加密操作。
/// </summary>
public static class Sm4GcmEncryption
{
    /// <summary>
    /// SM4 密钥长度，单位为字节。
    /// </summary>
    public const int KeySize = 16;

    /// <summary>
    /// 使用 SM4-GCM 加密明文。
    /// </summary>
    /// <param name="plaintext">要加密的明文。</param>
    /// <param name="key">16 字节 SM4 密钥。</param>
    /// <param name="associatedData">可选附加认证数据，不会写入密文。</param>
    /// <returns>包含 Nonce、密文和认证标签的载荷。</returns>
    public static Sm4GcmPayload Encrypt(byte[] plaintext, byte[] key, byte[] associatedData = null)
    {
        if (plaintext == null)
            throw new ArgumentNullException(nameof(plaintext));
        ValidateKey(key);
        var nonce = SecurityRandom.GetBytes(Sm4GcmPayload.NonceSize);
        var cipher = CreateCipher(true, key, nonce, associatedData);
        var output = new byte[cipher.GetOutputSize(plaintext.Length)];
        byte[] ciphertext = null;
        byte[] tag = null;
        try
        {
            var length = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
            length += cipher.DoFinal(output, length);
            ciphertext = new byte[length - Sm4GcmPayload.TagSize];
            tag = new byte[Sm4GcmPayload.TagSize];
            Buffer.BlockCopy(output, 0, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(output, ciphertext.Length, tag, 0, tag.Length);
            return new Sm4GcmPayload(nonce, ciphertext, tag);
        }
        finally
        {
            GmCryptographicOperationsCompat.ZeroMemory(nonce);
            GmCryptographicOperationsCompat.ZeroMemory(output);
            if (ciphertext != null)
                GmCryptographicOperationsCompat.ZeroMemory(ciphertext);
            if (tag != null)
                GmCryptographicOperationsCompat.ZeroMemory(tag);
        }
    }

    /// <summary>
    /// 验证 SM4-GCM 认证标签并解密载荷。
    /// </summary>
    /// <param name="payload">SM4-GCM 认证载荷。</param>
    /// <param name="key">16 字节 SM4 密钥。</param>
    /// <param name="associatedData">加密时使用的附加认证数据。</param>
    /// <returns>认证成功后的明文。</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">认证标签、密钥或附加认证数据无效时抛出。</exception>
    public static byte[] Decrypt(Sm4GcmPayload payload, byte[] key, byte[] associatedData = null)
    {
        if (payload == null)
            throw new ArgumentNullException(nameof(payload));
        ValidateKey(key);
        byte[] encrypted = null;
        byte[] output = null;
        byte[] payloadCiphertext = null;
        byte[] payloadTag = null;
        byte[] payloadNonce = null;
        try
        {
            payloadCiphertext = payload.Ciphertext;
            payloadTag = payload.Tag;
            payloadNonce = payload.Nonce;
            encrypted = new byte[payloadCiphertext.Length + payloadTag.Length];
            Buffer.BlockCopy(payloadCiphertext, 0, encrypted, 0, payloadCiphertext.Length);
            Buffer.BlockCopy(payloadTag, 0, encrypted, payloadCiphertext.Length, payloadTag.Length);
            var cipher = CreateCipher(false, key, payloadNonce, associatedData);
            output = new byte[cipher.GetOutputSize(encrypted.Length)];
            var length = cipher.ProcessBytes(encrypted, 0, encrypted.Length, output, 0);
            length += cipher.DoFinal(output, length);
            if (length == output.Length)
            {
                var fullOutput = output;
                output = null;
                return fullOutput;
            }
            var result = new byte[length];
            Buffer.BlockCopy(output, 0, result, 0, length);
            return result;
        }
        catch (Org.BouncyCastle.Crypto.InvalidCipherTextException exception)
        {
            throw new System.Security.Cryptography.CryptographicException("SM4-GCM 认证失败。", exception);
        }
        finally
        {
            if (encrypted != null)
                GmCryptographicOperationsCompat.ZeroMemory(encrypted);
            if (output != null)
                GmCryptographicOperationsCompat.ZeroMemory(output);
            if (payloadCiphertext != null)
                GmCryptographicOperationsCompat.ZeroMemory(payloadCiphertext);
            if (payloadTag != null)
                GmCryptographicOperationsCompat.ZeroMemory(payloadTag);
            if (payloadNonce != null)
                GmCryptographicOperationsCompat.ZeroMemory(payloadNonce);
        }
    }

    /// <summary>
    /// 创建配置完成的 SM4-GCM 密码器。
    /// </summary>
    /// <param name="forEncryption">为 <c>true</c> 时用于加密。</param>
    /// <param name="key">SM4 密钥。</param>
    /// <param name="nonce">GCM Nonce。</param>
    /// <param name="associatedData">附加认证数据。</param>
    /// <returns>已初始化的 GCM 密码器。</returns>
    private static GcmBlockCipher CreateCipher(bool forEncryption, byte[] key, byte[] nonce, byte[] associatedData)
    {
        var cipher = new GcmBlockCipher(new SM4Engine());
        cipher.Init(forEncryption, new AeadParameters(new KeyParameter(key), Sm4GcmPayload.TagSize * 8, nonce, associatedData));
        return cipher;
    }

    /// <summary>
    /// 验证 SM4 密钥长度。
    /// </summary>
    /// <param name="key">SM4 密钥。</param>
    /// <exception cref="ArgumentException">密钥不是 16 字节时抛出。</exception>
    private static void ValidateKey(byte[] key)
    {
        if (key == null || key.Length != KeySize)
            throw new ArgumentException("SM4 密钥必须为 16 字节。", nameof(key));
    }
}