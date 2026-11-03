using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Bing.Exceptions;

/// <summary>
/// Bing.Exceptions 的 BCL 性能与资源探针。
/// </summary>
/// <remarks>
/// 用可比输入记录运行时间、分配量和格式化输出边界。
/// </remarks>
internal static class Program
{
    /// <summary>
    /// 每个性能场景在正式采样前执行的预热次数。
    /// </summary>
    private const int WarmupIterations = 3;

    /// <summary>
    /// 每个性能场景用于计算中位数的正式样本数。
    /// </summary>
    private const int MeasurementSamples = 10;

    /// <summary>
    /// 运行同步及可用的异步探针，并输出结构化结果。
    /// </summary>
    /// <param name="args">可选标签和 JSON 结果文件路径参数。</param>
    /// <returns>探针成功时返回 0，处理失败时返回 1。</returns>
    private static async Task<int> Main(string[] args)
    {
        Console.InputEncoding = new UTF8Encoding(false);
        Console.OutputEncoding = new UTF8Encoding(false);

        try
        {
            var label = ReadOption(args, "--label") ?? "candidate";
            var resultsPath = ReadOption(args, "--results");
            var measurements = RunSynchronousMeasurements();
            var asyncStatus = await RunOptionalAsyncMeasurements(measurements);

            var report = new ProbeReport(
                label,
                DateTimeOffset.UtcNow,
                RuntimeInformation.FrameworkDescription,
                Environment.Version.ToString(),
                RuntimeInformation.OSDescription,
                RuntimeInformation.ProcessArchitecture.ToString(),
                typeof(Try).Assembly.FullName ?? "unknown",
                typeof(Try).Assembly.Location,
                WarmupIterations,
                MeasurementSamples,
                asyncStatus,
                measurements);

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            if (string.IsNullOrWhiteSpace(resultsPath))
            {
                Console.WriteLine(json);
            }
            else
            {
                var fullPath = Path.GetFullPath(resultsPath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, json, new UTF8Encoding(false));
                Console.WriteLine($"Probe results written to {fullPath}");
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    /// <summary>
    /// 运行与新异步入口无关的同步基线场景。
    /// </summary>
    /// <returns>按执行顺序排列的同步测量结果。</returns>
    private static List<ProbeMeasurement> RunSynchronousMeasurements()
    {
        var results = new List<ProbeMeasurement>();

        Add("Try.Create.Success", 1, 1, () => new ProbeSample(Try.Create(() => 42)));
        Add("Try.Create.Failure", 1, 1, () => new ProbeSample(Try.Create<int>(() => throw new InvalidOperationException("probe-create"))));
        Add("Try.Invoke.Success", 1, 1, () => new ProbeSample(Try.Invoke((Action)(static () => { }))));
        Add("Try.Invoke.Failure", 1, 1, () => new ProbeSample(Try.Invoke((Action)(static () => throw new InvalidOperationException("probe-invoke")))));

        var successful = Try.LiftValue(42);
        Add("Try.Map.Success", 1, 1, () => new ProbeSample(successful.Map(value => value + 1)));

        var failed = Try.LiftException<int>(new InvalidOperationException("probe-map"), "probe-cause");
        var mapCallbackCount = 0;
        Add("Try.Map.FailureShortCircuit", 1, 1, () => new ProbeSample(failed.Map<int>(_ =>
        {
            mapCallbackCount++;
            return 1;
        })));

        foreach (var depth in new[] { 1, 10, 100, 1000 })
        {
            var chain = BuildExceptionChain(depth);
            Add($"Unwrap.Untyped.Depth{depth}", depth, depth, () => new ProbeSample(ExceptionHelper.Unwrap(chain)));
            Add($"Unwrap.Typed.Depth{depth}", depth, depth, () => new ProbeSample(ExceptionHelper.Unwrap<InvalidOperationException>(chain)));

            results.Add(MeasureFormatting(
                $"FormatMessage.Depth{depth}",
                depth,
                depth,
                () => chain.FormatMessage()));
        }

        var aggregateLabels = Enumerable.Range(0, 16)
            .Select(index => $"aggregate-branch-{index:D2}")
            .ToArray();
        var aggregate = new AggregateException(
            "probe-aggregate",
            aggregateLabels.Select(label => (Exception)new InvalidOperationException(label)));
        results.Add(MeasureFormatting(
            "FormatMessage.Aggregate.DefaultEntry",
            aggregateLabels.Length,
            aggregateLabels.Length + 1,
            () => aggregate.FormatMessage(),
            aggregateLabels));

        var aggregateFormatter = FindAggregateFormatter();
        if (aggregateFormatter is not null)
        {
            var options = Activator.CreateInstance(aggregateFormatter.OptionsType)
                ?? throw new InvalidOperationException("Could not create ExceptionFormatOptions.");
            SetRequiredOption(options, "IncludeAggregateExceptions", true);
            SetRequiredOption(options, "MaxDepth", 8);
            SetRequiredOption(options, "MaxExceptionCount", aggregateLabels.Length + 1);
            SetRequiredOption(options, "MaxOutputLength", 8192);
            results.Add(MeasureFormatting(
                "FormatMessage.Aggregate.ExplicitBranches",
                aggregateLabels.Length,
                aggregateLabels.Length + 1,
                () => aggregateFormatter.Format(aggregate, options),
                aggregateLabels));
        }

        var input = Enumerable.Range(0, 128).ToArray();
        Add("Try.InvokeRange.Success", input.Length, input.Length, () =>
        {
            var actions = Try.InvokeRange(input, static (int _) => { }).ToArray();
            return new ProbeSample(actions, null, actions.Length);
        }, operationsPerSample: 1);
        Add("Try.InvokeRange.OneFailure", input.Length, input.Length, () =>
        {
            var actions = Try.InvokeRange(input, static (int value) =>
            {
                if (value == 64)
                    throw new InvalidOperationException("probe-range");
            }).ToArray();
            return new ProbeSample(actions, null, actions.Length);
        }, operationsPerSample: 1);

        Add("Try.CreateFromTask.Completed", 1, 1,
            () => new ProbeSample(Try.CreateFromTask(() => Task.FromResult(42))));

        if (mapCallbackCount != 0)
            throw new InvalidOperationException("Map executed its selector for an already failed Try value.");

        return results;

        void Add(string name, int inputSize, int exceptionNodeCount, Func<ProbeSample> operation, int operationsPerSample = 1000)
        {
            results.Add(Measure(name, inputSize, exceptionNodeCount, operation, operationsPerSample));
        }
    }

    /// <summary>
    /// 通过反射测量候选程序集提供的异步入口。
    /// </summary>
    /// <param name="measurements">接收异步探针测量结果的列表。</param>
    /// <returns>说明异步入口是否可用及其测量方式的状态文本。</returns>
    private static async Task<string> RunOptionalAsyncMeasurements(List<ProbeMeasurement> measurements)
    {
        var createMethod = FindGenericTaskApi("CreateAsync");
        var invokeMethod = FindTaskApi("InvokeTaskAsync");
        if (createMethod is null && invokeMethod is null)
            return "No new asynchronous entry points found; baseline remains valid.";

        if (createMethod is not null)
        {
            var create = (Func<Func<Task<int>>, string, Task<Try<int>>>)createMethod.CreateDelegate(
                typeof(Func<Func<Task<int>>, string, Task<Try<int>>>));
            measurements.Add(await MeasureAsync(
                "Try.CreateAsync.Completed",
                1,
                1,
                async () => new ProbeSample(await create(static () => Task.FromResult(42), "probe-async")),
                operationsPerSample: 20));
            measurements.Add(await MeasureAsync(
                "Try.CreateAsync.Pending",
                1,
                1,
                async () => new ProbeSample(await create(static async () =>
                {
                    await Task.Delay(1).ConfigureAwait(false);
                    return 42;
                }, "probe-async")),
                operationsPerSample: 1));
        }

        if (invokeMethod is not null)
        {
            var invoke = (Func<Func<Task>, string, Task<TryAction>>)invokeMethod.CreateDelegate(
                typeof(Func<Func<Task>, string, Task<TryAction>>));
            measurements.Add(await MeasureAsync(
                "Try.InvokeTaskAsync.Completed",
                1,
                1,
                async () => new ProbeSample(await invoke(static () => Task.CompletedTask, "probe-async")),
                operationsPerSample: 20));
            measurements.Add(await MeasureAsync(
                "Try.InvokeTaskAsync.Pending",
                1,
                1,
                async () => new ProbeSample(await invoke(static async () => await Task.Delay(1).ConfigureAwait(false), "probe-async")),
                operationsPerSample: 1));
        }

        return "New asynchronous entry points measured through reflected delegates; pending cases include completion latency.";
    }

    /// <summary>
    /// 对同步操作预热并测量每次操作的耗时和线程分配量。
    /// </summary>
    /// <param name="name">结果中的场景名称。</param>
    /// <param name="inputSize">每次操作处理的输入规模。</param>
    /// <param name="exceptionNodeCount">场景涉及的异常节点数量。</param>
    /// <param name="operation">执行一次场景并返回输出特征的委托。</param>
    /// <param name="operationsPerSample">每个正式样本执行的操作次数。</param>
    /// <returns>汇总中位耗时、分配量及最后一次输出特征的测量结果。</returns>
    private static ProbeMeasurement Measure(
        string name,
        int inputSize,
        int exceptionNodeCount,
        Func<ProbeSample> operation,
        int operationsPerSample)
    {
        for (var i = 0; i < WarmupIterations; i++)
            _ = operation();

        var samples = new List<RawSample>(MeasurementSamples);
        ProbeSample last = default;
        for (var i = 0; i < MeasurementSamples; i++)
        {
            CollectBeforeSample();
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            for (var operationIndex = 0; operationIndex < operationsPerSample; operationIndex++)
                last = operation();
            var elapsed = Stopwatch.GetTimestamp() - start;
            var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            samples.Add(new RawSample(
                elapsed * 1_000_000d / Stopwatch.Frequency / operationsPerSample,
                (double)allocated / operationsPerSample));
        }

        return CreateMeasurement(name, inputSize, exceptionNodeCount, operationsPerSample, last, samples);
    }

    /// <summary>
    /// 对异步操作预热并测量每次操作的耗时和进程分配量。
    /// </summary>
    /// <param name="name">结果中的场景名称。</param>
    /// <param name="inputSize">每次操作处理的输入规模。</param>
    /// <param name="exceptionNodeCount">场景涉及的异常节点数量。</param>
    /// <param name="operation">执行一次异步场景并返回输出特征的委托。</param>
    /// <param name="operationsPerSample">每个正式样本执行的操作次数。</param>
    /// <returns>汇总中位耗时、分配量及最后一次输出特征的异步结果。</returns>
    private static async Task<ProbeMeasurement> MeasureAsync(
        string name,
        int inputSize,
        int exceptionNodeCount,
        Func<Task<ProbeSample>> operation,
        int operationsPerSample)
    {
        for (var i = 0; i < WarmupIterations; i++)
            _ = await operation().ConfigureAwait(false);

        var samples = new List<RawSample>(MeasurementSamples);
        ProbeSample last = default;
        for (var i = 0; i < MeasurementSamples; i++)
        {
            CollectBeforeSample();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var start = Stopwatch.GetTimestamp();
            for (var operationIndex = 0; operationIndex < operationsPerSample; operationIndex++)
                last = await operation().ConfigureAwait(false);
            var elapsed = Stopwatch.GetTimestamp() - start;
            var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
            samples.Add(new RawSample(
                elapsed * 1_000_000d / Stopwatch.Frequency / operationsPerSample,
                (double)allocated / operationsPerSample));
        }

        return CreateMeasurement(name, inputSize, exceptionNodeCount, operationsPerSample, last, samples);
    }

    /// <summary>
    /// 将原始样本汇总为一个场景的测量记录。
    /// </summary>
    /// <param name="name">结果中的场景名称。</param>
    /// <param name="inputSize">每次操作处理的输入规模。</param>
    /// <param name="exceptionNodeCount">场景涉及的异常节点数量。</param>
    /// <param name="operationsPerSample">每个样本包含的操作次数。</param>
    /// <param name="last">最后一次操作的输出特征。</param>
    /// <param name="samples">已采集的耗时和分配量样本。</param>
    /// <returns>包含中位数和输出特征的测量记录。</returns>
    private static ProbeMeasurement CreateMeasurement(
        string name,
        int inputSize,
        int exceptionNodeCount,
        int operationsPerSample,
        ProbeSample last,
        List<RawSample> samples)
    {
        var elapsed = samples.Select(sample => sample.MicrosecondsPerOperation).OrderBy(value => value).ToArray();
        var allocated = samples.Select(sample => sample.BytesPerOperation).OrderBy(value => value).ToArray();
        return new ProbeMeasurement(
            name,
            inputSize,
            exceptionNodeCount,
            operationsPerSample,
            Median(elapsed),
            Median(allocated),
            last.OutputLength,
            last.OutputCount,
            last.TruncationReason);
    }

    /// <summary>
    /// 查找名称匹配且接受 Task&lt;T&gt; 工厂及 cause 参数的泛型异步 API。
    /// </summary>
    /// <param name="name">需要查找的公开方法名称。</param>
    /// <returns>闭合为 int 结果类型的方法；未找到时返回 null。</returns>
    private static MethodInfo? FindGenericTaskApi(string name)
    {
        foreach (var method in typeof(Try).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name != name || !method.IsGenericMethodDefinition)
                continue;
            var closed = method.MakeGenericMethod(typeof(int));
            var parameters = closed.GetParameters();
            if (parameters.Length == 2 && IsTaskFactory(parameters[0].ParameterType) && parameters[1].ParameterType == typeof(string))
                return closed;
        }
        return null;
    }

    /// <summary>
    /// 查找接受无结果 Task 工厂及 cause 参数的非泛型异步 API。
    /// </summary>
    /// <param name="name">需要查找的公开方法名称。</param>
    /// <returns>匹配的方法；未找到时返回 null。</returns>
    private static MethodInfo? FindTaskApi(string name) =>
        typeof(Try).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
            {
                if (method.Name != name || method.IsGenericMethodDefinition)
                    return false;
                var parameters = method.GetParameters();
                return parameters.Length == 2 && parameters[0].ParameterType == typeof(Func<Task>) && parameters[1].ParameterType == typeof(string);
            });

    /// <summary>
    /// 判断类型是否为返回 Task&lt;T&gt; 的委托类型。
    /// </summary>
    /// <param name="parameterType">需要检查的参数类型。</param>
    /// <returns>类型符合委托形状时返回 true，否则返回 false。</returns>
    private static bool IsTaskFactory(Type parameterType)
    {
        if (!parameterType.IsGenericType || parameterType.GetGenericTypeDefinition() != typeof(Func<>))
            return false;
        var resultType = parameterType.GetGenericArguments()[0];
        return resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>);
    }

