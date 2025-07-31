namespace Bing.Text.RegularExpressions;

/// <summary>
/// 常用正则表达式常量类
/// </summary>
/// <remarks>
/// 提供各种常用的正则表达式模式，用于数据验证、格式检查等场景。
/// 所有正则表达式均经过严格测试，符合相应的标准规范。
/// </remarks>
public sealed class RegexConst
{
    #region 基础字符和数字

    /// <summary>
    /// 英文字母、数字和下划线（完整匹配）
    /// </summary>
    /// <remarks>
    /// 匹配由字母、数字、下划线组成的完整字符串。
    /// 示例：abc123、_test、A1B2C3
    /// </remarks>
    public const string General = @"^\w+$";

    /// <summary>
    /// 一个或多个数字
    /// </summary>
    /// <remarks>
    /// 匹配连续的数字字符，可用于提取数字。
    /// 示例：123、4567、9
    /// </remarks>
    public const string Numbers = @"\d+";

    /// <summary>
    /// 仅包含数字的完整字符串
    /// </summary>
    /// <remarks>
    /// 匹配完全由数字组成的字符串。
    /// 示例：123、45678、0
    /// </remarks>
    public const string NumbersOnly = @"^\d+$";

    /// <summary>
    /// 英文字母
    /// </summary>
    /// <remarks>
    /// 匹配一个或多个英文字母（大小写均可）。
    /// 示例：abc、XYZ、Hello
    /// </remarks>
    public const string Word = @"[a-zA-Z]+";

    /// <summary>
    /// 仅包含英文字母的完整字符串
    /// </summary>
    /// <remarks>
    /// 匹配完全由英文字母组成的字符串。
    /// 示例：Hello、world、ABC
    /// </remarks>
    public const string WordOnly = @"^[a-zA-Z]+$";

    /// <summary>
    /// 字母和数字组合
    /// </summary>
    /// <remarks>
    /// 匹配由字母和数字组成的字符串，不包含特殊字符。
    /// 示例：abc123、Test123、A1B2
    /// </remarks>
    public const string AlphaNumeric = @"^[a-zA-Z0-9]+$";

    #endregion

    #region 中文相关

    /// <summary>
    /// 单个中文汉字
    /// </summary>
    /// <remarks>
    /// 使用简化的中文Unicode范围，覆盖常用中文字符。
    /// 包括基本汉字、扩展汉字A、康熙部首、CJK笔画等。
    /// 范围：\u2E80-\u2EFF (CJK部首补充)、\u2F00-\u2FDF (康熙部首)、
    ///       \u31C0-\u31EF (CJK笔画)、\u3400-\u4DBF (扩展A)、
    ///       \u4E00-\u9FFF (基本汉字)、\uF900-\uFAFF (兼容汉字)
    /// </remarks>
    public const string Chinese = @"[\u2E80-\u2EFF\u2F00-\u2FDF\u31C0-\u31EF\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]";

    /// <summary>
    /// 一个或多个中文汉字
    /// </summary>
    /// <remarks>
    /// 匹配连续的中文字符。
    /// 示例：中文、汉字、测试内容
    /// </remarks>
    // ReSharper disable once IdentifierTypo
    public const string Chineses = Chinese + "+";

    /// <summary>
    /// 仅包含中文汉字的完整字符串
    /// </summary>
    /// <remarks>
    /// 匹配完全由中文字符组成的字符串。
    /// 示例：中文测试、汉字验证
    /// </remarks>
    public const string ChineseOnly = @"^" + Chinese + "+$";

    /// <summary>
    /// 中文字、英文字母、数字和下划线
    /// </summary>
    /// <remarks>
    /// 匹配包含中文、英文、数字和下划线的字符串。
    /// 示例：中文Test123、用户名_123、测试ABC
    /// </remarks>
    public const string GeneralWithChinese = @"^[\u4E00-\u9FFF\w]+$";

