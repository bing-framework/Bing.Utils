# 第二批独立审查记录

审查者：luna_worker / file_detection_review；只读，无生产修改或测试执行。主代理执行修复与验证。

范围：新增 ZIP/Office 检测器、选项/结果、各入口集成、两组容器测试与参考指南；检查资源预算、损坏输入、路径/主关系与类型交叉验证、异常取消、流位置与 Probe、默认检测兼容。无扩大到其他模块。

## Findings

### R1 — MIME 类型比较大小写（CLOSED）

位置：FileContentOfficePackage.MainTypes 与主部件 ContentType 查询。

触发：有效包将主部件 ContentType 写为大小写等价值，原实现使用 Ordinal 导致识别退回普通 ZIP，从而误判 Office 扩展名。

修复：MainTypes 改用 StringComparer.OrdinalIgnoreCase。新增 Detect_MainContentTypeUsesCaseInsensitiveMimeComparison：混合大小写 MIME 仍为 DOCX，.docx 为 Match、.xlsx 为 Mismatch。

依据：[RFC 9110 §8.3.1](https://www.rfc-editor.org/rfc/rfc9110.html#section-8.3.1)、[PackagePart.ContentType](https://learn.microsoft.com/en-us/dotnet/api/system.io.packaging.packagepart.contenttype)。

独立复核：代码与回归断言满足原验收条件，标记 CLOSED。未报告其他 OPEN_ACTIONABLE。已完成一轮 Review → Fix → 定向复核。

## 结论

OPEN_ACTIONABLE = 0。无审批/外部阻塞；不继续自动修复循环。测试结果见 execution.md；ZIP64/分卷/加密元数据/正文与 CRC 未校验属于明示支持边界。
