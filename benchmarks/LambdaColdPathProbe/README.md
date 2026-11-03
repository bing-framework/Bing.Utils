# LambdaColdPathProbe

验证 checked 成员解析、PLINQ Contains 兼容性，并测量每次新建表达式后只求值一次的分配成本。无项目引用或第三方依赖，以参数加载指定版本的 Bing.Utils.dll。

```powershell
dotnet build benchmarks/LambdaColdPathProbe/LambdaColdPathProbe.csproj -c Release
dotnet benchmarks/LambdaColdPathProbe/bin/Release/net8.0/LambdaColdPathProbe.dll output/release/net8.0/Bing.Utils.dll
```

每个版本单独启动进程，预热 2000 次，采样 5 组、每组 10000 次，返回中位数；实际并发为 1。测量包含表达式建树和求值，不能直接与复用同一个表达式的热路径耗时比较。输出保留运行时和程序集 MVID。功能验收由对应回归测试断言，此探针输出实际结果供对照。

该源码与 2026-10-29 第四轮审查的临时探针一致。本轮 before/after 使用同一临时探针二进制；此目录用于持久化复现源码。
