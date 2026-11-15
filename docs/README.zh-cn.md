<p align="center">
  <img src="../Assets/SoraEXT.png" width="160" alt="SoraEXT 图标">
</p>

<h1 align="center">SoraEXT</h1>

[English](../README.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [简体中文](README.zh-cn.md) · [正體中文](README.zh-tw.md)

<p align="center">Windows 原生 Linux 分区挂载与管理工具</p>

<p align="center">
  <a href="../RELEASE_NOTE.md#zh-cn">版本说明</a> ·
  <a href="https://github.com/MinamotoDev/SoraEXT/releases">发布页面</a> ·
  <a href="https://github.com/MinamotoDev/SoraEXT/issues">问题反馈</a>
</p>

SoraEXT 使用 **WinUI 3** 构建原生 Windows 界面，通过官方签名的 **Ext4Fsd** 驱动访问 EXT2、EXT3 和 EXT4 分区，提供盘符管理、只读模式及开机静默挂载。

无需 WSL 或虚拟机。界面、开机挂载助手和文件系统驱动独立运行，关闭窗口后仍可访问已挂载的分区。

## 功能

- **分区信息**：查看文件系统、容量、可用空间、盘符、访问模式和卷标识。
- **EXT 挂载**：选择盘符、设置只读或读写模式，并配置开机挂载。
- **开机静默挂载**：通过 SYSTEM 计划任务按文件系统 UUID 恢复配置的盘符，无需打开主界面。
- **Windows 分区**：查看 NTFS 分区并在文件资源管理器中打开；为 ReFS 提供盘符分配与移除功能。
- **分区筛选**：隐藏 EFI、MSR、恢复及 Windows 隐藏分区；继续扫描被 Windows 标记为隐藏的 Linux 数据分区。
- **基于 WinUI 构建**：支持系统、浅色和深色主题，以及受支持系统上的云母背景与原生动画。
- **自动刷新**：返回窗口时自动刷新，刷新间隔至少 15 秒；也可按 `F5` 手动刷新。

SoraEXT 当前侧重挂载和访问管理，不提供分区创建、删除、格式化或容量调整。

## 系统要求

| 项目 | 要求 |
| --- | --- |
| 系统 |  Windows 11 |
| 架构 | x64 |
| 权限 | 需要 UAC 授权 |
| 文件系统驱动 | Ext4Fsd 0.71，安装包已包含 |
| 运行库 | 安装包自带 .NET 和 Windows App SDK 运行文件 |

## 安装与使用

1. 运行安装包并确认 UAC 授权。
2. 阅读并接受许可协议，点击“安装”。
3. 安装完成后，点击“启动”打开 SoraEXT。
4. 选择 EXT 分区，点击“挂载”，设置盘符及访问模式后保存。
5. 如需开机恢复，在该分区的挂载配置中启用“开机挂载”。

安装器会部署应用、官方驱动与启动助手，创建开始菜单快捷方式和 Windows 卸载入口。已有匹配版本的驱动会被验证并复用。

### 安装位置与数据

| 内容 | 默认路径 |
| --- | --- |
| 应用 | `C:\Program Files\MinamotoDev\SoraEXT` |
| 设置 | `%ProgramData%\SoraEXT\settings.json` |
| 安装日志 | `%ProgramData%\SoraEXT\Logs\setup.log` |
| 操作日志 | `%ProgramData%\SoraEXT\Logs\operations-YYYY-MM-DD.log` |
| 开机挂载日志 | `%ProgramData%\SoraEXT\Logs\mount-YYYY-MM-DD.log` |

开机任务名称为 `SoraEXTMount`。

### 卸载

通过 Windows“已安装的应用”卸载 SoraEXT，可移除应用、开机任务及快捷方式，保留用户设置与日志。

Ext4Fsd 使用独立的官方卸载入口；需要移除驱动时，请另外卸载并按提示重启。

## 已知问题

- **EXT 卸载可能阻塞**：驱动可能停留在 `FSCTL_LOCK_VOLUME` 锁卷阶段。
- **EXT 分区回收站**：当前存在回收站功能异常。

## 从源码构建

### 开发环境

- Windows x64。
- .NET 10 SDK。
- PowerShell 7。
- Visual Studio C++ 构建工具及 Windows SDK，用于 WinUI XAML 编译。
- 首次构建需要联网还原 NuGet 依赖。

主界面和安装器使用 Windows App SDK `2.6.5`。

### 构建应用

在仓库根目录执行：

```powershell
# 构建应用、启动助手并运行检查
.\build-ui.ps1

# 生成自包含开发版目录
.\build-ui.ps1 -Publish
```

开发版输出到 `artifacts\SoraEXT-Dev-0.N.0`，脚本根据已有目录自动递增版本。运行时需保留完整发布目录。

### 构建安装包

```powershell
# 准备官方驱动安装包的缓存目录
New-Item -ItemType Directory -Path .\artifacts\downloads -Force

.\build-release.ps1 `
  -DriverInstaller .\artifacts\downloads\Ext2Fsd-0.71-setup.exe
```

脚本会在指定文件不存在时下载官方安装包，并校验 SHA-256。完成后输出：

```text
artifacts\SoraEXT-0.1.0-release\SoraEXT-0.1.0-release-setup.exe
```

重新打包前请关闭正在运行的同路径安装器。

### 检查与界面预览

```powershell
# 协议、配置、超级块解析与本地化检查
dotnet run --project SoraEXT.Tests -c Release

# 只读扫描本机分区
dotnet run --project SoraEXT.Tests -c Release -- --scan
```

发布后的 `SoraEXT.exe` 支持 `--demo` 界面预览；可追加 `--language zh-Hans`、`zh-Hant`、`en`、`ja` 或 `ko`。演示模式使用示例数据。

## 项目结构

| 目录 | 内容 |
| --- | --- |
| `SoraEXT.App` | WinUI 3 主界面 |
| `SoraEXT.Core` | 分区扫描、驱动协议及挂载配置 |
| `SoraEXT.Agent` | 开机静默挂载助手 |
| `SoraEXT.Setup` | WinUI 3 安装器界面 |
| `SoraEXT.Bootstrap` | 单文件安装包启动器 |
| `SoraEXT.Tests` | 协议、配置及本地化检查 |
| `Setup` | 部署、打包辅助脚本与安装说明 |
| `Assets` | 应用图标 |
| `Ext4Fsd`、`Ext2Srv`、`Ext2Mgr` | 上游驱动、服务及管理工具源码 |

旧版工具与源码驱动通过 `build.ps1` / `Ext4Fsd.sln` 构建，与 WinUI 项目独立。发行安装包使用官方签名驱动。

## 项目来源与许可

SoraEXT 基于 [bobranten/Ext4Fsd](https://github.com/bobranten/Ext4Fsd) 开发；Ext4Fsd 源自 Matt Wu 的 Ext2Fsd 项目。

新增代码采用 **GPL-2.0**。上游许可与版权声明见 [Ext4Fsd/COPYRIGHT.TXT](../Ext4Fsd/COPYRIGHT.TXT)，原始项目说明保留于 [README.Ext4Fsd.md](../README.Ext4Fsd.md)。第三方 SDK、运行时及相关组件遵循各自许可。

感谢 Ext2Fsd、Ext4Fsd 的作者与贡献者。
