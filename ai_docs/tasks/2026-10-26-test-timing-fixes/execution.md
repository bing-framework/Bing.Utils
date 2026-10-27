# 计时测试修复记录

## 范围与实施

仅修改 Bing.Utils.Tests 测试代码。生产实现、项目引用、TFM、依赖与原性能阈值未改。主代理负责命令夹具/集成，luna_worker 负责映射测试提取与只读审查。

- 继承管道：先确认父、子进程完成启动及子进程写入 stdout/stderr，再由测试发出父进程退出许可。子进程持续持有管道直到 RELEASE；原 5 秒退出行为要求保留。30 秒就绪预算及 45 秒整体 watchdog 用于夹具启动和异常清理，不作为产品性能阈值。独立执行线程记录完成时间，避免 await continuation 调度延迟污染计时；失败信息报告任务状态和父/子存活情况。finally 发出释放信号、清理进程并观察执行任务的异常。
- 非零退出码：cmd.exe /d /q 或 /bin/sh 从 stdin 接收 exit 7；避免 PowerShell 启动和 cmd /c 在 netstandard2.0 参数转义下的特殊解析。继续断言 ExitCode=7 以及 EnsureSuccess 抛异常。
- 映射：目标方法从 MapperHelperTest 按方法粒度移至 MapperHelperPerformanceTest，复用公开嵌套模型。预热一次，预分配结果数组；计时只含 10000 次映射，保留小于 5000ms。计时后检查全部结果非空和代表性成员。原测试类其余方法未变。
- TimingSensitive 非并行集合只包含执行器回归类和新映射性能类，其他测试继续使用原调度。新性能类带 Category=Performance，可独立筛选。

归属依据：已读取 test-project-boundary refs；主要断言目标为 Bing.Utils，仍在主包测试项目，无跨模块迁移或重复测试。

## 验证

- 首轮 net8.0 相关类：24/24 PASS。
- 首轮 netcoreapp3.1：23/24；cmd /c 返回 1 而非 7，已定位参数引用差异并改为 stdin 命令，修复后最终 24/24 PASS。
- 独立只读审查发现 1 个计时观察点问题：await 后才停止时钟会把 continuation 延迟算入退出耗时；已改为执行线程记录完成时间，独立定向复核 CLOSED；无其他 OPEN_ACTIONABLE。
- Windows 本地验证；POSIX 脚本静态检查，未声称在 Linux 上执行。

独立运行映射性能用例：

```powershell
dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0 --no-build --filter FullyQualifiedName~MapperHelperPerformanceTest
```

此次修改改善测试测量与隔离，不代表生产映射算法性能提升。集合隔离不能消除其他进程导致的机器负载；若独立受控运行仍超出原阈值，仍应视作待分析性能结果。

- L0：五个测试文件 UTF-8/空白检查及 git diff --check 通过。
- 主包 net8.0 最终完整回归：PASS，7411/7411，0 失败/0 跳过，34 秒；包含本轮全部修改及之前文件识别用例，日志 full-net8.log。

## 收口

Implementation=PASS；L0=PASS；L1=PASS（最终 netcoreapp3.1 24/24）；L2=PASS（最终 net8.0 7411/7411）；Review=PASS。完成全部计划项，一轮审查修复及定向复核，OPEN_ACTIONABLE=0，无阻塞。仅本机 Windows 运行证据，POSIX 执行未验证。不修改生产代码，不提高原性能/退出行为阈值，不暂存或提交。停止重复测试与自动修复。

原第二批实施报告中的失败是历史运行记录；本记录的最终全量结果验证了修复后的当前工作区，不覆盖原始失败日志。
