# OTP Web 示例

net8.0 示例覆盖 TOTP/HOTP 绑定、确认、验证、设备列表、撤销和重同步。SQLite 权威状态，Data Protection 密钥保护，默认显式 Memory 限流，可选 Redis。

## 运行

```powershell
dotnet dev-certs https --trust
dotnet run --project samples/OtpWeb --launch-profile OtpWeb
```

打开 https://localhost:7248，建立开发演示身份，生成密钥后录入认证器并确认。首码已消费：TOTP 等下一周期，HOTP 生成下一码，再完成二次认证，随后可查看受保护内容。

演示账号由服务端固定建立，只在 Development 注册。重新建立身份会更换会话绑定，旧事务不可用；管理操作要求 5 分钟内主认证。超过分钟限流需等待。页面不加载外部资源、不生成二维码、不打印密钥和 URI。

## 数据与密钥

默认 samples/OtpWeb/App_Data 保存 otp.db 和 keys，已加入示例 .gitignore。用 Otp__DataDirectory 指定受限本地目录，每应用独占数据库。

Windows 默认当前身份 DPAPI 保护 key ring。Linux Development 仅用于本地演示；生产配置 Otp__KeyCertificateFile 和按需的 Otp__KeyCertificatePassword，证书秘密由宿主管理。备份需保留数据库、密钥环、保护证书/平台身份，轮换后保留旧 key。

```powershell
$env:Otp__RateLimiter = 'Redis'
$env:Otp__Redis = '127.0.0.1:6379,abortConnect=true'
dotnet run --project samples/OtpWeb --launch-profile OtpWeb
```

连接由容器释放；故障 503，无内存回退。Memory 只用于单实例，SQLite 失败锁定仍持久化。宿主可定时调用 IOtpStore.CleanupAsync 有界清理。

## 生产接入

生产不注册演示登录，宿主须先完成真实主认证，再签发可信账号、会话和 primary_at 的受保护票据，或替换认证方案及 OtpWebSupport.Context。受保护业务使用 mfa 策略，不能只检查 IsAuthenticated。

绑定/撤销/重同步仍需宿主按风险定义近期认证策略。示例演示 5 分钟主认证年龄，真实系统可要求密码重输、已有 MFA 或独立恢复流程。请求模型不接受 SubjectId、Purpose、BindingId 或认证时间。

HTTPS、Secure/HttpOnly/SameSiteStrict Cookie、变更请求 X-CSRF-TOKEN、防缓存 no-store 已启用。GET /api/session 获取与当前身份绑定的防伪令牌，登录/退出后重新获取。反向代理部署时仅信任已配置代理，正确恢复 HTTPS 和源 IP。

普通验证失败 otp_invalid，限流 429，存储/保护故障 503。只有 OTP 持久提交成功才签发 auth_level=mfa；响应丢失时新建事务并使用新码，不自动重放。禁止记录验证码请求体或注册响应。

真实 HTTP 测试在 tests/Bing.Utils.OneTimePassword.Tests.Integration。详见 [核心模块](../../docs/modules/Bing.Utils.OneTimePassword.md) 和 [执行记录](../../ai_docs/tasks/2026-10-29-otp-lifecycle/execution.md)。

设置 `BING_OTP_REDIS` 后，同一测试项目会运行 TOTP/HOTP 的真实 Redis 断连用例。测试连接单节点 Redis，通过本地 TCP 转发器完成绑定后关闭通道，断言返回 `503 otp_unavailable`，验证码与验证事务未消费、失败计数未增加、MFA 会话未签发。每个用例使用独立限流键前缀；不会关闭 Redis 服务或修改其他客户端的数据。未配置时明确跳过，已配置但不可达时失败。

```powershell
$env:BING_OTP_REDIS = '127.0.0.1:6379,abortConnect=true'
dotnet test tests/Bing.Utils.OneTimePassword.Tests.Integration -c Release
```
