# 实现检查

## 范围与结论

检查新增 Operations、Diagnostics、Tabular 模块和 StreamTransfer/CsvStream/TabularDataReader 的兼容性扩展，不包含任务开始前已有的文件检测、命令行和 MapperHelper 改动。

- 主代理检查流进度阶段、预算、资源所有权和取消路径。
- `luna_worker` 独立只读检查 TabularMap、CsvRow 与 CsvFormatException 适配；未发现与当前计划相关的 OPEN_ACTIONABLE。
- 检查确认：配置冻结、按结构缓存、读取值前检测歧义、可选列不赋值、值类型按同一装箱实例写入、取消原样传播、失败对象不返回、字段问题补入实际源坐标。

## 已关闭问题

| 状态 | 问题 | 修复与验证 |
|---|---|---|
| CLOSED | 值类型目标的校验上下文使用不同装箱实例 | 同一次校验复用一个对象；结构体属性/字段映射测试通过 |
| CLOSED | 资源预算被误用为 Total，或普通流写入误报 Commit | Total 保持未知，删除虚假的提交事件；读写进度测试通过 |
| CLOSED | 新数据库进度重载造成 default 字面量源兼容歧义 | 使用独立 ReadRowsWithProgressAsync，原签名保持；原签名反射与调用测试通过 |
| CLOSED | 已有取消被流释放异常覆盖 | 保留处理异常，同时尝试释放已接管资源；新增同步/异步及 CSV 回归测试通过 |
| CLOSED | 超限探测字节未体现在读取进度中 | 读取进度计入真实探测字节，但不写入超限内容；预算探测回归测试通过 |

## 当前分类

- Open Actionable：无已知实现缺陷。
- Blocked Approval：无。
- Blocked External：无已识别的实现阻塞。
- Not Applicable：新 API 没有历史等价性能基线；未声称性能提升。
- Accepted Limitations：源回调必须合作取消；失败不能撤销用户构造函数或 setter 的外部副作用；进度最终事件仅覆盖当前阶段。
- Verified Boundaries：读写预算和真实读写计数边界有单元测试。
- Deferred：Excel 读取、嵌套映射、Source Generator、读取器错误策略、进度节流均按批准计划排除。

最终构建和全量测试状态记录在 execution.md，不用静态检查结论替代测试结果。

## 全量回归失败的范围核对

`luna_worker` 对未通过的正则路径进行了只读核对：`Regexs.Timeout.cs` 调用 `RegexPool`，缓存和非缓存分支均把显式超时传递给 Regex；缓存项也按超时区分。相关源文件及失败测试不在本次差异中，与新增流处理/映射能力没有可见调用关联。

net5.0 的三次“未抛出 RegexMatchTimeoutException”失败在单独复验中仍可复现；未进一步确认运行时未超时的具体原因，按 DEFERRED 保留，后续处理正则模块时调查。HTTP/命令行的八次失败单独复验通过，不能据此将原始全量运行改记为通过。
