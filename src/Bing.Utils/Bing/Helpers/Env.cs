﻿using Bing.Extensions;

namespace Bing.Helpers;

/// <summary>
/// 环境操作
/// </summary>
public static class Env
{
    /// <summary>
    /// DOTNET_ENVIRONMENT
    /// </summary>
    private const string DOTNET_ENVIRONMENT = "DOTNET_ENVIRONMENT";

    /// <summary>
    /// ASPNETCORE_ENVIRONMENT
    /// </summary>
    private const string ASPNETCORE_ENVIRONMENT = "ASPNETCORE_ENVIRONMENT";

    /// <summary>
    /// Development
    /// </summary>
    private const string Development = "Development";

    /// <summary>
    /// 换行符
    /// </summary>
    public static string NewLine => System.Environment.NewLine;

    /// <summary>
    /// 设置环境变量
    /// </summary>
    /// <param name="name">环境变量名</param>
    /// <param name="value">值</param>
    public static void SetEnvironmentVariable(string name, object value) => System.Environment.SetEnvironmentVariable(name, value.SafeString());

    /// <summary>
    /// 获取环境变量
    /// </summary>
    /// <param name="name">环境变量名</param>
    public static string GetEnvironmentVariable(string name) => System.Environment.GetEnvironmentVariable(name);

    /// <summary>
    /// 获取环境变量
    /// </summary>
    /// <param name="name">环境变量名</param>
    public static T GetEnvironmentVariable<T>(string name) => Conv.To<T>(GetEnvironmentVariable(name));

    /// <summary>
    /// 获取环境名称
    /// </summary>
    public static string GetEnvironmentName()
    {
        var environment = GetEnvironmentVariable(ASPNETCORE_ENVIRONMENT);
        if (environment.IsEmpty() == false)
            return environment;
        return GetEnvironmentVariable(DOTNET_ENVIRONMENT);
    }

    /// <summary>
    /// 设置开发环境变量,如果环境变量已设置则忽略
    /// </summary>
    public static void SetDevelopment()
    {
        var environment = GetEnvironmentVariable(DOTNET_ENVIRONMENT);
        if (environment.IsEmpty() == false)
            return;
        environment = GetEnvironmentVariable(ASPNETCORE_ENVIRONMENT);
        if (environment.IsEmpty() == false)
            return;
        SetEnvironmentVariable(DOTNET_ENVIRONMENT, Development);
        SetEnvironmentVariable(ASPNETCORE_ENVIRONMENT, Development);
    }

    /// <summary>
    /// 是否开发环境
    /// </summary>
    public static bool IsDevelopment()
    {
        var environment = GetEnvironmentVariable(DOTNET_ENVIRONMENT);
        if (environment == Development)
            return true;
        environment = GetEnvironmentVariable(ASPNETCORE_ENVIRONMENT);
        if (environment == Development)
            return true;
        return false;
    }
}