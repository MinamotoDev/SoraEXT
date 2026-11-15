# SoraEXT 0.1.0-release 安装包

构建：在仓库根目录使用 PowerShell 7 执行 `.\build-release.ps1`。

产物：`artifacts\SoraEXT-0.1.0-release\SoraEXT-0.1.0-release-setup.exe`。
关于页显示 `SoraEXT 0.1.0`。开发版构建仍显示 `Dev`。

## 结构

- `SoraEXT.Bootstrap`：自包含单文件 EXE，请求管理员权限，把内嵌 ZIP 解压至 Program Files 下唯一的临时目录，启动原生 WinUI 3 安装界面，退出后清理临时目录。
- `SoraEXT.Setup`：WinUI 3 控件、主题、Windows 11 Mica，提供许可、安装进度、错误和卸载界面。
- 主界面固定为 560 × 460 逻辑像素，按系统 DPI 缩放；禁止调整窗口大小和最大化。错误文字在图标与许可区之间显示，主界面不使用滚动或分页。
- 安装器跟随 Windows 用户显示语言，提供简体中文、繁体中文、英语、日语、韩语，其他语言回落为英语。
- 旧驱动等待重启清理时返回退出码 20，界面显示本地化提示“先前安装的组件未清除，请先重新启动。”。
- `Deploy.ps1`：调用原版 Ext4Fsd 0.71 安装器，检查驱动哈希及签名，部署应用和 SYSTEM 开机任务，注册 Windows 卸载入口。
- 已存在匹配驱动时复用；不同版本、不完整安装或等待重启的驱动删除会阻止安装并给出说明。
- `Setup.exe --uninstall`：移除应用、任务和快捷方式，保留数据与用户设置；驱动有独立的官方卸载入口。卸载程序自身在下次重启删除。
- 设置：`%ProgramData%\SoraEXT\settings.json`；安装、操作和开机日志：`%ProgramData%\SoraEXT\Logs`。界面、安装器和 SYSTEM 启动助手统一使用此目录。
- 安装包含 .NET / Windows App SDK 运行库，可离线安装；官方驱动保持原签名，SoraEXT 安装 EXE 尚未签名。

## 本机清理与人工测试

`Reset-TestInstallation.ps1` 只在用户明确确认后运行：备份驱动卷映射，调用官方驱动卸载器，移除当前安装及启动任务。完成后重启，再测试安装包。

人工验证：全新安装、取消、重复安装、已存在驱动、应用运行时拒绝更新、失败后重试、Windows 卸载入口，以及重启后的启动挂载。
`--verify` 只验证解包内容，不执行安装。
