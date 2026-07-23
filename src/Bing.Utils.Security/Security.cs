using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Bing.Security
{
    /// <summary>密码学安全随机值及固定时间比较工具。</summary>
    public static class SecurityKeyGenerator
    {
        public static byte[] GenerateBytes(int length)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
            var result = new byte[length];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(result);
            return result;
        }

        public static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            var difference = 0;
            for (var i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }
    }

    /// <summary>Base64Url、Hex 及 UTF-8 编码工具。</summary>
    public static class SecurityEncoding
    {
        public static string ToBase64Url(byte[] value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static byte[] FromBase64Url(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var text = value.Replace('-', '+').Replace('_', '/');
            if (text.Length % 4 == 2) text += "==";
            else if (text.Length % 4 == 3) text += "=";
            else if (text.Length % 4 == 1) throw new FormatException("无效的 Base64Url 文本。");
            return Convert.FromBase64String(text);
        }

        public static string ToHex(byte[] value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            const string chars = "0123456789abcdef";
            var result = new char[value.Length * 2];
            for (var i = 0; i < value.Length; i++)
            {
                result[i * 2] = chars[value[i] >> 4];
                result[i * 2 + 1] = chars[value[i] & 15];
            }
            return new string(result);
        }

        internal static byte[] FromHex(string value)
        {
            if (value == null || value.Length % 2 != 0) throw new FormatException("无效的十六进制文本。");
            var result = new byte[value.Length / 2];
            for (var i = 0; i < result.Length; i++) result[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
            return result;
        }
    }
}

namespace Bing.Security.Hashing
{
    public enum HashAlgorithmType { Sha256, Sha384, Sha512, Md5, Sha1 }
    public enum HmacAlgorithmType { Sha256, Sha384, Sha512, Md5, Sha1 }

    /// <summary>SHA-2 哈希工具；MD5/SHA-1 仅保留用于旧数据兼容。</summary>
    public static class HashingProvider
    {
        public static byte[] Compute(byte[] value, HashAlgorithmType algorithm = HashAlgorithmType.Sha256)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            using (var hash = Create(algorithm)) return hash.ComputeHash(value);
        }

        public static byte[] Compute(string value, HashAlgorithmType algorithm = HashAlgorithmType.Sha256, Encoding encoding = null)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return Compute((encoding ?? Encoding.UTF8).GetBytes(value), algorithm);
        }

        public static byte[] Compute(Stream stream, HashAlgorithmType algorithm = HashAlgorithmType.Sha256)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using (var hash = Create(algorithm)) return hash.ComputeHash(stream);
        }

        public static string ComputeHex(string value, HashAlgorithmType algorithm = HashAlgorithmType.Sha256, Encoding encoding = null) =>
            Bing.Security.SecurityEncoding.ToHex(Compute(value, algorithm, encoding));

        private static HashAlgorithm Create(HashAlgorithmType algorithm)
        {
            switch (algorithm)
            {
                case HashAlgorithmType.Sha256: return SHA256.Create();
                case HashAlgorithmType.Sha384: return SHA384.Create();
                case HashAlgorithmType.Sha512: return SHA512.Create();
                case HashAlgorithmType.Md5: return MD5.Create();
                case HashAlgorithmType.Sha1: return SHA1.Create();
                default: throw new ArgumentOutOfRangeException(nameof(algorithm));
            }
        }
    }

    /// <summary>HMAC-SHA-2 工具；HMAC-MD5/SHA-1 仅用于兼容旧数据。</summary>
    public static class HmacProvider
    {
        public static byte[] Compute(byte[] value, byte[] key, HmacAlgorithmType algorithm = HmacAlgorithmType.Sha256)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (key == null || key.Length == 0) throw new ArgumentException("HMAC 密钥不能为空。", nameof(key));
            using (var hmac = Create(key, algorithm)) return hmac.ComputeHash(value);
        }

        public static byte[] Compute(string value, byte[] key, HmacAlgorithmType algorithm = HmacAlgorithmType.Sha256, Encoding encoding = null)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return Compute((encoding ?? Encoding.UTF8).GetBytes(value), key, algorithm);
        }

        public static string ComputeHex(string value, byte[] key, HmacAlgorithmType algorithm = HmacAlgorithmType.Sha256, Encoding encoding = null) =>
            Bing.Security.SecurityEncoding.ToHex(Compute(value, key, algorithm, encoding));

        private static HMAC Create(byte[] key, HmacAlgorithmType algorithm)
        {
            switch (algorithm)
            {
                case HmacAlgorithmType.Sha256: return new HMACSHA256(key);
                case HmacAlgorithmType.Sha384: return new HMACSHA384(key);
                case HmacAlgorithmType.Sha512: return new HMACSHA512(key);
                case HmacAlgorithmType.Md5: return new HMACMD5(key);
                case HmacAlgorithmType.Sha1: return new HMACSHA1(key);
                default: throw new ArgumentOutOfRangeException(nameof(algorithm));
            }
        }
    }
}

