using System;
using Bing.IdUtils.CombImplements;
using Bing.IdUtils.GuidImplements;

namespace Bing.IdUtils
{
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

                case GuidStyle.SqlTimestampStyle:
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

                case GuidStyle.SequentialAtEndStyle:
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

                case GuidStyle.SqlTimestampStyle:
                    return InternalCombImplementProxy.MsSql.Create(secureTimestamp);

                case GuidStyle.PostgreSqlTimeStampStyle:
                    return InternalCombImplementProxy.PostgreSql.Create(secureTimestamp);

                case GuidStyle.SequentialAsStringStyle:
                    return SequentialStylesProvider.Create(secureTimestamp, SequentialGuidTypes.SequentialAsString);

                case GuidStyle.SequentialAsBinaryStyle:
                    return SequentialStylesProvider.Create(secureTimestamp, SequentialGuidTypes.SequentialAsBinary);

                case GuidStyle.SequentialAtEndStyle:
                    return SequentialStylesProvider.Create(secureTimestamp, SequentialGuidTypes.SequentialAtEnd);

                case GuidStyle.EquifaxStyle:
                    return EquifaxStyleProvider.Create(secureTimestamp);

                default:
                    return TimeStampStyleProvider.Create(secureTimestamp);
            }
        }
    }
}