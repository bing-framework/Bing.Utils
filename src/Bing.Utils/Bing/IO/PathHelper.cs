using System.IO;
using Bing.Helpers;

namespace Bing.IO
{
    /// <summary>
    /// 路径操作辅助类
    /// </summary>
    public static class PathHelper
    {
        #region GetPhysicalPath(获取物理路径)

        /// <summary>
        /// 获取物理路径
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        public static string GetPhysicalPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;
            var rootPath = Platform.AppRoot;
            if (string.IsNullOrWhiteSpace(rootPath))
                return Path.GetFullPath(relativePath);
            return $"{Platform.AppRoot}\\{relativePath.Replace("/", "\\").TrimStart('\\')}";
        }

        #endregion

        #region GetWebRootPath(获取wwwroot路径)

        /// <summary>
        /// 获取wwwroot路径
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        public static string GetWebRootPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;
            var rootPath = Platform.AppRoot;
            if (string.IsNullOrWhiteSpace(rootPath))
                return Path.GetFullPath(relativePath);
            return $"{Platform.AppRoot}\\wwwroot\\{relativePath.Replace("/", "\\").TrimStart('\\')}";
        }

        #endregion
    }
}