    /// <summary>
    /// 中文姓名
    /// </summary>
    /// <remarks>
    /// 匹配中文姓名，支持少数民族姓名中的间隔点。
    /// 维吾尔族姓名里面的点是 · （输入法中文状态下，键盘左上角数字1前面的符号）
    /// 长度限制：2-60个字符
    /// 示例：张三、李小明、古丽·努尔
    /// </remarks>
    public const string ChineseName = @"^[\u3400-\u9FFF·]{2,60}$";

    #endregion

    #region 数字和金额

    /// <summary>
    /// 正整数
    /// </summary>
    /// <remarks>
    /// 匹配正整数（不包括0）。
    /// 示例：1、123、999
    /// </remarks>
    public const string PositiveInteger = @"^[1-9]\d*$";

    /// <summary>
    /// 非负整数（正整数和0）
    /// </summary>
    /// <remarks>
    /// 匹配0或正整数。
    /// 示例：0、1、123、999
    /// </remarks>
    public const string NonNegativeInteger = @"^(0|[1-9]\d*)$";

    /// <summary>
    /// 负整数
    /// </summary>
    /// <remarks>
    /// 匹配负整数。
    /// 示例：-1、-123、-999
    /// </remarks>
    public const string NegativeInteger = @"^-[1-9]\d*$";

    /// <summary>
    /// 整数（包括正数、负数和0）
    /// </summary>
    /// <remarks>
    /// 匹配所有整数。
    /// 示例：0、123、-456、999
    /// </remarks>
    public const string Integer = @"^-?(0|[1-9]\d*)$";

    /// <summary>
    /// 正小数
    /// </summary>
    /// <remarks>
    /// 匹配正小数。
    /// 示例：0.1、3.14、123.456
    /// </remarks>
    public const string PositiveDecimal = @"^[1-9]\d*\.\d+$|^0\.\d*[1-9]\d*$";

    /// <summary>
    /// 非负小数（正小数和0）
    /// </summary>
    /// <remarks>
    /// 匹配0或正小数。
    /// 示例：0、0.0、3.14、123.456
    /// </remarks>
    public const string NonNegativeDecimal = @"^[1-9]\d*\.\d+$|^0\.\d*[1-9]\d*$|^0?\.0+$|^0$";

    /// <summary>
    /// 负小数
    /// </summary>
    /// <remarks>
    /// 匹配负小数。
    /// 示例：-0.1、-3.14、-123.456
    /// </remarks>
    public const string NegativeDecimal = @"^-([1-9]\d*\.\d+|0\.\d*[1-9]\d*)$";

    /// <summary>
    /// 小数（包括正数、负数和0）
    /// </summary>
    /// <remarks>
    /// 匹配所有小数。
    /// 示例：0、3.14、-123.456、0.0
    /// </remarks>
    public const string Decimal = @"^-?([1-9]\d*\.\d+|0\.\d*[1-9]\d*|0?\.0+|0)$";

    /// <summary>
    /// 货币金额
    /// </summary>
    /// <remarks>
    /// 匹配货币格式的数字，支持小数点后1-2位。
    /// 示例：100、99.9、123.45
    /// </remarks>
    public const string Money = @"^(\d+(?:\.\d{1,2})?)$";

    /// <summary>
    /// 货币金额（更严格）
    /// </summary>
    /// <remarks>
    /// 匹配货币格式，支持千分位分隔符和小数。
    /// 示例：1,000、1,234.56、999.99
    /// </remarks>
    public const string MoneyStrict = @"^[1-9]{1}[0-9]{0,2}(,[0-9]{3})*(.[0-9]{1,2})?$|^[1-9]{1}[0-9]{0,}(.[0-9]{1,2})?$|^0(.[0-9]{1,2})?$|^[0-9]{1,3}(.[0-9]{1,2})?$";

    #endregion

    #region 网络相关

