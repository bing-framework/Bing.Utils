# 验证码生命周期

四个独立包提供图片验证码生成、原子存储和一次性消费。基础包依赖 Security 与 Drawing.Shared；SkiaSharp、Redis、DependencyInjection 按需安装。目标框架为 net8.0、net7.0、net6.0、netstandard2.0。

## 契约与默认值

`CaptchaService.CreateAsync(context)` 返回 ID、PNG 图片及 UTC 过期时间；`VerifyAsync(context, id, answer)` 返回 Success、Mismatch 或 InvalidOrExpired。默认值为失效。配置错误抛参数异常，渲染和存储故障分别抛 CaptchaRendererException、CaptchaStoreException，宿主映射为 503。禁止异常时放行业务。

默认 5 位去歧义大写字母数字，可选择 Number、ChineseChar；长度 4～8，有效期 120 秒（可配置 30～300），默认 PNG 160×60、最大 512×256、编码上限 64 KiB。AllowedPurposes 必须显式列出允许的业务用途，同时限制指标标签基数。

Purpose 由业务端固定；BindingId 必须来自可信会话；FlowId 区分表单，不能作为限流身份。记录经过 HMAC-SHA256 保护，认证字段包括上下文、随机 ID、毫秒级过期时间、规范化协议及密钥版本。字段以长度前缀 UTF-8 编码。答案采用 FormKC、Trim 和可配置 ASCII 大小写规则，规则随记录保存。

匹配当前 ID 的一次提交即消费，错误答案也消费。过期点本身已失效。格式错误和错 ID 不消费当前挑战。刷新按存储提交顺序替换当前记录；并发提交最多一个成功。业务后续失败也需要重新生成验证码。

## 接入

```csharp
using Bing.Captcha;
using Bing.Captcha.SkiaSharp;
using Microsoft.Extensions.DependencyInjection;

// keyBytes 来自宿主密钥配置，至少 32 字节，不写入源码。
services.AddSingleton<ICaptchaKeyProvider>(
    new StaticCaptchaKeyProvider("v1", new[] { new CaptchaKey("v1", keyBytes) }));
services.AddBingCaptcha(new CaptchaServiceOptions { AllowedPurposes = new[] { "login" }, BackendName = "memory" });
services.AddMemoryCaptchaStore();
services.AddCaptchaRenderer(_ => new SkiaSharpCaptchaRenderer());
```

启动时解析 ICaptchaService 验证所有组件。DI 工厂创建的 IDisposable 由容器释放；直接构造的存储和渲染器由调用方释放。CaptchaService 本身不释放注入资源。图片字节及摘要构造和访问均复制，不能把池化缓冲区暴露给调用方。

支持字符集检查的渲染器实现可选 `ICaptchaRendererValidator`。服务构造时传入完整生成字符集，SkiaSharp 会拒绝不兼容的 Type 或大小写组合；渲染器字符集可为生成字符集的超集。受限的自定义渲染器应实现该接口，未实现时由宿主保证兼容。

中文服务配置 Type=ChineseChar，同时渲染器配置同类型和 FontPath。初始化验证字体覆盖完整共享白名单；字体由调用方合法提供，不随包捆绑。固定字体载入后复用，每次请求的画布/画笔独立。新渲染器拒绝不生效的倾斜、间距设置及外部固定随机种子；原绘图 API 保持兼容。

## 存储及部署

MemoryCaptchaStore 默认最多 10000 条记录，过期优先清理，满容量淘汰最旧项。短锁保证原子替换和消费；后台分批清理，访问时也检查过期。只适用于同一实例，不作为 Redis 故障回退。

Redis 适配器命名空间为 Bing.Captcha.Redis，入口为 CaptchaRedisStore。传入共享 IConnectionMultiplexer 及按应用/环境隔离的 keyPrefix。只使用单键原子写入和 Lua 消费。记录采用有界、带版本的二进制格式，不保存图片和明文答案。需要支持绝对过期写入的 Redis 服务器。适配器不释放宿主连接，不进行应用级写入重试。

取消在派发前检查；网络命令派发后，超时或取消不能保证回滚，挑战可能已被消费。应刷新挑战，不重放请求。正常并发一次性保证不等价于 Redis 异步复制故障切换或数据回滚下的持久一致性。

密钥轮换时部署含新旧密钥的 StaticCaptchaKeyProvider，旧密钥至少保留最后一次发放后的最大有效期。多实例使用相同密钥配置、用途和规范化协议。命中未知密钥版本返回失效。

## Web 示例与可观测性

可运行入口见 [CaptchaWeb](../../samples/CaptchaWeb/README.md)。示例使用 HTTPS、受保护的匿名 Cookie、防伪令牌、按会话/IP 限流和 no-store。示例操作不等于真实账号认证；多实例由宿主提供分布式限流与共享 DataProtection 密钥。

Meter 名称为 Bing.Utils.Captcha。captcha.events 记录结果，captcha.duration 记录渲染/替换/消费毫秒数，captcha.image.bytes 记录编码大小。标签仅为配置过的 purpose、BackendName 和固定 result 类别，默认后端名 custom；宿主可设为 memory 或 redis。日志不输出答案、Cookie、原始绑定值或密钥。无需监控订阅者即可运行。

## 验证与优化

核心、DI 和 Redis 单元断言各在对应包的 Tests 项目，Web 协作放 Bing.Utils.Captcha.Tests.Integration，实际图像和 Redis 各有专属 Integration 项目。页面请求时序测试见示例 README。性能入口为 benchmarks/CaptchaProbe，默认进行 100 次请求、并发 1/8 的有限测量；传入 `--capacity` 测量默认 10000 条满容量后的 100 次插入，分别覆盖未过期淘汰和全部过期清理。不用作发布阈值。

当前优化包括批量拒绝采样、字体和字符集复用、有界状态、单次 Redis 原子消费。没有测量证据前不引入额外池化或缓存验证码图片。具体执行结果见 [本次记录](../../ai_docs/tasks/2026-10-28-captcha-lifecycle/execution.md) 及 [审查修复记录](../../ai_docs/tasks/2026-10-28-captcha-lifecycle/fix-review.md)。
