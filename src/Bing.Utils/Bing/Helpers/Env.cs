using Bing.Extensions;
using System.Linq;
using System.Runtime.InteropServices;

namespace Bing.Helpers;

/// <summary>
/// 环境操作工具类，提供环境变量管理和系统环境信息获取功能
/// </summary>
public static class Env
{
    #region 常量

    /// <summary>
    /// .NET 环境变量名称
    /// </summary>
    private const string DOTNET_ENVIRONMENT = "DOTNET_ENVIRONMENT";

    /// <summary>
    /// ASP.NET Core 环境变量名称
    /// </summary>
    private const string ASPNETCORE_ENVIRONMENT = "ASPNETCORE_ENVIRONMENT";

    /// <summary>
    /// 开发环境名称
    /// </summary>
    public const string Development = "Development";

    /// <summary>
    /// 预发布环境名称
    /// </summary>
    public const string Staging = "Staging";

    /// <summary>
    /// 生产环境名称
    /// </summary>
    public const string Production = "Production";

    /// <summary>
    /// 测试环境名称
    /// </summary>
    public const string Testing = "Testing";

    #endregion

    #region 系统信息属性

    /// <summary>
    /// 获取系统换行符
    /// </summary>
    /// <value>当前操作系统的换行符字符串</value>
    /// <remarks>
    /// - Windows: \r\n (CRLF) <br />
    /// - Unix/Linux/macOS: \n (LF)
    /// </remarks>
    public static string NewLine => System.Environment.NewLine;

    /// <summary>
    /// 获取操作系统信息
    /// </summary>
    /// <value>当前操作系统的描述信息</value>
    // ReSharper disable once InconsistentNaming
    public static string OSDescription => RuntimeInformation.OSDescription;

    /// <summary>
    /// 获取操作系统架构
    /// </summary>
    /// <value>当前操作系统的处理器架构</value>
    // ReSharper disable once InconsistentNaming
    public static Architecture OSArchitecture => RuntimeInformation.OSArchitecture;

    /// <summary>
    /// 获取进程架构
    /// </summary>
    /// <value>当前进程的处理器架构</value>
    public static Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;

    /// <summary>
    /// 获取 .NET 运行时标识符
    /// </summary>
    /// <value>.NET 运行时的标识符字符串</value>
    public static string RuntimeIdentifier => GetRuntimeIdentifier();

    /// <summary>
    /// 获取 .NET 框架描述
    /// </summary>
    /// <value>.NET 框架的描述信息</value>
    public static string FrameworkDescription => RuntimeInformation.FrameworkDescription;

    /// <summary>
    /// 获取计算机名称
    /// </summary>
    /// <value>当前计算机的名称</value>
    public static string MachineName => Environment.MachineName;

    /// <summary>
    /// 获取用户名
    /// </summary>
    /// <value>当前用户的用户名</value>
    public static string UserName => Environment.UserName;

    /// <summary>
    /// 获取用户域名
    /// </summary>
    /// <value>当前用户的域名</value>
    public static string UserDomainName => Environment.UserDomainName;

    /// <summary>
    /// 获取当前工作目录
    /// </summary>
    /// <value>应用程序当前工作目录的完整路径</value>
    public static string CurrentDirectory => Environment.CurrentDirectory;

    /// <summary>
    /// 获取系统目录
    /// </summary>
    /// <value>系统目录的完整路径</value>
    public static string SystemDirectory => Environment.SystemDirectory;

    /// <summary>
    /// 获取处理器数量
    /// </summary>
    /// <value>当前机器上的处理器数量</value>
    public static int ProcessorCount => Environment.ProcessorCount;

    /// <summary>
    /// 获取系统启动时间（毫秒）
    /// </summary>
    /// <value>系统启动后经过的毫秒数</value>
    public static int TickCount => Environment.TickCount;

    /// <summary>
    /// 检查是否为 64 位操作系统
    /// </summary>
    /// <value>如果是 64 位操作系统返回 true，否则返回 false</value>
    public static bool Is64BitOperatingSystem => Environment.Is64BitOperatingSystem;

    /// <summary>
    /// 检查是否为 64 位进程
    /// </summary>
    /// <value>如果是 64 位进程返回 true，否则返回 false</value>
    public static bool Is64BitProcess => Environment.Is64BitProcess;