    /// <summary>
    /// 构造指定层数的 InnerException 链供探针复用。
    /// </summary>
    /// <param name="depth">请求构造的异常链深度。</param>
    /// <returns>最外层异常；其 InnerException 链以 InvalidOperationException 结束。</returns>
    private static Exception BuildExceptionChain(int depth)
    {
        Exception current = new InvalidOperationException("probe-leaf");
        for (var level = 1; level < depth; level++)
            current = new Exception($"probe-node-{level}", current);
        return current;
    }

    /// <summary>
    /// 查找候选程序集接受格式化选项的公开扩展方法，避免基线编译依赖新 API。
    /// </summary>
    /// <returns>可调用的格式化器和选项类型；程序集不含该 API 时返回 null。</returns>
    private static AggregateFormatter? FindAggregateFormatter()
    {
        var assembly = typeof(Try).Assembly;
        var optionsType = assembly.GetType("Bing.Exceptions.ExceptionFormatOptions", throwOnError: false);
        if (optionsType is null)
            return null;

        var method = typeof(ExceptionExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(candidate =>
            {
                if (candidate.Name != "FormatMessage" || candidate.ReturnType != typeof(string))
                    return false;
                var parameters = candidate.GetParameters();
                return parameters.Length == 2
                    && parameters[0].ParameterType == typeof(Exception)
                    && parameters[1].ParameterType == optionsType;
            });
        if (method is null)
            throw new InvalidOperationException(
                "ExceptionFormatOptions is present, but its FormatMessage options overload was not found.");

        var exceptionParameter = Expression.Parameter(typeof(Exception), "exception");
        var optionsParameter = Expression.Parameter(typeof(object), "options");
        var call = Expression.Call(method, exceptionParameter, Expression.Convert(optionsParameter, optionsType));
        var format = Expression.Lambda<Func<Exception, object, string>>(
            call, exceptionParameter, optionsParameter).Compile();
        return new AggregateFormatter(optionsType, format);
    }

    /// <summary>
    /// 设置候选格式化选项的必需公开属性。
    /// </summary>
    /// <param name="options">当前候选程序集创建的格式化选项对象。</param>
    /// <param name="name">要设置的属性名称。</param>
    /// <param name="value">符合属性类型的配置值。</param>
    private static void SetRequiredOption(object options, string name, object value)
    {
        var property = options.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanWrite)
            throw new InvalidOperationException($"Required format option '{name}' was not found.");
        property.SetValue(options, value);
    }

