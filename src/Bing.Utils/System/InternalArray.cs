namespace System
{
    /// <summary>
    /// 内部数组
    /// </summary>
    internal static class InternalArray
    {
        /// <summary>
        /// 空数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        public static T[] ForEmpty<T>() => Array.Empty<T>();
    }
}