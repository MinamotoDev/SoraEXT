<p align="center">
  <img src="Assets/SoraEXT.png" width="160" alt="SoraEXT icon">
</p>

<h1 align="center">SoraEXT</h1>

[English](README.md) · [日本語](docs/README.ja.md) · [한국어](docs/README.ko.md) · [简体中文](docs/README.zh-cn.md) · [正體中文](docs/README.zh-tw.md)

<p align="center">A native Windows tool for mounting and managing Linux partitions</p>

<p align="center">
  <a href="RELEASE_NOTE.md#en">Release notes</a> ·
  <a href="https://github.com/MinamotoDev/SoraEXT/releases">Releases</a> ·
  <a href="https://github.com/MinamotoDev/SoraEXT/issues">Report an issue</a>
</p>

SoraEXT provides a native Windows interface built with **WinUI 3**. It uses the officially signed **Ext4Fsd** driver to access EXT2, EXT3 and EXT4 partitions, with drive-letter management, read-only mode and silent mounting at startup.

No WSL or virtual machine is required. The interface, startup mounting agent and filesystem driver run independently, so mounted partitions remain accessible after you close the app.

## Features

- **Partition details**: view the filesystem, capacity, free space, drive letter, access mode and volume identifier.
- **EXT mounting**: assign a drive letter, choose read-only or read/write access, and configure mounting at startup.
- **Silent startup mounting**: a scheduled task running as SYSTEM restores configured drive letters by filesystem UUID, without opening the interface.
- **Windows partitions**: view NTFS partitions and open them in File Explorer; assign and remove drive letters for ReFS.
- **Partition filtering**: hide EFI, MSR, recovery and hidden Windows partitions, while continuing to scan Linux data partitions that Windows marks as hidden.
- **Built with WinUI**: system, light and dark themes, with Mica and native animations on supported systems.
- **Automatic refresh**: refresh when returning to the window, with a minimum interval of 15 seconds; press `F5` to refresh manually.

SoraEXT currently focuses on mounting and access management. It does not create, delete, format or resize partitions.

## System requirements

| Item | Requirement |
| --- | --- |
| OS | Windows 11 |
| Architecture | x64 |
| Permissions | UAC approval required |
| Filesystem driver | Ext4Fsd 0.71, included in the installer |
| Runtimes | .NET and Windows App SDK runtime files included |

## Installation and usage

1. Run the installer and approve the UAC prompt.
2. Read and accept the license, then select **Install**.
3. When installation finishes, select **Launch** to open SoraEXT.
4. Select an EXT partition, choose **Mount**, set its drive letter and access mode, and save.
5. To restore the mount at startup, enable **Mount at startup** in the partition's mount settings.

The installer deploys the app, official driver and startup agent, and creates a Start menu shortcut and a Windows uninstall entry. An existing matching driver is verified and reused.

### Installation and data locations

| Content | Default location |
| --- | --- |
| App | `C:\Program Files\MinamotoDev\SoraEXT` |
| Settings | `%ProgramData%\SoraEXT\settings.json` |
| Setup log | `%ProgramData%\SoraEXT\Logs\setup.log` |
| Operation logs | `%ProgramData%\SoraEXT\Logs\operations-YYYY-MM-DD.log` |
| Startup mounting logs | `%ProgramData%\SoraEXT\Logs\mount-YYYY-MM-DD.log` |

The startup task is named `SoraEXTMount`.

### Uninstalling

Uninstall SoraEXT from **Installed apps** in Windows to remove the app, startup task and shortcuts. User settings and logs are kept.

Ext4Fsd has its own official uninstaller. To remove the driver, uninstall it separately and restart when prompted.

## Known issues

- **EXT unmounting may stall**: the driver may remain in the `FSCTL_LOCK_VOLUME` volume-lock stage.
- **Recycle Bin on EXT partitions**: Recycle Bin functionality currently has issues.

## Building from source

### Development environment

- Windows x64.
- .NET 10 SDK.
- PowerShell 7.
- Visual Studio C++ Build Tools and the Windows SDK for WinUI XAML compilation.
- An internet connection to restore NuGet dependencies on the first build.

The app and installer use Windows App SDK `2.6.5`.

### Building the app

Run these commands from the repository root:

```powershell
# Build the app and startup agent, then run checks
.\build-ui.ps1

# Publish a self-contained development build
.\build-ui.ps1 -Publish
```

Development builds are written to `artifacts\SoraEXT-Dev-0.N.0`. The script increments the version based on existing directories. Keep the entire published directory when running the app.

### Building the installer

```powershell
# Prepare a cache directory for the official driver installer
New-Item -ItemType Directory -Path .\artifacts\downloads -Force

.\build-release.ps1 `
  -DriverInstaller .\artifacts\downloads\Ext2Fsd-0.71-setup.exe
```

If the specified file is missing, the script downloads the official installer and verifies its SHA-256 checksum. Output:

```text
artifacts\SoraEXT-0.1.0-release\SoraEXT-0.1.0-release-setup.exe
```

Close any installer running from that path before rebuilding it.

### Checks and interface preview

```powershell
# Check the protocol, configuration, superblock parsing and localization
dotnet run --project SoraEXT.Tests -c Release

# Read-only scan of local partitions
dotnet run --project SoraEXT.Tests -c Release -- --scan
```

The published `SoraEXT.exe` supports `--demo` to preview the interface using sample data. Add `--language zh-Hans`, `zh-Hant`, `en`, `ja` or `ko` to preview a specific language.

## Project structure

| Directory | Contents |
| --- | --- |
| `SoraEXT.App` | WinUI 3 app interface |
| `SoraEXT.Core` | Partition scanning, driver protocol and mount configuration |
| `SoraEXT.Agent` | Silent startup mounting agent |
| `SoraEXT.Setup` | WinUI 3 installer interface |
| `SoraEXT.Bootstrap` | Single-file installer bootstrapper |
| `SoraEXT.Tests` | Protocol, configuration and localization checks |
| `Setup` | Deployment scripts, packaging helpers and setup documentation |
| `Assets` | App icons |
| `Ext4Fsd`, `Ext2Srv`, `Ext2Mgr` | Upstream driver, service and management tool source |

The legacy tools and driver source are built separately from the WinUI projects using `build.ps1` / `Ext4Fsd.sln`. Release installers use the officially signed driver.

## Origins and license

SoraEXT is based on [bobranten/Ext4Fsd](https://github.com/bobranten/Ext4Fsd), which originated from Matt Wu's Ext2Fsd project.

New code is licensed under **GPL-2.0**. See [Ext4Fsd/COPYRIGHT.TXT](Ext4Fsd/COPYRIGHT.TXT) for the upstream license and copyright notices. The original project README is preserved in [README.Ext4Fsd.md](README.Ext4Fsd.md). Third-party SDKs, runtimes and related components retain their own licenses.

Thanks to the authors and contributors of Ext2Fsd and Ext4Fsd.
