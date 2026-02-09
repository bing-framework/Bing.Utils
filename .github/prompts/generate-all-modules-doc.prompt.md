---
mode: agent
description: 按顺序批量生成/更新 Bing.Utils 模块文档（一次只改一个文件）
tools: ['codebase', 'editFiles', 'search']
---

你是本仓库的技术文档工程师。  
请按固定顺序批量更新模块文档。**一次只修改一个文件**，每完成一个模块就输出简短进度，然后继续下一个，直到全部完成。

## 全局规则（必须遵守）
1. 只依据代码事实，禁止编造。
2. 每次仅修改当前目标模块文档，不修改其他文件。
3. 保留原有 Markdown 标题层级与表格结构。
4. 无法确认的信息写入“待确认”。
5. 每个主要章节末尾增加“证据定位”小节，包含：
   - 文件路径
   - 类型名/方法名
   - 该证据支持的结论（1句话）
6. 若无性能证据，性能章节写“待补 benchmark”，不要杜撰数据。
7. 输出中文，术语可保留英文（API、benchmark、guard clause）。
8. 若某模块源码或测试目录不存在，保留章节并标注“目录不存在（待确认）”。

## 统一模板
- 参考：`docs/TEMPLATE.module.bing-utils.md`

## 批处理清单与映射
按以下顺序处理：

1) `docs/modules/core.md`  
   - src: `src/Bing.Utils`  
   - tests: `tests/Bing.Utils.Tests`

2) `docs/modules/text.md`  
   - src: `src/Bing.Utils.Text`  
   - tests: `tests/Bing.Utils.Text.Tests`

3) `docs/modules/collections.md`  
   - src: `src/Bing.Utils.Collections`  
   - tests: `tests/Bing.Utils.Collections.Tests`

4) `docs/modules/reflection.md`  
   - src: `src/Bing.Utils.Reflection`  
   - tests: `tests/Bing.Utils.Reflection.Tests`

5) `docs/modules/http.md`  
   - src: `src/Bing.Utils.Http`  
   - tests: `tests/Bing.Utils.Http.Tests`

6) `docs/modules/drawing.md`  
   - src: `src/Bing.Utils.Drawing`  
   - tests: `tests/Bing.Utils.Drawing.Tests`

7) `docs/modules/drawing-imagesharp.md`  
   - src: `src/Bing.Utils.Drawing.ImageSharp`  
   - tests: `tests/Bing.Utils.Drawing.Tests`（如无专属测试，注明共用测试）

8) `docs/modules/drawing-skiasharp.md`  
   - src: `src/Bing.Utils.Drawing.SkiaSharp`  
   - tests: `tests/Bing.Utils.Drawing.Tests`（如无专属测试，注明共用测试）

## 每个模块的执行步骤（循环）
对当前模块执行以下步骤：

1. 读取当前文档（目标 md）与模板要求。
2. 扫描对应 src/tests 目录，提取：
   - public 类型（class/interface/record/enum）
   - 核心 public 方法（优先高层 API）
   - 关键依赖与引用线索
   - 边界与异常处理线索（参数校验、TryXxx、异常类型）
   - 测试覆盖点（由测试命名与断言推断）
3. 回填章节：
   - 模块定位
   - 目录结构
   - 对外 API（表格）
   - 核心类型（表格）
   - 依赖关系
   - 关键实现说明
   - 性能与复杂度
   - 测试策略
   - 版本与兼容性
   - 使用示例（无可靠示例则写“待补”）
4. 在文档末尾更新：
   - 待确认
   - 证据定位（至少 5 条；小模块可 3 条）

## 进度输出格式
每完成一个模块后输出一行：

`[done] <module-doc> | evidence=<N> | apis=<N> | tests=<N> | todo=<N>`

其中：
- evidence：证据定位条数
- apis：API 表格条数
- tests：测试策略中引用的测试文件/用例数
- todo：待确认项数量

## 全部完成后
最后再执行一次：

- 更新 `docs/README.md`，补充：
  1) 模块能力矩阵（模块/职责/关键类型/测试状态）
  2) 阅读顺序（从 core 到 integrations）
  3) 待确认汇总（按模块统计）
- 仅在已有结构内追加，不重写整页。