    /// <summary>
    /// 统计输出文本中完整出现的聚合分支消息数，不推断实际遍历节点数。
    /// </summary>
    /// <param name="formatted">异常格式化后的文本。</param>
    /// <param name="branchLabels">输入聚合分支各自唯一的消息标记。</param>
    /// <returns>输出中可识别的分支消息数量。</returns>
    private static int CountVisibleAggregateMessages(string formatted, string[] branchLabels)
    {
        var count = 0;
        foreach (var label in branchLabels)
        {
            if (formatted.Contains(label, StringComparison.Ordinal))
                count++;
        }
        return count;
    }

    /// <summary>
    /// 测量格式化并在计时结束后统计可见消息和截断原因。
    /// </summary>
    /// <param name="name">结果中的场景名称。</param>
    /// <param name="inputSize">输入规模；聚合场景表示分支数。</param>
    /// <param name="exceptionNodeCount">输入异常节点总数，不代表格式化后可见节点数。</param>
    /// <param name="format">执行一次格式化并返回文本的委托。</param>
    /// <param name="visibleMessageLabels">可选的分支唯一消息标记；用于统计输出中可见消息，不代表遍历节点数。</param>
    /// <returns>包含格式化测量值、可见消息数和截断原因的记录。</returns>
    private static ProbeMeasurement MeasureFormatting(
        string name,
        int inputSize,
        int exceptionNodeCount,
        Func<string> format,
        string[]? visibleMessageLabels = null)
    {
        string? lastFormatted = null;
        var measurement = Measure(name, inputSize, exceptionNodeCount, () =>
        {
            lastFormatted = format();
            return new ProbeSample(lastFormatted, lastFormatted.Length);
        }, operationsPerSample: 1);
        var output = lastFormatted ?? throw new InvalidOperationException("FormatMessage produced no output.");
        return measurement with
        {
            OutputCount = visibleMessageLabels is null
                ? measurement.OutputCount
                : CountVisibleAggregateMessages(output, visibleMessageLabels),
            TruncationReason = DetectTruncation(output)
        };
    }

