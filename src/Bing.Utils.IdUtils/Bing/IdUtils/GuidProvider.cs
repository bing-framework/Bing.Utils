using Bing.IdUtils.CombImplements;
using Bing.IdUtils.GuidImplements;

namespace Bing.IdUtils;

/// <summary>
/// GUID 提供程序
/// </summary>
public static partial class GuidProvider
{
    /// <summary>
    /// 创建一个随机的 <see cref="Guid"/>
    /// </summary>
    public static Guid CreateRandom() => Create(GuidStyle.BasicStyle);

    /// <summary>
    /// 创建一个 <see cref="Guid"/>
    /// </summary>
    /// <param name="style">GUID 风格</param>
    /// <param name="mode">不重复模式</param>
    public static Guid Create(GuidStyle style = GuidStyle.BasicStyle, NoRepeatMode mode = NoRepeatMode.Off)
    {
        switch (style)
        {
            case GuidStyle.BasicStyle:
                return Guid.NewGuid();

            case GuidStyle.CombStyle:
                return TimeStampStyleProvider.Create(mode);

            case GuidStyle.TimeStampStyle:
                return TimeStampStyleProvider.Create(mode);

            case GuidStyle.UnixTimeStampStyle:
                return UnixTimeStampStyleProvider.Create(mode);

            case GuidStyle.LegacySqlTimeStampStyle:
                return mode == NoRepeatMode.On
                    ? InternalCombImplementProxy.LegacyWithNoRepeat.Create()
                    : InternalCombImplementProxy.Legacy.Create();

            case GuidStyle.SqlTimeStampStyle:
                return mode == NoRepeatMode.On
                    ? InternalCombImplementProxy.MsSqlWithNoRepeat.Create()
                    : InternalCombImplementProxy.MsSql.Create();

            case GuidStyle.PostgreSqlTimeStampStyle:
                return mode == NoRepeatMode.On
                    ? InternalCombImplementProxy.PostgreSqlWithNoRepeat.Create()
                    : InternalCombImplementProxy.PostgreSql.Create();

            case GuidStyle.SequentialAsStringStyle:
                return SequentialStylesProvider.Create(SequentialGuidTypes.SequentialAsString, mode);

            case GuidStyle.SequentialAsBinaryStyle:
                return SequentialStylesProvider.Create(SequentialGuidTypes.SequentialAsBinary, mode);

            case GuidStyle.SequentialAsEndStyle:
                return SequentialStylesProvider.Create(SequentialGuidTypes.SequentialAtEnd, mode);

            case GuidStyle.EquifaxStyle:
                return EquifaxStyleProvider.Create(mode);

            default:
                return Guid.NewGuid();
        }
    }

    /// <summary>
    /// 创建一个 <see cref="Guid"/>
    /// </summary>
    /// <param name="secureTimestamp">安全时间戳</param>
    /// <param name="style">GUID 风格</param>
    public static Guid Create(DateTime secureTimestamp, GuidStyle style = GuidStyle.BasicStyle)
    {
        switch (style)
        {
            case GuidStyle.BasicStyle:
                return Guid.NewGuid();

            case GuidStyle.CombStyle:
                return TimeStampStyleProvider.Create(secureTimestamp);

            case GuidStyle.TimeStampStyle:
                return TimeStampStyleProvider.Create(secureTimestamp);

            case GuidStyle.UnixTimeStampStyle:
                return UnixTimeStampStyleProvider.Create(secureTimestamp);

            case GuidStyle.LegacySqlTimeStampStyle:
                return InternalCombImplementProxy.Legacy.Create(secureTimestamp);

            case GuidStyle.SqlTimeStampStyle:
                return InternalCombImplementProxy.MsSql.Create(secureTimestamp);

            case GuidStyle.PostgreSqlTimeStampStyle:
                return InternalCombImplementProxy.PostgreSql.Create(secureTimestamp);

            case GuidStyle.SequentialAsStringStyle:
                return SequentialStylesProvider.Create(secureTimestamp, SequentialGuidTypes.SequentialAsString);

            case GuidStyle.SequentialAsBinaryStyle:
                return SequentialStylesProvider.Create(secureTimestamp, SequentialGuidTypes.SequentialAsBinary);

            case GuidStyle.SequentialAsEndStyle:
                return SequentialStylesProvider.Create(secureTimestamp, SequentialGuidTypes.SequentialAtEnd);

            case GuidStyle.EquifaxStyle:
                return EquifaxStyleProvider.Create(secureTimestamp);

            default:
                return TimeStampStyleProvider.Create(secureTimestamp);
        }
    }

    /// <summary>
    /// 创建一个 <see cref="Guid"/>
    /// </summary>
    /// <param name="endianGuidBytes">大/小端点字节数组</param>
    /// <param name="style">GUID 字节风格</param>
    public static Guid Create(byte[] endianGuidBytes, GuidBytesStyle style)
    {
        switch (style)
        {
            case GuidBytesStyle.LittleEndianByteArray:
                return LittleEndianByteArrayProvider.Create(endianGuidBytes);
            case GuidBytesStyle.BigEndianByteArray:
                return BigEndianByteArrayProvider.Create(endianGuidBytes);
            default:
                return Guid.NewGuid();
        }
    }

    /// <summary>
    /// 创建一个 <see cref="Guid"/>
    /// </summary>
    /// <param name="namespace">命名空间</param>
    /// <param name="name">名称</param>
    /// <param name="version">GUID版本</param>
    public static Guid Create(Guid @namespace, byte[] name, GuidVersion version)
    {
        switch (version)
        {
            case GuidVersion.Random:
                return CreateRandom();
            case GuidVersion.NameBasedMd5:
                return NamedGuidProvider.Create(@namespace, name, GuidVersion.NameBasedMd5);
            case GuidVersion.NameBasedSha1:
                return NamedGuidProvider.Create(@namespace, name, GuidVersion.NameBasedSha1);
            case GuidVersion.TimeBased:
                return GuidProvider.Create(CombStyle.NormalStyle);
            //case GuidVersion.DceSecurity:
            //    break;
            default:
                return Guid.NewGuid();
        }
    }
}