# 验证码实施与验证记录

日期：2026-10-29。计划：[plan.md](plan.md)。用户已批准全量执行；未暂存、提交或发布。

## Change Impact Analysis

| 范围 | 影响与验证选择 |
| --- | --- |
| 四个新包 | 新 API；四目标框架构建、包引用消费者验证 |
| Core/Memory/DI | 状态、安全和资源行为；net8/net7/net6 单元测试 |
| Drawing.Shared | 仅新增友元程序集；复用原中文白名单，不改变旧字符语义 |
| Drawing.SkiaSharp | 增加借用字体重载，旧入口兼容；真实 PNG/中文及旧验证码回归 |
| Redis | 独立存储协议及原子脚本；模拟故障测试＋隔离真实服务测试 |
| Web | 示例协作、Cookie、防伪和限流；专属 Integration 项目 |
| 文档/解决方案 | 链接、UTF-8、差异检查；文档不触发重复代码测试 |

未改变现有 Security 实现；回归仅覆盖被复用的 HMAC、固定时间比较与随机数。没有正式性能/容量阈值，也不重建既有 baseline。

## 当前任务状态

T1～T7 实现及本机验证完成；随后按用户审查修正初始化竞态、启动配置校验及测试缺口。当前候选以 [审查修复记录](fix-review.md) 和 [独立复核](review-fixes.md) 为准，R1～R5 均 CLOSED。2026-10-29 已在隔离的真实 Redis 7.2.11 上完成 [10 项健康服务验收](redis-validation.md)，无失败或跳过；仅远端 CI 运行证据仍待补充。

IMPLEMENTATION_STATUS=PASS；TEST_STATUS=LOCAL_PASS；REDIS_INTEGRATION_STATUS=PASS；PERFORMANCE_EVIDENCE_STATUS=MEASURED（无批准阈值）；RESOURCE_EVIDENCE_STATUS=LOCAL_PASS；EXTERNAL_GATE_STATUS=BLOCKED_EXTERNAL（仅远端 CI）；RELEASE_STATUS=NOT_REQUESTED（未发布）。

## 已执行证据

| 检查 | 最终结果 |
| --- | --- |
| 核心单元测试 | net8/net7/net6 各 26/26 |
| DI 单元测试 | net8/net7/net6 各 2/2；从核心迁移到专属项目，符合主要断言归属 |
| Redis 适配器单元测试 | net8/net7/net6 各 6/6，使用模拟连接，验证取消、晚到结果/故障、不重试、协议往返及主节点标记 |
| SkiaSharp Integration | 16/16，真实 PNG、配置快照、字节限额、字体所有权、中文缺字拒绝及服务字符集兼容检查 |
| Web Integration | 6/6，防伪、Cookie/流程隔离、刷新/重放、独立会话/IP 限流、no-store、渲染/存储故障 503 |
| 页面脚本回归 | 3/3，慢初始化、失败重试、串行刷新及错误后刷新；执行真实页面脚本 |
| 旧 SkiaSharp/ImageSharp/System.Drawing 验证码回归 | 10/10、13/13、7/7（net8） |
| Security 定向回归 | HMAC/固定时间比较/安全随机数 10/10（net8） |
| Redis 真实 Integration | 既有 loopback 连接故障 1 项通过；本轮隔离 Redis 7.2.11 健康服务 10/10，通过且无跳过，见 redis-validation.md |
| 四包全框架构建与 pack | net8/net7/net6/netstandard2.0 成功；产物见 output/captcha-packages |
| 解决方案最终 Release 构建 | 成功，0 error、30 warning；各项目按自身框架构建 |
| NuGet 独立消费者 | 四框架编译；net8/net7/net6 真实 PNG 与一次性校验运行通过 |
| UTF-8/Markdown/差异 | 修改及新增文本 UTF-8 可读；验证码文档链接与围栏完整；git diff --check 无错误 |

本机字体：中文 `C:\Windows\Fonts\msyh.ttc`、Latin `C:\Windows\Fonts\arial.ttf`。CI 对应 Noto CJK / DejaVu Sans。

Web 首轮发现测试把已有会话误当新会话；修正测试准备顺序后通过。`dotnet build Bing.Utils.sln -c Release -f net8.0` 不适用于仓库中只支持 netstandard 的 Text/Samples 项目，且并行公共输出存在竞争；最终采用 `dotnet build Bing.Utils.sln -c Release -m:1 --no-restore`，按项目自身目标框架完成构建。未修改这些既有项目。

保留而未压制的警告：Redis 2.13.1 的传递依赖 System.IO.Hashing 9.0.12 声明未测试 net6/net7；另有现有项目兼容性、过时 API 及打包 README 提示。三运行时测试通过只说明本次本地行为，不代表依赖供应方扩大支持承诺。

新增四包及受影响的 Drawing.Shared/Drawing.SkiaSharp、Security 依赖包共 7 个本地 nupkg 和符号包，沿用仓库 1.5.0，不发布。本轮重新打包四个验证码包；消费者通过新的独立缓存 `output/captcha-fix-consumer-cache` 及 packageSourceMapping 强制从本地输出读取 Bing.Utils 包，避免误用历史缓存，并验证新增的可选校验接口。

## Finding 分类

