# 测试项目归属规则（Bing.Utils）

> 适用于 `Bing.Utils` 基础设施工具库仓库的测试组织规范。
> 目标：避免测试放错项目、降低耦合、提升模块边界清晰度与回归效率。

## 1. 目的

统一测试归属规则，解决以下问题：

* 模块专属测试混入 `Bing.Utils.Tests`
* 同一功能分散在多个测试项目，难以维护
* 重构或迁移时无法快速判断测试应该放哪里
* CI 分层执行（按模块跑测试）困难

---

## 2. 测试项目角色定义

### 2.1 `Bing.Utils.Tests`

用于承载 **主包（`Bing.Utils`）自身功能** 的测试，以及少量**跨模块通用行为测试**（满足条件时）。

适合放入：

* `Bing.Utils` 主包中的类型、扩展方法、工具方法测试
* 不属于任何专属子包的通用能力测试
* 跨模块组合行为测试（但不偏向某个模块，且无专属项目更合适时）

不应放入：

* `Bing.Utils.IdUtils` 的专属测试
* `Bing.Utils.Text` 的专属测试
* `Bing.Utils.Collections` 的专属测试
* 任何已经存在专属测试项目的模块测试（除非是明确的跨模块集成场景）

---

### 2.2 `Bing.Utils.<Module>.Tests`

用于承载 **模块专属单元测试与模块内部行为测试**。

例如：

* `Bing.Utils.IdUtils.Tests`
* `Bing.Utils.Collections.Tests`
* `Bing.Utils.DateTime.Tests`
* `Bing.Utils.Http.Tests`

适合放入：

* 直接测试该模块命名空间下的 public API
* 模块特有算法、格式、解析、转换逻辑测试
* 模块边界条件、异常、参数化测试
* 模块内回归测试（bugfix 对应用例）

不应放入：

* 与该模块无关的测试
* 仅仅“顺手用到”该模块但主要验证其他模块的测试（应放主责模块）

---

### 2.3 `Bing.Utils.Http.Tests.Integration`（或其他 `*.Integration`）

用于承载 **需要真实组件协作的集成测试**。

适合放入：

* Http 请求构建 + 序列化 + 响应解析链路测试
* 本地 TestServer / MockHttp / WireMock 驱动的行为验证
* 文件系统/图像库/编码器等真实组件协作测试（非外网）

不应放入：

* 纯逻辑单元测试（应放对应 `*.Tests`）
* 依赖不稳定外网或不可控环境的测试（除非显式隔离并标注）

---

## 3. 测试归属判定规则（核心）

当新增/迁移测试时，按以下顺序判断：

### 规则 1：按“被测对象所属模块”归属（优先级最高）

如果测试主要验证的类型/方法属于 `Bing.Utils.<Module>`，则测试应放到 `Bing.Utils.<Module>.Tests`。

**示例**

* 测试 SnowflakeId 生成规则 → `Bing.Utils.IdUtils.Tests`
* 测试字符串脱敏扩展方法（在 Text 模块）→ `Bing.Utils.Text.Tests`

---

### 规则 2：按“主要断言目标”归属

如果一个测试调用了多个模块，但断言核心是某一模块行为，则归属该模块。

**示例**

* 使用 `Text` 模块辅助构造输入，但断言的是 `IdUtils` 的解析结果 → `Bing.Utils.IdUtils.Tests`
* 使用 `DateTime` 格式化时间后发起 HTTP 请求，但断言的是请求签名生成逻辑（在 Http 模块）→ `Bing.Utils.Http.Tests`

---

### 规则 3：跨模块协同且无法归属单一模块时，放主包测试或集成测试

如果测试明确验证多个模块协同行为，且不适合拆分为单模块测试：

* 偏纯逻辑协同：可放 `Bing.Utils.Tests`
* 偏真实组件协作：放 `*.Integration`

---

### 规则 4：回归测试跟随缺陷归属模块

修复某个模块 bug 时新增的回归测试，应放在该模块专属测试项目中。

---

### 规则 5：不要因为“已有同类工具类”就放错项目

即使 `Bing.Utils.Tests` 已有类似测试类，也不能因为方便继续放在主测试项目。
应优先保证模块边界清晰，再考虑统一风格。

---

## 4. 混合测试处理规范（很常见）

某些老测试类可能同时覆盖多个模块（例如一个类里既测 `IdUtils` 又测 `Text`）。

### 4.1 优先按测试方法拆分

