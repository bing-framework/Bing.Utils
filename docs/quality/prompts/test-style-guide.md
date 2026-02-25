# 单元测试命名与注释规范（Test Style Guide）

> 版本：v1.1（英文方法名 + 中文注释 + 三段式注释：目的/场景/预期）

## 1. 目的
统一仓库内单元测试的命名风格与注释风格，提升以下质量：

- 可读性（看到方法名就知道测什么）
- 可维护性（后续补测、重构、迁移更容易）
- 可审阅性（PR review 更快）
- 一致性（不同模块风格统一）

本规范适用于：
- `02-tests/*` 下的 C# 单元测试项目（xUnit 为主）

---

## 2. 基本原则

1. **方法名用英文**
2. **注释文案用中文**
3. **测试方法注释使用三段式：目的 / 场景 / 预期**
4. **不为了命名规范而改变测试逻辑**
5. **优先表达行为与预期，而不是实现细节**
6. **参数化测试名称表达规则，不写具体参数值**

---

## 3. 测试类命名规范

### 3.1 命名风格
保持仓库现有风格一致（当前若以 `*Test` 为主，则继续沿用）。

推荐：
- `StringHelperTest`
- `MaskHelperTest`
- `SnowflakeIdGeneratorTest`

> 若仓库后续统一为 `*Tests`，应通过专项整理一次性切换，不在日常增量修改中混用。

### 3.2 测试类注释（中文）
测试类建议补充 XML `<summary>` 注释，说明被测类与测试用途。

示例：
```csharp
/// <summary>
/// StringHelper 单元测试
/// </summary>
public class StringHelperTest
{
}
```

---

## 4. 测试方法命名规范（英文）

### 4.1 统一格式（推荐）
使用以下格式命名测试方法：

`MethodName_WhenCondition_ExpectedResult`

结构说明：
- `MethodName`：被测方法名（或核心行为名）
- `WhenCondition`：触发场景 / 前置条件
- `ExpectedResult`：预期结果 / 行为

---

### 4.2 命名示例（推荐）
#### 普通测试（Fact）
- `ToInt_WhenInputIsNull_ReturnsDefaultValue`
- `ToInt_WhenInputIsEmpty_ReturnsDefaultValue`
- `ToInt_WhenInputIsValidNumber_ReturnsParsedValue`
- `MaskPhone_WhenInputIsValidPhone_ReturnsMaskedValue`
- `SubstringSafe_WhenRangeIsOutOfBounds_ReturnsExpectedSegment`

#### 异常场景
- `Parse_WhenInputIsNull_ThrowsArgumentNullException`
- `CreateId_WhenWorkerIdIsOutOfRange_ThrowsArgumentOutOfRangeException`

#### 布尔结果
- `IsEmail_WhenFormatIsValid_ReturnsTrue`
- `IsEmail_WhenFormatIsInvalid_ReturnsFalse`

---

### 4.3 参数化测试（Theory）命名规则
参数化测试的方法名应表达“**规则 + 预期**”，不要把具体参数值写到方法名中。

推荐：
- `ToInt_WhenInputIsInvalidNumber_ReturnsDefaultValue`
- `ToInt_WhenInputIsBoundaryValue_ReturnsParsedResult`

不推荐：
- `ToInt_WhenInputIsNullOrEmptyOrAbc_ReturnsDefaultValue`

> 具体样例放在 `[InlineData]` / `[MemberData]` 中，不放在方法名里。

---

### 4.4 命名约束（建议）
- 使用 PascalCase
- 不使用含糊缩写（如 `Test1`, `DoIt`, `CaseA`）
- 不以 `Test_` 前缀命名（因为测试上下文已明确）
- 不重复类名信息（例如在 `StringHelperTest` 中无需写 `StringHelper_...`，除非有歧义）
- 同一测试类内方法名不得重复

---

## 5. 测试方法注释规范（中文）

### 5.1 是否必须写注释
建议为测试方法补充 XML `<summary>` 注释，尤其是以下场景：
- 业务含义不直观
- 异常路径 / 边界路径
- 参数化测试（Theory）
- 回归缺陷对应测试（可注明来源）

### 5.2 注释内容结构（推荐）
测试方法注释建议使用“三段式”结构：
- **目的**：为什么要测这个用例（风险点 / 规则点 / 回归点）
- **场景**：输入 / 前置条件
- **预期**：返回值 / 行为 / 异常

推荐模板：
```csharp
/// <summary>
/// 目的：验证某项规则或风险点。
/// 场景：输入/前置条件说明。
/// 预期：返回值/行为/异常说明。
/// </summary>
```

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

示例（异常）：
```csharp
/// <summary>
/// 目的：验证参数边界校验是否生效，避免非法节点编号参与ID生成。
/// 场景：工作节点编号超出允许范围。
/// 预期：抛出参数越界异常。
/// </summary>
[Fact]
public void CreateId_WhenWorkerIdIsOutOfRange_ThrowsArgumentOutOfRangeException()
{
}
```

### 5.3 注释文案要求
- 使用中文
- 简洁明确，避免空话（如“测试一下”“验证方法”）
- 不照抄方法名全文（注释补充目的、场景与预期）
- 不臆造业务背景，以源码、注释、断言语义为准
- “目的”优先描述风险、规则或回归价值，而不是重复场景

---

## 6. 参数化测试建议（xUnit）

对于纯函数/工具方法，优先使用参数化测试减少重复代码：

- 使用 `[Theory] + [InlineData]`
- 当数据较复杂时使用 `[MemberData]` / `[ClassData]`
- 方法名表达规则，具体样例放在数据源

示例：
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
    // Arrange / Act / Assert
}
```

---

## 7. 不应在本规范化任务中修改的内容

以下内容不属于“命名与注释规范化”任务范围，除非另有明确指令：

- 测试断言逻辑
- 测试输入数据
- 测试执行流程
- 生产代码（`01-src/*`）
- 大规模测试重构（Fixture 抽取、测试基础设施改造）
- csproj 引用变更（通常不需要）

---

## 8. 推荐与 Codex 配合方式

建议使用：
- `docs/quality/prompts/test-style-normalization.prompt.md`

典型用法：
1. 指定 1~3 个测试文件先做规范化
2. 输出“重命名映射表（旧名 -> 新名）”
3. 检查注释文案是否准确
4. 再扩大到批量文件

---

## 9. 重命名映射输出要求（建议）
在执行规范化任务时，建议输出映射清单，便于审阅：

- `OldTestName` -> `NewTestName`
- `Test1` -> `ToInt_WhenInputIsNull_ReturnsDefaultValue`

若存在冲突，应在输出中说明冲突处理策略（例如细化 `WhenCondition`）。

---

## 10. 快速示例（对照）

### 10.1 不推荐
```csharp
[Fact]
public void Test1()
{
}
```

### 10.2 推荐
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

---

## 11. 后续演进（可选）
后续可通过 Roslyn Analyzer / 自定义检查规则做增量约束，例如：
- 检查测试方法名是否符合 `MethodName_WhenCondition_ExpectedResult`
- 检查测试类/测试方法是否缺少 `<summary>`
- 检查测试方法注释是否包含“目的 / 场景 / 预期”

在引入自动检查前，建议先使用 Codex 批量规范化现有测试文件。
