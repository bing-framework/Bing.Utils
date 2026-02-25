# 单元测试命名与注释规范化总控提示词（Test Style Normalization）

> 版本：v1.1（英文方法名 + 中文注释 + 三段式注释：目的/场景/预期）

使用 AGENTS.md 规则，并遵循仓库现有 C# / xUnit 测试风格，以及 `docs/quality/test-style-guide.md`（如存在）。

任务：对指定单元测试文件执行“测试方法命名规范化 + 注释补全（中文）”，不修改测试断言语义，不修改生产代码。

## 目标
1. 统一测试方法命名格式（英文）
2. 补全测试类与测试方法的 XML 注释（中文）
3. 测试方法注释使用“三段式”：**目的 / 场景 / 预期**
4. 保持测试逻辑、断言、测试数据不变
5. 输出重命名映射与注释补全清单，便于审阅

---

## 命名规范（必须统一）
### 测试方法命名（英文）
统一格式：

`MethodName_WhenCondition_ExpectedResult`

示例：
- `ToInt_WhenInputIsNull_ReturnsDefaultValue`
- `ToInt_WhenInputIsInvalidNumber_ReturnsDefaultValue`
- `MaskPhone_WhenInputIsValidPhone_ReturnsMaskedValue`
- `SubstringSafe_WhenRangeIsOutOfBounds_ReturnsExpectedSegment`

### 参数化测试（Theory）
- 方法名表达“规则与预期”，不要把具体参数值写进方法名
- 具体参数放在 `[InlineData]` / `[MemberData]` 中

示例：
- `ToInt_WhenInputIsInvalidNumber_ReturnsDefaultValue`
- `ToInt_WhenInputIsBoundaryValue_ReturnsParsedResult`

### 测试类命名
- 保持仓库现有风格一致（例如 `*Test` 或 `*Tests`）
- 本任务默认 **不修改测试类名**，除非你明确要求

---

## 注释规范（必须统一，中文）
### 测试类注释（XML summary）
为测试类补充中文 `<summary>`，说明“被测类 + 测试用途”。

示例：
```csharp
/// <summary>
/// StringHelper 单元测试
/// </summary>
public class StringHelperTest
{
}
```

### 测试方法注释（XML summary）
为测试方法补充中文 `<summary>`，至少包含：
- **目的**（为什么测：规则点 / 风险点 / 回归点）
- **场景**（输入 / 前置条件）
- **预期**（返回值 / 行为 / 异常）

示例（Fact）：
```csharp
/// <summary>
/// 目的：验证空输入时的默认值处理逻辑，避免空字符串导致解析异常。
/// 场景：输入为空字符串。
/// 预期：返回默认值。
/// </summary>
[Fact]
public void ToInt_WhenInputIsEmpty_ReturnsDefaultValue()
{
}
```

示例（Theory）：
```csharp
/// <summary>
/// 目的：验证非法数字文本输入的容错行为，确保解析失败时返回默认值。
/// 场景：输入非法数字文本。
/// 预期：返回默认值。
/// </summary>
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("abc")]
public void ToInt_WhenInputIsInvalidNumber_ReturnsDefaultValue(string input)
{
}
```

### 注释文案要求
- 使用中文
- 简洁明确，避免空泛描述（例如“测试方法”“测试一下”）
- 不重复方法名全文（注释补充目的、场景与预期）
- 不臆造业务背景，以源码与断言语义为准
- **“目的”必须体现规则价值、风险点或回归意义，不能仅重复场景**

---

## 输入参数（执行前填写）
- 目标测试文件（单个或多个）：
  - `<TEST_FILE_PATH_1>`
  - `<TEST_FILE_PATH_2>`
- 是否包含参数化测试：`是/否/未知`
- 是否允许调整测试类名：`否（默认）`
- 是否要求输出重命名映射表：`是（默认）`

---

## 执行范围
- 允许修改：
  - 指定测试文件（仅命名与注释）
- 不允许修改：
  - `01-src/*`（生产代码）
  - 非指定测试文件
  - 测试逻辑、断言、测试数据、测试流程
  - csproj / 引用（除非仅因注释格式导致格式化需要，通常不需要）

---

## 必须遵循的规则
1. 先读取并简要总结：
   - `AGENTS.md`
   - `docs/quality/test-style-guide.md`（如存在）
   - 指定测试文件
