# 验证码审查修复独立复核

复核范围：仅 R1～R5，本轮实现和测试。只读查看代码，未运行构建或测试；测试证据来自主代理报告。

| ID | 状态 | 结论 |
| --- | --- | --- |
| R1 | CLOSED | 页面在会话令牌成功后才发起验证码 POST；刷新和提交期间锁住重复操作，失败后可恢复。页面脚本测试执行实际 HTML 中的脚本，覆盖慢初始化、失败重试、刷新串行及提交后刷新。 |
| R2 | CLOSED | 核心构造服务时调用可选渲染器校验器；SkiaSharp 对生成字符集做精确覆盖校验，接受渲染字符集超集。集成测试覆盖不兼容配置在首次请求前失败和兼容配置实际生成。未实现该可选接口的自定义渲染器由宿主负责兼容性，接口文档已说明。 |
| R3 | CLOSED | Redis 单测覆盖写故障且不重试、消费派发后取消与晚到成功结果、写入派发后取消时观察晚到故障；专属集成测试覆盖本地连接故障。主代理报告单测 net6/net7/net8 各 6/6，连接故障集成 1 项通过。 |
| R4 | CLOSED | Web 测试覆盖创建和校验两种存储故障均返回 503/no-store、共享 IP 跨会话限流、错误答案刷新流程；主代理报告 Web 集成 6/6、页面脚本 3/3。 |
| R5 | CLOSED | 新探针覆盖 10000 条满容量、未过期淘汰和过期优先清理两类场景，并记录每次延迟、分配量及最终记录数。结果未显示当前要求下需要新增算法改动。 |

## 剩余边界

本机健康 Redis 集成的 10 项测试因未配置服务而跳过；真实 Redis TTL、两个独立客户端并发消费和服务端序列化仍为 `BLOCKED_EXTERNAL`，不能依据本地连接故障用例推断已通过。远端 CI 也未在此复核。

容量探针为 net8 Release 单线程短测量，未建立批准的性能阈值，也未测并发锁竞争。报告值为满容量插入 100 次：原有记录未过期时总计 24.548 ms、均值 0.240 ms、p95 0.340 ms、最大 5.072 ms；记录均过期时总计 1.425 ms、均值 0.014 ms、p95 0.005 ms、最大 0.991 ms。两种情况下记录数均未超过 10000。将性能证据标记为 `MEASURED`；如需发布性能门禁，预算仍需另行批准（`BLOCKED_APPROVAL`）。

## 代码位置

- R1：`samples/CaptchaWeb/wwwroot/index.html`、`samples/CaptchaWeb/tests/captcha-page.test.cjs`
- R2：`src/Bing.Utils.Captcha/Contracts.cs`、`src/Bing.Utils.Captcha/CaptchaService.cs`、`src/Bing.Utils.Captcha.SkiaSharp/SkiaSharpCaptchaRenderer.cs`、`tests/Bing.Utils.Captcha.SkiaSharp.Tests.Integration/ConfigurationIntegrationTests.cs`
- R3：`tests/Bing.Utils.Captcha.Redis.Tests/CaptchaRedisStoreTests.cs`、`tests/Bing.Utils.Captcha.Redis.Tests.Integration/CaptchaRedisStoreIntegrationTest.cs`
- R4：`tests/Bing.Utils.Captcha.Tests.Integration/WebTests.cs`
- R5：`benchmarks/CaptchaProbe/CapacityProbe.cs`、`src/Bing.Utils.Captcha/MemoryCaptchaStore.cs`

复核分类：`OPEN_ACTIONABLE=0`；未发现需要再开启自动修复的问题。
