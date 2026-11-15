#Requires -RunAsAdministrator
param(
    [ValidateSet('Install','Uninstall')][string]$Mode = 'Install',
    [string]$Bootstrap
)
$ErrorActionPreference = 'Stop'
$destination = "$env:ProgramFiles\MinamotoDev\SoraEXT"
$task = 'SoraEXTMount'
$uninstallKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SoraEXT'
$legacyUninstallKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\app.minamotodev.soraext'
$logDirectory = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'SoraEXT\Logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
Start-Transcript -Path "$logDirectory\setup.log" -Append | Out-Null
try {
    $running = Get-Process SoraEXT,SoraEXT.Agent -ErrorAction SilentlyContinue
    if ($running) { throw '请关闭 SoraEXT，并等待启动助手退出，然后重试。' }
    if ($Mode -eq 'Uninstall') {
        foreach ($name in @($task, 'app.minamotodev.soraext.mount')) {
            if (Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue) {
                Unregister-ScheduledTask -TaskName $name -Confirm:$false
            }
        }
        $shortcut = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\SoraEXT.lnk"
        if (Test-Path $shortcut) { Remove-Item $shortcut -Force }
        if (Test-Path $destination) {
            Get-ChildItem $destination -Force | Where-Object Name -ne 'Setup.exe' | Remove-Item -Recurse -Force
        }
        if (Test-Path $uninstallKey) { Remove-Item $uninstallKey -Recurse -Force }
        if (Test-Path $legacyUninstallKey) { Remove-Item $legacyUninstallKey -Recurse -Force }
        if (Test-Path 'HKLM:\SOFTWARE\MinamotoDev\SoraEXT') {
            Remove-Item 'HKLM:\SOFTWARE\MinamotoDev\SoraEXT' -Recurse -Force
        }
        Write-Output '应用和开机任务已移除。Ext4Fsd 驱动与用户配置保留。'
    } else {
        $pending = (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager' -Name PendingFileRenameOperations -ErrorAction SilentlyContinue).PendingFileRenameOperations
        if ($pending | Where-Object { $_ -match '(?i)Ext2Fsd' }) {
            Write-Output '先前安装的组件未清除，请先重新启动。'
            exit 20
        }
        $driver = Get-Service Ext2Fsd -ErrorAction SilentlyContinue
        $server = Get-Service Ext2Srv -ErrorAction SilentlyContinue
        if (!$driver -and !$server) {
            $installer = Start-Process "$PSScriptRoot\Ext2Fsd-0.71-setup.exe" -ArgumentList '/S' -PassThru -Wait
            if ($installer.ExitCode -notin @(0,3010)) { throw "Ext4Fsd 安装失败：$($installer.ExitCode)" }
        } elseif (!$driver -or !$server) {
            throw '检测到不完整的 Ext4Fsd 安装，请先通过 Windows 卸载旧驱动并重启。'
        }
        $driverPath = "$env:windir\System32\drivers\Ext2Fsd.sys"
        if (!(Test-Path $driverPath)) { throw '未找到已安装的 Ext4Fsd 驱动。' }
        $expectedHash = (Get-Content "$PSScriptRoot\driver.sha256" -Raw).Trim()
        if ((Get-FileHash $driverPath -Algorithm SHA256).Hash -ne $expectedHash) {
            throw '已安装的驱动与此安装包的官方 Ext4Fsd 0.71 不一致，请先卸载旧驱动并重启。'
        }
        if ((Get-AuthenticodeSignature $driverPath).Status -ne 'Valid') { throw '驱动签名验证失败。' }
        & sc.exe config Ext2Fsd start= system | Out-Null
        if ($LASTEXITCODE -ne 0) { throw '配置驱动启动方式失败。' }
        Set-Service Ext2Srv -StartupType Automatic
        Start-Service Ext2Fsd
        Start-Service Ext2Srv
        & "$PSScriptRoot\Install-SoraEXT.ps1" -Source "$PSScriptRoot\App" -Destination $destination
        Copy-Item $Bootstrap "$destination\Setup.exe" -Force
        New-Item $uninstallKey -Force | Out-Null
        $values = @{
            DisplayName='SoraEXT'; DisplayVersion='0.1.0'; Publisher='MinamotoDev'
            InstallLocation=$destination; DisplayIcon="$destination\SoraEXT.exe"
            UninstallString="`"$destination\Setup.exe`" --uninstall"
        }
        foreach ($entry in $values.GetEnumerator()) {
            New-ItemProperty $uninstallKey -Name $entry.Key -Value $entry.Value -PropertyType String -Force | Out-Null
        }
        New-ItemProperty $uninstallKey -Name NoModify -Value 1 -PropertyType DWord -Force | Out-Null
        New-ItemProperty $uninstallKey -Name NoRepair -Value 1 -PropertyType DWord -Force | Out-Null
        $registered = Get-ScheduledTask -TaskName $task
        if (!$registered -or !(Test-Path "$destination\Agent\SoraEXT.Agent.exe")) { throw '启动助手安装验证失败。' }
        if (Test-Path $legacyUninstallKey) { Remove-Item $legacyUninstallKey -Recurse -Force }
        Write-Output 'SoraEXT 0.1.0 已安装，开机挂载任务已启用。'
    }
} catch {
    Write-Output $_.Exception.Message
    exit 1
} finally {
    Stop-Transcript | Out-Null
}