| Finding | 状态 | 处理 |
| --- | --- | --- |
| Redis 未匹配 ID 时未知协议被删除 | CLOSED | 保留并报故障；真实 Redis 7.2.11 服务断言通过 |
| UTF-8 协议边界与公共字符长度不一致 | CLOSED | 单字段最多 192 字节，总体有界；协议单测通过，真实中文往返归入外部 Redis 验收 |
| Redis 派发后等待取消 | CLOSED | 取消等待、观察在途失败、不重试；三运行时模拟测试通过 |
| 中文模式未强制显式字体 | CLOSED | 启动校验与真实字体拒绝测试通过 |
| 本机缺少独立 Redis 服务 | CLOSED | 临时 WSL 环境提供 Redis 7.2.11，10 项健康服务测试通过，测试后清理环境 |
| 远端 CI 尚未运行 | BLOCKED_EXTERNAL | CI 工作流提供 Redis 7 容器；本地证据不替代远端运行 |

OPEN_ACTIONABLE=0。不把 CI 文件或本机 Redis 验收视为 CI 已运行证据。没有重新运行与当前变化无关的全量测试或重建性能 baseline。

| TODO 分组 | 当前内容 |
| --- | --- |
| Completed | 四包、Web、测试、文档、构建打包、消费者和探针 |
| Open Actionable | 无 |
| Blocked Approval | 无，用户已批准执行范围 |
| Blocked External | 仅远端 CI 运行证据；健康 Redis 验收已通过 |
| Not Applicable | 新 API 不存在同构历史性能基线，不制作虚假的 before 对比 |
| Accepted Limitations | 内存和示例限流仅覆盖单实例；多实例需宿主 Redis/分布式限流；不自动降级。Redis 故障切换回滚不承诺持久一次性，宿主负责部署选择 |
| Verified Boundaries | 输入长度、尺寸、字节限额、容量、过期点的受控测试通过 |
| Deferred | 滑块/点选/音频与跨区域多主为已锁定后续范围，不影响本次图片生命周期实现 |

## 有限性能探针

以下为首次实施时的测量，保留作历史参考；本轮增加字符集缓存后未重跑绘图探针，不据此宣称本轮性能。新的默认满容量测量见 [审查修复记录](fix-review.md)。

Windows、本机 Intel Core Ultra 9 185H、.NET SDK 8.0.425 / net8 Release。默认 5 位字母数字、160×60 PNG、有界内存，每个并发档 100 次创建及错误答案消费。包含本次 JIT/线程调度影响，无单独预热，无正式发布阈值。

| 并发 | 总耗时 ms | 平均生成 ms | 平均校验 ms | 进程托管分配 bytes | 平均 PNG bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 239.18 | 2.272 | 0.036 | 7370000 | 5426 |
| 8 | 43.30 | 3.217 | 0.087 | 7159128 | 5391 |

并发平均值包含等待，不等同于总耗时/条数；托管分配不含 Skia 原生堆。此次有限测量未建立池化等额外优化的必要性，没有引入池化或全局图片缓存。

## 复现入口

在仓库根目录执行；核心/DI/Redis 单元项目不指定 `-f` 时运行三个框架。

```powershell
dotnet test tests/Bing.Utils.Captcha.Tests/Bing.Utils.Captcha.Tests.csproj -c Release
dotnet test tests/Bing.Utils.Captcha.DependencyInjection.Tests/Bing.Utils.Captcha.DependencyInjection.Tests.csproj -c Release
dotnet test tests/Bing.Utils.Captcha.Redis.Tests/Bing.Utils.Captcha.Redis.Tests.csproj -c Release
dotnet test tests/Bing.Utils.Captcha.Tests.Integration/Bing.Utils.Captcha.Tests.Integration.csproj -c Release
$env:BING_CAPTCHA_TEST_FONT = 'C:\Windows\Fonts\msyh.ttc'
$env:BING_CAPTCHA_TEST_LATIN_FONT = 'C:\Windows\Fonts\arial.ttf'
dotnet test tests/Bing.Utils.Captcha.SkiaSharp.Tests.Integration/Bing.Utils.Captcha.SkiaSharp.Tests.Integration.csproj -c Release
# 由宿主提供隔离测试服务的 BING_CAPTCHA_REDIS 后执行；不在文档中保存凭据。
dotnet test tests/Bing.Utils.Captcha.Redis.Tests.Integration/Bing.Utils.Captcha.Redis.Tests.Integration.csproj -c Release
dotnet build Bing.Utils.sln -c Release -m:1
dotnet run --project benchmarks/CaptchaProbe/CaptchaProbe.csproj -c Release
dotnet run --project benchmarks/CaptchaProbe/CaptchaProbe.csproj -c Release -- --capacity
node --test samples/CaptchaWeb/tests/captcha-page.test.cjs
git diff --check
```

```powershell
foreach ($name in @('Security','Drawing.Shared','Drawing.SkiaSharp','Captcha','Captcha.SkiaSharp','Captcha.Redis','Captcha.DependencyInjection')) {
    dotnet pack "src/Bing.Utils.$name/Bing.Utils.$name.csproj" -c Release -m:1 -o output/captcha-packages
    if ($LASTEXITCODE -ne 0) { throw "打包失败：$name" }
}
dotnet restore ai_docs/tasks/2026-10-28-captcha-lifecycle/consumer/Consumer.csproj --packages output/captcha-consumer-cache
dotnet build ai_docs/tasks/2026-10-28-captcha-lifecycle/consumer/Consumer.csproj -c Release --no-restore
dotnet run --project ai_docs/tasks/2026-10-28-captcha-lifecycle/consumer/Consumer.csproj -c Release -f net8.0 --no-build
```

本地详细构建/打包/探针日志位于忽略目录 output，结论与可复现命令保存在本文件。测试服务仅清理其唯一前缀下已知键，未访问业务 Redis、未清空数据库。