* 将 `IdUtils` 相关测试方法迁移到 `Bing.Utils.IdUtils.Tests`
* 将其他模块测试方法保留或迁移到各自模块测试项目

### 4.2 允许复制最小辅助代码，避免强耦合共享

如果拆分后依赖原类中的私有辅助方法/测试数据：

* 优先在目标测试项目中复制最小必要辅助代码（短期）
* 后续再抽取共享测试基础设施（长期）

### 4.3 暂无法拆分时必须标记

若测试夹具强耦合、拆分风险高：

* 在原测试类加 `TODO` 注释说明原因
* 在迁移计划中登记，后续专项处理

---

## 5. 命名与目录建议

### 5.1 测试类命名

推荐格式：

* `<TypeName>Test`
* `<TypeName>Tests`
* `<MethodOrFeatureName>Test`

示例：

* `SnowflakeIdGeneratorTests`
* `GuidHelperTests`
* `IdCardParserTests`

---

### 5.2 测试方法命名

推荐格式：

`MethodName_Scenario_ExpectedResult`

示例：

* `NextId_WhenCalledConsecutively_ShouldReturnUniqueValues`
* `Parse_WhenInputIsNull_ShouldThrowArgumentNullException`
* `Format_WhenOptionDisabled_ShouldReturnRawValue`

---

### 5.3 目录组织（模块专属测试项目内）

建议按“被测类型/功能域”组织，而非按测试类型组织。

示例（`Bing.Utils.IdUtils.Tests`）：

* `Generators/`
* `Parsers/`
* `Encoders/`
* `Extensions/`
* `Regression/`

---

## 6. 新增 Public API 的测试准入要求

当模块新增 public API 时，至少补以下测试（放对应模块专属测试项目）：

1. **正常路径**（Happy Path）
2. **边界输入**（null / empty / default / min / max）
3. **异常路径**（应抛异常时断言异常类型）
4. **至少一个参数化用例**（若方法存在多输入变体）
5. **回归测试**（若 API 来自 bugfix）

---

## 7. 迁移测试时的执行规范

迁移（如从 `Bing.Utils.Tests` → `Bing.Utils.IdUtils.Tests`）时应遵守：

* 优先 `move/rename`，减少历史丢失
* 不改变断言语义与测试意图
* 最小化修改（命名空间、using、引用、辅助类）
* 迁移后清理旧测试，避免重复执行
* 编译验证两个项目都通过
* 输出迁移报告（迁移清单 + 待确认项）

---

## 8. 常见反例（避免踩坑）

### 反例 1：顺手放在 `Bing.Utils.Tests`

> “这个测试不多，先放主测试项目吧，后面再挪。”

问题：后续极易越积越多，模块边界失真。

---

### 反例 2：按使用到的模块数量决定归属

> “这个测试用了两个模块，所以放主项目。”

应看 **主要断言目标**，不是看引用数量。

---

### 反例 3：集成测试和单元测试混放

> 纯逻辑测试放进 `*.Integration`，导致执行慢且不稳定。

应明确分层，保证 PR 阶段快速反馈。

---

## 9. 建议的 CI 执行分层（可选）

* **PR 必跑**

  * 所有模块专属单元测试（`*.Tests`）
  * 快速集成测试（已标记可稳定运行）
* **Nightly**

  * 全量集成测试（`*.Integration`）
  * 回归套件（`Regression` 目录或 Trait 分组）

---

## 10. 迁移场景示例（当前仓库）

### 示例：IdUtils 测试迁移

目标：

* 将 `Bing.Utils.Tests` 中所有 `IdUtils` 相关测试迁移至 `Bing.Utils.IdUtils.Tests`

判定依据：

* 直接测试 `Bing.Utils.IdUtils` 命名空间类型
* 测试类/方法语义为 ID 生成、解析、编码、唯一性、格式化等
* 修复 `IdUtils` 缺陷对应的回归测试

迁移后结果期望：

* `Bing.Utils.IdUtils.Tests` 成为 IdUtils 唯一专属测试承载项目
* `Bing.Utils.Tests` 仅保留主包/通用/跨模块测试

---

## 11. 后续优化建议（长期）

1. 抽取 `tests/Common`（共享测试辅助设施）

   * 测试数据生成器
   * 公共断言扩展
   * 时间/随机桩对象

2. 为测试项目增加模块边界检查（脚本或审查规则）

   * 防止 `IdUtils` 测试再次进入 `Bing.Utils.Tests`

3. 建立 defect-to-test 映射文档

   * 缺陷修复必须带回归测试