    /// <summary>
    /// IPv4 地址
    /// </summary>
    /// <remarks>
    /// 匹配标准的IPv4地址格式，采用分组方式便于解析地址的每一个段。
    /// 支持范围：0.0.0.0 到 255.255.255.255
    /// 示例：192.168.1.1、10.0.0.1、255.255.255.255
    /// </remarks>
    public const string IPv4 = @"^(25[0-5]|2[0-4]\d|[0-1]?\d?\d)\.(25[0-5]|2[0-4]\d|[0-1]?\d?\d)\.(25[0-5]|2[0-4]\d|[0-1]?\d?\d)\.(25[0-5]|2[0-4]\d|[0-1]?\d?\d)$";

    /// <summary>
    /// IPv6 地址
    /// </summary>
    /// <remarks>
    /// 匹配标准的IPv6地址格式，支持各种简写形式。
    /// 示例：2001:0db8:85a3:0000:0000:8a2e:0370:7334、::1、fe80::1
    /// </remarks>
    public const string IPv6 = @"(([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]+|::(ffff(:0{1,4})?:)?((25[0-5]|(2[0-4]|1?[0-9])?[0-9])\.){3}(25[0-5]|(2[0-4]|1?[0-9])?[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1?[0-9])?[0-9])\.){3}(25[0-5]|(2[0-4]|1?[0-9])?[0-9]))";

    /// <summary>
    /// MAC 地址
    /// </summary>
    /// <remarks>
    /// 匹配各种格式的MAC地址。
    /// 支持格式：aa:bb:cc:dd:ee:ff、aa-bb-cc-dd-ee-ff、aabb.ccdd.eeff、aabbccddeeff
    /// 示例：00:1B:44:11:3A:B7、00-1B-44-11-3A-B7
    /// </remarks>
    public const string MacAddress = @"((?:[a-fA-F0-9]{1,2}[:-]){5}[a-fA-F0-9]{1,2})|((?:[a-fA-F0-9]{1,4}[.]){2}[a-fA-F0-9]{1,4})|[a-fA-F0-9]{12}|0x(\d{12}).+ETHER";

    /// <summary>
    /// 端口号
    /// </summary>
    /// <remarks>
    /// 匹配有效的端口号范围：1-65535。
    /// 示例：80、443、8080、65535
    /// </remarks>
    public const string Port = @"^([1-9][0-9]{0,3}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$";

    #endregion

    #region URL 和 URI

    /// <summary>
    /// URI 格式
    /// </summary>
    /// <remarks>
    /// 根据 RFC 3986 标准定义的URI格式。
    /// 参考：https://www.ietf.org/rfc/rfc3986.html#appendix-B
    /// 示例：http://example.com、ftp://files.example.com/file.txt
    /// </remarks>
    public const string Uri = @"^(([^:/?#]+):)?(//([^/?#]*))?([^?#]*)(\?([^#]*))?(#(.*))?";

    /// <summary>
    /// URL 格式
    /// </summary>
    /// <remarks>
    /// 匹配标准的URL格式。
    /// 示例：http://www.example.com、https://example.com/path
    /// </remarks>
    public const string Url = @"[a-zA-Z]+://[\w-+&@#/%?=~_|!:,.;]*[\w-+&@#/%=~_|]";

    /// <summary>
    /// HTTP/HTTPS URL
    /// </summary>
    /// <remarks>
    /// 匹配HTTP、HTTPS、FTP、File协议的URL。
    /// 来源：http://urlregex.com/
    /// 示例：https://www.example.com、ftp://files.example.com
    /// </remarks>
    public const string UrlByHttp = @"(https?|ftp|file)://[\w-+&@#/%?=~_|!:,.;]*[\w-+&@#/%=~_|]";

    /// <summary>
    /// 域名
    /// </summary>
    /// <remarks>
    /// 匹配有效的域名格式。
    /// 示例：example.com、www.example.com、sub.domain.example.org
    /// </remarks>
    public const string Domain = @"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$";

