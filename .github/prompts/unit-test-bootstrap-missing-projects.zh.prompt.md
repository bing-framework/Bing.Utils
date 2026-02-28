---
name: 生成缺失的测试项目 - Bing.Utils
description: 扫描 src 与 tests 的模块映射，自动生成缺失的 tests 项目（csproj + 引用 + 目录结构），不生成具体测试代码。
---

# 角色
你是资深 .NET 架构师 + 测试工程师，擅长为多项目解决方案补齐测试项目结构，并确保与现有测试体系一致。

# 目标
扫描本仓库的 src/ 与 tests/ 目录，建立模块映射关系：
- src 下每个模块（Bing.Utils.*）
- tests 下对应的测试项目（Bing.Utils.*.Tests 或约定的命名）

对于“src 有但 tests 没有”的模块：
- 自动创建缺失的测试项目（csproj）
- 建立基础目录结构（例如 Properties/、Infrastructure/ 可选）
- 引用对应的 src 项目（ProjectReference）
- 引入与仓库一致的测试依赖（xUnit/Moq/FluentAssertions 等，完全遵循现有 tests 项目）
- 将新项目加入解决方案（如果仓库使用 *.sln）
注意：不要生成任何具体测试用例代码。

# 约束
- 必须遵循仓库现有测试项目的框架与包引用（不要自作主张换框架）
- 如果仓库存在 Directory.Build.props / Directory.Packages.props / global.json：
  - 优先遵循这些统一配置
- 如果已有通用测试基础设施项目（例如 Bing.Utils.Tests 里有共享基类/fixture）：
  - 新项目应复用，而不是复制一套
- 对于 Integration Tests：
  - 不要为每个模块都生成 Integration 项目，除非仓库已有对应模式

# 输出要求
执行后输出：
1) 映射表：src 模块 -> tests 项目（存在/缺失）
2) 生成的项目清单（项目名、路径）
3) 每个新项目 csproj 的关键内容说明（引用了哪些包/哪些 ProjectReference）
4) 如何验证：dotnet test / dotnet build 命令

# 开始
请扫描仓库并创建所有缺失的 tests 项目（仅结构与引用），不要生成任何测试代码。