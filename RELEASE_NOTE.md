[English](#en) · [日本語](#ja) · [한국어](#ko) · [简体中文](#zh-cn) · [正體中文](#zh-tw)

<a id="en"></a>

# Release notes

## 0.1.0-release

The first SoraEXT release, with a native Windows partition-mounting interface and a standalone EXE installer.

- Installer: `SoraEXT-0.1.0-release-setup.exe`
- App version display: `SoraEXT 0.1.0`
- Architecture: Windows x64
- Windows 11.

### Partitions and mounting

- Added a WinUI 3 partition list, details, mount settings and activity log.
- Access EXT2, EXT3 and EXT4 through the officially signed Ext4Fsd 0.71 driver, with filesystem names matching the driver.
- Configure EXT drive letters, read-only/read-write access and startup mounting by UUID.
- Added a standalone startup agent that restores configured drive letters through a scheduled task running as SYSTEM.
- View NTFS partition information and open partitions in File Explorer.
- Assign and remove ReFS drive letters through the Windows storage management APIs.
- Filter out EFI, MSR, recovery and hidden Windows partitions; fixed Linux data partitions being omitted because Windows marks them as hidden.

### Interface and language

- System, light and dark themes, with Mica backgrounds.
- The app follows the Windows display language by default. A manually selected language takes effect after restarting the app.
- Launching the app requires UAC approval.

### Installation and deployment

- A single-file EXE installer with a native WinUI 3 interface includes app runtimes, the startup agent and the official driver installer.
- Creates a Start menu shortcut, a startup scheduled task and a Windows uninstall entry.
- Settings and logs are stored under `%ProgramData%\SoraEXT`.

### Known issues and validation scope

- The official driver may stall at `FSCTL_LOCK_VOLUME` while unmounting EXT volumes.
- Recycle Bin issues on EXT partitions remain unresolved.

### Uninstalling

Uninstalling SoraEXT removes the app, startup task and shortcuts while keeping settings and logs. Remove the Ext4Fsd driver separately using its official uninstaller; a restart may be required afterward.

---

Earlier `SoraEXT-Dev-0.N.0` versions used development iteration numbers. Release tracking in this file starts with `0.1.0-release`.

---

<a id="ja"></a>

# リリースノート

## 0.1.0-release

SoraEXT の初回リリースです。Windows ネイティブのパーティションマウント画面と、単独で実行できる EXE インストーラーを提供します。

- インストーラー：`SoraEXT-0.1.0-release-setup.exe`
- アプリ内のバージョン表示：`SoraEXT 0.1.0`
- アーキテクチャ：Windows x64
- Windows 11。

### パーティションとマウント

- WinUI 3 によるパーティション一覧、詳細、マウント設定、操作履歴を追加しました。
- 公式の署名済み Ext4Fsd 0.71 ドライバーで EXT2、EXT3、EXT4 にアクセスします。ファイルシステム名の表示はドライバーと統一しています。
- EXT のドライブ文字、読み取り専用／読み書きモード、UUID に基づく起動時のマウントを設定できます。
- SYSTEM として実行されるスケジュールされたタスクで、設定済みのドライブ文字を復元する独立した起動ヘルパーを追加しました。
- NTFS パーティションの情報確認と、エクスプローラーでの表示に対応しました。
- Windows のストレージ管理 API を使い、ReFS のドライブ文字を割り当て・削除できます。
- EFI、MSR、回復用、非表示の Windows パーティションを除外します。Windows の非表示属性により Linux データパーティションが表示されない問題を修正しました。

### 画面と言語

- システム設定に従うテーマ、ライト、ダーク、Mica 背景に対応しています。
- 表示言語は既定で Windows の表示言語に従います。手動で選択した言語はアプリの再起動後に適用されます。
- アプリの起動には UAC の承認が必要です。

### インストールと配置

- WinUI 3 のネイティブ画面を持つ単一 EXE インストーラーに、アプリのランタイム、起動ヘルパー、公式ドライバーのインストーラーを同梱しました。
- スタートメニューのショートカット、起動タスク、Windows のアンインストール項目を登録します。
- 設定とログの保存先を `%ProgramData%\SoraEXT` に統一しました。

### 既知の問題と検証状況

- EXT のアンマウント時に、公式ドライバーが `FSCTL_LOCK_VOLUME` の処理で停止する場合があります。
- EXT パーティションのごみ箱に関する不具合は未修正です。

### アンインストール

SoraEXT をアンインストールすると、アプリ、起動タスク、ショートカットが削除され、設定とログは保持されます。Ext4Fsd ドライバーは公式アンインストーラーで別途削除してください。ドライバーの削除後は再起動が必要になる場合があります。

---

初期の `SoraEXT-Dev-0.N.0` は開発中の更新番号です。このファイルでは `0.1.0-release` から配布版の変更を記録します。

---

<a id="ko"></a>

# 릴리스 노트

## 0.1.0-release

SoraEXT의 첫 릴리스입니다. Windows 네이티브 파티션 탑재 화면과 독립 실행형 EXE 설치 프로그램을 제공합니다.

- 설치 파일: `SoraEXT-0.1.0-release-setup.exe`
- 앱에 표시되는 버전: `SoraEXT 0.1.0`
- 아키텍처: Windows x64
- Windows 11.

### 파티션 및 탑재

- WinUI 3 파티션 목록, 상세 정보, 탑재 설정, 작업 기록을 추가했습니다.
- 공식 서명된 Ext4Fsd 0.71 드라이버로 EXT2, EXT3, EXT4에 액세스하며, 파일 시스템 이름 표시를 드라이버와 통일했습니다.
- EXT 드라이브 문자, 읽기 전용/읽기·쓰기 모드, UUID 기준 시작 시 탑재를 설정할 수 있습니다.
- SYSTEM 계정으로 실행되는 예약 작업을 통해 설정된 드라이브 문자를 복원하는 독립 시작 도우미를 추가했습니다.
- NTFS 파티션 정보 확인과 파일 탐색기에서 열기를 지원합니다.
- Windows 저장소 관리 API를 통해 ReFS 드라이브 문자를 할당하거나 제거할 수 있습니다.
- EFI, MSR, 복구 파티션 및 Windows 숨김 파티션을 제외합니다. Windows의 숨김 속성 때문에 Linux 데이터 파티션이 표시되지 않던 문제를 수정했습니다.

### 화면 및 언어

- 시스템 설정에 따르는 테마, 밝은 테마, 어두운 테마와 운모 배경을 지원합니다.
- 앱은 기본적으로 Windows 표시 언어를 따릅니다. 언어를 직접 선택하면 앱을 다시 시작한 후 적용됩니다.
- 앱을 실행하려면 UAC 승인이 필요합니다.

### 설치 및 배포

- WinUI 3 네이티브 화면을 갖춘 단일 EXE 설치 프로그램에 앱 런타임, 시작 도우미, 공식 드라이버 설치 프로그램을 포함했습니다.
- 시작 메뉴 바로 가기, 시작 예약 작업, Windows 제거 항목을 등록합니다.
- 설정과 로그의 저장 위치를 `%ProgramData%\SoraEXT`로 통일했습니다.

### 알려진 문제 및 검증 범위

- EXT 탑재 해제 시 공식 드라이버가 `FSCTL_LOCK_VOLUME` 단계에서 멈출 수 있습니다.
- EXT 파티션의 휴지통 문제는 아직 해결되지 않았습니다.

### 제거 안내

SoraEXT를 제거하면 앱, 시작 작업, 바로 가기가 삭제되며 설정과 로그는 유지됩니다. Ext4Fsd 드라이버는 공식 제거 프로그램으로 별도로 제거하세요. 드라이버를 제거한 후 다시 시작해야 할 수 있습니다.

---

이전 `SoraEXT-Dev-0.N.0`은 개발 단계의 버전 번호입니다. 이 문서는 `0.1.0-release`부터 릴리스 변경 사항을 기록합니다.

---

<a id="zh-cn"></a>

# 版本说明

## 0.1.0-release

SoraEXT 首个发行版本，提供原生 Windows 分区挂载界面及独立 EXE 安装包。

- 安装包名称：`SoraEXT-0.1.0-release-setup.exe`
- 应用版本显示：`SoraEXT 0.1.0`
- 架构：Windows x64
- Windows 11。

### 分区与挂载

- 新增 WinUI 3 分区列表、详情、挂载配置和操作记录。
- 通过官方签名 Ext4Fsd 0.71 驱动访问 EXT2、EXT3、EXT4，名称显示与驱动保持一致。
- 支持 EXT 盘符设置、只读/读写模式及按 UUID 配置开机挂载。
- 新增独立启动助手，以 SYSTEM 计划任务在开机时恢复配置的盘符。
- NTFS 分区支持信息查看及在文件资源管理器中打开。
- ReFS 分区通过 Windows 存储管理接口分配和移除盘符。
- 过滤 EFI、MSR、恢复及 Windows 隐藏分区；修正 Linux 数据分区因 Windows 隐藏标记而未显示的问题。

### 界面与语言

- 支持系统、浅色、深色主题与云母背景。
- 应用语言默认跟随 Windows 显示语言，可手动选择并在重启后生效。
- 启动应用时需要 UAC 授权。

### 安装与部署

- 提供 WinUI 3 原生界面的单文件 EXE 安装包，包含应用运行库、启动助手及官方驱动安装程序。
- 创建开始菜单快捷方式、开机计划任务及 Windows 卸载入口。
- 统一将设置和日志保存到 `%ProgramData%\SoraEXT`。

### 已知问题与验证范围

- 官方驱动在 EXT 卸载的 `FSCTL_LOCK_VOLUME` 阶段可能阻塞。
- EXT 分区回收站功能异常尚未修复。

### 卸载说明

卸载 SoraEXT 会移除应用、开机任务和快捷方式，保留设置及日志。Ext4Fsd 驱动通过其官方卸载入口单独移除；驱动卸载后可能需要重启。

---

早期 `SoraEXT-Dev-0.N.0` 为开发迭代编号，本文件从 `0.1.0-release` 开始记录发行版本。

---

<a id="zh-tw"></a>

# 版本說明

## 0.1.0-release

SoraEXT 首個發行版本，提供原生 Windows 分割區掛載介面及獨立 EXE 安裝程式。

- 安裝檔名稱：`SoraEXT-0.1.0-release-setup.exe`
- 應用程式版本顯示：`SoraEXT 0.1.0`
- 架構：Windows x64
- Windows 11。

### 分割區與掛載

- 新增 WinUI 3 分割區清單、詳細資訊、掛載設定和操作記錄。
- 透過官方簽署的 Ext4Fsd 0.71 驅動程式存取 EXT2、EXT3、EXT4，檔案系統名稱顯示與驅動程式一致。
- 支援 EXT 磁碟機代號設定、唯讀／讀寫模式，以及依 UUID 設定開機掛載。
- 新增獨立啟動助手，透過以 SYSTEM 身分執行的排程工作，在開機時還原設定的磁碟機代號。
- NTFS 分割區支援資訊檢視及在檔案總管中開啟。
- ReFS 分割區透過 Windows 儲存管理介面指派或移除磁碟機代號。
- 篩除 EFI、MSR、復原及 Windows 隱藏分割區；修正 Linux 資料分割區因 Windows 隱藏屬性而未顯示的問題。

### 介面與語言

- 支援系統、淺色、深色佈景主題與雲母背景。
- 應用程式語言預設跟隨 Windows 顯示語言，可手動選擇並於重新啟動應用程式後生效。
- 啟動應用程式時需要 UAC 授權。

### 安裝與部署

- 提供 WinUI 3 原生介面的單一 EXE 安裝程式，內含應用程式執行階段、啟動助手及官方驅動安裝程式。
- 建立開始功能表捷徑、開機排程工作及 Windows 解除安裝項目。
- 統一將設定和記錄儲存於 `%ProgramData%\SoraEXT`。

### 已知問題與驗證範圍

- 官方驅動程式在 EXT 解除掛載的 `FSCTL_LOCK_VOLUME` 階段可能停滯。
- EXT 分割區資源回收筒功能異常尚未修復。

### 解除安裝說明

解除安裝 SoraEXT 會移除應用程式、開機工作和捷徑，保留設定及記錄。Ext4Fsd 驅動程式需透過官方解除安裝程式另外移除；移除驅動程式後可能需要重新啟動。

---

早期 `SoraEXT-Dev-0.N.0` 為開發階段的版本編號，本文件從 `0.1.0-release` 開始記錄發行版本。
