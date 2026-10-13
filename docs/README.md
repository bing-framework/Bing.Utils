# Bing.Utils 文档

## 快速入口

- [项目概览](00-overview.md)
- [能力地图](00-capability-map.md)
- [文档与实现盘点](00-documentation-audit.md)
- [分类模块目录](01-module-catalog.md)
- [模块文档索引](modules/README.md)
- [API 参考说明](reference/api-reference.md)
- [安全与密码学专题](security.md)
- [质量与验证资料](quality/)
- [贡献指南](CONTRIBUTING.md)

## 维护约定

- 模块标准入口统一使用 `docs/modules/Bing.Utils*.md` 命名，并与 `src` 下项目一一对应。
- 每份模块文档至少说明功能、核心逻辑和文件位置、接口示例、依赖配置、限制及待确认项。
- 未在代码或测试中确认的能力必须标记为“待实现/待确认”，不得根据项目名推断实现。
- `docs/quality`、专题文档和小写旧模块文档用于保存迁移、审查与深度实现资料，不代替模块入口。