    /// <summary>
    /// 从格式化文本中提取一个或多个明确的截断原因。
    /// </summary>
    /// <param name="value">需要检查的格式化文本。</param>
    /// <returns>以分号分隔的原因码；未截断时返回 null。</returns>
    private static string? DetectTruncation(string value)
    {
        string? reasons = null;
        if (ContainsAny(value, "[已截断：深度上限]", "[truncated:depth limit]", "[truncated:depth_limit]"))
            reasons = AppendReason(reasons, "depth_limit");
        if (ContainsAny(value, "[已截断：异常节点数上限]", "[truncated:exception count limit]", "[truncated:node count limit]", "[truncated:node_count_limit]"))
            reasons = AppendReason(reasons, "node_count_limit");
        if (ContainsAny(value, "[已截断：输出长度上限]", "[truncated:output length limit]", "[truncated:output_length_limit]"))
            reasons = AppendReason(reasons, "output_length_limit");

        if (reasons is not null)
            return reasons;
        return ContainsAny(value, "[已截断：", "[truncated:") ? "unknown_truncation" : null;
    }

    /// <summary>
    /// 在文本中查找固定数量的可选标记，避免为 params 数组分配内存。
    /// </summary>
    /// <param name="value">待检查的文本。</param>
    /// <param name="firstMarker">始终检查的标记。</param>
    /// <param name="secondMarker">第二个需要检查的标记。</param>
    /// <param name="thirdMarker">可选的第三个标记。</param>
    /// <param name="fourthMarker">可选的第四个标记。</param>
    /// <returns>找到任一标记时返回 true，否则返回 false。</returns>
    private static bool ContainsAny(
        string value,
        string firstMarker,
        string secondMarker,
        string? thirdMarker = null,
        string? fourthMarker = null) =>
        value.Contains(firstMarker, StringComparison.OrdinalIgnoreCase)
        || value.Contains(secondMarker, StringComparison.OrdinalIgnoreCase)
        || (thirdMarker is not null && value.Contains(thirdMarker, StringComparison.OrdinalIgnoreCase))
        || (fourthMarker is not null && value.Contains(fourthMarker, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 将一个已识别的原因码追加到已有原因集合。
    /// </summary>
    /// <param name="current">之前识别到的原因码；尚无原因时为 null。</param>
    /// <param name="reason">本次识别到的原因码。</param>
    /// <returns>单个或以分号连接的原因码文本。</returns>
    private static string AppendReason(string? current, string reason) =>
        current is null ? reason : string.Concat(current, ";", reason);

    /// <summary>
    /// 在正式采样前执行完整垃圾回收以降低残留对象的影响。
    /// </summary>
    private static void CollectBeforeSample()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    /// <summary>
    /// 计算已升序排列的样本值的中位数。
    /// </summary>
    /// <param name="values">已升序排列的数值样本。</param>
    /// <returns>样本中位数；输入为空时返回 0。</returns>
    private static double Median(double[] values)
    {
        if (values.Length == 0)
            return 0;
        var middle = values.Length / 2;
        return values.Length % 2 == 0 ? (values[middle - 1] + values[middle]) / 2 : values[middle];
    }

    /// <summary>
    /// 读取指定命令行选项后面的值。
    /// </summary>
    /// <param name="args">待解析的命令行参数。</param>
    /// <param name="name">要查找的选项名称。</param>
    /// <returns>选项值；未指定该选项时返回 null。</returns>
    private static string? ReadOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"Missing value for {name}.");
                return args[i + 1];
            }
        }
        return null;
    }

    /// <summary>
    /// 表示一次操作的返回对象和可选输出统计。
    /// </summary>
    /// <param name="Value">被测操作返回的对象。</param>
    /// <param name="OutputLength">格式化文本长度；非格式化场景为 null。</param>
    /// <param name="OutputCount">操作产生的项目数或格式化输出中可见的分支消息数；未统计时为 null。</param>
    /// <param name="TruncationReason">检测到截断时的说明；未截断时为 null。</param>
    private readonly record struct ProbeSample(
        object? Value,
        int? OutputLength = null,
        int? OutputCount = null,
        string? TruncationReason = null);

    /// <summary>
    /// 保存候选程序集的 options 类型及不经反射 Invoke 的格式化调用委托。
    /// </summary>
    /// <param name="OptionsType">候选程序集中公开的格式化选项类型。</param>
    /// <param name="Format">使用给定异常和 options 对象格式化文本的委托。</param>
    private sealed record AggregateFormatter(Type OptionsType, Func<Exception, object, string> Format);

    /// <summary>
    /// 保存单次正式样本的每操作耗时和分配量。
    /// </summary>
    /// <param name="MicrosecondsPerOperation">单次操作耗时，单位为微秒。</param>
    /// <param name="BytesPerOperation">单次操作分配的字节数。</param>
    private readonly record struct RawSample(double MicrosecondsPerOperation, double BytesPerOperation);

    /// <summary>
    /// 汇总一个性能场景的输入、统计值及输出边界信息。
    /// </summary>
    /// <param name="Name">场景名称。</param>
    /// <param name="InputSize">每次操作处理的输入规模；聚合场景表示分支数。</param>
    /// <param name="ExceptionNodeCount">输入异常节点总数，不代表格式化后可见的输出节点数。</param>
    /// <param name="OperationsPerSample">每个样本包含的操作次数。</param>
    /// <param name="MedianMicrosecondsPerOperation">每次操作的中位耗时，单位为微秒。</param>
    /// <param name="MedianAllocatedBytesPerOperation">每次操作的中位分配量，单位为字节。</param>
    /// <param name="OutputLength">最后一次操作输出的字符数；未统计时为 null。</param>
    /// <param name="OutputCount">最后一次操作输出的项目数或可见分支消息数，不表示遍历节点数；未统计时为 null。</param>
    /// <param name="TruncationReason">输出截断原因；未截断时为 null。</param>
    private sealed record ProbeMeasurement(
        string Name,
        int InputSize,
        int ExceptionNodeCount,
        int OperationsPerSample,
        double MedianMicrosecondsPerOperation,
        double MedianAllocatedBytesPerOperation,
        int? OutputLength,
        int? OutputCount,
        string? TruncationReason);

    /// <summary>
    /// 记录一次探针运行的环境信息、运行时版本和测量结果。
    /// </summary>
    /// <param name="Label">用于区分基线或候选运行的标签。</param>
    /// <param name="CapturedAtUtc">结果生成时间，使用 UTC。</param>
    /// <param name="FrameworkDescription">运行时框架描述。</param>
    /// <param name="RuntimeVersion">运行时版本。</param>
    /// <param name="OperatingSystem">操作系统描述。</param>
    /// <param name="ProcessArchitecture">进程架构名称。</param>
    /// <param name="BingUtilsAssembly">加载的 Bing.Utils 程序集名称。</param>
    /// <param name="BingUtilsAssemblyPath">加载的 Bing.Utils 程序集路径。</param>
    /// <param name="WarmupIterations">每个场景的预热次数。</param>
    /// <param name="MeasurementSamples">每个场景的正式样本数。</param>
    /// <param name="AsyncProbeStatus">异步入口的发现和测量状态。</param>
    /// <param name="Measurements">本次运行产生的测量结果。</param>
    private sealed record ProbeReport(
        string Label,
        DateTimeOffset CapturedAtUtc,
        string FrameworkDescription,
        string RuntimeVersion,
        string OperatingSystem,
        string ProcessArchitecture,
        string BingUtilsAssembly,
        string BingUtilsAssemblyPath,
        int WarmupIterations,
        int MeasurementSamples,
        string AsyncProbeStatus,
        IReadOnlyList<ProbeMeasurement> Measurements);
}
