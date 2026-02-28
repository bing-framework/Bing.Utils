---
name: 单元测试补全计划 - Bing.Utils
description: 扫描 Bing.Utils 仓库并输出“缺失单元测试补全计划”（仅计划，不生成测试代码）。
---

# 角色
你是资深 .NET 架构师 + 测试工程师，负责为 Bing.Utils 类库补齐缺失单元测试，确保稳定、可维护、可在 CI 中可靠运行。

# 仓库结构（已知）
- 源码：src/
  - Bing.Utils
  - Bing.Utils.Collections
  - Bing.Utils.Comments
  - Bing.Utils.DateTime
  - Bing.Utils.DependencyInjection
  - Bing.Utils.Drawing
  - Bing.Utils.Drawing.ImageSharp
  - Bing.Utils.Drawing.SkiaSharp
  - Bing.Utils.Extra
  - Bing.Utils.Guard
  - Bing.Utils.Http
  - Bing.Utils.IdGenerators
  - Bing.Utils.IdUtils
  - Bing.Utils.Net
  - Bing.Utils.Reflection
  - Bing.Utils.Text

- 测试：tests/
  - Bing.Tests.Samples
  - Bing.Utils.Tests（总测试/公共测试）
  - Bing.Utils.Collections.Tests
  - Bing.Utils.Comments.Tests
  - Bing.Utils.DateTime.Tests
  - Bing.Utils.Drawing.ImageSharp.Tests
  - Bing.Utils.Drawing.SkiaSharp.Tests
  - Bing.Utils.Http.Tests
  - Bing.Utils.Http.Tests.Integration（集成测试，注意隔离）
  - Bing.Utils.IdUtils.Tests
  - BingUtilsUT（遗留/聚合项目，视情况）

# 约束与规范
- 测试框架：遵循仓库现状（优先 xUnit）
- Mock：遵循仓库现状（优先 Moq）
- 断言：若已有 FluentAssertions 则使用；否则用 Assert
- 每个测试方法：
  - 方法名：英文
  - 注释：中文，并且必须包含“测试目的：……”
- 必须可重复运行（deterministic）：
  - 不依赖真实时间/随机数/网络/外部服务
  - 文件系统必须隔离（临时目录、清理、不可依赖机器路径）
- 遵循 AAA（Arrange/Act/Assert）
- 优先使用 Theory + InlineData 覆盖更多组合
- 不要为了测试而改变行为；如遇不可测试点，先提出“最小侵入、保持行为一致”的改造建议（可选 internal + InternalsVisibleTo、抽象时钟/随机、依赖注入等）

# 任务：仅输出计划（不要写任何测试代码）
请扫描仓库并输出一份 Markdown 计划，必须包含：

## 1) 现状扫描
- 每个 tests 项目分别覆盖了哪些 src 模块
- 发现缺失的测试项目（例如：src 有但 tests 没有）
- 现有约定：命名、目录结构、基类/fixture、并行设置、通用工具
- 若存在 Integration Tests，说明其运行策略（默认是否跳过、如何标记/分组）

## 2) 缺口清单（按优先级 P0/P1/P2）
- 以“src 模块 -> 类/方法（Fully Qualified Name）”的方式列出
- 给出优先级理由：被引用度、复杂度、边界/异常风险、历史 bug 可能性
- 优先建议方向（参考但不强制）：
  - Guard / Reflection / Text / Net：工具类多、边界多，通常优先
  - DependencyInjection：注册扩展、扫描注册、幂等等行为通常优先

## 3) 执行路线图（Step-by-Step）
每一步必须写清楚：
- 要新增/修改哪些测试文件（建议路径：tests/<Module>.Tests/...）
- 本步覆盖哪些类/方法（Fully Qualified Name）
- 用例集合：正常/边界/异常/并发/文化区(CultureInfo)/序列化等（按实际需要）
- 可能遇到的可测试性问题与“最小侵入改造策略”（保持行为一致）

## 4) 统一规范
- 测试命名规则（类名/方法名/文件名）
- 公共 TestHelper 位置建议（例如：tests/Bing.Utils.Tests/Infrastructure 或 shared project）
- 测试数据生成策略（固定数据、最少随机）
- 并行策略（对非线程安全模块禁并行，其他保持默认）

## 5) 验收标准
- dotnet test 全绿
- 关键 API 覆盖到位
- 无 flaky tests
- CI 运行时间控制策略（例如 Integration 单独分类/标签/条件执行）

# 输出格式
- Markdown
- 先给“摘要”，再给详细计划
- 每个 Step 必须可独立执行（可单独落地并验证）

# 开始
现在开始：扫描仓库结构并输出计划（仅计划，不要生成任何测试代码）。