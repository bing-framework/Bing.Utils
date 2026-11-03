# 验证码生命周期实施计划

执行日期：2026-10-29。依据本会话用户确认的 T1～T7 完整计划。

## 已冻结契约

四个包：Captcha、Captcha.SkiaSharp、Captcha.Redis、Captcha.DependencyInjection；四目标框架 net8/net7/net6/netstandard2.0。默认五位去歧义字符、两分钟绝对有效期、一次性消费、用途/会话/流程绑定。支持数字和中文。核心独立时钟，HMAC-SHA256 和带长度 UTF-8 字段。Redis 原子替换和比较消费，网络派发后不保证回滚。

## 实施清单

- [x] T1 工程骨架及契约文档。
- [x] T2 核心生命周期、密钥轮换、规范化、指标。
- [x] T3 有界内存、原子操作、过期清理。
- [x] T4 SkiaSharp 字体重载与中文覆盖验证。
- [x] T5 Redis 实现、真实集成测试代码、隔离 CI 服务配置。
- [x] T6 DI 与可运行 Web 示例、防伪和限流。
- [x] T7 本机多框架测试、打包消费者、性能探针和文档。
- [ ] 外部验收：真实 Redis 与远端 CI 运行证据（BLOCKED_EXTERNAL）。

## 验证与边界

先定向核心测试，再扩大到图像/Web/Redis。核心单测运行 net8/net7/net6；四包构建全部目标框架。最终 solution Release net8 构建、包消费者验证。仅文档变化复用代码证据。缺少 Redis 或中文字体时明确记录外部缺口；配置存在但不可用必须失败。没有 OPEN_ACTIONABLE 不进入额外修复循环。

旧 System.Drawing/ImageSharp 全面改造、交互验证码和跨区域一致性不在范围。性能只报告测量，不设置发布阈值。不暂存、提交、发布或变更发行版本。

最终验证与外部缺口见 [执行记录](execution.md)。
