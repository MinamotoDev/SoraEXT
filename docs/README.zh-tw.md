<p align="center">
  <img src="../Assets/SoraEXT.png" width="160" alt="SoraEXT 圖示">
</p>

<h1 align="center">SoraEXT</h1>

[English](../README.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [简体中文](README.zh-cn.md) · [正體中文](README.zh-tw.md)

<p align="center">Windows 原生 Linux 分割區掛載與管理工具</p>

<p align="center">
  <a href="../RELEASE_NOTE.md#zh-tw">版本說明</a> ·
  <a href="https://github.com/MinamotoDev/SoraEXT/releases">發行版本</a> ·
  <a href="https://github.com/MinamotoDev/SoraEXT/issues">問題回報</a>
</p>

SoraEXT 使用 **WinUI 3** 建構原生 Windows 介面，透過官方簽署的 **Ext4Fsd** 驅動程式存取 EXT2、EXT3 和 EXT4 分割區，提供磁碟機代號管理、唯讀模式及開機靜默掛載。

無需 WSL 或虛擬機器。介面、開機掛載助手和檔案系統驅動程式各自獨立執行，關閉視窗後仍可存取已掛載的分割區。

## 功能

- **分割區資訊**：檢視檔案系統、容量、可用空間、磁碟機代號、存取模式和磁碟區識別碼。
- **EXT 掛載**：選擇磁碟機代號、設定唯讀或讀寫模式，並設定開機掛載。
- **開機靜默掛載**：透過以 SYSTEM 身分執行的排程工作，依檔案系統 UUID 還原設定的磁碟機代號，無需開啟主介面。
- **Windows 分割區**：檢視 NTFS 分割區並在檔案總管中開啟；為 ReFS 提供磁碟機代號指派與移除功能。
- **分割區篩選**：隱藏 EFI、MSR、復原及 Windows 隱藏分割區；繼續掃描被 Windows 標記為隱藏的 Linux 資料分割區。
- **以 WinUI 建構**：支援系統、淺色和深色佈景主題，以及支援系統上的雲母背景與原生動畫。
- **自動重新整理**：返回視窗時自動重新整理，間隔至少 15 秒；也可按 `F5` 手動重新整理。

SoraEXT 目前著重於掛載和存取管理，不提供分割區建立、刪除、格式化或容量調整。

## 系統需求

| 項目 | 需求 |
| --- | --- |
| 系統 | Windows 11 |
| 架構 | x64 |
| 權限 | 需要 UAC 授權 |
| 檔案系統驅動程式 | Ext4Fsd 0.71，安裝程式已包含 |
| 執行階段 | 安裝程式內含 .NET 和 Windows App SDK 執行階段檔案 |

## 安裝與使用

1. 執行安裝程式並確認 UAC 授權。
2. 閱讀並接受授權條款，按一下「安裝」。
3. 安裝完成後，按一下「啟動」開啟 SoraEXT。
4. 選取 EXT 分割區，按一下「掛載」，設定磁碟機代號及存取模式後儲存。
5. 如需開機還原，請在該分割區的掛載設定中啟用「開機掛載」。

安裝程式會部署應用程式、官方驅動程式與啟動助手，建立開始功能表捷徑和 Windows 解除安裝項目。若已有相符版本的驅動程式，會驗證並沿用。

### 安裝位置與資料

| 內容 | 預設路徑 |
| --- | --- |
| 應用程式 | `C:\Program Files\MinamotoDev\SoraEXT` |
| 設定 | `%ProgramData%\SoraEXT\settings.json` |
| 安裝記錄 | `%ProgramData%\SoraEXT\Logs\setup.log` |
| 操作記錄 | `%ProgramData%\SoraEXT\Logs\operations-YYYY-MM-DD.log` |
| 開機掛載記錄 | `%ProgramData%\SoraEXT\Logs\mount-YYYY-MM-DD.log` |

開機工作名稱為 `SoraEXTMount`。

### 解除安裝

透過 Windows「已安裝的應用程式」解除安裝 SoraEXT，可移除應用程式、開機工作及捷徑，保留使用者設定與記錄。

Ext4Fsd 有獨立的官方解除安裝程式；需要移除驅動程式時，請另外解除安裝並依提示重新啟動。

## 已知問題

- **EXT 解除掛載可能停滯**：驅動程式可能停留在 `FSCTL_LOCK_VOLUME` 鎖定磁碟區階段。
- **EXT 分割區資源回收筒**：目前資源回收筒功能有異常。

## 從原始碼建置

### 開發環境

- Windows x64。
- .NET 10 SDK。
- PowerShell 7。
- Visual Studio C++ 建置工具及 Windows SDK，用於編譯 WinUI XAML。
- 首次建置需要連線還原 NuGet 相依套件。

主介面和安裝程式使用 Windows App SDK `2.6.5`。

### 建置應用程式

在儲存庫根目錄執行：

```powershell
# 建置應用程式、啟動助手並執行檢查
.\build-ui.ps1

# 產生自包含開發版目錄
.\build-ui.ps1 -Publish
```

開發版輸出至 `artifacts\SoraEXT-Dev-0.N.0`，指令碼會根據現有目錄自動遞增版本。執行時需保留完整發佈目錄。

### 建置安裝程式

```powershell
# 準備官方驅動安裝程式的快取目錄
New-Item -ItemType Directory -Path .\artifacts\downloads -Force

.\build-release.ps1 `
  -DriverInstaller .\artifacts\downloads\Ext2Fsd-0.71-setup.exe
```

指定檔案不存在時，指令碼會下載官方安裝程式並驗證 SHA-256。完成後輸出：

```text
artifacts\SoraEXT-0.1.0-release\SoraEXT-0.1.0-release-setup.exe
```

重新打包前，請關閉從同一路徑執行的安裝程式。

### 檢查與介面預覽

```powershell
# 通訊協定、設定、超級區塊解析及在地化檢查
dotnet run --project SoraEXT.Tests -c Release

# 唯讀掃描本機分割區
dotnet run --project SoraEXT.Tests -c Release -- --scan
```

發佈後的 `SoraEXT.exe` 支援 `--demo` 介面預覽；可加上 `--language zh-Hans`、`zh-Hant`、`en`、`ja` 或 `ko`。示範模式使用範例資料。

## 專案結構

| 目錄 | 內容 |
| --- | --- |
| `SoraEXT.App` | WinUI 3 主介面 |
| `SoraEXT.Core` | 分割區掃描、驅動通訊協定及掛載設定 |
| `SoraEXT.Agent` | 開機靜默掛載助手 |
| `SoraEXT.Setup` | WinUI 3 安裝介面 |
| `SoraEXT.Bootstrap` | 單一檔案安裝啟動器 |
| `SoraEXT.Tests` | 通訊協定、設定及在地化檢查 |
| `Setup` | 部署、打包輔助指令碼與安裝說明 |
| `Assets` | 應用程式圖示 |
| `Ext4Fsd`、`Ext2Srv`、`Ext2Mgr` | 上游驅動程式、服務及管理工具原始碼 |

舊版工具與驅動原始碼透過 `build.ps1` / `Ext4Fsd.sln` 建置，與 WinUI 專案獨立。發行版安裝程式使用官方簽署的驅動程式。

## 專案來源與授權

SoraEXT 基於 [bobranten/Ext4Fsd](https://github.com/bobranten/Ext4Fsd) 開發；Ext4Fsd 源自 Matt Wu 的 Ext2Fsd 專案。

新增程式碼採用 **GPL-2.0**。上游授權與著作權聲明見 [Ext4Fsd/COPYRIGHT.TXT](../Ext4Fsd/COPYRIGHT.TXT)，原始專案說明保留於 [README.Ext4Fsd.md](../README.Ext4Fsd.md)。第三方 SDK、執行階段及相關元件遵循各自授權。

感謝 Ext2Fsd、Ext4Fsd 的作者與貢獻者。
