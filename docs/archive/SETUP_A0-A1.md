# 本机工具与首次 Unity 导入

> 归档：保留早期安装调查，不作为当前操作说明。当前入口为 [SETUP](../SETUP.md)，验收状态见 [STATE](../STATE.zh-CN.md)。

> 当时更新（2026-11-07）：Unity、CLI 已安装配置，原生构建与真实 Windows 画面 A1 试用通过。参见 [A1 复盘](../A1_REVIEW.zh-CN.md)。以下保留先前阶段记录，其中“未安装/未编译/未渲染”是历史状态。

核对日期：2026-11-07（Asia/Bangkok）。本轮只准备了源码工程与仓库内的 .NET 测试工具；没有安装 Unity、Meta Horizon Link，也没有连接头显或 PUMP。

## 已准备的工具

- Unity 工程目录：`game/`。
- Editor 固定为 **6000.3.28f1**，revision **8f082d2fe476**。该版本由 Unity 官方 Release API 与发布页共同核对，2026-11-03 发布。
- 本地 .NET SDK：**8.0.427**，位于 `tools/dotnet/`；仅用于不依赖 UnityEngine 的逻辑测试，不是游戏发布依赖。
- SDK ZIP 为 285,280,040 字节，SHA-512 已与微软发布元数据比对；下载来源、UTC 时间和 SHA-256/SHA-512 保存在 `tools/downloads/dotnet-sdk-source.json`。
- Unity Hub **3.22.6** Windows x64 安装器已下载到 `tools/downloads/UnityHubSetup-x64.exe`，大小 **193,133,232 字节**。Windows Authenticode 检查为 **Valid**，签名发行方为 **Unity Technologies SF**；没有运行安装器。来源与签名记录在 `tools/downloads/unity-hub-source.json`。
- 未修改系统 PATH，未系统安装 .NET。`.NET` 不是 Unity Editor 的替代品。

在 PowerShell 中运行纯逻辑检查：

```powershell
Set-Location C:\projects\climb
.\scripts\test-core.ps1
```

脚本使用本地 SDK，关闭遥测，清空 NuGet 包源并把缓存放在 `tools/`。先以 .NET Standard 2.1 / C# 8 编译核心，使用 SDK 自带的参考程序集检查 API 和语言兼容性，再运行 .NET 8 console 逻辑测试。两个项目的中间文件分别存储，整个脚本由一个进程顺序执行。项目不引用 NuGet 包，也不扫描或连接任何设备。测试通过只表示纯 C# 核心通过这些检查，不表示 Unity 已编译、场景已渲染或 VR 已验收。

在另一台电脑或工具目录缺失时，可运行 `scripts/setup-dotnet.ps1` 下载同一官方 SDK 并按固定 SHA-512 校验。它不会覆盖非空或不同版本的工具目录，也不会系统安装；下载失败的临时文件会保留供检查。

## Unity 与 Link 待安装项

1. 人工运行已验证签名的 `tools/downloads/UnityHubSetup-x64.exe` 安装 Hub。来源为 [Unity 官方下载入口](https://unity.com/download) 对应的 [Windows x64 CDN 安装包](https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup-x64.exe)。这个流动 URL 可能更新，重新下载时应重新检查来源及签名。
2. 用户在 Hub 完成登录，并按实际资格激活许可证；个人项目不自动等于符合所有免费版条款。
3. 在 Hub 安装 [Unity 6000.3.28f1](https://unity.com/releases/editor/whats-new/6000.3.28f1)。官方 API 给出的 Windows x64 Editor 下载约 **4.21 GB**、安装约 **8.39 GB**；Hub、包缓存和其他组件另计。首轮只需要 Windows 桌面，Android/iOS/WebGL 工具暂不加装。
4. 用 Hub 将 `C:\projects\climb\game` 加入项目列表并打开，等待 Package Manager 实际解析依赖。
5. 完成首次导入、编译和桌面场景检查后，再按 [Meta 官方 Link 开发说明](https://developers.meta.com/vr/documentation/unity/unity-link/) 安装/配置 Link 与 OpenXR runtime，测试头显基础画面。

不要把原生 Quest 功能支持直接当作 Link 功能支持。手势等扩展需在实际 PC VR 路径单独验证。

本次 Hub 下载时间为 `2026-11-06T19:59:16.8707871Z`；SHA-256 为 `2bd8b7086c0bc986f033a31b55bc5d1f5452d10cab66b5f41c98ab5d84cabfa2`。签名证书 thumbprint 为 `228FB6411B0A144478C86AAA3CD9473C43A8ABA7`。这只是安装器来源与完整性证据，M1 环境仍需完成安装、许可、实际工程导入与桌面运行验证。

## 初始包集合

`game/Packages/manifest.json` 是待 Editor 首次解析的版本种子，不是已经在 Editor 中验证的 lockfile。

| 包 | 固定版本 | 核对依据 |
|---|---|---|
| Universal Render Pipeline | 17.3.0 | Unity 官方 Graphics `6000.3/staging` 包元数据指定 Unity 6000.3 |
| Input System | 1.20.1 | Unity 6000.3 官方包文档和注册表 |
| XR Plugin Management | 4.6.1 | Unity 6000.3 官方包文档和注册表 |
| OpenXR Plugin | 1.16.1 | Unity 6000.3 官方 released 版本，优先于盲取 registry latest |
| Test Framework | 1.4.6 | Unity 官方注册表的正式版本；首次 Editor 导入仍需核对与内置 core-package 解析结果 |

另外明确声明当前灰盒所需的内置 audio、imgui、jsonserialize、physics、ui、uielements、xr 模块。

灰盒 TextMesh/字体所需的模块引用仍需以实际 Editor 导入编译确认；没有添加未经核实的独立 `com.unity.modules.textrendering` 包。

尚未手工生成 `packages-lock.json`，也没有预设 XR Loader、Interaction Profile 或 URP 管线资源为已配置。Editor 成功解析后保留真实 lockfile；如果 core package 版本需要由此 Editor 调整，记录实际版本和原因再提交，不伪造解析结果。

XR Interaction Toolkit、全量 Meta SDK 和第三方蓝牙插件都未加入；具体交互需要它们时再选择与 Editor 相容的正式版本。测试脚本不会因为 Unity 未安装而把纯逻辑结果写成 M1 完成。

## 来源

- [Unity Release API](https://services.api.unity.com/unity/editor/release/v1/releases?limit=1&version=6000.3.28f1)
- [Unity 6000.3.28f1 发布说明](https://unity.com/releases/editor/whats-new/6000.3.28f1)
- [官方 Graphics 6000.3 URP 元数据](https://github.com/Unity-Technologies/Graphics/blob/6000.3/staging/Packages/com.unity.render-pipelines.universal/package.json)
- [Input System 官方包文档](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.inputsystem.html)
- [XR Management 官方包文档](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.xr.management.html)
- [OpenXR 官方包文档](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.xr.openxr.html)
- [Unity 包注册表](https://packages.unity.com/com.unity.test-framework)
- [微软 .NET 8 发布元数据](https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json)