namespace Bing.Security.Symmetric
{
    /// <summary>AES-CBC + HMAC-SHA256 加密封装。</summary>
    public sealed class AesEncryptionEnvelope
    {
        public AesEncryptionEnvelope(byte[] iv, byte[] cipherText, byte[] authenticationTag)
        {
            Iv = iv ?? throw new ArgumentNullException(nameof(iv));
            CipherText = cipherText ?? throw new ArgumentNullException(nameof(cipherText));
            AuthenticationTag = authenticationTag ?? throw new ArgumentNullException(nameof(authenticationTag));
        }
        public byte[] Iv { get; }
        public byte[] CipherText { get; }
        public byte[] AuthenticationTag { get; }
        public override string ToString() => "BSE1." + Bing.Security.SecurityEncoding.ToBase64Url(Iv) + "." + Bing.Security.SecurityEncoding.ToBase64Url(CipherText) + "." + Bing.Security.SecurityEncoding.ToBase64Url(AuthenticationTag);
        public static AesEncryptionEnvelope Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("加密封装不能为空。", nameof(value));
            var parts = value.Split('.');
            if (parts.Length != 4 || parts[0] != "BSE1") throw new FormatException("无法识别的加密封装格式。");
            return new AesEncryptionEnvelope(Bing.Security.SecurityEncoding.FromBase64Url(parts[1]), Bing.Security.SecurityEncoding.FromBase64Url(parts[2]), Bing.Security.SecurityEncoding.FromBase64Url(parts[3]));
        }
    }

    /// <summary>提供带认证保护的 AES 加密。默认随机 IV，不复用静态密钥或静态偏移量。</summary>
    public static class AesCipher
    {
        private static readonly byte[] EncryptionLabel = Encoding.UTF8.GetBytes("Bing.Utils.Security/AES/Encryption");
        private static readonly byte[] AuthenticationLabel = Encoding.UTF8.GetBytes("Bing.Utils.Security/AES/Authentication");

        public static byte[] GenerateKey(int length = 32)
        {
            if (length < 16) throw new ArgumentOutOfRangeException(nameof(length));
            return Bing.Security.SecurityKeyGenerator.GenerateBytes(length);
        }

        public static AesEncryptionEnvelope Encrypt(byte[] plainText, byte[] key)
        {
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));
            ValidateKey(key);
            var encryptionKey = Derive(key, EncryptionLabel);
            var authenticationKey = Derive(key, AuthenticationLabel);
            var iv = Bing.Security.SecurityKeyGenerator.GenerateBytes(16);
            byte[] cipher;
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionKey;
                aes.IV = iv;
                using (var transform = aes.CreateEncryptor()) cipher = transform.TransformFinalBlock(plainText, 0, plainText.Length);
            }
            byte[] tag;
            using (var hmac = new HMACSHA256(authenticationKey))
            {
                var authenticated = new byte[iv.Length + cipher.Length];
                Buffer.BlockCopy(iv, 0, authenticated, 0, iv.Length);
                Buffer.BlockCopy(cipher, 0, authenticated, iv.Length, cipher.Length);
                tag = hmac.ComputeHash(authenticated);
            }
            return new AesEncryptionEnvelope(iv, cipher, tag);
        }

        public static string Encrypt(string plainText, byte[] key, Encoding encoding = null) => Encrypt((encoding ?? Encoding.UTF8).GetBytes(plainText ?? throw new ArgumentNullException(nameof(plainText))), key).ToString();

        public static byte[] Decrypt(AesEncryptionEnvelope envelope, byte[] key)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            ValidateKey(key);
            var encryptionKey = Derive(key, EncryptionLabel);
            var authenticationKey = Derive(key, AuthenticationLabel);
            byte[] expected;
            using (var hmac = new HMACSHA256(authenticationKey))
            {
                var authenticated = new byte[envelope.Iv.Length + envelope.CipherText.Length];
                Buffer.BlockCopy(envelope.Iv, 0, authenticated, 0, envelope.Iv.Length);
                Buffer.BlockCopy(envelope.CipherText, 0, authenticated, envelope.Iv.Length, envelope.CipherText.Length);
                expected = hmac.ComputeHash(authenticated);
            }
            if (!Bing.Security.SecurityKeyGenerator.FixedTimeEquals(expected, envelope.AuthenticationTag)) throw new CryptographicException("密文完整性校验失败。");
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionKey;
                aes.IV = envelope.Iv;
                using (var transform = aes.CreateDecryptor()) return transform.TransformFinalBlock(envelope.CipherText, 0, envelope.CipherText.Length);
            }
        }

        public static string Decrypt(string envelope, byte[] key, Encoding encoding = null) => (encoding ?? Encoding.UTF8).GetString(Decrypt(AesEncryptionEnvelope.Parse(envelope), key));

        private static byte[] Derive(byte[] key, byte[] label)
        {
            using (var hmac = new HMACSHA256(key)) return hmac.ComputeHash(label);
        }

        private static void ValidateKey(byte[] key)
        {
            if (key == null || key.Length < 16) throw new ArgumentException("主密钥长度不得小于 16 字节。", nameof(key));
        }
    }
}

