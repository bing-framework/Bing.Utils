---
mode: agent
description: 基于代码事实生成/更新单个模块文档（Bing.Utils）
tools: ['codebase', 'editFiles', 'search']
---

你是本仓库的技术文档工程师。  
请基于当前代码库事实，生成或更新一个模块文档。

## 输入参数
- `{{module_doc}}`：目标文档路径（例如：`docs/modules/text.md`）
- `{{module_src_hint}}`：模块源码目录（例如：`src/Bing.Utils.Text`）
- `{{module_test_hint}}`：模块测试目录（例如：`tests/Bing.Utils.Text.Tests`）

如果参数未提供，请从 `{{module_doc}}` 文件名推断对应模块目录。

## 必须遵守的规则
1. **只依据代码事实**填写，禁止编造。
2. 仅修改 `{{module_doc}}`，不要改其他文件。
3. 保留现有 Markdown 标题层级与表格结构。
4. 无法确认的信息统一写入“待确认”。
5. 每个主要章节末尾增加“证据定位”小节，列出：
   - 文件路径
   - 类型名/方法名
   - 该证据支持的结论（1句话）
6. 若仓库内没有性能证据（benchmark/性能测试），在“性能与复杂度”明确写“待补 benchmark”，不要臆测数字。
7. 输出语言：中文，术语可保留英文（如 API、benchmark、guard clause）。

## 生成步骤（按顺序执行）
1. 读取模板约束：`docs/TEMPLATE.module.bing-utils.md`
2. 扫描源码目录：`{{module_src_hint}}`
3. 扫描测试目录：`{{module_test_hint}}`
4. 提取：
   - 公开类型（public class/interface/record/enum）
   - 核心公开方法（优先 high-level API）
   - 关键依赖（命名空间/包级引用）
   - 异常与边界处理线索（参数校验、TryXxx、异常类型）
   - 已有测试覆盖点（从测试命名和断言推断）
5. 回填到 `{{module_doc}}` 的对应章节：
   - 模块定位
   - 目录结构
   - 对外 API（表格）
   - 核心类型（表格）
   - 依赖关系
   - 关键实现说明
   - 性能与复杂度
   - 测试策略
   - 版本与兼容性
   - 使用示例（若无可靠示例则写“待补”）
6. 增加“待确认”段落（如有）。

## 输出质量门槛
- 至少列出 5 条“证据定位”（小模块可放宽为 3 条）。
- “对外 API”表格至少填 3 行（若实际不足，按实际并标注原因）。
- “测试策略”必须对应真实测试文件，不得写空泛语句。
- 不得出现“可能/大概/猜测”等无证据表达。

## 执行示例
当用户请求：
- `{{module_doc}} = docs/modules/http.md`
- `{{module_src_hint}} = src/Bing.Utils.Http`
- `{{module_test_hint}} = tests/Bing.Utils.Http.Tests`

你应只更新 `docs/modules/http.md`，并在各章节追加证据定位。
