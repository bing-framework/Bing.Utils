﻿using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Bing.Extensions;

/// <summary>
/// 程序集(<see cref="Assembly"/>) 扩展
/// </summary>
public static class AssemblyExtensions
{
    #region GetFileVersion(获取程序集的文件版本)

    /// <summary>
    /// 获取程序集的文件版本
    /// </summary>
    /// <param name="assembly">程序集</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Version GetFileVersion(this Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));
        var info = FileVersionInfo.GetVersionInfo(assembly.Location);
        return new Version(info.FileVersion);
    }

    #endregion

    #region GetProductVersion(获取程序集的产品版本)

    /// <summary>
    /// 获取程序集的产品版本
    /// </summary>
    /// <param name="assembly">程序集</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Version GetProductVersion(this Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));
        var info = FileVersionInfo.GetVersionInfo(assembly.Location);
        return new Version(info.FileVersion);
    }

    #endregion
}