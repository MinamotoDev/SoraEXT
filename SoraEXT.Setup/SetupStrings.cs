using System.Globalization;
using System.Runtime.InteropServices;

namespace SoraEXT.Setup;

internal static class SetupStrings
{
    [DllImport("kernel32.dll")]
    private static extern ushort GetUserDefaultUILanguage();
    public static string Language { get; private set; } = "en";
    private static int index = 2;
    // Simplified Chinese, Traditional Chinese, English, Japanese, Korean.
    private static readonly Dictionary<string, string[]> strings = new()
    {
        ["Title"] = ["SoraEXT 安装程序", "SoraEXT 安裝程式", "SoraEXT Setup", "SoraEXT セットアップ", "SoraEXT 설치"],
        ["Agreement"] = ["我已阅读并接受 GPL-2.0 许可协议", "我已閱讀並接受 GPL-2.0 授權條款", "I have read and accept the GPL-2.0 license", "GPL-2.0 ライセンスを読み、同意します", "GPL-2.0 라이선스를 읽었으며 동의합니다"],
        ["License"] = ["查看许可与第三方说明", "檢視授權與第三方聲明", "View license and third-party notices", "ライセンスと第三者に関する通知", "라이선스 및 타사 고지 보기"],
        ["PoweredBy"] = ["由 Ext4Fsd 驱动", "由 Ext4Fsd 驅動", "Powered by Ext4Fsd", "Ext4Fsd により動作", "Ext4Fsd 드라이버 사용"],
        ["Cancel"] = ["取消", "取消", "Cancel", "キャンセル", "취소"],
        ["Install"] = ["安装", "安裝", "Install", "インストール", "설치"],
        ["Uninstall"] = ["卸载", "解除安裝", "Uninstall", "アンインストール", "제거"],
        ["Close"] = ["关闭", "關閉", "Close", "閉じる", "닫기"],
        ["Done"] = ["完成", "完成", "Done", "完了", "완료"],
        ["Launch"] = ["启动", "啟動", "Launch", "起動", "실행"],
        ["LaunchFailed"] = ["启动失败，请重试。", "啟動失敗，請重試。", "Could not launch the app. Please retry.", "起動できませんでした。再試行してください。", "앱을 실행하지 못했습니다. 다시 시도하세요."],
        ["Retry"] = ["重试", "重試", "Retry", "再試行", "다시 시도"],
        ["Installing"] = ["正在安装…", "正在安裝…", "Installing…", "インストール中…", "설치 중…"],
        ["Uninstalling"] = ["正在卸载…", "正在解除安裝…", "Uninstalling…", "アンインストール中…", "제거 중…"],
        ["RestartRequired"] = ["先前安装的组件未清除，请先重新启动。", "先前安裝的元件尚未清除，請先重新啟動。", "Previously installed components have not been removed. Please restart first.", "以前インストールしたコンポーネントの削除が完了していません。先に再起動してください。", "이전에 설치한 구성 요소가 아직 제거되지 않았습니다. 먼저 다시 시작하세요."],
        ["Failed"] = ["操作未完成。请查看安装日志后重试。", "操作未完成。請檢視安裝記錄後重試。", "Setup could not finish. Check the setup log and retry.", "処理を完了できませんでした。セットアップログを確認して再試行してください。", "작업을 완료하지 못했습니다. 설치 로그를 확인한 후 다시 시도하세요."],
        ["Installed"] = ["安装完成。", "安裝完成。", "Installation complete.", "インストールが完了しました。", "설치가 완료되었습니다."],
        ["Removed"] = ["卸载完成。重启后清理卸载程序文件。", "解除安裝完成。重新啟動後清除解除安裝程式檔案。", "Uninstalled. Restart to remove the remaining uninstaller files.", "アンインストールが完了しました。再起動後にアンインストーラーのファイルが削除されます。", "제거가 완료되었습니다. 다시 시작하면 남은 제거 프로그램 파일이 삭제됩니다."],
        ["RemoveDescription"] = ["移除应用和开机任务，保留数据与设置。Ext4Fsd 驱动可单独卸载。", "移除應用程式和開機工作，保留資料與設定。Ext4Fsd 驅動程式可另外解除安裝。", "Remove the app and startup task. Keep data and settings. Uninstall Ext4Fsd separately.", "アプリと起動タスクを削除します。データと設定は保持されます。Ext4Fsd は別途削除できます。", "앱과 시작 작업을 제거하고 데이터와 설정은 유지합니다. Ext4Fsd는 별도로 제거할 수 있습니다."]
    };
    public static void Configure()
    {
        var tag = CultureInfo.GetCultureInfo(GetUserDefaultUILanguage()).Name.ToLowerInvariant();
        (Language, index) = tag.StartsWith("zh")
            ? (tag.Contains("hant") || tag is "zh-tw" or "zh-hk" or "zh-mo" ? ("zh-Hant", 1) : ("zh-Hans", 0))
            : tag.StartsWith("ja") ? ("ja", 3) : tag.StartsWith("ko") ? ("ko", 4) : ("en", 2);
        Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = Language;
    }
    public static string Get(string key) => strings[key][index];
}
