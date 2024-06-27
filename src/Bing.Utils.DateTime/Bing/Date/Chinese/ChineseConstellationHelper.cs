namespace Bing.Date.Chinese;

/// <summary>
/// 二十八星宿帮助类
/// </summary>
public static class ChineseConstellationHelper
{
    /// <summary>
    /// 二十八星宿 - 简体
    /// </summary>
    private static readonly string[] NAME_S =
    [
        //四        五      六         日        一      二      三  
        "角木蛟", "亢金龙", "女土蝠", "房日兔", "心月狐", "尾火虎", "箕水豹",
        "斗木獬", "牛金牛", "氐土貉", "虚日鼠", "危月燕", "室火猪", "壁水獝",
        "奎木狼", "娄金狗", "胃土彘", "昴日鸡", "毕月乌", "觜火猴", "参水猿",
        "井木犴", "鬼金羊", "柳土獐", "星日马", "张月鹿", "翼火蛇", "轸水蚓",
    ];

    /// <summary>
    /// 二十八星宿 - 繁体
    /// </summary>
    private static readonly string[] NAME_Z =
    [
        //四        五      六         日        一      二      三  
        "角木蛟", "亢金龍", "女土蝠", "房日兔", "心月狐", "尾火虎", "箕水豹",
        "斗木獬", "牛金牛", "氐土貉", "虛日鼠", "危月燕", "室火豬", "壁水貐",
        "奎木狼", "婁金狗", "胃土雉", "昴日雞", "畢月烏", "觜火猴", "參水猿",
        "井木犴", "鬼金羊", "柳土獐", "星日馬", "張月鹿", "翼火蛇", "軫水蚓",
    ];

    /// <summary>
    /// 28星宿参考值，本日为角。
    /// </summary>
    private static readonly DateTime ChineseConstellationReferDay = new(2007, 9, 13);

    /// <summary>
    /// 获取二十八星宿
    /// </summary>
    /// <param name="dt">需要获取星座的日期</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文字符，默认为 false，即使用简体中文字符</param>
    /// <returns>二十八星宿</returns>
    public static string Get(DateTime dt, bool traditionalChineseCharacters = false)
    {
        var dict = traditionalChineseCharacters ? NAME_Z : NAME_S;
        var offset = (dt - ChineseConstellationReferDay).Days;
        var modStarDay = offset % 28;
        return modStarDay >= 0 ? dict[modStarDay] : dict[27 + modStarDay];
    }
}