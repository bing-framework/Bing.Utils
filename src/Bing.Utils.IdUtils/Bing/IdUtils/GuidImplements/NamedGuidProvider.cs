using System.Security.Cryptography;
using Bing.IdUtils.GuidImplements.Internals;

namespace Bing.IdUtils.GuidImplements;

/// <summary>
/// 基于名称实现的 GUID 提供程序
/// </summary>
internal static class NamedGuidProvider
{
    /// <summary>
    /// 创建一个 <see cref="Guid"/>
    /// </summary>
    /// <param name="namespace">命名空间</param>
    /// <param name="name">名称</param>
    /// <param name="version">GUID版本</param>
    public static Guid Create(Guid @namespace, byte[] name, GuidVersion version)
    {
        var namespaceBytes = @namespace.ToBigEndianByteArray();

        byte[] hash;
        using (var algorithm = version == GuidVersion.NameBasedMd5 ? MD5.Create() : SHA1.Create() as HashAlgorithm)
        {
            algorithm.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0);
            algorithm.TransformFinalBlock(name, 0, name.Length);
            hash = algorithm.Hash!;
        }

        var guidBytes = new byte[16];
        Array.Copy(hash, 0, guidBytes, 0, 16);
        GuidUtility.EndianSwap(guidBytes);

        // Variant RFC4122
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80); // big-endian octet 8

        // Version
        guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | ((int)version << 4)); // big-endian octet 6

        return new Guid(guidBytes);
    }
}