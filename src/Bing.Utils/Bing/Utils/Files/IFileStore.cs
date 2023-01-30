﻿namespace Bing.Utils.Files;

/// <summary>
/// 文件存储服务
/// </summary>
public interface IFileStore
{
    /// <summary>
    /// 保存文件，返回完整文件路径
    /// </summary>
    Task<string> SaveAsync();
}