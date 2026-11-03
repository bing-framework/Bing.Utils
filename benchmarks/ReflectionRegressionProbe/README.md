# ReflectionRegressionProbe

补充 `ReflectionAccessorProbe` 未覆盖的 Lambda 求值、两个属性的动态实例首次访问，以及强类型访问器重复获取场景。该探针源代码与 2026-10-29 二次审查的临时探针一致，不引用项目或第三方包，可用同一二进制加载修复前后的库。

```powershell
dotnet build benchmarks/ReflectionRegressionProbe/ReflectionRegressionProbe.csproj -c Release
dotnet benchmarks/ReflectionRegressionProbe/bin/Release/net8.0/ReflectionRegressionProbe.dll output/release/net8.0/Bing.Utils.dll output/release/net8.0/Bing.Utils.Reflection.dll --cases --perf
```

每个版本启动新进程。`--cases` 输出接口属性隐藏、嵌套 Contains、条件计数的实际行为；功能验收使用对应单元测试断言。`--perf` 预热 2,000 次，每个场景采样 5 次、每次 10,000 次，报告中位数。方法和访问器在计时前通过反射绑定，计时循环不包含 `MethodInfo.Invoke`。

分配量来自 `GC.GetAllocatedBytesForCurrentThread`。动态 setter 复用已装箱输入；两属性首次读取在每次迭代新建实例；访问器 factory 场景有意重复取得委托，invoke 场景复用委托。复杂方法调用保留实际表达式求值语义，不能把历史错误地返回方法参数当作通用性能目标。

输出包含运行时、程序集 MVID 和工作负载参数。短探针用于定位退化和对比实现，没有设置发布性能阈值；多线程正确性、缓存容量和回收行为由回归测试验证。