    #endregion

    #region 邮箱相关

    /// <summary>
    /// 电子邮箱地址
    /// </summary>
    /// <remarks>
    /// 符合 RFC 5322 规范的邮箱格式。
    /// 正则表达式来源：http://emailregex.com/
    /// 示例：user@example.com、test.email@domain.co.uk
    /// </remarks>
    public const string Email = @"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])";

    /// <summary>
    /// 支持中文的电子邮箱地址
    /// </summary>
    /// <remarks>
    /// 在标准邮箱格式基础上，添加了对中文字符的支持。
    /// 示例：用户@example.com、测试@domain.cn
    /// </remarks>
    public const string EmailWithChinese = @"(?:[a-z0-9\u4e00-\u9fa5!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9\u4e00-\u9fa5!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9\u4e00-\u9fa5](?:[a-z0-9\u4e00-\u9fa5-]*[a-z0-9\u4e00-\u9fa5])?\\.)+[a-z0-9\u4e00-\u9fa5](?:[a-z0-9\u4e00-\u9fa5-]*[a-z0-9\u4e00-\u9fa5])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9\u4e00-\u9fa5-]*[a-z0-9\u4e00-\u9fa5]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])";

    /// <summary>
    /// 简单邮箱格式
    /// </summary>
    /// <remarks>
    /// 简化的邮箱验证，适用于大多数常见场景。
    /// 示例：user@example.com、test@domain.org
    /// </remarks>
    public const string EmailSimple = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

    #endregion

    #region 电话号码

    /// <summary>
    /// 中国大陆手机号码
    /// </summary>
    /// <remarks>
    /// 匹配中国大陆手机号码，支持+86前缀。
    /// 格式：+86 180 4953 1399（2位区域码+11位数字）
    /// 号段：13x、14x、15x、16x、17x、18x、19x
    /// 示例：13812345678、+8613812345678、8613812345678
    /// </remarks>
    public const string Mobile = @"(?:0|86|\+86)?1[3-9]\d{9}";

    /// <summary>
    /// 中国香港手机号码
    /// </summary>
    /// <remarks>
    /// 匹配中国香港手机号码，支持+852前缀。
    /// 格式：+852 5100 4810（三位区域码+8位数字）
    /// 示例：51004810、+85251004810、85251004810
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    public const string MobileByHK = @"(?:0|852|\+852)?\d{8}";

    /// <summary>
    /// 中国台湾手机号码
    /// </summary>
    /// <remarks>
    /// 匹配中国台湾手机号码，支持+886前缀。
    /// 格式：+886 09 60 000000（三位区域码+以09开头的10位数字）
    /// 示例：0960000000、+8860960000000、8860960000000
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    public const string MobileByTW = @"(?:0|886|\+886)?(?:|-)09\d{8}";

    /// <summary>
    /// 中国澳门手机号码
    /// </summary>
    /// <remarks>
    /// 匹配中国澳门手机号码，支持+853前缀。
    /// 格式：+853 68 00000（三位区域码+以6开头的8位数字）
    /// 示例：68000000、+85368000000、85368000000
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    public const string MobileByMO = @"(?:0|853|\+853)?(?:|-)6\d{7}";

    /// <summary>
    /// 座机号码
    /// </summary>
    /// <remarks>
    /// 匹配中国大陆座机号码。
    /// 格式：区号-号码，区号3-4位，号码6-8位
    /// 示例：010-12345678、0571-87654321、400-123-4567
    /// </remarks>
    public const string Tel = @"(010|02\d|0[3-9]\d{2})-?(\d{6,8})";

    /// <summary>
    /// 座机号码、400电话、800电话
    /// </summary>
    /// <remarks>
    /// 匹配座机号码、400客服电话、800免费电话。
    /// 示例：0571-87654321、400-123-4567、800-123-4567
    /// </remarks>
    public const string Tel400800 = @"0\d{2,3}[\- ]?[1-9]\d{6,7}|[48]00[\- ]?[1-9]\d{2}[\- ]?\d{4}";

