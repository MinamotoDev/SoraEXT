<p align="center">
  <img src="../Assets/SoraEXT.png" width="160" alt="SoraEXT アイコン">
</p>

<h1 align="center">SoraEXT</h1>

[English](../README.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [简体中文](README.zh-cn.md) · [正體中文](README.zh-tw.md)

<p align="center">Linux パーティションをマウント・管理する Windows ネイティブツール</p>

<p align="center">
  <a href="../RELEASE_NOTE.md#ja">リリースノート</a> ·
  <a href="https://github.com/MinamotoDev/SoraEXT/releases">リリース</a> ·
  <a href="https://github.com/MinamotoDev/SoraEXT/issues">不具合の報告</a>
</p>

SoraEXT は **WinUI 3** で構築された Windows ネイティブアプリです。公式の署名済み **Ext4Fsd** ドライバーを使用して EXT2、EXT3、EXT4 パーティションにアクセスし、ドライブ文字の管理、読み取り専用モード、起動時のサイレントマウントを提供します。

WSL や仮想マシンは不要です。画面、起動時のマウントヘルパー、ファイルシステムドライバーはそれぞれ独立して動作するため、ウィンドウを閉じてもマウント済みのパーティションにアクセスできます。

## 機能

- **パーティション情報**：ファイルシステム、容量、空き領域、ドライブ文字、アクセスモード、ボリューム ID を確認できます。
- **EXT のマウント**：ドライブ文字、読み取り専用／読み書きモード、起動時のマウントを設定できます。
- **起動時のサイレントマウント**：SYSTEM として実行されるスケジュールされたタスクが、ファイルシステムの UUID を使って設定済みのドライブ文字を復元します。アプリ画面を開く必要はありません。
- **Windows パーティション**：NTFS パーティションの情報表示とエクスプローラーでの表示、ReFS のドライブ文字の割り当てと削除に対応します。
- **パーティションの絞り込み**：EFI、MSR、回復用、非表示の Windows パーティションを除外します。Windows で非表示とされている Linux データパーティションは引き続きスキャンします。
- **WinUI ベースの画面**：システム設定に従うテーマ、ライト、ダークに対応し、対応環境では Mica 背景とネイティブアニメーションを利用できます。
- **自動更新**：ウィンドウに戻ると情報を更新します。更新間隔は最短 15 秒で、`F5` による手動更新も可能です。

現在の SoraEXT はマウントとアクセスの管理を対象としており、パーティションの作成、削除、フォーマット、サイズ変更は行いません。

## 動作環境

| 項目 | 要件 |
| --- | --- |
| OS | Windows 11 |
| アーキテクチャ | x64 |
| 権限 | UAC の承認が必要 |
| ファイルシステムドライバー | Ext4Fsd 0.71（インストーラーに同梱） |
| ランタイム | .NET と Windows App SDK のランタイムを同梱 |

## インストールと使い方

1. インストーラーを実行し、UAC の確認を承認します。
2. ライセンスを読んで同意し、「インストール」を選択します。
3. インストール完了後、「起動」を選択して SoraEXT を開きます。
4. EXT パーティションを選び、「マウント」からドライブ文字とアクセスモードを設定して保存します。
5. 起動時にマウントを復元する場合は、そのパーティションのマウント設定で起動時のマウントを有効にします。

インストーラーはアプリ、公式ドライバー、起動ヘルパーを配置し、スタートメニューのショートカットと Windows のアンインストール項目を登録します。同じバージョンのドライバーが既にある場合は、検証してそのまま使用します。

### インストール先とデータ

| 内容 | 既定の保存先 |
| --- | --- |
| アプリ | `C:\Program Files\MinamotoDev\SoraEXT` |
| 設定 | `%ProgramData%\SoraEXT\settings.json` |
| インストールログ | `%ProgramData%\SoraEXT\Logs\setup.log` |
| 操作ログ | `%ProgramData%\SoraEXT\Logs\operations-YYYY-MM-DD.log` |
| 起動時のマウントログ | `%ProgramData%\SoraEXT\Logs\mount-YYYY-MM-DD.log` |

起動タスクの名前は `SoraEXTMount` です。

### アンインストール

Windows の「インストールされているアプリ」から SoraEXT をアンインストールすると、アプリ、起動タスク、ショートカットが削除されます。設定とログは保持されます。

Ext4Fsd には独立した公式アンインストーラーがあります。ドライバーを削除する場合は別途アンインストールし、案内に従って再起動してください。

## 既知の問題

- **EXT のアンマウントが停止する場合があります**：ドライバーが `FSCTL_LOCK_VOLUME` によるボリュームロック処理から戻らないことがあります。
- **EXT パーティションのごみ箱**：現在、ごみ箱機能に不具合があります。

## ソースからビルド

### 開発環境

- Windows x64。
- .NET 10 SDK。
- PowerShell 7。
- WinUI XAML のコンパイルに必要な Visual Studio C++ ビルドツールと Windows SDK。
- 初回ビルド時に NuGet の依存パッケージを復元するためのインターネット接続。

アプリとインストーラーには Windows App SDK `2.6.5` を使用しています。

### アプリのビルド

リポジトリのルートで実行します。

```powershell
# アプリと起動ヘルパーをビルドし、チェックを実行
.\build-ui.ps1

# ランタイムを含む開発版を発行
.\build-ui.ps1 -Publish
```

開発版は `artifacts\SoraEXT-Dev-0.N.0` に出力されます。スクリプトは既存のディレクトリからバージョンを自動的に繰り上げます。実行時は発行先ディレクトリ全体を保持してください。

### インストーラーのビルド

```powershell
# 公式ドライバーインストーラーのキャッシュ先を作成
New-Item -ItemType Directory -Path .\artifacts\downloads -Force

.\build-release.ps1 `
  -DriverInstaller .\artifacts\downloads\Ext2Fsd-0.71-setup.exe
```

指定したファイルが存在しない場合は、公式インストーラーをダウンロードして SHA-256 を検証します。出力先：

```text
artifacts\SoraEXT-0.1.0-release\SoraEXT-0.1.0-release-setup.exe
```

再ビルドする前に、同じパスから実行中のインストーラーを閉じてください。

### チェックと画面プレビュー

```powershell
# プロトコル、設定、スーパーブロック解析、ローカライズのチェック
dotnet run --project SoraEXT.Tests -c Release

# ローカルパーティションの読み取り専用スキャン
dotnet run --project SoraEXT.Tests -c Release -- --scan
```

発行した `SoraEXT.exe` は `--demo` でサンプルデータを使った画面プレビューを表示できます。`--language zh-Hans`、`zh-Hant`、`en`、`ja`、`ko` を追加すると表示言語を指定できます。

## プロジェクト構成

| ディレクトリ | 内容 |
| --- | --- |
| `SoraEXT.App` | WinUI 3 アプリ画面 |
| `SoraEXT.Core` | パーティションのスキャン、ドライバープロトコル、マウント設定 |
| `SoraEXT.Agent` | 起動時のサイレントマウントヘルパー |
| `SoraEXT.Setup` | WinUI 3 インストーラー画面 |
| `SoraEXT.Bootstrap` | 単一 EXE インストーラーの起動処理 |
| `SoraEXT.Tests` | プロトコル、設定、ローカライズのチェック |
| `Setup` | 配置・パッケージ作成用スクリプトとインストールの説明 |
| `Assets` | アプリアイコン |
| `Ext4Fsd`、`Ext2Srv`、`Ext2Mgr` | 上流のドライバー、サービス、管理ツールのソース |

従来のツールとドライバーのソースは `build.ps1` / `Ext4Fsd.sln` でビルドし、WinUI プロジェクトとは独立しています。配布用インストーラーには公式の署名済みドライバーを使用します。

## 開発元とライセンス

SoraEXT は [bobranten/Ext4Fsd](https://github.com/bobranten/Ext4Fsd) を基に開発されています。Ext4Fsd は Matt Wu 氏の Ext2Fsd プロジェクトから派生したものです。

追加コードには **GPL-2.0** を適用します。上流のライセンスと著作権表示は [Ext4Fsd/COPYRIGHT.TXT](../Ext4Fsd/COPYRIGHT.TXT)、元のプロジェクト説明は [README.Ext4Fsd.md](../README.Ext4Fsd.md) を参照してください。サードパーティーの SDK、ランタイム、関連コンポーネントには、それぞれのライセンスが適用されます。

Ext2Fsd、Ext4Fsd の作者と貢献者の皆様に感謝します。
