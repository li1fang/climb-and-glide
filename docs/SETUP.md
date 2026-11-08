# 安装与复现

当前验收范围见 [STATE](STATE.zh-CN.md)。本文件是克隆后的操作入口，不是安装历史。

PC VR 独立的准备、启动与佩戴验收步骤见 [PCVR_PLAN](PCVR_PLAN.zh-CN.md)。以下桌面命令仍为默认入口；桌面包排除 XR Loader，独立的 WindowsVR 包由专门脚本启动。

## 环境

- Windows、Git、PowerShell。
- Unity Editor：读取 `game/ProjectSettings/ProjectVersion.txt` 并安装同一版本；通过 [Unity Hub](https://unity.com/download) 登录并激活适合自己的许可证。
- Unity CLI：按 [官方安装说明](https://docs.unity.com/en-us/unity-cli/use-unity-cli) 安装独立版本。脚本默认查找 `tools/unity-cli/unity.exe`，也可通过 `-UnityCli` 传入完整路径。本机 A2 使用 beta.17；这是验收工具版本，不声称其他版本已验证。
- Node.js：用于 CSV 分析脚本，本机验证为 v24.22.1；无 npm 包或 npm install 步骤。
- 离线测试 SDK：由 `scripts/setup-dotnet.ps1` 下载到仓库的 tools，版本与官方摘要在脚本内维护；不修改系统 PATH。

Unity CLI 仍为实验性工具；安装方法以其官方文档为准。包声明和实际依赖以 manifest/lock 为准，不手工复制 PackageCache。

## 纯逻辑检查

从仓库根目录执行：

```powershell
.\scripts\setup-dotnet.ps1
.\scripts\test-core.ps1
.\scripts\test-core.ps1 -Trial
```

这些命令不启动 Unity 或设备。先编译 Unity 所用的 .NET Standard/C# 兼容目标，再运行离线行为检查；`-Trial` 增加完整流程回放。输出在 artifacts 下。不要同时运行两份测试脚本。

## 编辑器与实际画面

1. 用对应 Editor 打开 `game`，等待 Package Manager 解析和原生编译。保留实际生成的 `.meta` 和锁文件。
2. 使用 **Climb > Create or Open Demo Scene**，按 Play；或 **Climb > Build Windows Demo**。
3. CLI 构建和自动化试用：

```powershell
# 使用本机默认目录，或显式传入自己的路径：
.\scripts\run-a2.ps1 -Editor 'D:\Unity\Editor\Unity.exe' -UnityCli 'D:\Tools\UnityCLI\unity.exe'
# 同一个已构建二进制的左侧路线，不需要 Editor/CLI：
.\scripts\run-a2.ps1 -SkipBuild -LeftTarget
```

关闭同一项目的其他 Editor 再运行批处理构建。脚本构建后启动可见 Windows Player，等待自动结束，记录 CSV、截图、日志与二进制 SHA-256；仍需要查看截图做视觉验收。`-SkipBuild` 不检查源码是否比现有包新，不可用来宣称最新源码已编译。

手动运行 `game/Builds/Windows/ClimbDemo.exe`：Q/E 模拟拉绳，Space 平台起飞，F 直接飞行，C 回到梯子，Tab 换目标，鼠标右键观察。完整按键见 README。

[人工桌面试玩说明](DESKTOP_PLAYTEST.zh-CN.md) 包含五分钟建议流程。自动试用启动会等待至少 3 秒总预热、连续至少 1 秒且 60 帧稳定帧（每帧不超过 100 ms），在当前已验证帧开始运行；45 秒仍不稳定则失败。正式运行仍保留核心长帧暂停，并输出 failure.json、退出码 3，失败证据也复制到 artifacts。

可控故障复现（仅 `--auto-trial` 有效，人工操作不会触发）：

```powershell
.\scripts\run-a2.ps1 -SkipBuild -Stall Startup # 就绪边界阻塞 2300 ms，预期恢复并跑完整路线
.\scripts\run-a2.ps1 -SkipBuild -Stall Runtime # 正式运行后阻塞 2300 ms，预期失败并保存 session_paused
```

启动卡顿不占用游戏时间；正式运行的卡顿不允许悄悄跳过。startup.json 记录启动时间、稳定窗口中断次数和是否注入故障。历史 A2 包没有此文件，新分析器针对修复后构建，分析旧证据请使用对应旧提交。

## 范围与故障定位

- 当前画面使用 Resources 内的自定义灰盒 shader；安装 URP 包不表示实际启用 URP 管线。
- OpenXR 包存在不等于 PC VR 已配置或执行；本轮无需头显、Meta Link 或 PUMP。
- 构建失败先查当次 artifacts 目录中的 build.log；首次导入失败不要用离线测试通过替代 Unity 原生编译。
- 原始日志可能有本机路径及授权诊断信息，不直接上传公开 issue。
- `scripts/run-a1.ps1` 仅为兼容入口，转发当前 A2；历史 A1 结果不是当前源码的独立构建目标。

## 历史

[早期安装与版本调查](archive/SETUP_A0-A1.md) 保留原始现场，内容不作为当前待办。A1/A2 实际执行结果见各自复盘。