    /// <summary>
    /// 国际电话号码
    /// </summary>
    /// <remarks>
    /// 匹配国际格式的电话号码。
    /// 示例：+1-234-567-8900、+44-20-7946-0958
    /// </remarks>
    public const string InternationalPhone = @"^\+?[1-9]\d{1,14}$";

    #endregion

    #region 身份证和证件

    /// <summary>
    /// 18位身份证号码
    /// </summary>
    /// <remarks>
    /// 匹配中国大陆18位身份证号码格式。
    /// 格式：6位地区码 + 8位出生日期 + 3位顺序码 + 1位校验码
    /// 校验码可以是数字或X（大小写）
    /// 示例：110101199003078515、31010519900307851X
    /// </remarks>
    public const string CitizenId = @"[1-9]\d{5}[1-2]\d{3}((0\d)|(1[0-2]))(([012]\d)|3[0-1])\d{3}(\d|X|x)";

    /// <summary>
    /// 15位身份证号码（老版）
    /// </summary>
    /// <remarks>
    /// 匹配中国大陆15位身份证号码格式（已停用）。
    /// 示例：110101900307851
    /// </remarks>
    public const string CitizenId15 = @"^[1-9]\d{5}\d{2}((0[1-9])|(10|11|12))(([0-2][1-9])|10|20|30|31)\d{2}$";

    /// <summary>
    /// 身份证号码（支持15位和18位）
    /// </summary>
    /// <remarks>
    /// 同时支持15位和18位身份证号码。
    /// </remarks>
    public const string CitizenIdAll = @"^[1-9]\d{5}(18|19|20)\d{2}((0[1-9])|(1[0-2]))(([0-2][1-9])|10|20|30|31)\d{3}[0-9Xx]$|^[1-9]\d{5}\d{2}((0[1-9])|(10|11|12))(([0-2][1-9])|10|20|30|31)\d{2}$";

    /// <summary>
    /// 护照号码
    /// </summary>
    /// <remarks>
    /// 匹配中国护照号码格式。
    /// 格式：1位字母 + 8位数字，或 2位字母 + 7位数字
    /// 示例：E12345678、EA1234567
    /// </remarks>
    public const string Passport = @"^[a-zA-Z]{1,2}\d{7,8}$";

    /// <summary>
    /// 军官证号码
    /// </summary>
    /// <remarks>
    /// 匹配军官证号码格式。
    /// 示例：军字第1234567号、12345678
    /// </remarks>
    public const string MilitaryId = @"^[\u4e00-\u9fa5](字第)(\d{4,8})(号?)$|^\d{7,10}$";

    #endregion

    #region 车辆相关

    /// <summary>
    /// 中国车牌号码（兼容新能源车牌）
    /// </summary>
    /// <remarks>
    /// 匹配中国大陆车牌号码，包括普通车牌和新能源车牌。
    /// 支持：蓝牌、黄牌、绿牌（新能源）、白牌（军警）等
    /// 示例：京A12345、沪A123D4（新能源）、京A12345挂
    /// </remarks>
    public const string PlateNumber = @"^(([京津沪渝冀豫云辽黑湘皖鲁新苏浙赣鄂桂甘晋蒙陕吉闽贵粤青藏川宁琼使领][A-Z](([0-9]{5}[ABCDEFGHJK])|([ABCDEFGHJK]([A-HJ-NP-Z0-9])[0-9]{4})))|([京津沪渝冀豫云辽黑湘皖鲁新苏浙赣鄂桂甘晋蒙陕吉闽贵粤青藏川宁琼使领]\d{3}\d{1,3}[领])|([京津沪渝冀豫云辽黑湘皖鲁新苏浙赣鄂桂甘晋蒙陕吉闽贵粤青藏川宁琼使领][A-Z][A-HJ-NP-Z0-9]{4}[A-HJ-NP-Z0-9挂学警港澳使领]))$";

