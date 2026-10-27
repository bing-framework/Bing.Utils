# 既有计时测试修复

用户已授权修复测试；不修改生产实现、包版本或性能阈值。

- [x] 映射：把失败的性能方法移到独立非并行测试集合；保留 10000 次/5000ms，增加预热并把全部结果断言移出计时段。luna_worker 仅负责映射文件与集合定义。
- [x] 命令：主代理为继承管道测试增加子进程就绪、父进程退出许可、子进程释放信号。启动准备最多 30 秒，退出行为仍要求 5 秒内完成；总进程超时只是清理 watchdog。失败路径释放并回收父子进程、观察后台任务异常。
- [x] 非零退出码用例使用系统轻量 shell 的 exit 命令，保持退出码与 EnsureSuccess 断言。
- [x] 主代理集成并验证；独立只读审查；仅修复 OPEN_ACTIONABLE。

测试归属按 test-project-boundary refs：主要目标为 Bing.Utils，保留 Bing.Utils.Tests；只按方法拆出计时用例，无迁移到其他模块。集合只约束这些测试，普通测试保留并行。

Change Impact Analysis：ChangedFiles 为命令测试、MapperHelperTest、新性能测试及集合；ChangedProjects=Bing.Utils.Tests；PublicContracts/ProductionRuntimePaths/Providers/TFMs/BuildPackaging/BenchmarkHarness 均不变；ChangedDocsOnly=false；无生产消费者受影响；Risk=MEDIUM（测试进程生命周期）。

验证：L0 编译及 UTF-8/diff 检查；L1 net8.0 精准命令/映射测试；兼容 netcoreapp3.1 相同用例；L2 收口主包 net8 全套检验集合调度。串行运行构建与测试，复用文件识别已通过的证据，不重复其多 TFM 矩阵。没有新增性能阈值或性能提升声明。最多三轮 Review/Fix，无可执行 finding 后停止。

状态：完成。net8.0 完整套件 7411/7411，netcoreapp3.1 精准用例 24/24，独立审查 CLOSED。
