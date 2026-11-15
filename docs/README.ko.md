<p align="center">
  <img src="../Assets/SoraEXT.png" width="160" alt="SoraEXT 아이콘">
</p>

<h1 align="center">SoraEXT</h1>

[English](../README.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [简体中文](README.zh-cn.md) · [正體中文](README.zh-tw.md)

<p align="center">Linux 파티션을 탑재하고 관리하는 Windows 네이티브 도구</p>

<p align="center">
  <a href="../RELEASE_NOTE.md#ko">릴리스 노트</a> ·
  <a href="https://github.com/MinamotoDev/SoraEXT/releases">릴리스</a> ·
  <a href="https://github.com/MinamotoDev/SoraEXT/issues">문제 보고</a>
</p>

SoraEXT는 **WinUI 3**로 만든 Windows 네이티브 앱입니다. 공식 서명된 **Ext4Fsd** 드라이버로 EXT2, EXT3, EXT4 파티션에 액세스하며, 드라이브 문자 관리, 읽기 전용 모드, 부팅 시 백그라운드 자동 탑재 기능을 제공합니다.

WSL이나 가상 머신이 필요하지 않습니다. 앱 화면, 시작 시 탑재 도우미, 파일 시스템 드라이버가 각각 독립적으로 실행되므로 창을 닫아도 탑재된 파티션에 계속 액세스할 수 있습니다.

## 기능

- **파티션 정보**: 파일 시스템, 용량, 사용 가능한 공간, 드라이브 문자, 액세스 모드, 볼륨 ID를 확인할 수 있습니다.
- **EXT 탑재**: 드라이브 문자를 선택하고 읽기 전용 또는 읽기/쓰기 모드와 시작 시 탑재를 설정할 수 있습니다.
- **부팅 시 자동 탑재**: SYSTEM 계정으로 실행되는 예약 작업이 파일 시스템 UUID를 기준으로 설정된 드라이브 문자를 복원합니다. 앱 화면을 열 필요가 없습니다.
- **Windows 파티션**: NTFS 파티션 정보를 확인하고 파일 탐색기에서 열 수 있습니다. ReFS는 드라이브 문자 할당과 제거를 지원합니다.
- **파티션 필터링**: EFI, MSR, 복구 파티션 및 Windows 숨김 파티션을 표시하지 않습니다. Windows에서 숨김으로 표시한 Linux 데이터 파티션은 계속 검색합니다.
- **WinUI 기반 화면**: 시스템 설정에 따르는 테마, 밝은 테마와 어두운 테마를 지원합니다. 지원되는 시스템에서는 운모 배경과 네이티브 애니메이션을 사용합니다.
- **자동 새로 고침**: 창으로 돌아오면 최소 15초 간격으로 정보를 갱신합니다. `F5`로 직접 새로 고칠 수도 있습니다.

현재 SoraEXT는 탑재와 액세스 관리에 중점을 두며, 파티션 생성, 삭제, 포맷, 크기 변경 기능은 제공하지 않습니다.

## 시스템 요구 사항

| 항목 | 요구 사항 |
| --- | --- |
| 운영 체제 | Windows 11 |
| 아키텍처 | x64 |
| 권한 | UAC 승인 필요 |
| 파일 시스템 드라이버 | Ext4Fsd 0.71, 설치 프로그램에 포함 |
| 런타임 | .NET 및 Windows App SDK 런타임 파일 포함 |

## 설치 및 사용 방법

1. 설치 프로그램을 실행하고 UAC 요청을 승인합니다.
2. 라이선스를 읽고 동의한 다음 **설치**를 선택합니다.
3. 설치가 끝나면 **실행**을 선택하여 SoraEXT를 엽니다.
4. EXT 파티션을 선택하고 **탑재**를 누른 다음 드라이브 문자와 액세스 모드를 설정하고 저장합니다.
5. 부팅할 때 탑재 상태를 복원하려면 해당 파티션의 탑재 설정에서 **시작 시 탑재**를 켭니다.

설치 프로그램은 앱, 공식 드라이버, 시작 도우미를 배포하고 시작 메뉴 바로 가기와 Windows 제거 항목을 등록합니다. 일치하는 버전의 드라이버가 이미 설치되어 있으면 검증 후 재사용합니다.

### 설치 위치 및 데이터

| 내용 | 기본 경로 |
| --- | --- |
| 앱 | `C:\Program Files\MinamotoDev\SoraEXT` |
| 설정 | `%ProgramData%\SoraEXT\settings.json` |
| 설치 로그 | `%ProgramData%\SoraEXT\Logs\setup.log` |
| 작업 로그 | `%ProgramData%\SoraEXT\Logs\operations-YYYY-MM-DD.log` |
| 시작 시 탑재 로그 | `%ProgramData%\SoraEXT\Logs\mount-YYYY-MM-DD.log` |

시작 작업의 이름은 `SoraEXTMount`입니다.

### 제거

Windows의 **설치된 앱**에서 SoraEXT를 제거하면 앱, 시작 작업, 바로 가기가 삭제됩니다. 사용자 설정과 로그는 유지됩니다.

Ext4Fsd에는 별도의 공식 제거 프로그램이 있습니다. 드라이버도 제거하려면 따로 제거한 후 안내에 따라 다시 시작하세요.

## 알려진 문제

- **EXT 탑재 해제가 멈출 수 있음**: 드라이버가 `FSCTL_LOCK_VOLUME` 볼륨 잠금 단계에서 응답하지 않을 수 있습니다.
- **EXT 파티션의 휴지통**: 현재 휴지통 기능에 문제가 있습니다.

## 소스에서 빌드

### 개발 환경

- Windows x64.
- .NET 10 SDK.
- PowerShell 7.
- WinUI XAML 컴파일에 필요한 Visual Studio C++ 빌드 도구 및 Windows SDK.
- 처음 빌드할 때 NuGet 종속성을 복원하기 위한 인터넷 연결.

앱과 설치 프로그램은 Windows App SDK `2.6.5`를 사용합니다.

### 앱 빌드

저장소 루트에서 실행하세요.

```powershell
# 앱과 시작 도우미를 빌드하고 검사 실행
.\build-ui.ps1

# 런타임이 포함된 자체 포함 개발 빌드 게시
.\build-ui.ps1 -Publish
```

개발 빌드는 `artifacts\SoraEXT-Dev-0.N.0`에 저장됩니다. 스크립트는 기존 폴더를 기준으로 버전을 자동으로 올립니다. 실행할 때는 게시된 폴더 전체를 유지해야 합니다.

### 설치 프로그램 빌드

```powershell
# 공식 드라이버 설치 프로그램을 저장할 캐시 폴더 생성
New-Item -ItemType Directory -Path .\artifacts\downloads -Force

.\build-release.ps1 `
  -DriverInstaller .\artifacts\downloads\Ext2Fsd-0.71-setup.exe
```

지정한 파일이 없으면 스크립트가 공식 설치 프로그램을 다운로드하고 SHA-256을 검증합니다. 출력 경로:

```text
artifacts\SoraEXT-0.1.0-release\SoraEXT-0.1.0-release-setup.exe
```

다시 빌드하기 전에 같은 경로에서 실행 중인 설치 프로그램을 닫으세요.

### 검사 및 화면 미리 보기

```powershell
# 프로토콜, 설정, 슈퍼블록 해석 및 현지화 검사
dotnet run --project SoraEXT.Tests -c Release

# 로컬 파티션 읽기 전용 검색
dotnet run --project SoraEXT.Tests -c Release -- --scan
```

게시된 `SoraEXT.exe`에 `--demo`를 지정하면 예제 데이터로 화면을 미리 볼 수 있습니다. `--language zh-Hans`, `zh-Hant`, `en`, `ja`, `ko`를 추가하여 언어를 선택할 수 있습니다.

## 프로젝트 구조

| 폴더 | 내용 |
| --- | --- |
| `SoraEXT.App` | WinUI 3 앱 화면 |
| `SoraEXT.Core` | 파티션 검색, 드라이버 프로토콜 및 탑재 설정 |
| `SoraEXT.Agent` | 시작 시 백그라운드 탑재 도우미 |
| `SoraEXT.Setup` | WinUI 3 설치 화면 |
| `SoraEXT.Bootstrap` | 단일 실행 파일 설치 부트스트래퍼 |
| `SoraEXT.Tests` | 프로토콜, 설정 및 현지화 검사 |
| `Setup` | 배포 및 패키징 보조 스크립트와 설치 문서 |
| `Assets` | 앱 아이콘 |
| `Ext4Fsd`, `Ext2Srv`, `Ext2Mgr` | 업스트림 드라이버, 서비스 및 관리 도구 소스 |

기존 도구와 드라이버 소스는 `build.ps1` / `Ext4Fsd.sln`으로 빌드하며 WinUI 프로젝트와 독립적입니다. 릴리스 설치 프로그램에는 공식 서명된 드라이버를 사용합니다.

## 프로젝트 기반 및 라이선스

SoraEXT는 [bobranten/Ext4Fsd](https://github.com/bobranten/Ext4Fsd)를 기반으로 개발되었습니다. Ext4Fsd는 Matt Wu의 Ext2Fsd 프로젝트에서 파생되었습니다.

새로 추가된 코드는 **GPL-2.0**을 따릅니다. 업스트림 라이선스와 저작권 고지는 [Ext4Fsd/COPYRIGHT.TXT](../Ext4Fsd/COPYRIGHT.TXT)에 있으며, 원래 프로젝트 설명은 [README.Ext4Fsd.md](../README.Ext4Fsd.md)에 보존되어 있습니다. 타사 SDK, 런타임 및 관련 구성 요소에는 각각의 라이선스가 적용됩니다.

Ext2Fsd와 Ext4Fsd의 제작자 및 기여자 여러분께 감사드립니다.
