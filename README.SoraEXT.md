# SoraEXT

Windows 分区管理界面。WinUI 3 / .NET 10；ext 使用官方已签名的 Ext4Fsd 驱动，NTFS / ReFS 使用 Windows 存储管理接口。

应用 ID：`app.minamotodev.soraext`。原始驱动项目仍在 `Ext4Fsd.sln`。

## 构建与运行

开发环境：

- Windows 10 2004 或更新版本，x64。
- .NET 10 SDK。
- Windows SDK、Visual Studio C++ 构建工具（WinUI XAML 编译所需）。
- NuGet 自动还原 Windows App SDK 2.6.5；无需管理员权限。
- 实际管理分区需要官方 Ext4Fsd 0.71 与 Ext2Srv。

```powershell
.\build-ui.ps1
.\build-ui.ps1 -Publish
.\artifacts\SoraEXT-Dev-0.1.0\SoraEXT.exe
```

发布目录包含 .NET 与 Windows App SDK 运行文件。分发时保留整个目录。
后续开发版使用 `SoraEXT-Dev-0.1.0`、`SoraEXT-Dev-0.2.0` 等目录名。
构建脚本按已存在的开发版目录自动递增中间版本号（例如 `0.9.0 → 0.10.0`、`0.99.0 → 0.100.0`），也可通过 `-Version 0.1.0` 指定；现有旧目录保留原名。
`build.ps1` 用于旧版 MFC 程序及未签名的源码驱动，与 WinUI 构建独立。

```powershell
# 仅演示界面，不连接或更改磁盘
.\artifacts\SoraEXT-Dev-0.1.0\SoraEXT.exe --demo

# 协议、配置及超级块解析检查
dotnet run --project SoraEXT.Tests -c Release

# 只读集成检查：枚举本机已挂载 ext 卷
dotnet run --project SoraEXT.Tests -c Release -- --scan
```

## 界面

- 分区：文件系统类型、容量、可用空间、访问模式、盘符、卷标识、开机挂载配置。
- 分区页不显示标题副文案；统计卡片固定显示“由 Ext4Fsd 驱动”。
- 保存和卸载成功后更新列表与运行记录，不显示成功提示条。
- 挂载设置：分配盘符、只读模式、开机挂载。
- 卸载：锁定卷、刷新数据、卸载文件系统、移除全局盘符。
- NTFS：显示分区信息，仅提供“在文件资源管理器中打开”操作。
- ReFS：通过 `MSFT_Partition` 的 `AddAccessPath` / `RemoveAccessPath` 分配或移除盘符，由 Windows 保存映射。移除盘符不执行文件系统卸载或硬件弹出。
- EFI、MSR、恢复和系统隐藏分区不显示；Windows 主分区仍可查看并在文件资源管理器中打开。
- Linux 数据分区优先按 GPT 类型 / MBR `0x83` 识别：Windows 标记的 `IsHidden` 不会将这类 ext 扫描目标排除。文件系统类型和 UUID 由超级块确认。
- 详情移除重复的标题和磁盘描述行，缩紧间距；小窗口保留滚动。列表和详情内容为滚动条预留独立空间，列表选中项采用原生圆角样式。
- 运行记录：保留本次会话最近 100 条记录。
- 设置：跟随系统 / 浅色 / 深色、云母背景、激活时刷新。
- 语言：默认跟随 Windows 显示语言，选项顺序为跟随系统、English、日本語、한국어、简体中文、正體中文。选择后重启生效；应用文案、动态状态、操作提示与无障碍名称均提供本地化。用户卷标、设备标识和技术代码保持原样，数字格式遵循用户区域设置。
- `F5` 刷新，Tab 键导航，图标操作具备工具提示与无障碍名称。

应用启动要求管理员权限，Windows 在启动时请求 UAC 授权；取消授权则不启动。已提升权限的启动环境直接运行。界面读取物理分区超级块并执行管理操作。

配置对话框只在点击“保存”后写入。应用会重查 UUID 和设备路径、验证盘符占用，并在失败时尝试恢复原有配置。保留 Ext2Mgr 设置的 UID/GID、字符编码和文件名过滤规则。

## 开机静默挂载

`SoraEXT.Agent` 是独立的无窗口程序，通过官方 Ext2Srv 建立全局盘符。
按 ext 文件系统 UUID 查找分区，读取官方注册表中的 `MountPoint` 配置；启动后最多重试约两分钟，等待磁盘和驱动就绪。之后退出。

在用户主动打开的管理员 PowerShell 中运行：

```powershell
.\Setup\Install-SoraEXT.ps1 -Source .\artifacts\SoraEXT-Dev-0.1.0
```