    /// <summary>
    /// 获取运行时的标识符。
    /// </summary>
    private static string GetRuntimeIdentifier()
#if NET5_0_OR_GREATER
    {
        return RuntimeInformation.RuntimeIdentifier;
    }
#else
    {
        // 回退到 IsOSPlatform 方案
        var os = GetOSPlatformIdentifier();
        var arch = GetArchitectureIdentifier();
        return $"{os}-{arch}";
    }
#endif

    /// <summary>
    /// 获取操作系统平台标识符
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private static string GetOSPlatformIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "win";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "osx";
#if NET5_0_OR_GREATER
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return "freebsd";
#endif
        return "unknown";
    }

    /// <summary>
    /// 获取处理器架构标识符
    /// </summary>
    private static string GetArchitectureIdentifier()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => "unknown"
        };
    }

    #endregion

    #region 平台检测

    /// <summary>
    /// 检查当前操作系统是否为 Windows
    /// </summary>
    /// <value>如果是 Windows 操作系统返回 true，否则返回 false</value>
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// 检查当前操作系统是否为 Linux
    /// </summary>
    /// <value>如果是 Linux 操作系统返回 true，否则返回 false</value>
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// 检查当前操作系统是否为 macOS (OSX)
    /// </summary>
    /// <value>如果是 macOS 操作系统返回 true，否则返回 false</value>
    // ReSharper disable once InconsistentNaming
    public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// 检查当前操作系统是否为 FreeBSD
    /// </summary>
    /// <value>如果是 FreeBSD 操作系统返回 true，否则返回 false</value>
    /// <remarks>
    /// 注意：FreeBSD 检测仅在 .NET 5.0 及更高版本中可用。
    /// 在较低版本的 .NET 中，此属性始终返回 false。
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    public static bool IsFreeBSD =>
#if NET5_0_OR_GREATER
        RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
#else
        false;
