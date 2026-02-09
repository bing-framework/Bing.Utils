# Bing.Utils 文档索引

> 本目录用于沉淀 Bing.Utils 各模块设计、API、测试与兼容性信息。  
> 建议每个模块按统一模板维护：`docs/TEMPLATE.module.md` 或 `docs/TEMPLATE.module.bing-utils.md`。

## 文档结构建议

- `00-overview.md`：项目总览（定位、模块图、依赖关系）
- `01-architecture.md`：分层与依赖约束
- `02-naming-conventions.md`：命名与代码风格
- `03-testing-strategy.md`：测试与基准策略
- `modules/`：按模块拆分的设计文档

## 模块文档索引（建议文件名）

- [Bing.Utils（核心）](./modules/core.md)
- [Bing.Utils.Text](./modules/text.md)
- [Bing.Utils.Collections](./modules/collections.md)
- [Bing.Utils.Reflection](./modules/reflection.md)
- [Bing.Utils.Http](./modules/http.md)
- [Bing.Utils.Drawing](./modules/drawing.md)
- [Bing.Utils.Drawing.ImageSharp](./modules/drawing-imagesharp.md)
- [Bing.Utils.Drawing.SkiaSharp](./modules/drawing-skiasharp.md)

## 模块文档最小要求

每个模块文档至少包含：
1. 模块定位（目标/非目标）
2. 对外 API 清单
3. 核心类型与职责
4. 依赖关系（直接/可选/禁止）
5. 关键实现说明（流程、边界、异常）
6. 测试策略（正常、边界、异常）
7. 兼容性与升级说明

## 与 Copilot 配合方式

1. 在仓库根目录创建并维护：`.github/copilot-instructions.md`
2. 使用 `@workspace` 让 Copilot 基于全仓生成文档草稿
3. 要求“每个结论附文件路径/类型名”，不确定项标记“待确认”
4. 先生成提纲，再分章节产出，最后人工校对

## 快速开始

- 复制模板：`docs/TEMPLATE.module.bing-utils.md`
- 新建模块文档：`docs/modules/{module}.md`
- 完成后在本文件补充链接与状态

---

如需批量自动生成模块文档，可在 Copilot Chat 使用：

```text
@workspace
基于 docs/TEMPLATE.module.bing-utils.md，
为 src 下每个项目生成对应模块文档到 docs/modules/。
要求：中文输出；结论附文件路径与类型名；不确定项放“待确认”。
```
