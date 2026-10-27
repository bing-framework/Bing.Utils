# OPC 兼容性夹具

这七个小型 ZIP 由 `System.IO.Packaging 8.0.1` 的 `Package.CreatePart` / `CreateRelationship` 独立产生，并在写入前由同一库重新打开读取；清单、Default / Override 和关系 XML 均由生产器生成，不调用文件检测器或复制其路径校验实现。

这些是识别逻辑使用的最小 OPC 包，主部件内容为 `<document/>`，不宣称是 Word 可完整呈现的文档。完整 Office 应用产物的格式验证不属于这些夹具的目标。

| 文件 | 覆盖 |
| --- | --- |
| ordinary.zip | 标准主路径，对照样本 |
| space-default.zip | 主部件 URI 含 %20，以 Default 声明 MIME |
| space-override.zip | 同扩展名具有不同 MIME，由生产器生成含 %20 的 Override |
| main-colon-extension.zip | 主部件 Default Extension 为 a:b |
| main-escaped-extension.zip | 主部件 Default Extension 为 a%2Bb |
| attachment-colon-extension.zip | 标准主文档 + 无关附件 a:b，验证不能因附件误拒整包 |
| attachment-escaped-extension.zip | 标准主文档 + 无关附件 a%2Bb |

测试读取固定二进制文件，复用测试项目现有 Resources 复制规则；不需要 System.IO.Packaging 测试或生产依赖。需要重新生成时，在 PowerShell 7 中向 `generate-opc-fixtures.ps1` 的 `-PackagingAssemblyPath` 传入已安装的 System.IO.Packaging 8.0.1 / net8.0 程序集完整路径。所有 XML 文本使用 UTF-8，ZIP 时间字段固定到 2000-01-01 UTC，根关系 ID 固定为 rId1。