namespace Bing.Security.Passwords
{
    /// <summary>PBKDF2-SHA256 密码哈希。格式：BSP1.iterations.salt.hash。</summary>
    public static class Pbkdf2PasswordHasher
    {
        public static string Hash(string password, int iterations = 210000, int saltLength = 16, int hashLength = 32)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (iterations < 10000) throw new ArgumentOutOfRangeException(nameof(iterations));
            var salt = Bing.Security.SecurityKeyGenerator.GenerateBytes(saltLength);
            var hash = Derive(password, salt, iterations, hashLength);
            return "BSP1." + iterations + "." + Bing.Security.SecurityEncoding.ToBase64Url(salt) + "." + Bing.Security.SecurityEncoding.ToBase64Url(hash);
        }

        public static bool Verify(string password, string encodedHash)
        {
            if (password == null || string.IsNullOrWhiteSpace(encodedHash)) return false;
            var parts = encodedHash.Split('.');
            int iterations;
            if (parts.Length != 4 || parts[0] != "BSP1" || !int.TryParse(parts[1], out iterations)) return false;
            try
            {
                var salt = Bing.Security.SecurityEncoding.FromBase64Url(parts[2]);
                var expected = Bing.Security.SecurityEncoding.FromBase64Url(parts[3]);
                return Bing.Security.SecurityKeyGenerator.FixedTimeEquals(expected, Derive(password, salt, iterations, expected.Length));
            }
            catch (FormatException) { return false; }
        }

        private static byte[] Derive(string password, byte[] salt, int iterations, int length)
        {
            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256)) return derive.GetBytes(length);
        }
    }
}

namespace Bing.Security.Asymmetric
{
    public sealed class RsaKeyPair
    {
        public RsaKeyPair(RSAParameters publicKey, RSAParameters privateKey) { PublicKey = publicKey; PrivateKey = privateKey; }
        public RSAParameters PublicKey { get; }
        public RSAParameters PrivateKey { get; }
    }

    /// <summary>RSA-OAEP-SHA256 加解密及 RSA-PSS-SHA256 签名验证。</summary>
    public static class RsaCipher
    {
        public static RsaKeyPair CreateKeyPair(int keySize = 3072)
        {
            if (keySize < 2048) throw new ArgumentOutOfRangeException(nameof(keySize));
            using (var rsa = RSA.Create())
            {
                rsa.KeySize = keySize;
                return new RsaKeyPair(rsa.ExportParameters(false), rsa.ExportParameters(true));
            }
        }

        public static byte[] Encrypt(byte[] value, RSAParameters publicKey)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            using (var rsa = RSA.Create()) { rsa.ImportParameters(publicKey); return rsa.Encrypt(value, RSAEncryptionPadding.OaepSHA256); }
        }
        public static byte[] Decrypt(byte[] value, RSAParameters privateKey)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            using (var rsa = RSA.Create()) { rsa.ImportParameters(privateKey); return rsa.Decrypt(value, RSAEncryptionPadding.OaepSHA256); }
        }
        public static byte[] Sign(byte[] value, RSAParameters privateKey)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            using (var rsa = RSA.Create()) { rsa.ImportParameters(privateKey); return rsa.SignData(value, HashAlgorithmName.SHA256, RSASignaturePadding.Pss); }
        }
        public static bool Verify(byte[] value, byte[] signature, RSAParameters publicKey)
        {
            if (value == null || signature == null) return false;
            using (var rsa = RSA.Create()) { rsa.ImportParameters(publicKey); return rsa.VerifyData(value, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss); }
        }
    }
}