    /// <summary>
    /// 车架号（车辆识别代号 VIN）
    /// </summary>
    /// <remarks>
    /// 车辆识别代号由世界制造厂识别代号(WMI)、车辆说明部分(VDS)、车辆指示部分(VIS)三部分组成，共17位字符。
    /// 标准：GB 16735-2019
    /// 不包含字母：I、O、Q
    /// 示例：LDC613P23A1305189、LSJA24U62JG269225
    /// </remarks>
    public const string CarVin = @"^[A-HJ-NPR-Z0-9]{8}[X0-9]([A-HJ-NPR-Z0-9]{3}\d{5}|[A-HJ-NPR-Z0-9]{5}\d{3})$";

    /// <summary>
    /// 驾驶证档案编号
    /// </summary>
    /// <remarks>
    /// 匹配中国驾驶证档案编号。
    /// 格式：12位数字
    /// 示例：430101758218
    /// </remarks>
    public const string CarDrivingLicence = @"^[0-9]{12}$";

    #endregion

    #region 日期和时间

    /// <summary>
    /// 生日日期
    /// </summary>
    /// <remarks>
    /// 匹配多种生日格式。
    /// 支持格式：YYYY/MM/DD、YYYY-MM-DD、YYYY.MM.DD、YYYY年MM月DD日等
    /// 示例：1990/03/07、1990-03-07、1990年3月7日
    /// </remarks>
    public const string Birthday = @"^(\d{2,4})([/\-.年]?)(\d{1,2})([/\-.月]?)(\d{1,2})日?$";

    /// <summary>
    /// 时间格式
    /// </summary>
    /// <remarks>
    /// 匹配多种时间格式。
    /// 支持格式：HH:MM、HH:MM:SS、HH时MM分、HH时MM分SS秒等
    /// 示例：14:30、14:30:25、14时30分、14时30分25秒
    /// </remarks>
    public const string Time = @"\d{1,2}[:时]\d{1,2}([:分]\d{1,2})?秒?";

    /// <summary>
    /// 标准时间格式（24小时制）
    /// </summary>
    /// <remarks>
    /// 匹配24小时制时间格式 HH:MM 或 HH:MM:SS。
    /// 示例：09:30、14:30:45、23:59:59
    /// </remarks>
    public const string Time24Hour = @"^([01]?[0-9]|2[0-3]):[0-5][0-9](:[0-5][0-9])?$";

    /// <summary>
    /// 12小时制时间格式
    /// </summary>
    /// <remarks>
    /// 匹配12小时制时间格式，包含AM/PM。
    /// 示例：09:30 AM、2:30 PM、11:59:59 PM
    /// </remarks>
    public const string Time12Hour = @"^(1[0-2]|0?[1-9]):[0-5][0-9](:[0-5][0-9])?\s?(AM|PM|am|pm)$";

    /// <summary>
    /// 日期格式（YYYY-MM-DD）
    /// </summary>
    /// <remarks>
    /// 匹配标准的日期格式 YYYY-MM-DD。
    /// 示例：2023-12-25、1990-01-01
    /// </remarks>
    public const string Date = @"^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])$";

    /// <summary>
    /// 日期时间格式
    /// </summary>
    /// <remarks>
    /// 匹配标准的日期时间格式 YYYY-MM-DD HH:MM:SS。
    /// 示例：2023-12-25 14:30:45
    /// </remarks>
    public const string DateTime = @"^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])\s([01]?[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$";

    #endregion

    #region 编码和标识

    /// <summary>
    /// UUID（标准格式）
    /// </summary>
    /// <remarks>
    /// 匹配标准的UUID格式，包含连字符。
    /// 格式：xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
    /// 示例：550e8400-e29b-41d4-a716-446655440000
    /// </remarks>
    public const string Uuid = @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";