2. 先输出“规范化计划”（不要直接改代码）
3. 重命名时保持方法语义不变，不得误改测试意图
4. 若无法从代码/断言清晰判断测试意图，暂停并列出疑问项
5. 若重命名后出现重名冲突，给出冲突处理方案（如细化 Condition）
6. 注释文案必须使用中文三段式：**目的 / 场景 / 预期**
7. 方法名必须使用英文，格式为 `MethodName_WhenCondition_ExpectedResult`

---

## 执行步骤（按顺序）

### 第1步：分析当前问题（先不改代码）
对每个指定测试文件输出：
- 测试类名称与被测对象（如可推断）
- 测试方法命名问题清单（不规范/含糊/缩写不清/旧风格混杂）
- 注释缺失情况（类注释 / 方法注释）
- 三段式注释缺失情况（是否缺“目的”）
- 潜在风险（方法意图不清、重复方法名风险、Theory 命名歧义）

### 第2步：规范化计划（先不改代码）
按“文件 -> 方法”输出计划，包含：
- 原方法名
- 新方法名（英文，`MethodName_WhenCondition_ExpectedResult`）
- 命名依据（从断言/输入/行为推断）
- 是否补类注释
- 是否补方法注释（中文三段式摘要）
- “目的”文案草案（说明规则价值 / 风险点 / 回归意义）
- 是否存在冲突风险

### 第3步：执行规范化（修改测试文件）
仅执行以下修改：
1. 测试方法重命名（英文格式）
2. 测试类 `<summary>` 注释补全（中文）
3. 测试方法 `<summary>` 注释补全（中文三段式：目的 / 场景 / 预期）

要求：
- 不修改断言语义
- 不修改测试输入数据
- 不调整测试执行顺序（除非格式化工具自动处理）
- 保持现有 xUnit 特性标注（`[Fact]`, `[Theory]`, `[InlineData]` 等）

### 第4步：最小验证
输出：
- 文件级静态检查结果（命名是否统一、注释是否补齐、三段式是否完整）
- 若可执行：目标测试项目编译/测试情况
- 若不可执行：说明原因，并给出替代验证（结构检查/语义一致性检查）

同时检查：
- 是否出现重名测试方法
- 是否有注释与方法语义不一致
- 是否仅修改允许范围（指定测试文件）

### 第5步：结果总结（必须输出）
请按以下结构输出：
1. 已读取规则与文件摘要
2. 命名问题与注释缺失概览
3. 重命名映射表（旧名 -> 新名）
4. 注释补全清单（类/方法，含“目的”摘要）
5. 验证结果（编译/测试/替代验证）
6. 未处理项与原因（TODO）
7. 下一步建议（如批量规范化更多文件）

---

## 执行策略（连续运行）
- 将本次工作视为一个端到端任务，连续执行直到完成：
  分析 -> 计划 -> 命名规范化与注释补全 -> 验证 -> 总结
- 不要在每个小步骤后询问“是否继续”
- 仅在以下情况暂停并请求确认：
  1) 无法从测试代码判断真实测试意图（可能导致错误命名）
  2) 重命名将产生方法名冲突，且无法安全自动消解
  3) 指定文件存在大量风格冲突，超出本轮“仅命名+注释”范围

---

## 快速调用示例（单文件）
```text
请读取并执行 `docs/quality/prompts/test-style-normalization.prompt.md`。
同时遵循 `docs/quality/test-style-guide.md`。

目标测试文件：
- 02-tests/Bing.Utils.Text.Tests/StringHelperTest.cs

规则：
- 方法名用英文，格式为 `MethodName_WhenCondition_ExpectedResult`
- 注释用中文（类和测试方法补全 summary）
- 注释必须使用三段式：目的 / 场景 / 预期
- 输出重命名映射表
- 不修改测试逻辑与断言
- 连续执行，不要中途停下来确认
```

## 快速调用示例（多文件）
```text
请读取并执行 `docs/quality/prompts/test-style-normalization.prompt.md`。
同时遵循 `docs/quality/test-style-guide.md`。

目标测试文件：
- 02-tests/Bing.Utils.Text.Tests/StringHelperTest.cs
- 02-tests/Bing.Utils.Text.Tests/MaskHelperTest.cs
- 02-tests/Bing.Utils.Collections.Tests/CollectionExtensionsTest.cs

规则：
- 方法名用英文，格式为 `MethodName_WhenCondition_ExpectedResult`
- 注释用中文（类和测试方法补全 summary）
- 注释必须使用三段式：目的 / 场景 / 预期
- 输出重命名映射表
- 不修改测试逻辑与断言
- 连续执行，不要中途停下来确认
```
