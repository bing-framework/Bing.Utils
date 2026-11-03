# TOTP/HOTP 实施计划

2026-10-29 用户批准全量执行：TOTP/HOTP 算法、绑定确认、验证、撤销、重同步、SQLite 权威状态、Redis 限流、Data Protection、DI、Web 示例及验收。

## 冻结范围
五包采用 net8/net7/net6/netstandard2.0，命名空间 Bing.OneTimePassword。默认 SHA1/6 位、TOTP 30 秒且前后各一步；HOTP 前瞻 5，重同步连续两码最多 100 偏移。全凭据 TOTP 防重放，HOTP 不回绕。

SQLite 原子提交凭据、验证事务及账号失败状态；Data Protection 绑定应用/账号/凭据/算法。Redis 仅限流，不存权威 OTP 状态。5 个有效凭据、1 个待确认；绑定 10 分钟，事务 5 分钟；15 分钟 5 次失败锁定 15 分钟。

包依赖 Sqlite/DataProtection.Abstractions 8.0.33、Redis 2.13.1、DI Abstractions 8.0.2。恢复码、Passkey、云 KMS 和真实账号系统后续建设。不提交、不发布、不改发行版本；保留现有验证码/OCR 工作。

## TODO
- [x] T1 项目骨架、契约、方案修订。
- [x] T2 HOTP/TOTP、Base32、URI、RFC 向量。
- [x] T3 生命周期、密钥保护和测试存储。
- [x] T4 SQLite 原子存储和持久化测试。
- [x] T5 Redis 限流、DI 和故障测试。
- [x] T6 Web 示例和模块文档。
- [x] T7 多框架测试、打包、消费者、最终审查。

## 验证
按 test-project-boundary 放置单元与 Integration 测试。先输出影响分析，局部→模块→集成→最终 solution Release -m:1。验证 RFC4226/6238、跨账号/会话/用途、重放/并发/撤销/锁定、时间/计数器边界、重启、回滚、密文置换和Web安全。未运行的Redis/远端CI单独记录。仅 OPEN_ACTIONABLE 进入修复，遵循 GOAL_RULES.md 证据复用及停止规则。

## 执行结果

[验收记录](execution.md)：T1–T7 全部完成，OPEN_ACTIONABLE=0。

## 本地 Redis 补验

用户要求使用本地 Redis 完成验证，远端 CI 暂不纳入验收范围。

- [x] 临时 Redis 8.6 容器验证真实并发、隔离和过期。
- [x] TOTP/HOTP 绑定后断开测试 TCP 通道，验证 503、状态未消费及未签发 MFA 会话。
- [x] Windows Web 11 项、Redis 3 项与本机 Linux Web 11 项通过；清理临时容器并更新证据。

## 专项边界测试补齐

- [x] HOTP 重同步偏移 100/101、歧义拒绝与计数器耗尽边界。
- [x] TOTP 真实窗口碰撞、最大可表示时间及 Base32 全部非零尾位类型。
- [x] 精准测试、核心三框架回归、HOTP 相关 SQLite/Web 验证与证据更新。

影响范围：核心测试、HOTP 重同步内部候选选择函数及测试友元声明；无公开契约或依赖版本变化。先运行新增用例，再按内部重构影响验证生命周期调用链；不重跑无关性能基线或远端 CI。
