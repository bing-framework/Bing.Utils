using Bing.IO;

namespace Bing.Helpers;

/// <summary>
/// 常用公共操作工具类
/// </summary>
/// <remarks>
/// 提供常用的系统操作、类型处理、数据交换等通用功能。
/// 本类作为各种工具方法的统一入口点，简化日常开发中的常见操作。
/// </remarks>
public static partial class Common
{
    #region ApplicationBaseDirectory(当前应用程序基路径)

    /// <summary>
    /// 获取当前应用程序基路径
    /// </summary>
    /// <value>应用程序基路径，路径末尾包含目录分隔符</value>
    /// <remarks>
    /// 等价于  <see cref="AppContext.BaseDirectory"/>。
    /// 此属性在应用程序生命周期内是不变的，可以安全地缓存。
    /// </remarks>
    public static string ApplicationBaseDirectory => AppContext.BaseDirectory;

    #endregion

    #region Line(换行符)

    /// <summary>
    /// 获取当前操作系统的换行符
    /// </summary>
    /// <value>
    /// Windows: \r\n (CRLF)<br/>
    /// Unix/Linux/macOS: \n (LF)
    /// </value>
    /// <remarks>
    /// 委托给 <see cref="Env.NewLine"/> 以保持一致性。
    /// </remarks>
    public static string Line => Env.NewLine;

    #endregion

    #region GetType(获取类型)

    /// <summary>
    /// 获取指定泛型类型的实际类型（处理可空类型）
    /// </summary>
    /// <typeparam name="T">要获取的类型</typeparam>
    /// <returns>实际类型，如果是可空类型则返回其基础类型</returns>
    /// <remarks>
    /// 对于可空值类型（如 int?），返回其基础类型（如 int）。
    /// 对于引用类型和非可空值类型，返回类型本身。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type GetType<T>() => GetType(typeof(T));

