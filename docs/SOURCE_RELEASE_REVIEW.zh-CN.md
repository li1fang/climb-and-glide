# SSOT 与首次源码发布整理记录

日期：2026-11-07。

## 修改

- 当前验收状态集中到 STATE；安装记录归档，SETUP 改为新电脑复现入口。
- 加入 MIT 许可证、贡献说明、第三方依赖说明和源码清单检查。用户批准 `li1fang/climb-and-glide` 为临时仓库名，未来可以更名。
- `run-a2.ps1` 接受 `-UnityCli`；`-SkipBuild` 只需已有 Player 与 Node，不再要求本机安装 Editor/CLI。
- 未改游戏运行时代码，未重建 Player；这次试用检验的是脚本与此前已构建的 A2 二进制。

## 验证

- 核心兼容编译通过，0 警告/0 错误；FlightChecks、LadderChecks、ValleyChecks 全部通过。
- PowerShell 文件语法检查通过；仓库 Markdown 相对链接检查通过。
- 源码清单与常见敏感内容模式检查通过。正式 LICENSE 加入前，`-RequireLicense` 已验证会拒绝缺失许可证；加入后通过。
- `artifacts/a2-20261107-080702`：显式传入不存在的 Editor/CLI 路径并用 `-SkipBuild`，Player 正常启动，但首个记录帧 dt 为 2.284498 s，触发核心长帧暂停，爬升保持 0。诊断后停止此次试用进程，退出 -1；保留部分 CSV/截图/日志。**这次不是成功验收。**
- `artifacts/a2-20261107-080951`：独立重跑相同二进制和参数，通过完整流程与分析器，退出 0。飞行 64.06 s，落点距目标 1.68 m，最小 AGL 0；六张阶段截图存在，复查落地截图。此结果验证 `-SkipBuild` 无需 Editor/CLI 的入口，但不消除上一次启动长帧的已知问题。

## 公开发布核验

GitHub 已读回确认 `li1fang/climb-and-glide` 为 PUBLIC，默认分支 main，识别许可证为 MIT；首个提交为 `87c26f07579b91f9596cb83cb9784ae439d1beda`，105 个源码/文档/设置文件。远端清单无 tools、artifacts、Library 或 Builds。未发布可下载 Player Release。

此问题属于此前固定预热策略的可靠性限制；没有为了让本次测试通过而解除暂停保护。当前源码基线可发布，但自动化启动仍需改进。原始本机记录保留在忽略目录中，公开仓库只包含摘要。