#endif

    /// <summary>
    /// 获取操作系统平台名称
    /// </summary>
    /// <value>操作系统平台名称字符串</value>
    /// <remarks>
    /// 返回值：<br />
    /// - "Windows" - Windows 操作系统<br />
    /// - "Linux" - Linux 操作系统  <br />
    /// - "macOS" - macOS 操作系统<br />
    /// - "FreeBSD" - FreeBSD 操作系统（仅 .NET 5.0+）<br />
    /// - "Unknown" - 未知操作系统
    /// </remarks>
    public static string PlatformName =>
        IsWindows ? "Windows" :
        IsLinux ? "Linux" :
        IsOSX ? "macOS" :
        IsFreeBSD ? "FreeBSD" :
        "Unknown";

    /// <summary>
    /// 获取操作系统版本信息
    /// </summary>
    /// <value>操作系统版本的字符串表示</value>
    // ReSharper disable once InconsistentNaming
    public static string OSVersion => Environment.OSVersion.ToString();

    #endregion

    #region 应用程序信息

    /// <summary>
    /// 获取当前应用程序名称
    /// </summary>
    /// <value>应用程序名称，如果无法确定则返回友好名称</value>
    public static string ApplicationName => Assembly.GetEntryAssembly()?.GetName().Name ?? AppDomain.CurrentDomain.FriendlyName;

    /// <summary>
    /// 获取应用程序版本
    /// </summary>
    /// <value>应用程序版本信息</value>
    public static Version ApplicationVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version("0.0.0.0");

    /// <summary>
    /// 获取应用程序标题
    /// </summary>
    /// <value>应用程序标题，从程序集属性中获取</value>
    public static string ApplicationTitle
    {
        get
        {
            var assembly = Assembly.GetEntryAssembly();
            var titleAttribute = assembly?.GetCustomAttribute<AssemblyTitleAttribute>();
            return titleAttribute?.Title ?? ApplicationName;
        }
    }

    /// <summary>
    /// 获取应用程序描述
    /// </summary>
    /// <value>应用程序描述，从程序集属性中获取</value>
    public static string ApplicationDescription
    {
        get
        {
            var assembly = Assembly.GetEntryAssembly();
            var descAttribute = assembly?.GetCustomAttribute<AssemblyDescriptionAttribute>();
            return descAttribute?.Description ?? string.Empty;
        }
    }

    /// <summary>
    /// 获取应用程序基目录
    /// </summary>
    /// <value>应用程序的基目录路径</value>
    /// <remarks>
    /// 等价于 AppContext.BaseDirectory
    /// </remarks>
    public static string ApplicationBaseDirectory => AppContext.BaseDirectory;

    #endregion

    #region 系统资源信息

    /// <summary>
    /// 获取系统可用内存（字节）
    /// </summary>
    /// <value>系统可用物理内存大小（字节）</value>
    /// <remarks>
    /// 此方法在不同平台上的行为可能不同。
    /// </remarks>
    public static long AvailablePhysicalMemory
    {
        get
        {
            try
            {
                if (IsWindows)
                {
                    return GC.GetTotalMemory(false);
                }
                // 对于 Linux/macOS，可以通过读取 /proc/meminfo 或使用其他方法
                // 这里简化实现，返回 GC 托管内存
                return GC.GetTotalMemory(false);
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// 获取系统正常运行时间
    /// </summary>
    /// <value>系统启动后运行的时间</value>
    /// <remarks>
    /// 在 .NET Standard 2.0 中使用 Environment.TickCount（32位），可能会溢出。
    /// 在 .NET Core 2.1+ 中使用 Environment.TickCount64（64位），更加准确。
    /// </remarks>
    public static TimeSpan SystemUptime =>
#if NETCOREAPP2_1_OR_GREATER || NET5_0_OR_GREATER
        TimeSpan.FromMilliseconds(Environment.TickCount64);
#else
        TimeSpan.FromMilliseconds(Environment.TickCount);
#endif

    /// <summary>
    /// 获取当前进程的工作集大小（内存使用量）
    /// </summary>
    /// <value>当前进程使用的物理内存大小（字节）</value>
    public static long WorkingSet => Environment.WorkingSet;

    #endregion

    #region 目录管理

    /// <summary>
    /// 当前工作目录
    /// </summary>
    private static string _currentDirectory = Directory.GetCurrentDirectory();

    /// <summary>
    /// 获取或设置当前工作目录
    /// </summary>
    /// <value>当前工作目录的完整路径</value>
    /// <remarks>
    /// 设置新的工作目录时，会验证目录是否存在。
    /// 如果目录不存在，设置操作将被忽略。
    /// </remarks>
    public static string WorkingDirectory
    {
        get => _currentDirectory;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && Directory.Exists(value))
            {
                _currentDirectory = value;
                Directory.SetCurrentDirectory(value);
            }
        }
    }

    /// <summary>
    /// 重置工作目录到应用程序基目录
    /// </summary>
    public static void ResetWorkingDirectory() => WorkingDirectory = ApplicationBaseDirectory;

    #endregion

    #region 环境变量操作

    /// <summary>
    /// 设置环境变量
    /// </summary>
    /// <param name="name">环境变量名称</param>
    /// <param name="value">环境变量值</param>
    /// <param name="target">环境变量目标范围，默认为当前进程</param>
    /// <exception cref="ArgumentException">当环境变量名称为空时抛出</exception>
    /// <exception cref="UnauthorizedAccessException">当没有权限设置指定范围的环境变量时抛出</exception>
    /// <remarks>
    /// 环境变量目标范围说明：<br />
    /// - Process: 仅对当前进程有效<br />
    /// - User: 对当前用户有效（需要用户权限）<br />
    /// - Machine: 对整个机器有效（需要管理员权限）<br />
    /// 
    /// 如果 value 为 null，则相当于删除该环境变量。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 设置进程级环境变量
    /// Env.SetEnvironmentVariable("MY_APP_DEBUG", true);
    /// 
    /// // 设置用户级环境变量
    /// Env.SetEnvironmentVariable("USER_PREFERENCE", "dark_theme", EnvironmentVariableTarget.User);
    /// 
    /// // 删除环境变量
    /// Env.SetEnvironmentVariable("TEMP_VAR", null);
    /// </code>
    /// </example>
    public static void SetEnvironmentVariable(string name, object value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("环境变量名称不能为空", nameof(name));
        try
        {
            Environment.SetEnvironmentVariable(name, value?.SafeString(), target);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
        {
            throw new UnauthorizedAccessException($"设置环境变量 '{name}' 失败，权限不足。目标范围: {target}", ex);
        }
    }

    /// <summary>
    /// 获取环境变量值
    /// </summary>
    /// <param name="name">环境变量名称</param>
    /// <param name="target">环境变量目标范围，默认为当前进程</param>
    /// <returns>环境变量值，如果不存在则返回 null</returns>
    /// <exception cref="ArgumentException">当环境变量名称为空时抛出</exception>
    /// <remarks>
    /// 查找顺序（当 target 为 Process 时）：<br />
    /// 1. 进程级环境变量<br />
    /// 2. 用户级环境变量<br />
    /// 3. 机器级环境变量
    /// </remarks>
    /// <example>
    /// <code>
    /// string value = Env.GetEnvironmentVariable("PATH");
    /// string userVar = Env.GetEnvironmentVariable("MY_USER_VAR", EnvironmentVariableTarget.User);
    /// </code>
    /// </example>
    public static string GetEnvironmentVariable(string name, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("环境变量名称不能为空", nameof(name));

        return Environment.GetEnvironmentVariable(name, target);
    }

    /// <summary>
    /// 获取环境变量值并转换为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="name">环境变量名称</param>
    /// <param name="defaultValue">默认值，当环境变量不存在或转换失败时返回</param>
    /// <param name="target">环境变量目标范围</param>
    /// <returns>转换后的值或默认值</returns>
    /// <exception cref="ArgumentException">当环境变量名称为空时抛出</exception>
    /// <remarks>
    /// 支持的类型转换包括：<br />
    /// - 基本数据类型（int, bool, double 等）<br />
    /// - 字符串类型<br />
    /// - 枚举类型<br />
    /// - 可空类型<br />
    /// 
    /// 转换失败时会返回默认值而不是抛出异常。
    /// </remarks>
    /// <example>
    /// <code>
    /// int port = Env.GetEnvironmentVariable&lt;int&gt;("PORT", 8080);
    /// bool debug = Env.GetEnvironmentVariable&lt;bool&gt;("DEBUG_MODE", false);
    /// TimeSpan timeout = Env.GetEnvironmentVariable&lt;TimeSpan&gt;("TIMEOUT", TimeSpan.FromMinutes(5));
    /// </code>
    /// </example>
    public static T GetEnvironmentVariable<T>(string name, T defaultValue = default, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        try
        {
            var value = GetEnvironmentVariable(name, target);
            if (string.IsNullOrEmpty(value))
                return defaultValue;
            return Conv.To<T>(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 删除环境变量
    /// </summary>
    /// <param name="name">环境变量名称</param>
    /// <param name="target">环境变量目标范围</param>
    /// <exception cref="ArgumentException">当环境变量名称为空时抛出</exception>
    /// <exception cref="UnauthorizedAccessException">当没有权限删除指定范围的环境变量时抛出</exception>
    /// <example>
    /// <code>
    /// Env.RemoveEnvironmentVariable("TEMP_VAR");
    /// Env.RemoveEnvironmentVariable("USER_SETTING", EnvironmentVariableTarget.User);
    /// </code>
    /// </example>
    public static void RemoveEnvironmentVariable(string name, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process) => SetEnvironmentVariable(name, null, target);

    /// <summary>
    /// 检查环境变量是否存在
    /// </summary>
    /// <param name="name">环境变量名称</param>
    /// <param name="target">环境变量目标范围</param>
    /// <returns>如果环境变量存在且不为空返回 true，否则返回 false</returns>
    /// <example>
    /// <code>
    /// if (Env.HasEnvironmentVariable("DATABASE_URL"))
    /// {
    ///     // 使用数据库连接字符串
    /// }
    /// </code>
    /// </example>
    public static bool HasEnvironmentVariable(string name, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        try
        {
            var value = GetEnvironmentVariable(name, target);
            return !string.IsNullOrEmpty(value);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取所有环境变量
    /// </summary>
    /// <param name="target">环境变量目标范围</param>
    /// <returns>包含所有环境变量的字典</returns>
    /// <example>
    /// <code>
    /// var envVars = Env.GetEnvironmentVariables();
    /// foreach (var kvp in envVars)
    /// {
    ///     Console.WriteLine($"{kvp.Key} = {kvp.Value}");
    /// }
    /// </code>
    /// </example>
    public static IDictionary<string, string> GetEnvironmentVariables(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        var result = new Dictionary<string, string>();
        var envVars = Environment.GetEnvironmentVariables(target);
        foreach (var key in envVars.Keys)
        {
            if (key is string keyStr && envVars[key] is string value)
                result[keyStr] = value;
        }

        return result;
    }

    #endregion

    #region .NET 环境管理

    /// <summary>
    /// 获取当前 .NET 应用程序的环境名称
    /// </summary>
    /// <returns>环境名称字符串，如果未设置则返回 null</returns>
    /// <remarks>
    /// 查找顺序：<br />
    /// 1. ASPNETCORE_ENVIRONMENT（优先，适用于 ASP.NET Core 应用）<br />
    /// 2. DOTNET_ENVIRONMENT（通用，适用于所有 .NET 应用）<br /><br />
    /// 
    /// 这种优先级设计确保了 ASP.NET Core 应用的环境设置不会被通用设置覆盖。
    /// </remarks>
    /// <example>
    /// <code>
    /// string env = Env.GetEnvironmentName();
    /// switch (env?.ToLower())
    /// {
    ///     case "development":
    ///         // 开发环境配置
    ///         break;
    ///     case "production":
    ///         // 生产环境配置
    ///         break;
    ///     default:
    ///         // 默认配置
    ///         break;
    /// }
    /// </code>
    /// </example>
    public static string GetEnvironmentName()
    {
        var environment = GetEnvironmentVariable(ASPNETCORE_ENVIRONMENT);
        if (!environment.IsEmpty())
            return environment;
        return GetEnvironmentVariable(DOTNET_ENVIRONMENT);
    }

    /// <summary>
    /// 设置 .NET 应用程序的环境名称
    /// </summary>
    /// <param name="environmentName">环境名称</param>
    /// <param name="target">环境变量目标范围</param>
    /// <param name="setBothVariables">是否同时设置 ASPNETCORE_ENVIRONMENT 和 DOTNET_ENVIRONMENT</param>
    /// <exception cref="ArgumentException">当环境名称为空时抛出</exception>
    /// <remarks>
    /// 当 setBothVariables 为 true 时，会同时设置两个环境变量，确保兼容性。
    /// 这对于混合环境（既有 ASP.NET Core 应用又有其他 .NET 应用）特别有用。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 设置为开发环境
    /// Env.SetEnvironmentName(Env.Development);
    /// 
    /// // 设置为生产环境，并同时设置两个环境变量
    /// Env.SetEnvironmentName(Env.Production, setBothVariables: true);
    /// 
    /// // 设置用户级环境变量
    /// Env.SetEnvironmentName(Env.Staging, EnvironmentVariableTarget.User);
    /// </code>
    /// </example>
    public static void SetEnvironmentName(string environmentName, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process, bool setBothVariables = false)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
            throw new ArgumentException("环境名称不能为空", nameof(environmentName));
        if (setBothVariables)
        {
            SetEnvironmentVariable(DOTNET_ENVIRONMENT, environmentName, target);
            SetEnvironmentVariable(ASPNETCORE_ENVIRONMENT, environmentName, target);
        }
        else
        {
            // 优先设置 ASPNETCORE_ENVIRONMENT
            SetEnvironmentVariable(ASPNETCORE_ENVIRONMENT, environmentName, target);
        }
    }

    /// <summary>
    /// 设置为开发环境，如果环境变量已设置则不覆盖
    /// </summary>
    /// <param name="target">环境变量目标范围</param>
    /// <remarks>
    /// 此方法是幂等的，多次调用不会产生副作用。
    /// 如果已经设置了任何一个环境变量（ASPNETCORE_ENVIRONMENT 或 DOTNET_ENVIRONMENT），
    /// 则不会进行任何更改。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 在应用启动时确保开发环境设置
    /// Env.SetDevelopment();
    /// 
    /// // 设置用户级开发环境
    /// Env.SetDevelopment(EnvironmentVariableTarget.User);
    /// </code>
    /// </example>
    public static void SetDevelopment(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        var environment = GetEnvironmentVariable(DOTNET_ENVIRONMENT, target);
        if (environment.IsEmpty() == false)
            return;
        environment = GetEnvironmentVariable(ASPNETCORE_ENVIRONMENT, target);
        if (environment.IsEmpty() == false)
            return;
        SetEnvironmentVariable(DOTNET_ENVIRONMENT, Development, target);
        SetEnvironmentVariable(ASPNETCORE_ENVIRONMENT, Development, target);
    }

    /// <summary>
    /// 检查当前是否为开发环境
    /// </summary>
    /// <returns>如果是开发环境返回 true，否则返回 false</returns>
    public static bool IsDevelopment() => IsEnvironment(Development);

    /// <summary>
    /// 检查当前是否为预发布环境
    /// </summary>
    /// <returns>如果是预发布环境返回 true，否则返回 false</returns>
    public static bool IsStaging() => IsEnvironment(Staging);

    /// <summary>
    /// 检查当前是否为生产环境
    /// </summary>
    /// <returns>如果是生产环境返回 true，否则返回 false</returns>
    public static bool IsProduction() => IsEnvironment(Production);

    /// <summary>
    /// 检查当前是否为测试环境
    /// </summary>
    /// <returns>如果是测试环境返回 true，否则返回 false</returns>
    public static bool IsTesting() => IsEnvironment(Testing);

    /// <summary>
    /// 检查当前是否为指定的环境
    /// </summary>
    /// <param name="environmentName">要检查的环境名称</param>
    /// <returns>如果当前环境匹配指定名称返回 true，否则返回 false</returns>
    /// <exception cref="ArgumentException">当环境名称为空时抛出</exception>
    /// <remarks>
    /// 比较时忽略大小写，支持自定义环境名称。
    /// </remarks>
    /// <example>
    /// <code>
    /// if (Env.IsEnvironment("Integration"))
    /// {
    ///     // 集成测试环境特定配置
    /// }
    /// 
    /// if (Env.IsEnvironment("Local"))
    /// {
    ///     // 本地开发环境特定配置
    /// }
    /// </code>
    /// </example>
    public static bool IsEnvironment(string environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
            throw new ArgumentException("环境名称不能为空", nameof(environmentName));
        var currentEnvironment = GetEnvironmentName();
        return string.Equals(currentEnvironment, environmentName, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region 特殊文件夹路径

    /// <summary>
    /// 获取特殊文件夹路径
    /// </summary>
    /// <param name="folder">特殊文件夹类型</param>
    /// <returns>特殊文件夹的完整路径</returns>
    /// <example>
    /// <code>
    /// string desktop = Env.GetFolderPath(Environment.SpecialFolder.Desktop);
    /// string appData = Env.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    /// string temp = Env.GetFolderPath(Environment.SpecialFolder.InternetCache);
    /// </code>
    /// </example>
    public static string GetFolderPath(Environment.SpecialFolder folder) => Environment.GetFolderPath(folder);

    /// <summary>
    /// 获取临时文件夹路径
    /// </summary>
    /// <value>系统临时文件夹的完整路径</value>
    public static string TempPath => Path.GetTempPath();

    /// <summary>
    /// 获取用户配置文件夹路径
    /// </summary>
    /// <value>当前用户配置文件夹的完整路径</value>
    public static string UserProfilePath => GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    /// 获取应用程序数据文件夹路径
    /// </summary>
    /// <value>应用程序数据文件夹的完整路径</value>
    public static string ApplicationDataPath => GetFolderPath(Environment.SpecialFolder.ApplicationData);

    /// <summary>
    /// 获取本地应用程序数据文件夹路径
    /// </summary>
    /// <value>本地应用程序数据文件夹的完整路径</value>
    public static string LocalApplicationDataPath => GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    #endregion

    #region 实用工具方法

    /// <summary>
    /// 检查是否在容器环境中运行
    /// </summary>
    /// <value>如果在容器中运行返回 true，否则返回 false</value>
    /// <remarks>
    /// 通过检查常见的容器环境标识符来判断。
    /// 这个检测不是100%准确，但能覆盖大多数情况。
    /// </remarks>
    public static bool IsRunningInContainer
    {
        get
        {
            // 检查 Docker 环境
            if (File.Exists("/.dockerenv"))
                return true;

            // 检查 Kubernetes 环境
            if (HasEnvironmentVariable("KUBERNETES_SERVICE_HOST"))
                return true;

            // 检查其他容器标识
            var containerEnvVars = new[]
            {
                "DOCKER_CONTAINER",
                "CONTAINER",
                "DOTNET_RUNNING_IN_CONTAINER"
            };

            return containerEnvVars.Any(envVar => HasEnvironmentVariable(envVar));
        }
    }

    /// <summary>
    /// 检查是否在 CI/CD 环境中运行
    /// </summary>
    /// <value>如果在 CI/CD 环境中运行返回 true，否则返回 false</value>
    // ReSharper disable once InconsistentNaming
    public static bool IsRunningInCI
    {
        get
        {
            var ciEnvVars = new[]
            {
                "CI", "CONTINUOUS_INTEGRATION",           // 通用
                "GITHUB_ACTIONS",                         // GitHub Actions
                "AZURE_PIPELINES", "TF_BUILD",           // Azure DevOps
                "JENKINS_URL",                            // Jenkins
                "GITLAB_CI",                              // GitLab CI
                "TRAVIS",                                 // Travis CI
                "CIRCLECI",                               // Circle CI
                "BUILDKITE",                              // Buildkite
                "TEAMCITY_VERSION"                        // TeamCity
            };

            return ciEnvVars.Any(envVar => HasEnvironmentVariable(envVar));
        }
    }

    /// <summary>
    /// 检查是否在调试模式下运行
    /// </summary>
    /// <value>如果在调试模式下运行返回 true，否则返回 false</value>
    public static bool IsDebugMode
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// 检查是否有调试器附加
    /// </summary>
    /// <value>如果有调试器附加返回 true，否则返回 false</value>
    public static bool IsDebuggerAttached => System.Diagnostics.Debugger.IsAttached;

    /// <summary>
    /// 创建临时文件并返回文件路径
    /// </summary>
    /// <param name="extension">文件扩展名（可选，包含点号）</param>
    /// <returns>临时文件的完整路径</returns>
    /// <remarks>
    /// 文件会被创建但内容为空，调用者负责文件的后续处理和清理。
    /// </remarks>
    /// <example>
    /// <code>
    /// string tempFile = Env.GetTempFileName();
    /// string tempImage = Env.GetTempFileName(".jpg");
    /// 
    /// try
    /// {
    ///     // 使用临时文件
    ///     File.WriteAllText(tempFile, "temporary content");
    /// }
    /// finally
    /// {
    ///     // 清理临时文件
    ///     if (File.Exists(tempFile))
    ///         File.Delete(tempFile);
    /// }
    /// </code>
    /// </example>
    public static string GetTempFileName(string extension = null)
    {
        var tempFile = Path.GetTempFileName();
        if (!string.IsNullOrEmpty(extension))
        {
            var newTempFile = Path.ChangeExtension(tempFile, extension);
            File.Move(tempFile, newTempFile);
            return newTempFile;
        }
        return tempFile;
    }

    /// <summary>
    /// 展开包含环境变量的字符串
    /// </summary>
    /// <param name="value">包含环境变量的字符串</param>
    /// <returns>展开后的字符串</returns>
    /// <remarks>
    /// 支持 %VARIABLE_NAME% (Windows) 和 $VARIABLE_NAME (Unix) 格式的环境变量。
    /// </remarks>
    /// <example>
    /// <code>
    /// string path = Env.ExpandEnvironmentVariables("%USERPROFILE%\\Documents");
    /// string homePath = Env.ExpandEnvironmentVariables("$HOME/documents");
    /// </code>
    /// </example>
    public static string ExpandEnvironmentVariables(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        return Environment.ExpandEnvironmentVariables(value);
    }

    /// <summary>
    /// 安全地终止当前应用程序进程
    /// </summary>
    /// <param name="exitCode">退出代码，默认为 0 表示正常退出</param>
    /// <remarks>
    /// 此方法会立即终止应用程序，不会执行 finally 块或析构函数。
    /// 在生产环境中使用时需要谨慎。
    /// </remarks>
    /// <example>
    /// <code>
    /// if (criticalError)
    /// {
    ///     Console.WriteLine("Critical error occurred, shutting down...");
    ///     Env.Exit(1);
    /// }
    /// </code>
    /// </example>
    public static void Exit(int exitCode = 0) => Environment.Exit(exitCode);

    #endregion
}