    /// <summary>
    /// UUID（不带连字符）
    /// </summary>
    /// <remarks>
    /// 匹配不带连字符的UUID格式。
    /// 格式：xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
    /// 示例：550e8400e29b41d4a716446655440000
    /// </remarks>
    public const string UuidSimple = @"^[0-9a-fA-F]{32}$";

    /// <summary>
    /// 16进制字符串
    /// </summary>
    /// <remarks>
    /// 匹配十六进制字符串（只包含0-9、a-f、A-F）。
    /// 示例：FF00CC、abc123、1234567890ABCDEF
    /// </remarks>
    public const string Hex = @"^[a-fA-F0-9]+$";

    /// <summary>
    /// Base64 编码字符串
    /// </summary>
    /// <remarks>
    /// 匹配Base64编码的字符串。
    /// 示例：SGVsbG8gV29ybGQ=、dGVzdA==
    /// </remarks>
    public const string Base64 = @"^[A-Za-z0-9+/]*={0,2}$";

    /// <summary>
    /// MD5 哈希值
    /// </summary>
    /// <remarks>
    /// 匹配32位MD5哈希值。
    /// 示例：5d41402abc4b2a76b9719d911017c592
    /// </remarks>
    public const string Md5 = @"^[a-fA-F0-9]{32}$";

    /// <summary>
    /// SHA1 哈希值
    /// </summary>
    /// <remarks>
    /// 匹配40位SHA1哈希值。
    /// 示例：aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d
    /// </remarks>
    public const string Sha1 = @"^[a-fA-F0-9]{40}$";

    /// <summary>
    /// SHA256 哈希值
    /// </summary>
    /// <remarks>
    /// 匹配64位SHA256哈希值。
    /// 示例：e258d248fda94c63753607f7c4494ee0fcbe92f1a76bfdac795c9d84101eb317
    /// </remarks>
    public const string Sha256 = @"^[a-fA-F0-9]{64}$";

    #endregion

    #region 中国特色标识

    /// <summary>
    /// 邮政编码（兼容港澳台）
    /// </summary>
    /// <remarks>
    /// 匹配中国大陆、香港、澳门、台湾的邮政编码。
    /// 大陆：6位数字，香港：特殊编码，澳门：特殊编码，台湾：3+2位数字
    /// 示例：100000、518000、999077、999078
    /// </remarks>
    public const string ZipCode = @"^(0[1-7]|1[0-356]|2[0-7]|3[0-6]|4[0-7]|5[0-7]|6[0-7]|7[0-5]|8[0-9]|9[0-8])\d{4}|99907[78]$";

    /// <summary>
    /// 统一社会信用代码
    /// </summary>
    /// <remarks>
    /// 匹配18位统一社会信用代码。
    /// 组成：登记管理部门代码(1位) + 机构类别代码(1位) + 登记管理机关行政区划码(6位) + 主体标识码(9位) + 校验码(1位)
    /// 字符集：0-9、A-H、J-N、P、Q、R、T、U、W、X、Y
    /// 示例：91110000MA001234X5、12345678MA1234567X
    /// </remarks>
    public const string CreditCode = @"^[0-9A-HJ-NPQRTUWXY]{2}\d{6}[0-9A-HJ-NPQRTUWXY]{10}$";

    /// <summary>
    /// 组织机构代码
    /// </summary>
    /// <remarks>
    /// 匹配9位组织机构代码。
    /// 格式：8位本体代码 + 1位校验码，中间用连字符分隔
    /// 示例：12345678-9、ABCDEFGH-1
    /// </remarks>
    public const string OrganizationCode = @"^[A-Z0-9]{8}-[A-Z0-9]$";

    /// <summary>
    /// 营业执照注册号（15位）
    /// </summary>
    /// <remarks>
    /// 匹配企业营业执照15位注册号。
    /// 示例：123456789012345
    /// </remarks>
    public const string BusinessLicense = @"^[0-9]{15}$";

