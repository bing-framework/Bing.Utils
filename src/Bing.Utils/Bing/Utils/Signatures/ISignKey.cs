namespace Bing.Utils.Signatures;

/// <summary>
/// 签名密钥
/// </summary>
public interface ISignKey
{
    /// <summary>
    /// 获取私钥
    /// </summary>
    /// <returns>签名所需的私钥。</returns>
    string GetKey();

    /// <summary>
    /// 获取公钥
    /// </summary>
    /// <returns>签名所需的公钥。</returns>
    string GetPublicKey();
}
