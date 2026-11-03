# ReflectionAccessorProbe

以同一个 .NET 8 探针二进制分别加载修复前、修复后的库，观察属性访问和属性列表查询的耗时、线程分配量。探针无第三方依赖，不参与解决方案构建。

```powershell
dotnet build benchmarks/ReflectionAccessorProbe/ReflectionAccessorProbe.csproj -c Release
dotnet benchmarks/ReflectionAccessorProbe/bin/Release/net8.0/ReflectionAccessorProbe.dll C:/baseline/Bing.Utils.dll C:/baseline/Bing.Utils.Reflection.dll
dotnet benchmarks/ReflectionAccessorProbe/bin/Release/net8.0/ReflectionAccessorProbe.dll output/release/net8.0/Bing.Utils.dll output/release/net8.0/Bing.Utils.Reflection.dll
```

两个版本分别在新进程中运行。基线目录应同时保留依赖 DLL；测试前固定探针和程序集，停止其他构建及测试。输出 JSON 包含运行时、程序集位置与 MVID、预热次数、迭代次数和实际并发度。

每个场景预热 10,000 次，再独立采样 5 次，每次执行 50,000 次，报告耗时和 `GC.GetAllocatedBytesForCurrentThread` 的中位数。委托在计时前绑定，强类型 getter 在计时循环中不装箱。动态 setter 复用已装箱输入；`dynamic-create-and-read` 单独观察实例创建后的首次属性访问成本。

旧 object getter 与新增强类型 getter 使用同一个 int 属性；旧程序集没有新 API 时跳过该项。属性列表样本包含普通、只读、私有 setter 和 init 属性。动态对象为可回收生成类型。

结果用于此次实现对比，不设置发布阈值，也不代表所有类型、属性数量、硬件或并发工作负载。首次访问与重复访问须同时考察；多属性实例会按需创建有界字典。
