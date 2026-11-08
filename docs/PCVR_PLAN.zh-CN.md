# PC VR 滑翔验证计划（V0）

日期：2026-11-08。目标：Quest Pro 通过 Windows PC VR 验证滑翔中的观察、速度感、转弯和舒适度。用户已授权进入 PC VR；不接入 PUMP，不新增玩法。首次操作使用 Touch Pro 扳机模拟双绳拉量，没有真实绳长或阻力。

## 阶段与门槛

| 步骤 | 自动完成内容 | 验收 / 人工依赖 |
|---|---|---|
| V0.1 工程准备 | Windows OpenXR Loader、Touch/Touch Pro action asset、专用启动、头部位姿、暂停、HUD、诊断日志 | 原生编译；真实生成 settings/.meta；不等于头显成功 |
| V0.2 无头显检查 | 纯核心回归、真实 Windows 桌面全流程；无 runtime 时 VR 入口明确失败，默认桌面不启动 XR | 日志、退出码、截图；不使用 mock 结果冒充 Quest |
| V0.3 Link 与设备接通 | 提供官方安装器和预检查入口 | 用户完成安装/登录、将 Meta Link 设为活动 OpenXR Runtime，首次进入 Quest Link |
| V0.4 静止观察 | 游戏保持静止、暂停等待 A 确认 | 双眼画面、头部转动/平移、控制器左右与按键、HUD 可读；此项通过后才起飞 |
| V0.5 滑翔体验 | 复用现有飞行与目标路线、记录状态 | 直飞 → 小幅左右转 → 双拉制动 → 换目标 → 着陆；观察与操纵分离、舒适度和稳定性 |
| V0.6 故障恢复 | 追踪失效/摘头显/失去 XR 焦点时暂停，不自动继续 | 实际验证恢复追踪后双扳机松开、按 A 才继续；长帧也保留暂停 |

M1/M2 的全部验收不会因配置/编译通过而自动完成。每个门槛分别更新 STATE。

## 技术选择

- 两个 Windows 输出：`Builds/Windows` 桌面包不包含 XR Loader；`Builds/WindowsVR` 包含手动启动的 OpenXR，启动脚本传入 `--vr`。只关闭自动初始化仍可能触发原生插件预初始化，因此用构建隔离保持桌面不探测 runtime。
- Input System 的持久 action asset 在初始化 Loader 前启用；同时支持 Oculus Touch 和 Touch Pro profile，控制器模拟拉绳，无震动或力反馈输出。
- 第一轮采用 MultiPass，沿用当前灰盒 shader/TextMesh；这是兼容性基线，VR 帧率、两眼显示必须实测后再判断是否优化渲染方式。
- 启动脚本在 VR 模式请求 D3D11；头部每帧和渲染前更新。头部转动不写入飞行转向；相机不随伞翼倾斜滚转。
- 请求 Device tracking origin，按实际返回模式处理眼高；Floor 模式不重复加身高。实际站立/坐姿与重置中心仍须头显验证。
- HUD 在前方约 2 m，显示状态、目标角度/水平距离、AGL、速度、按键。它是否太近、太大或遮挡，要由佩戴测试判断。
- 追踪失效、运行时失焦或 userPresence（若设备提供）为 false 时暂停；恢复不自动解锁。记录只在本机 artifacts / LocalLow，不上传原始设备日志。

## 本机准备与阻碍

首次检查未发现 Meta Link 安装记录、OVRService 或 HKLM 的活动 OpenXR Runtime。官方安装器已下载到 `tools/downloads/MetaLinkSetup.exe`，Authenticode Valid，签名发行方 Meta Platforms, Inc.；安装窗口已打开，但未完成安装/登录。一次 UI 自动化未可靠推进，停止重复点击；不能称为已安装。

## 集中人工测试窗口

先完成 Link 安装和登录（如有 UAC，由用户处理），连接 Quest Pro 并进入 Quest Link；在 Link 的 General 中设置 Meta Link 为活动 OpenXR Runtime。首轮可优先用已有兼容 USB 3 数据线，若使用 Air Link，记录连接方式，不把链路延迟与游戏帧时间混为一谈。无需购买线材或启用手势/透视功能。

构建（在项目根目录；Editor/CLI 路径按本机安装调整）：

```powershell
.\tools\unity-cli\unity.exe build .\game --editor-path C:\Unity\6000.3.28f1\Editor\Unity.exe --target StandaloneWindows64 --execute-method Climb.EditorTools.DemoSceneBuilder.BuildPcVr --log-file .\artifacts\pcvr-build.log --no-tail --format json
```

也可用 Unity 菜单 **Climb > Build PC VR Test**。`BuildDesktopAndPcVr` 可在同一个 Editor 进程串行构建两个包。

运行：

```powershell
.\scripts\run-pcvr.ps1
```

初始静止暂停。右手 A：双扳机松开后，从高处起飞 / 恢复 / 落地后重试；右手 B：暂停；左手 X：换目标；左手 Y：回静止起点并暂停。扳机 L/R 左右转，双拉制动。首轮直接练飞，爬梯保留在桌面入口，不要求戴头显反复爬梯。

建议一次 10 分钟左右，实际时长按体验调整：

1. 先静止看前后左右、上下和轻微平移，确认世界稳定、双眼正确、HUD 能读。
2. 起飞后先不转弯，再短按左、右扳机；观察与转弯应彼此独立。
3. 共同制动、切换落点、着陆；不追求自动玩家的落点精度。
4. B 暂停后确认头部观察仍可工作；再检查控制器追踪恢复不会自动继续。
5. 记录“是否舒服、是否能预测转向、是否愿意再飞一次”。有不适即暂停/结束，不需要坚持完成整条路线。

回报最有用的信息：Link 是否连通、双眼是否正常、哪一步操作与预期不符、在哪种运动时不舒服。不要只给整体好/不好玩。

## 诊断入口

`run-pcvr.ps1 -Probe` 最多等待 45 s；应用探测通常在 15 s 内结束，0 表示取得当前有效追踪输入，4 表示不可用。它不是画面/舒适度验收。每次启动把活动 runtime 路径和 DLL 摘要写入 artifacts；Player 输出 PCVR 日志目录。原始日志含本机信息，默认不公开。

## 官方依据

- [Meta Quest Pro / Link 安装入口](https://www.meta.com/quest/setup/)
- [Meta：使用 Link 开发与设置活动 OpenXR Runtime](https://developers.meta.com/vr/documentation/unity/unity-link/)
- [Unity OpenXR 输入文档](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/input.html)

同时核对本机已解析 OpenXR 与 XR Management 包的 API 源码；固定依赖版本继续以 Packages 下文件为准。
