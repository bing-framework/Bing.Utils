# Emoji 首版实施与验证记录

- Task：Bing.Utils.Extra Emoji；Round：1；日期：2026-09-27。
- Provider/model：Codex / GPT-6；独立测试子任务由 Luna implementer 完成，主代理集成、加强断言并验证。
- Candidate：基于 `f7ef727` 的本任务未提交工作区变更；无版本修改、提交或发布。
- Implementation：5/5（数据、识别内核、公开接口、测试与文档、收口验证）。
- Required local gates：7/7；Implementation status：PASS；阶段状态：实现及计划内本地验证完成。

## 变更影响分析

| 项目 | 范围 |
| --- | --- |
| ChangedFiles | Extra Emoji 源码、原始数据及生成器、专属测试、包许可配置、文档及导航 |
| ChangedProjects | Bing.Utils.Extra、Bing.Utils.Extra.Tests |
| ChangedPublicContracts | 新增 EmojiUtil、EmojiInfo、EmojiMatch，无已有 API 改动 |
| ChangedRuntimePaths | Extra 内置数据惰性初始化、序列匹配、别名转换 |
| ChangedProviders | 无 |
| ChangedTFMs | 无；保留 netstandard2.0、net6.0、net7.0、net8.0 及原测试目标 |
| ChangedBuildPackaging | 新增生成 C# 源码及包内两份许可、数据来源说明；无新增包依赖 |
| ChangedBenchmarkHarness | 无 |
| ChangedDocsOnly | 否 |
| AffectedDependents | Extra 专属测试与包消费者；核心库没有反向依赖 |
| RiskLevel | MEDIUM：新增公开文本解析能力与数据表 |

## 验证证据

| Gate | 结果 |
| --- | --- |
| 1. 数据可重复生成 | `python -X utf8 build/generate-emoji-data.py --check` 通过；3,963 规范表情、5,235 序列、1,913 别名 |
| 2. 四框架 Release 构建 | netstandard2.0 / net6.0 / net7.0 / net8.0 全通过，0 警告、0 错误 |
| 3. net8.0 模块完整测试 | 23 通过、0 失败、0 跳过 |
| 4. net7.0 模块完整测试 | 23 通过、0 失败、0 跳过 |
| 5. net6.0 模块完整测试 | 23 通过、0 失败、0 跳过 |
| 6. 独立进程首次并发调用 | 单独筛选 FirstUse_ConcurrentCalls，1 通过；64 个任务验证一致性，未宣称 64 路实际并行 |
| 7. 打包与消费者 | nupkg / snupkg 生成；检查四框架 DLL、两份许可及来源说明；从 nupkg 提取 netstandard2.0 DLL 后由 net8.0 消费者验证计数和别名转换通过 |

L0：构建、生成结果、UTF-8/差异检查及文档配置静态检查通过。
L1：首次并发测试通过。L2：三个可用运行时上的模块完整测试通过。
L3：包内容与 netstandard2.0 消费者验证通过。
L4/L5：按批准计划未运行全解决方案测试或重型性能测试。

测试逐条验证官方三类 qualification 的识别、Count=1、匹配原文长度和移除；逐项验证 gemoji 别名对应正确表情和首选别名输出。
独立手写场景覆盖旗帜、肤色、家庭/职业 ZWJ、键帽、标签旗帜、UTF-16 位置、未知别名、大小写、文本样式、独立组件、孤立代理项、只读集合和回调行为。

### 实际使用的命令

默认还原遇到 NuGet HTTPS 的 NU1301 / TLS 凭证错误。使用本机已有固定版本缓存完成离线还原，未更改仓库 NuGet 源或依赖版本。
本次离线还原禁用联网漏洞查询；此结果不代表完成在线漏洞审计。

```powershell
dotnet restore src/Bing.Utils.Extra/Bing.Utils.Extra.csproj --source C:\Users\jianx\.nuget\packages --packages C:\Users\jianx\.nuget\packages -p:NuGetAudit=false
dotnet restore tests/Bing.Utils.Extra.Tests/Bing.Utils.Extra.Tests.csproj --source C:\Users\jianx\.nuget\packages --packages C:\Users\jianx\.nuget\packages -p:NuGetAudit=false
dotnet build src/Bing.Utils.Extra/Bing.Utils.Extra.csproj -c Release --no-restore
dotnet test tests/Bing.Utils.Extra.Tests/Bing.Utils.Extra.Tests.csproj -c Release -f net8.0 --no-restore --filter FullyQualifiedName~FirstUse_ConcurrentCalls
dotnet test tests/Bing.Utils.Extra.Tests/Bing.Utils.Extra.Tests.csproj -c Release -f net8.0 --no-restore
dotnet test tests/Bing.Utils.Extra.Tests/Bing.Utils.Extra.Tests.csproj -c Release -f net7.0 --no-restore
dotnet test tests/Bing.Utils.Extra.Tests/Bing.Utils.Extra.Tests.csproj -c Release -f net6.0 --no-restore
dotnet pack src/Bing.Utils.Extra/Bing.Utils.Extra.csproj -c Release --no-build --no-restore -o output/emoji-packages
python -X utf8 build/generate-emoji-data.py --check
git diff --check
```

包路径：`output/emoji-packages/Bing.Utils.Extra.1.5.0.nupkg`；消费者临时项目位于忽略目录 `output/release/emoji-consumer`。
打包只提示缺少包自述文件（非失败项）；已通过模块文档提供说明，未扩展包元数据范围。

## 当前 TODO / Finding 分类

- Completed：批准的首版接口、数据、测试、文档与包验证全部完成。
- Open Actionable：无。
- Blocked Approval：无。
- Blocked External：无计划内必需门禁阻塞；最初联网还原问题已通过本地缓存绕过。
- Not Applicable：新功能没有同语义历史性能基线；未设置性能门槛。
- Accepted Limitations：本机缺少 .NET 5 / Core 3.1，对应运行时测试未验证；未构建 DocFX 站点；字形显示由字体决定；未知组合允许已知子序列匹配；没有别名的表情保留原文。
- Verified Boundaries：无资源/容量边界实验。
- Deferred：HTML 实体、中文检索、标签查询、肤色变体管理、旧 API 迁移。
- Reused Evidence：测试加强后生产源码与包配置未变化，复用四框架构建、打包和消费者结果；只补文档时不重跑代码测试。
- No-Progress Check：CHANGED；不存在自动 Review/Fix 循环。
- Next Action：STOP；当前阶段没有剩余可执行项。

未执行独立代码审查阶段；本记录包含主代理最终差异检查及本地验证结果，不代替外部 CI 或发布验证。