    /// <summary>
    /// 获取指定类型的实际类型（处理可空类型）
    /// </summary>
    /// <param name="type">要处理的类型</param>
    /// <returns>实际类型，如果是可空类型则返回其基础类型</returns>
    /// <exception cref="ArgumentNullException">当 type 为 null 时抛出</exception>
    /// <remarks>
    /// 此方法主要用于统一处理可空类型和非可空类型，在反射和类型检查场景中特别有用。
    /// </remarks>
    public static Type GetType(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type), "类型参数不能为 null");
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    #endregion

    #region Swap(交换值)

    /// <summary>
    /// 交换两个变量的值
    /// </summary>
    /// <typeparam name="T">变量类型</typeparam>
    /// <param name="a">变量 A</param>
    /// <param name="b">变量 B</param>
    /// <remarks>
    /// 使用 C# 7.0 的元组解构语法实现高效的值交换。
    /// 此方法对值类型和引用类型都有效。
    /// </remarks>
    /// <example>
    /// <code>
    /// int x = 10, y = 20;
    /// Common.Swap(ref x, ref y);  // x = 20, y = 10
    /// 
    /// string str1 = "Hello", str2 = "World";
    /// Common.Swap(ref str1, ref str2);  // str1 = "World", str2 = "Hello"
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(ref T a, ref T b) => (a, b) = (b, a);

    #endregion

    #region GetPhysicalPath(获取物理路径)

    /// <summary>
    /// 将相对路径转换为物理路径
    /// </summary>
    /// <param name="relativePath">相对路径，例如："config/app.json" 或 "/logs/app.log"</param>
    /// <param name="basePath">基路径，默认为应用程序基目录</param>
    /// <returns>完整的物理路径</returns>
    /// <exception cref="ArgumentNullException">当 relativePath 为 null 或空白时抛出</exception>
    /// <exception cref="ArgumentException">当路径包含非法字符时抛出</exception>
    /// <remarks>
    /// 此方法委托给 <see cref="PathHelper.GetPhysicalPath"/> 以保持功能一致性。
    /// 会自动处理路径分隔符的标准化和安全性验证。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 使用默认基路径
    /// string configPath = Common.GetPhysicalPath("config/app.json");
    /// 
    /// // 使用自定义基路径
    /// string logPath = Common.GetPhysicalPath("app.log", @"C:\Logs");
    /// 
    /// // 处理前导斜杠
    /// string dataPath = Common.GetPhysicalPath("/data/users.db");
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetPhysicalPath(string relativePath, string basePath = null) => PathHelper.GetPhysicalPath(relativePath, basePath);

    #endregion

    #region JoinPath(连接路径)

    /// <summary>
    /// 连接多个 URL 路径片段
    /// </summary>
    /// <param name="paths">要连接的路径片段数组</param>
    /// <returns>连接后的完整路径</returns>
    /// <remarks>
    /// 此方法委托给 <see cref="Url.Combine"/> 处理 URL 路径连接。
    /// 会自动处理路径分隔符，避免重复的斜杠。
    /// 主要用于 URL 路径连接，对于本地文件路径建议使用 <see cref="Path.Combine(string[])"/>。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string JoinPath(params string[] paths) => Url.Combine(paths);

    #endregion

    #region GetCurrentDirectory(获取当前目录路径)

    /// <summary>
    /// 获取当前工作目录的完整路径
    /// </summary>
    /// <returns>当前工作目录的完整路径</returns>
    /// <exception cref="UnauthorizedAccessException">当调用方没有所需的权限时抛出</exception>
    /// <exception cref="NotSupportedException">当操作系统不支持时抛出</exception>
    /// <remarks>
    /// 直接委托给 <see cref="Directory.GetCurrentDirectory"/>。
    /// 注意：当前目录可能在应用程序运行期间发生变化。
    /// 如需获取应用程序基目录，请使用 <see cref="ApplicationBaseDirectory"/>。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetCurrentDirectory() => Directory.GetCurrentDirectory();

    #endregion

    #region GetParentDirectory(获取当前目录的上级目录)

    /// <summary>
    /// 获取指定深度的上级目录路径
    /// </summary>
    /// <param name="depth">向上遍历的深度，默认为 1</param>
    /// <param name="startPath">起始路径，默认为当前工作目录</param>
    /// <returns>上级目录的完整路径</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 depth 为负数时抛出</exception>
    /// <exception cref="ArgumentException">当 startPath 无效时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当起始目录不存在时抛出</exception>
    /// <remarks>
    /// 如果指定的深度超过了可用的父级目录数量，将返回最顶层的可访问目录。
    /// 深度为 0 时返回起始路径本身。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 获取上一级目录
    /// string parentDir = Common.GetParentDirectory();
    /// 
    /// // 获取上两级目录
    /// string grandParentDir = Common.GetParentDirectory(2);
    /// 
    /// // 从指定路径开始获取父级目录
    /// string parent = Common.GetParentDirectory(1, @"C:\Projects\MyApp\bin");
    /// // 结果: "C:\Projects\MyApp"
    /// </code>
    /// </example>
    public static string GetParentDirectory(int depth = 1, string startPath = null)
    {
        if (depth < 0)
            throw new ArgumentOutOfRangeException(nameof(depth), "深度不能为负数");
        // 获取起始路径
        var currentPath = startPath ?? GetCurrentDirectory();
        if (string.IsNullOrWhiteSpace(currentPath))
            throw new ArgumentException("起始路径不能为空", nameof(startPath));
        // 验证起始路径是否存在
        if (!Directory.Exists(currentPath))
            throw new DirectoryNotFoundException($"起始目录不存在: {currentPath}");
        // 深度为 0 时直接返回起始路径
        if (depth == 0)
            return Path.GetFullPath(currentPath);
        var path = Path.GetFullPath(currentPath);
        for (var i = 0; i < depth; i++)
        {
            var parentPath = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parentPath))
                break;  // 已到达根目录，无法继续向上
            path = parentPath;
        }
        return path;
    }

    #endregion

    #region GetParentDirectoryOf(获取指定路径的直接父级目录)

    /// <summary>
    /// 获取指定路径的直接父级目录
    /// </summary>
    /// <param name="path">要获取父级目录的路径</param>
    /// <returns>父级目录路径，如果已是根目录则返回 null</returns>
    /// <exception cref="ArgumentException">当路径为空或无效时抛出</exception>
    /// <example>
    /// <code>
    /// string parent = Common.GetParentDirectoryOf(@"C:\Projects\MyApp\bin");
    /// // 结果: "C:\Projects\MyApp"
    /// 
    /// string rootParent = Common.GetParentDirectoryOf(@"C:\");
    /// // 结果: null
    /// </code>
    /// </example>
    public static string GetParentDirectoryOf(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("路径不能为空", nameof(path));
        try
        {
            return Path.GetDirectoryName(Path.GetFullPath(path));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
        {
            throw new ArgumentException($"无效的路径格式: {path}", nameof(path), ex);
        }
    }

    #endregion

    #region SafeExecute(安全地执行操作)

    /// <summary>
    /// 安全地执行操作，捕获异常并返回默认值
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="func">要执行的操作</param>
    /// <param name="defaultValue">异常时返回的默认值</param>
    /// <returns>操作结果或默认值</returns>
    /// <exception cref="ArgumentNullException">当 func 为 null 时抛出</exception>
    /// <remarks>
    /// 这是一个通用的异常安全包装器，可以用于任何可能抛出异常的操作。
    /// 主要用于需要容错处理的场景。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 安全地解析整数
    /// int result = Common.SafeExecute(() => int.Parse("invalid"), 0);
    /// // 结果: 0
    /// 
    /// // 安全地获取文件信息
    /// var fileInfo = Common.SafeExecute(() => new FileInfo("nonexistent.txt"), null);
    /// // 结果: null
    /// </code>
    /// </example>
    public static T SafeExecute<T>(Func<T> func, T defaultValue = default)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func), "执行函数不能为 null");
        try
        {
            return func();
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全地执行操作，捕获异常但不返回值
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <returns>如果执行成功返回 true，否则返回 false</returns>
    /// <exception cref="ArgumentNullException">当 action 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// // 安全地创建目录
    /// bool success = Common.SafeExecute(() => Directory.CreateDirectory("new-folder"));
    /// if (success)
    /// {
    ///     Console.WriteLine("目录创建成功");
    /// }
    /// </code>
    /// </example>
    public static bool SafeExecute(Action action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action), "执行操作不能为 null");
        try
        {
            action();
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region RetryExecute(重试执行操作)

    /// <summary>
    /// 重试执行操作，直到成功或达到最大重试次数
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="func">要执行的操作</param>
    /// <param name="maxRetries">最大重试次数，默认为 3</param>
    /// <param name="delay">重试间隔时间，默认为 100ms</param>
    /// <returns>操作结果</returns>
    /// <exception cref="ArgumentNullException">当 func 为 null 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 maxRetries 小于 0 时抛出</exception>
    /// <exception cref="AggregateException">当所有重试都失败时抛出，包含所有异常</exception>
    /// <example>
    /// <code>
    /// // 重试网络请求
    /// var result = Common.RetryExecute(() => 
    /// {
    ///     // 可能失败的网络操作
    ///     return httpClient.GetStringAsync("https://api.example.com/data");
    /// }, maxRetries: 5, delay: TimeSpan.FromSeconds(1));
    /// </code>
    /// </example>
    public static T RetryExecute<T>(Func<T> func, int maxRetries = 3, TimeSpan? delay = null)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func), "执行函数不能为 null");
        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "最大重试次数不能为负数");
        var actualDelay = delay ?? TimeSpan.FromMilliseconds(100);
        var exceptions = new List<Exception>();
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                if (attempt == maxRetries)
                    break;
                if (actualDelay > TimeSpan.Zero)
                    System.Threading.Thread.Sleep(actualDelay);
            }
        }
        throw new AggregateException($"操作在 {maxRetries + 1} 次尝试后仍然失败", exceptions);
    }

    #endregion

    #region Clamp(确保值在指定范围内)

    /// <summary>
    /// 确保值在指定范围内
    /// </summary>
    /// <typeparam name="T">可比较的类型</typeparam>
    /// <param name="value">要检查的值</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <returns>限制在范围内的值</returns>
    /// <exception cref="ArgumentException">当最小值大于最大值时抛出</exception>
    /// <example>
    /// <code>
    /// int clampedValue = Common.Clamp(150, 0, 100);  // 结果: 100
    /// int clampedValue2 = Common.Clamp(-10, 0, 100); // 结果: 0
    /// int clampedValue3 = Common.Clamp(50, 0, 100);  // 结果: 50
    /// </code>
    /// </example>
    public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
    {
        if (min.CompareTo(max) > 0)
            throw new ArgumentException("最小值不能大于最大值", nameof(min));
        if (value.CompareTo(min) < 0)
            return min;
        if (value.CompareTo(max) > 0)
            return max;
        return value;
    }

    #endregion
}