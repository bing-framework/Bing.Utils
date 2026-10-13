# NativeAOT 基线差异复核（2026-10-13）

状态：已批准。用户于 2026-10-13 明确接受 [诊断提案](reflection-nativeaot-diagnostic-proposal.json) 中的 24 个精确探针指纹；新版基线和责任清单已作为工作流输入。

## 已修复的问题

初次发布相对历史基线新增 30 个仓库指纹。生产源码中的 6 个来自契约传播缺失，现已修复：

- `Reflections.MemberPath.cs`：显式根类型的内部读写入口补传裁剪契约，两条 IL2026 消除。
- `Reflections.Assembly.cs`：`GetAssemblies` 声明动态加载程序集及依赖的元数据保留要求，一条 IL2026 消除。
- `StrictConstructorBinder.cs`：可回收构造分支补传 RequiresDynamicCode、RequiresUnreferencedCode 和公共构造函数 DAM，一条 IL2067、一条 IL2026 和一条 IL3050 消除。

核心反射 net8.0 378/378、netstandard2.0 编译、新鲜 NativeAOT 发布及原生元数据 smoke 均通过；没有新增无条件 suppression。

## 批准前的差异

配置为 `sdk=8.0.425;tfm=net8.0;rid=win-x64;trim=partial;aot=true;mode=ci-single-publish`。日志共 260 条诊断，244 条仓库源，218 个仓库指纹；新增 ID 为 0，未解析为 0，非仓库责任门禁通过。相对历史基线仍有 24 个新增仓库指纹，全部属于 `benchmarks/ReflectionTrimProbe/Program.cs`：15 条 IL2026、9 条 IL3050。仓库指纹与责任 allowlist 严格门禁失败，远程 CI 待确认。

这些警告来自探针调用 `CreateInstance/CreateInstances/CreateGenericInstance`、`GetBaseMethod/GetMethodBySignature`、`SetFieldValue<T>`、`Types.IsDefaultValue` 与 `Type.MakeGenericType`。它们明确提示元数据或动态代码限制；探针运行成功不表示限制不存在。

## 已批准的处理结果

1. 保留现有 339 项、405 次诊断的历史基线及责任清单。
2. 创建 `benchmarks/ReflectionTrimProbe/NativeAotRepositoryDiagnosticBaseline.20261013.json` 和 `NativeAotRepositoryDiagnosticAllowlist.20261013.json`，保留全部历史项并加入批准的 24 项；每个新增项的 `maxCount=1`，新版为 363 项、429 次出现上限。提案 JSON 记录批准状态、完整指纹、诊断消息、责任人和理由。
3. 工作流默认输入和上传清单改为新版文件；保留未解析、ID、指纹、仓库责任、非仓库责任五项严格门禁。
4. 使用已有发布日志和源码映射复核，新版五项门禁全部通过：260 条诊断、244 条仓库源、218 个仓库指纹；新增 ID、新增仓库指纹、未批准仓库诊断、未归属非仓库诊断和未解析诊断均为 0。历史基线没有被覆盖；本次没有重新发布探针。远程 CI 仍待确认。

工作流本身另已修复两个 PowerShell-only 脚本的返回状态误判和四处多行条件语法错误，85 个多行 `run` 块静态解析通过。此结果不代替实际远程 CI。

本次验证日志与源码映射保存在忽略的 `benchmarks/ReflectionTrimProbe/bin/commit-fix-*-20261013.*`，批准后复核报告为 `benchmarks/ReflectionTrimProbe/bin/approved-warning-contracts-report-20261013.json`。提交文档保留可审阅的差异数据，不依赖这些本机产物作为可移植发布证据。
