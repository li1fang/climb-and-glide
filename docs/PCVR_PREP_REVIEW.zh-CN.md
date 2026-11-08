# V0 PC VR 准备与无头显验收记录

日期：2026-11-08。对应 [阶段计划](PCVR_PLAN.zh-CN.md)。这是工程准备验收，不是 Quest Pro 实机验收。

## 已完成

- Windows OpenXR Loader、Touch/Touch Pro profiles、持久 Input System action asset、MultiPass 配置由 Unity Editor API 生成并保存，保留真实 .meta。
- `PcVrSession` 管理显式 XR 初始化、头部位姿、控制器输入有效性、失焦/失追踪暂停与本机诊断记录；现有滑翔规则未改。
- VR 前方 HUD 和控制器 A/B/X/Y 操作已实现，初始保持静止暂停；必须有效追踪且松开双扳机后按 A 才开始/恢复。
- `run-pcvr.ps1` 对正常启动先检查活动 runtime；`-Probe` 能在缺少 runtime 时返回失败，而不是把桌面画面当成 VR。
- PC VR 与桌面分别输出 WindowsVR / Windows，桌面包不带 XR Loader。

## 检查与发现

1. 纯核心及 Unity 兼容编译通过，Flight/Ladder/Valley/Startup 回归通过；本次没有改动纯游戏核心。
2. 初版共用 Windows 包即使关闭自动启动，OpenXR 原生插件仍会预初始化探测 runtime。`a2-20261108-094055` 和 `094551` 虽走完桌面路线，但原生日志有 XR_ERROR_RUNTIME_UNAVAILABLE；不作为最终隔离验收。
3. 原分析器漏掉 `[Error  ]` 原生日志格式，现已补上。它另将一条明确的视频解码软件回退提示作为警告保留；普通 Error 和 XR 原生错误仍失败，三个日志分类用例已验证。重新分析 `094551` 已标为失败，不能沿用旧的 passed 结果。
4. `pcvr-build-20261108-094952` 在同一 Unity CLI / Editor 进程中先后构建两个包，退出 0。桌面 boot.config 不含 xrsdk-pre-init-library；VR 包包含 UnityOpenXR。构建日志仍有此前已知的许可证握手恢复信息。
5. 最终 VR 包 `pcvr-20261108-095138` 探测：活动 runtime 为 null，原生 XR_ERROR_RUNTIME_UNAVAILABLE，程序记录 OpenXR unavailable，running=false、valid=false，退出码 4。脚本保留 session.csv 与 Player 日志；这是预期的无 runtime 失败路径通过，不是 VR 成功。
6. 最终独立桌面包 `a2-20261108-095149` 完整回放通过：6,774 帧，总长 113.28 s，飞行 64.06 s，落点距目标 1.67 m，最小 AGL 0；六张截图存在，已查看飞行中段和落地。日志没有 XR Runtime 探测错误，分析器 errors/warnings 均为空。全程最大帧时间 139.12 ms，飞行段最大 31.42 ms；锁定 60 FPS 的桌面数据不表示头显性能。
7. 无活动 runtime 时，正常 `run-pcvr.ps1` 在启动 Player 前明确拒绝；`-Probe` 才允许诊断启动。PowerShell 语法、文档链接、源码范围检查通过。

## 本机仍需用户完成

Meta Link 官方安装器已下载并验证 Meta Platforms, Inc. 签名；安装界面未被自动化可靠推进，因此安装/登录和 Link 激活尚未完成。没有修改系统活动 Runtime 注册表，没有配对或佩戴头显测试。

下一次集中窗口先完成软件安装/登录和 Link 连接，再按计划做静止观察、短直飞、小转弯、制动、落地和暂停恢复。USB/Air Link 方式及刷新率以实际设置记录。当前 session.csv 是约 4 Hz 的状态诊断快照，不能据此声称已经测出头显逐帧 P95 或端到端延迟。

PUMP、BLE、手势、新玩法均未接入。本机生成的包与原始日志继续留在忽略目录，公开仓库只保留源码与报告摘要。
