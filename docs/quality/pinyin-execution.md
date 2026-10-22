# 拼音声调与词组读音执行记录

- 日期：2026-09-28；基于当前未提交工作区。
- 范围：新增带调和词组上下文拼音入口，原有 GBK 区码接口保持原行为。
- Change Impact：修改 Bing.Utils.Text 的数据生成脚本、资源、公开 API、专属测试及模块文档；运行路径仅在调用新入口时加载词库；目标框架不变；不增加运行时第三方依赖。

## 实现

- `GetPinyinWithTone(text, separator)` 返回带调拼音；`GetContextualPinyin(text, separator)` 用相同读音返回无调拼音。
- 词组采用最长完整匹配；未命中时使用单字首选读音。非中文、未收录字符和孤立代理项保持原文。
- 固定 pinyin-data v0.15.0 的 41,923 条单字记录及 phrase-pinyin-data v0.19.0 的 47,111 个去重词组。压缩数据合计 664,811 字节；首次调用新入口时懒加载，运行不联网。
- 原始数据和 MIT 许可保存在 `asset/pinyin`，NuGet 包只包含压缩资源和许可文件。生成器支持 `--check`。

## 验证

- L0：生成器 `--check` 通过；Text 四个目标框架 Release 构建通过，0 错误。`CaseFormatter._humanizerMode` 的既有 CS0414 警告仍存在。
- L1：拼音定向测试 22 项通过，覆盖词组、声调、首选单字读音、`ü`、补充平面汉字、孤立代理项和旧接口行为。
- L2：Text 模块 net6.0、net7.0、net8.0 各 249 项通过。
- L3：Text NuGet 包构建通过，四个程序集及三份许可/来源文件齐全；当前包 2,836,277 字节。使用隔离缓存从当前包离线还原的消费者验证了带调、词组及旧无调入口。没有修改发行版本或发布。
- `git diff --check` 通过；本轮未运行无关全解决方案及性能基准。

## 当前状态

- Completed：最终 Emoji 包消费者验证；拼音固定快照、两个新入口、回归测试、文档与打包。
- Open Actionable：无。
- Blocked Approval：无。
- Blocked External：无。
- Not Applicable：未设置性能门槛或运行大规模基准。
- Accepted Limitations：未收录词组回退单字首选读音；同词组多条记录按数据源首条处理；没有语法或上下文推断。缺少 .NET 5 和 .NET Core 3.1 运行时，相应旧目标未运行。
- Verified Boundaries：报告当前压缩资源和包体实测大小，不声称词组命中率或性能上限。
- Deferred（当时状态）：简繁文本转换与中文分词属于独立后续能力；两项已在后续轮次实现，当前状态见模块文档。
- Next Action：STOP。
- Goal Status：COMPLETED。
