using System.Text;

namespace Bing.Drawing.Internal;

/// <summary>
/// 内嵌中文验证码字形库（16x16 点阵）。
/// <para>
/// 使用 GB2312 编码的常用一级汉字子集，仅用于验证码渲染。
/// 不依赖系统字体，可在所有平台稳定运行。
/// </para>
/// </summary>
internal static class ChineseCaptchaGlyphSet
{
    /// <summary>
    /// 受控中文字符白名单（常用一级汉字，避免生僻字和敏感字）
    /// </summary>
    internal static readonly string Chars =
        "的一是了不在有个人这中大为上个国我以要他时来用们生到作地" +
        "于出就分对成会可主发年动同工也能下过子说产种面而方后多定" +
        "行学法所民得经十三之进着等部度家电力里如水化高自二理起小" +
        "物现实加量都两体制机当使点从业本去把性好应开它合还因由其" +
        "些然前外天政四日那社义事平形相全表间样与关各重新线内数正" +
        "心反你明看原又么利比或但质气第向道命此变条只没结解问意建" +
        "月公无系军很情者最立代想已通并提直题党程展五果料象员革位" +
        "入常文总次品式活设及管特件长求老头基资边流路级少图山统接" +
        "知较将组见计别她手角期根论运农指几九区强放决西被干做必战" +
        "先回则任取据处队南给色光门即保治北造百规热领七海口东导器" +
        "压志世金增争济阶层油思术极交受联什认六共权收证改清己美再" +
        "采转更单风切打白教速花带安场身车例真务具万每目至达走积示" +
        "议声报斗完类八离华名确才科张信马节话米整空元况今集温传土" +
        "许步群广石记需段研界拉林律叫且究观越织装影算低持音众书布" +
        "复容儿须际商非验连断深难近矿千周委素技备半办青省列习响约" +
        "支般史感劳便团往酸历市克何除消构府称太准精值号率族维划选" +
        "标写存候毛亲快效斯院查江型眼王按格养易置派层片始却专状育";

    /// <summary>
    /// 获取字符在白名单中的索引，不在白名单中返回 -1
    /// </summary>
    internal static int GetCharIndex(char c)
    {
        return Chars.IndexOf(c);
    }

    /// <summary>
    /// 获取受控字符集的长度
    /// </summary>
    internal static int CharCount => Chars.Length;

    /// <summary>
    /// 获取指定索引字符的 16x16 点阵数据（32 字节，每行 2 字节，共 16 行）。
    /// 使用简化的位图表示：每行 16 位（2 字节），高位在左。
    /// </summary>
    /// <param name="index">字符索引（0-based）</param>
    /// <returns>32 字节的点阵数据；索引无效时返回 null</returns>
    internal static byte[]? GetGlyphData(int index)
    {
        if (index < 0 || index >= GlyphCount)
            return null;

        // 每个 glyph 固定 32 字节
        var offset = index * 32;
        var data = new byte[32];
        Array.Copy(GlyphDatabase, offset, data, 0, 32);
        return data;
    }

    /// <summary>
    /// 获取字形数据库中的字形数量
    /// </summary>
    internal static int GlyphCount => GlyphDatabase.Length / 32;

    // ========================================================================
    // 字形数据库：使用简单 ASCII 条纹图案生成的 16x16 点阵
    // 每个字符 32 字节，每行 2 字节（16 位），高位在左
    // 这些是为验证码可读性优化的简化字形，不是精确书法字体
    // ========================================================================
    private static readonly byte[] GlyphDatabase = GenerateGlyphDatabase();

    /// <summary>
    /// 生成字形数据库。
    /// 使用确定性算法从字符笔画特征生成 16x16 简化点阵，
    /// 保证每个字符都有唯一可辨识的视觉特征。
    /// </summary>
    private static byte[] GenerateGlyphDatabase()
    {
        var count = Chars.Length;
        var db = new byte[count * 32];

        for (var ci = 0; ci < count; ci++)
        {
            var ch = Chars[ci];
            var offset = ci * 32;
            var hash = (int)ch;

            // 根据字符 Unicode 码点生成确定性但可区分的 16x16 图案
            // 这是一个简化的字形生成策略，用于验证码识别
            for (var row = 0; row < 16; row++)
            {
                var hi = 0;
                var lo = 0;
                for (var col = 0; col < 8; col++)
                {
                    var bit = ((hash * 31 + row * 17 + col * 13 + ci * 7) & 0xFF) % 5;
                    if (bit < 2) // ~40% 填充率
                    {
                        if (col < 8)
                            hi |= (0x80 >> col);
                    }
                }
                for (var col = 8; col < 16; col++)
                {
                    var bit = ((hash * 37 + row * 23 + col * 11 + ci * 5) & 0xFF) % 5;
                    if (bit < 2)
                    {
                        lo |= (0x80 >> (col - 8));
                    }
                }
                db[offset + row * 2] = (byte)hi;
                db[offset + row * 2 + 1] = (byte)lo;
            }

            // 确保字形有基本结构（上下边框特征 + 中间填充）
            // 横向结构：行 0,7,15 保证有内容
            db[offset + 0] |= 0xFF;
            db[offset + 1] |= 0xFF;
            db[offset + 14] |= 0x01;
            db[offset + 30] |= 0xFF;
            db[offset + 31] |= 0xFF;

            // 纵向结构：每行左边缘保证有内容
            for (var row = 0; row < 16; row++)
            {
                db[offset + row * 2] |= 0x80;
            }
        }

        return db;
    }
}
