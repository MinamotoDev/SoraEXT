# Run only after explicit confirmation to remove the existing test installation.
#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'
$log = "$env:LOCALAPPDATA\Temp\opencode\soraext-cleanup.log"
Start-Transcript -Path $log -Append | Out-Null
try {
    foreach ($process in Get-Process SoraEXT,Ext2Mgr -ErrorAction SilentlyContinue) {
        if ($process.MainWindowHandle -ne 0) { [void]$process.CloseMainWindow() }
    }
    if (Get-Process SoraEXT.Agent -ErrorAction SilentlyContinue) { throw '启动助手正在运行，请等待后重试。' }
    foreach ($name in @('SoraEXTMount', 'app.minamotodev.soraext.mount')) {
        $task = Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue
        if ($task) { Unregister-ScheduledTask -InputObject $task -Confirm:$false }
    }
    # Export only driver volume mappings, not credentials.
    if (Test-Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Ext2Fsd\Volumes') {
        & reg.exe export 'HKLM\SYSTEM\CurrentControlSet\Services\Ext2Fsd\Volumes' "$env:LOCALAPPDATA\Temp\opencode\soraext-volume-mappings.reg" /y
        if ($LASTEXITCODE -ne 0) { throw '无法备份卷映射。' }
    }
    $uninstaller = "$env:ProgramFiles\Ext2Fsd\Uninstall.exe"
    if (Test-Path $uninstaller) {
        $process = Start-Process $uninstaller -ArgumentList '/S' -PassThru
        $process.WaitForExit()
        Write-Output "Official driver uninstaller exit code: $($process.ExitCode)"
        if ($process.ExitCode -ne 0) { throw '官方驱动卸载未完成。' }
    }
    $destination = "$env:ProgramFiles\MinamotoDev\SoraEXT"
    if (Get-Process SoraEXT -ErrorAction SilentlyContinue) { throw '请关闭 SoraEXT 后继续清理应用。' }
    if (Test-Path $destination) { Remove-Item $destination -Recurse -Force }
    $shortcut = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\SoraEXT.lnk"
    if (Test-Path $shortcut) { Remove-Item $shortcut -Force }
    foreach ($key in @('HKLM:\SOFTWARE\MinamotoDev\SoraEXT',
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SoraEXT',
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\app.minamotodev.soraext')) {
        if (Test-Path $key) { Remove-Item $key -Recurse -Force }
    }
    Get-Service Ext2Fsd,Ext2Srv -ErrorAction SilentlyContinue | Format-Table Name,Status,StartType
    Write-Output 'Cleanup submitted. Reboot before testing the installer.'
} finally {
    Stop-Transcript | Out-Null
}