namespace Bing.Encryption.Core
{
    [Obsolete("请迁移到 Bing.Utils.Security 的 Base64Url、Hex 或字节数组 API。")]
    public enum OutType { Base64, Hex }
}

namespace Bing.Encryption
{
    using Bing.Encryption.Core;
    using Bing.Security.Hashing;

    [Obsolete("请使用 System.Convert 或 Bing.Security.SecurityEncoding。")]
    public static class Base64ConvertProvider
    {
        public static string Encode(string value, Encoding encoding = null) => Convert.ToBase64String((encoding ?? Encoding.UTF8).GetBytes(value ?? throw new ArgumentNullException(nameof(value))));
        public static string Decode(string value, Encoding encoding = null) => (encoding ?? Encoding.UTF8).GetString(Convert.FromBase64String(value ?? throw new ArgumentNullException(nameof(value))));
    }

    [Obsolete("请迁移到 Bing.Security.Hashing.HashingProvider。")]
    public sealed class SHA256HashingProvider
    {
        private SHA256HashingProvider() { }
        public static string Signature(string value, OutType outType = OutType.Hex, Encoding encoding = null)
        {
            var result = HashingProvider.Compute(value, HashAlgorithmType.Sha256, encoding);
            return outType == OutType.Base64 ? Convert.ToBase64String(result) : Bing.Security.SecurityEncoding.ToHex(result);
        }
        public static bool Verify(string comparison, string value, OutType outType = OutType.Hex, Encoding encoding = null) => comparison == Signature(value, outType, encoding);
    }

    [Obsolete("请迁移到 Bing.Security.Hashing.HmacProvider。")]
    public sealed class HMACSHA256HashingProvider
    {
        private HMACSHA256HashingProvider() { }
        public static string Signature(string value, string key, OutType outType = OutType.Hex, Encoding encoding = null)
        {
            var codec = encoding ?? Encoding.UTF8;
            var result = HmacProvider.Compute(value, codec.GetBytes(key), HmacAlgorithmType.Sha256, codec);
            return outType == OutType.Base64 ? Convert.ToBase64String(result) : Bing.Security.SecurityEncoding.ToHex(result);
        }
        public static bool Verify(string comparison, string value, string key, OutType outType = OutType.Hex, Encoding encoding = null) => comparison == Signature(value, key, outType, encoding);
    }

    [Obsolete("旧 AES API 不提供认证保护，仅用于旧数据迁移。请使用 Bing.Security.Symmetric.AesCipher。")]
    public sealed class AESEncryptionProvider
    {
        private AESEncryptionProvider() { }
        public static string Encrypt(string value, string key, string iv = null, string salt = null, OutType outType = OutType.Base64, Encoding encoding = null)
        {
            var codec = encoding ?? Encoding.UTF8;
            var bytes = Transform(codec.GetBytes(value ?? throw new ArgumentNullException(nameof(value))), Derive(key, salt, codec, 32), Derive(iv, salt, codec, 16), true);
            return outType == OutType.Base64 ? Convert.ToBase64String(bytes) : Bing.Security.SecurityEncoding.ToHex(bytes);
        }
        public static string Decrypt(string value, string key, string iv = null, string salt = null, OutType outType = OutType.Base64, Encoding encoding = null)
        {
            var codec = encoding ?? Encoding.UTF8;
            var bytes = outType == OutType.Base64 ? Convert.FromBase64String(value) : Bing.Security.SecurityEncoding.FromHex(value);
            return codec.GetString(Transform(bytes, Derive(key, salt, codec, 32), Derive(iv, salt, codec, 16), false));
        }
        private static byte[] Derive(string value, string salt, Encoding encoding, int length)
        {
            if (string.IsNullOrWhiteSpace(value)) return new byte[length];
            if (string.IsNullOrWhiteSpace(salt))
            {
                var result = new byte[length];
                var source = encoding.GetBytes(value.PadRight(length));
                Buffer.BlockCopy(source, 0, result, 0, Math.Min(source.Length, result.Length));
                return result;
            }
            using (var derive = new Rfc2898DeriveBytes(encoding.GetBytes(value), encoding.GetBytes(salt), 1000)) return derive.GetBytes(length);
        }
        private static byte[] Transform(byte[] value, byte[] key, byte[] iv, bool encrypt)
        {
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7; aes.Key = key; aes.IV = iv;
                using (var transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor()) return transform.TransformFinalBlock(value, 0, value.Length);
            }
        }
    }
}
