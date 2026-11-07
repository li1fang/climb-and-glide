# 首次开源准备

日期：2026-11-07。范围为 A2 桌面源码基线，不发布硬件控制，也不把 VR 标为已完成。

## 可审阅的发布提案

- 仓库：GitHub `li1fang/climb-and-glide`，public；用户已批准。当前是工作名，未来再选更适合游戏的名字。
- 描述：Personal Unity ladder-climbing and paragliding demo with synthetic dual-rope input.
- 许可证：用户已选择 MIT，正式文本见根目录 LICENSE。
- 内容：源码、真实 Unity `.meta`、项目/依赖锁定文件、离线检查、自动试用脚本、SSOT 和历史复盘。
- 不包含：Unity/.NET/CLI 安装包、包缓存、构建产物、原始运行日志、许可证/凭据、其他项目、设备数据。
- 首发仅源码；原始运行编号保留在报告中，本机证据不随克隆提供。克隆者可以重新生成自己的试用记录。

## 本轮处理

- 当前状态收敛到 [STATE](STATE.zh-CN.md)，安装文档重写为克隆后的操作步骤；旧记录保留在 archive。
- 补充贡献说明和第三方依赖说明；保留 Package Manager 的实际解析结果。
- 扩充忽略规则，新增 `scripts/check-source.ps1`：枚举 Git 候选文件、限制发布范围、检查异常大文件及常见敏感信息模式、输出逐文件 SHA-256 清单。
- 试用脚本接受外部 CLI 路径；已有构建的 `-SkipBuild` 不再要求安装 Editor/CLI。
- 不改游戏行为。原生 A2 验收仍对应历史报告中的二进制；本轮验证文档、源码清单和脚本入口。

## 发布前门槛

1. 已完成：作者确定仓库归属/名称及 MIT 许可证，正式 LICENSE 已加入。
2. `scripts/check-source.ps1 -RequireLicense` 通过，人工复查清单。模式检查不保证发现所有敏感信息。
3. 只提交清单内源码；检查暂存差异，再创建 public 远端并推送。不要推送本地 artifacts 或 tools。
4. 读回 GitHub 仓库和提交，确认 README、许可与文件列表；记录首个公开提交。

当前尚未创建公开仓库、推送代码或发布 Release；这份准备稿不能称为已开源。
