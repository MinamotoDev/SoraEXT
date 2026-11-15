# Run explicitly in an elevated PowerShell session after build-ui.ps1 -Publish.
# Requires the official signed Ext4Fsd driver and Ext2Srv to be installed.
# This script installs the UI and the headless startup task.
#Requires -RunAsAdministrator
param(
    [string]$Source = (Join-Path $PSScriptRoot '..\artifacts\SoraEXT'),
    [string]$Destination = "$env:ProgramFiles\MinamotoDev\SoraEXT"
)
$ErrorActionPreference = 'Stop'
$Source = (Resolve-Path $Source).Path
if (!(Test-Path "$Source\SoraEXT.exe") -or !(Test-Path "$Source\Agent\SoraEXT.Agent.exe")) {
    throw 'Run .\build-ui.ps1 -Publish first.'
}
if (!(Get-Service Ext2Fsd -ErrorAction SilentlyContinue) -or !(Get-Service Ext2Srv -ErrorAction SilentlyContinue)) {
    throw 'Install the official Ext4Fsd driver and Ext2Srv first.'
}
$fullDestination = [IO.Path]::GetFullPath($Destination)
if (!$fullDestination.StartsWith("$env:ProgramFiles\", [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The SYSTEM startup task requires a protected Program Files installation.'
}
if (Get-Process SoraEXT,SoraEXT.Agent -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith("$fullDestination\", [StringComparison]::OrdinalIgnoreCase) }) {
    throw 'Close the installed copy of SoraEXT before updating it.'
}
New-Item -ItemType Directory -Path $fullDestination -Force | Out-Null
Copy-Item "$Source\*" $fullDestination -Recurse -Force
$action = New-ScheduledTaskAction -Execute "$fullDestination\Agent\SoraEXT.Agent.exe" -WorkingDirectory "$fullDestination\Agent"
$trigger = New-ScheduledTaskTrigger -AtStartup
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Minutes 5) -MultipleInstances IgnoreNew
$principal = New-ScheduledTaskPrincipal -UserId SYSTEM -LogonType ServiceAccount -RunLevel Highest
Register-ScheduledTask -TaskName 'SoraEXTMount' -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force | Out-Null
New-Item 'HKLM:\SOFTWARE\MinamotoDev\SoraEXT' -Force | Out-Null
New-ItemProperty 'HKLM:\SOFTWARE\MinamotoDev\SoraEXT' -Name StartupTask -Value 'SoraEXTMount' -PropertyType String -Force | Out-Null
# Migrate previous installations after registering the replacement successfully.
$legacyTask = Get-ScheduledTask -TaskName 'app.minamotodev.soraext.mount' -ErrorAction SilentlyContinue
if ($legacyTask) { Unregister-ScheduledTask -InputObject $legacyTask -Confirm:$false }
$logDirectory = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'SoraEXT\Logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
Set-Service Ext2Srv -StartupType Automatic
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut("$env:ProgramData\Microsoft\Windows\Start Menu\Programs\SoraEXT.lnk")
$shortcut.TargetPath = "$fullDestination\SoraEXT.exe"
$shortcut.IconLocation = "$fullDestination\SoraEXT.exe,0"
$shortcut.WorkingDirectory = $fullDestination
$shortcut.Save()
Write-Host "Installed: $fullDestination"
Write-Host 'Startup task: SoraEXTMount'
