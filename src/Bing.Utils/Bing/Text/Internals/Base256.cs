namespace Bing.Text.Internals;

/// <summary>
/// 表示 Base256 编码实现的类。
/// </summary>
internal class Base256 : BaseXCore
{
    /// <summary>
    /// 默认的 Base256 字符集。。
    /// </summary>
    public const string DefaultAlphabet = "!#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~ ¡¢£¤¥¦§¨©ª«¬­®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĲĳĴĵĶķĸĹĺĻļĽľĿŀŁł";

    /// <summary>
    /// 默认的特殊字符，Base256不使用特殊字符。
    /// </summary>
    /// <remarks>
    /// 由于Base256编码不需要填充字符，因此这里使用(char)0作为默认值。
    /// </remarks>
    public const char DefaultSpecial = (char)0;

    /// <inheritdoc />
    public override bool HasSpecial => false;

    /// <summary>
    /// 初始化一个 <see cref="Base256"/> 类型的实例。
    /// </summary>
    /// <param name="alphabet">使用的字符集，默认为Base256的默认字符集。</param>
    /// <param name="special">特殊字符。Base256编码中通常不使用特殊字符，故默认为(char)0。</param>
    /// <param name="encoding">用于编码字符串的编码方式。如果未指定，则默认使用UTF-8编码。</param>
    public Base256(string alphabet = DefaultAlphabet, char special = DefaultSpecial, Encoding encoding = null)
        : base(256, alphabet, special, encoding)
    {
    }

    /// <inheritdoc />
    public override string Encode(byte[] data)
    {
        if (data is null)
            return string.Empty;

        var result = new char[data.Length];

        for (var i = 0; i < data.Length; i++)
            result[i] = Alphabet[data[i]];

        return new string(result);
    }

    /// <inheritdoc />
    public override byte[] Decode(string data)
    {
        unchecked
        {
            if (string.IsNullOrEmpty(data))
                return Array.Empty<byte>();

            var result = new byte[data.Length];

            for (var i = 0; i < data.Length; i++)
                result[i] = (byte)InvAlphabet[data[i]];

            return result;
        }
    }
}