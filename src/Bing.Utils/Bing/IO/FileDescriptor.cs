﻿namespace Bing.IO;

/// <summary>
/// 文件描述符
/// </summary>
public class FileDescriptor
{
    /// <summary>
    /// 初始化一个<see cref="FileDescriptor"/>类型的实例
    /// </summary>
    public FileDescriptor()
    {
    }

    /// <summary>
    /// 初始化一个<see cref="FileDescriptor"/>类型的实例
    /// </summary>
    /// <param name="name">名称</param>
    /// <param name="size">大小</param>
    public FileDescriptor(string name, long size = 0L)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name), "文件名称不能为空");
        Name = name;
        Size = new FileSize(size);
        Extension = System.IO.Path.GetExtension(Name)?.TrimStart('.');
    }

    /// <summary>
    /// 文件目录名称
    /// </summary>
    public string DirectoryName { get; set; }

    /// <summary>
    /// 原始文件名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 文件存储名称
    /// </summary>
    public string StorageName { get; set; }

    /// <summary>
    /// 文件大小
    /// </summary>
    public FileSize Size { get; }

    /// <summary>
    /// 扩展名
    /// </summary>
    public string Extension { get; }

    /// <summary>
    /// 文件的MD5值
    /// </summary>
    public string Md5 { get; set; }

    /// <summary>
    /// 访问地址
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// 文件的完整路径名称
    /// </summary>
    public string FullName => System.IO.Path.Combine(DirectoryName, StorageName);

    /// <summary>
    /// 文件标识
    /// </summary>
    public string Id { get; set; }
}