    /// <summary>
    /// 税务登记号
    /// </summary>
    /// <remarks>
    /// 匹配税务登记号格式。
    /// 格式：地区码(6位) + 组织机构代码(9位) + 类型码(1位)
    /// 示例：12345612345678901
    /// </remarks>
    public const string TaxNumber = @"^[0-9]{6}[0-9A-Z]{9}[0-9]$";

    #endregion

    #region 银行卡和支付

    /// <summary>
    /// 银行卡号
    /// </summary>
    /// <remarks>
    /// 匹配银行卡号格式，支持13-19位数字。
    /// 示例：6228480402564890018、4367423898237492
    /// </remarks>
    public const string BankCard = @"^[1-9]\d{12,18}$";

    /// <summary>
    /// 信用卡号
    /// </summary>
    /// <remarks>
    /// 匹配主要信用卡号格式。
    /// 支持：Visa、MasterCard、American Express、Discover等
    /// </remarks>
    public const string CreditCard = @"^(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13}|3[0-9]{13}|6(?:011|5[0-9]{2})[0-9]{12})$";

    /// <summary>
    /// 支付宝账号
    /// </summary>
    /// <remarks>
    /// 匹配支付宝账号格式（手机号或邮箱）。
    /// </remarks>
    public const string Alipay = @"^(1[3-9]\d{9}|[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})$";

    #endregion

    #region 其他实用正则

    /// <summary>
    /// 正则表达式分组变量
    /// </summary>
    /// <remarks>
    /// 匹配正则表达式中的分组变量格式。
    /// 示例：$1、$2、$10
    /// </remarks>
    public const string GroupVar = @"\$(\d+)";

    /// <summary>
    /// 强密码
    /// </summary>
    /// <remarks>
    /// 匹配强密码：至少8位，包含大写字母、小写字母、数字和特殊字符。
    /// </remarks>
    public const string StrongPassword = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

    /// <summary>
    /// 中等强度密码
    /// </summary>
    /// <remarks>
    /// 匹配中等强度密码：至少6位，包含字母和数字。
    /// </remarks>
    public const string MediumPassword = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d@$!%*?&]{6,}$";

    /// <summary>
    /// 版本号
    /// </summary>
    /// <remarks>
    /// 匹配语义化版本号格式。
    /// 格式：主版本号.次版本号.修订号[-预发布版本][+构建元数据]
    /// 示例：1.0.0、2.1.3-alpha、1.0.0-beta.1+build.123
    /// </remarks>
    public const string Version = @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";

    /// <summary>
    /// HTML 标签
    /// </summary>
    /// <remarks>
    /// 匹配HTML标签。
    /// 示例：&lt;div&gt;、&lt;span class="test"&gt;、&lt;/div&gt;
    /// </remarks>
    public const string HtmlTag = @"<[^>]+>";

    /// <summary>
    /// 颜色值（十六进制）
    /// </summary>
    /// <remarks>
    /// 匹配十六进制颜色值。
    /// 支持格式：#RGB、#RRGGBB
    /// 示例：#FF0000、#ff0000、#F00、#f00
    /// </remarks>
    public const string ColorHex = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";

    /// <summary>
    /// QQ号码
    /// </summary>
    /// <remarks>
    /// 匹配QQ号码格式：5-11位数字，不能以0开头。
    /// 示例：12345、1234567890
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    public const string QQNumber = @"^[1-9]\d{4,10}$";

    /// <summary>
    /// 微信号
    /// </summary>
    /// <remarks>
    /// 匹配微信号格式：字母开头，字母、数字、下划线、减号，6-20位。
    /// 示例：wechat_123、user-name、test_user
    /// </remarks>
    public const string WeChatId = @"^[a-zA-Z]([-_a-zA-Z0-9]{5,19})+$";

    #endregion
}