此脚本：

1. 将发布文件复制到 `C:\Program Files\MinamotoDev\SoraEXT`。
2. 创建 `app.minamotodev.soraext.mount` 开机任务，以 SYSTEM 运行启动助手。
3. 设置 Ext2Srv 自动启动，添加开始菜单快捷方式。

启动任务采用受保护的 Program Files 路径。驱动由官方安装包安装。
日志位于 `%ProgramData%\SoraEXT\Logs`。
界面偏好位于 `%ProgramData%\SoraEXT\settings.json`。

本机于 2026-11-15 将 `0.4.0` 部署至上述 Program Files 路径并注册开机任务。
首次手动触发时，助手识别到 `Spare` 已挂载为 F:，保留现有映射；普通权限扫描核对通过。

已有 Ext2Mgr 或 Session Manager 挂载配置可能也会恢复盘符。助手遇到已挂载卷会保留当前挂载。
关闭 SoraEXT 窗口不会卸载分区。回收站由 Windows Shell 和文件系统驱动处理。

## 性能设计

- 物理盘读取、WMI 枚举、服务等待与管道通信放在后台线程。
- 扫描与管理操作串行化，界面显示忙状态，防止重复提交。
- 列表使用 ListView 虚拟化与差量更新；保留未变化的项目。
- 返回窗口刷新间隔至少 15 秒；空闲时没有扫描计时器。
- 页面和详情切换使用 200–250 ms 合成层淡入、上移动画；列表增删和重排使用 WinUI ThemeTransition。实时遵循系统动画开关。
- 数据刷新原位更新列表项，保留选择与容器，避免容量变化反复触发入场动画。
- 原生 NavigationView、ContentDialog、ToggleSwitch、ProgressBar；Segoe Fluent 图标。
- 界面尺寸使用 DPI 换算，窄窗口切为上下布局。
- 首次渲染回调耗时和扫描耗时显示在运行记录中。

## 验证范围

已完成 Release 构建、协议/配置测试、普通权限读取 F:、深浅主题与挂载对话框界面检查。
完整自包含发布已验证启动，包含 UI、启动助手及应用 XAML 资源索引。
2026-11-14 重启后复测：24 项协议/配置检查通过；发布版窗口正常响应，单次工作集采样约 162 MiB。
本次重启后未检测到 F: 盘符，扫描得到 0 个已挂载 ext 卷；此前 F: 读取检查通过，恢复挂载后需重新验证。
挂载设置写入、卸载及开机任务需要管理员集成验证；本轮无人值守开发没有执行这些操作。
后续管理员卸载实测发现阻塞：原生线程堆栈显示 `NtFsControlFile → DeviceIoControl`，
控制码为 `0x90018`（`FSCTL_LOCK_VOLUME`），尚未执行卸载文件系统和移除盘符。
诊断版增加逐步状态、15 秒等待提示及操作日志（当前路径为 `%ProgramData%\SoraEXT\Logs\operations-日期.log`）。
资源管理器通知已改为 `SHCNF_FLUSHNOWAIT`；该改动处理通知等待，不解决已确认的驱动锁卷阻塞。
`--demo` 使用明确标记的演示数据。实际启动默认读取本机分区。

新增 Windows 后端已完成只读集成核对：识别 NTFS 主分区及数据分区并过滤隐藏恢复分区，容量和文件系统与 `DriveInfo` 一致。本机未发现 ReFS 卷，其盘符变更路径尚未进行实际写入集成验证。
四语言资源完整性、格式占位符一致性、语言回退及上述只读集成检查共 44 项通过。
`0.2.0` 修正 Linux 数据分区被 `IsHidden` 误过滤的问题；新增回归检查，并以管理员只读扫描验证 `Spare` 的超级块、UUID 和 ext4 类型，合计 49 项检查通过。
可使用 `--demo --language zh-Hans` / `zh-Hant` / `ja` / `ko` / `en` 检查预览界面；语言覆盖仅在演示模式生效，不更改已保存的语言设置。

## 许可与参考

新增代码采用 GPL-2.0，与上游项目许可保持一致；原作者版权见 `Ext4Fsd/COPYRIGHT.TXT`。
微软 SDK、运行时和字体采用各自许可。

- [Ext4Fsd](https://github.com/bobranten/Ext4Fsd)
- [NavigationView](https://learn.microsoft.com/windows/apps/develop/ui/controls/navigationview)
- [Fluent motion](https://learn.microsoft.com/windows/apps/develop/motion/motion-in-practice)
- [Windows App SDK 部署](https://learn.microsoft.com/windows/apps/windows-app-sdk/deploy-unpackaged-apps)
