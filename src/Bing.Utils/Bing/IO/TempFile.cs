using System.IO;

namespace Bing.IO
{
    /// <summary>
    /// 临时文件
    /// </summary>
    public class TempFile : TempPath
    {
        /// <summary>
        /// 初始化一个<see cref="TempFile"/>类型的实例
        /// </summary>
        /// <param name="prefix">临时文件前缀</param>
        public TempFile(string prefix = null) : base(prefix)
        {
        }

        /// <summary>
        /// 初始化路径
        /// </summary>
        protected override void InitializePath() => File.Create(FullPath).Dispose();

        /// <summary>
        /// 释放资源。确保删除临时路径
        /// </summary>
        /// <param name="disposing">是否释放中</param>
        protected override void Dispose(bool disposing)
        {
            if (!File.Exists(FullPath))
                return;
            try
            {
                File.Delete(FullPath);
            }
            catch
            {
                // ignored
            }
        }
    }
}
