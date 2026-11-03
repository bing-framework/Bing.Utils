# 验证码 Web 示例

运行目标为 .NET 8。该示例演示受保护的一次性提交，不提供账号认证。

用环境变量 `Captcha__Key` 配置至少 32 字节的 Base64 密钥。开发时可由密码学随机源生成；生产从密钥管理服务或宿主配置获取，不能使用测试密钥。

```powershell
$captchaKey = [byte[]]::new(32)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($captchaKey)
$env:Captcha__Key = [Convert]::ToBase64String($captchaKey)
dotnet run --project samples/CaptchaWeb --urls https://localhost:7184
```

使用受信任的 ASP.NET Core 开发证书访问页面。Cookie 设为 Secure，因此只通过 HTTPS 使用。生产轮换不应每次启动生成新密钥；应使用包含新旧版本的密钥环。

Linux 宿主需另行引用与 SkiaSharp 版本一致的 `SkiaSharp.NativeAssets.Linux` 2.88.9，并安装可用字体。中文模式应同时配置核心与渲染器的 ChineseChar 类型和显式字体路径。

- GET /api/session 初始化受保护匿名 Cookie 和防伪令牌。
- POST /api/captchas 接收 flowId，返回 ID、PNG Base64 和过期时间。
- POST /api/demo/submit 接收 flowId/captchaId/answer，校验成功后完成示例操作。

两种 POST 都必须带 X-CSRF-TOKEN。响应禁止缓存，失败后需要刷新。每 IP 每分钟 60 次、每会话每分钟 20 次是可调整的示例设置；计数包含静态资源和会话初始化。使用进程内限流和内存存储，多实例应接入 Redis 与网关/宿主分布式限流。反向代理部署需显式配置受信任代理，不能直接信任任意转发头。

前端等待会话初始化完成后再生成图片，初始化或生成失败后可以手动重试。初始化、刷新和提交期间禁用重复操作；刷新清除旧答案、图片和有效期。每个页面独立创建流程 ID；业务端固定用途，不接受客户端指定用途或绑定身份。

页面时序回归使用 Node.js 内置测试器，执行真实页面脚本并控制网络返回顺序；覆盖慢会话、初始化失败后重试、串行刷新和错误提交后重新生成。Node.js 仅用于测试，运行示例不需要它。

```powershell
node --test samples/CaptchaWeb/tests/captcha-page.test.cjs
dotnet test tests/Bing.Utils.Captcha.Tests.Integration/Bing.Utils.Captcha.Tests.Integration.csproj -c Release
```

HTTP 集成测试另行验证 Cookie/流程隔离、防伪、会话与共享 IP 限流、错误响应 no-store，以及渲染/存储故障返回 503 并拒绝业务操作。
