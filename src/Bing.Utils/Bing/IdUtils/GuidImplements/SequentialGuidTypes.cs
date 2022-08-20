namespace Bing.IdUtils.GuidImplements
{
    /// <summary>
    /// 顺序 GUID 类型
    /// </summary>
    internal enum SequentialGuidTypes
    {
        /// <summary>
        /// 生成的 GUID 按照字符串顺序排列
        /// </summary>
        /// <remarks>主要用于 MySQL 和 PostgreSQL</remarks>
        SequentialAsString,

        /// <summary>
        /// 生成的 GUID 按照二进制的顺序排列
        /// </summary>
        /// <remarks>主要用于 Oracle</remarks>
        SequentialAsBinary,

        /// <summary>
        /// 生成的GUID 像SQL Server, 按照末尾部分排列
        /// </summary>
        /// <remarks>主要用于 SQL Server</remarks>
        SequentialAtEnd
    }
}