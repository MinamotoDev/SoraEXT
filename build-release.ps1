param(
    [string]$DriverInstaller = "$env:LOCALAPPDATA\Temp\opencode\Ext2Fsd-0.71-setup.exe",
    [string]$OutputDirectory = 'artifacts\SoraEXT-0.1.0-release'
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$stage = Join-Path $PSScriptRoot 'artifacts\setup-stage'
$ui = Join-Path $stage 'UI'
$payload = Join-Path $ui 'Payload'
$output = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $OutputDirectory))
New-Item -ItemType Directory -Path $payload,$output -Force | Out-Null
if (!(Test-Path $DriverInstaller)) {
    Invoke-WebRequest 'https://github.com/bobranten/Ext4Fsd/releases/download/v0.71/Ext2Fsd-0.71-setup.exe' -OutFile $DriverInstaller
}
if ((Get-FileHash $DriverInstaller -Algorithm SHA256).Hash -ne '3C127F8E70C6B056A0185850EFB71B9C45D2FF493B7DF6B7B648EB89EC84214D') {
    throw 'Official driver installer checksum mismatch.'
}
Copy-Item $DriverInstaller "$payload\Ext2Fsd-0.71-setup.exe" -Force
foreach ($script in @('Deploy.ps1','Install-SoraEXT.ps1')) {
    Set-Content "$payload\$script" (Get-Content "Setup/$script" -Raw) -Encoding utf8BOM
}
Set-Content "$payload\driver.sha256" '06F6B4A6BC7AAF568D0442A3415394B2B7806C1BD94D35B314BEBFA0993898D9' -Encoding ascii
$notice = "SoraEXT 0.1.0-release`r`nExt4Fsd 0.71: https://github.com/bobranten/Ext4Fsd/tree/v0.71`r`nIncludes the unmodified official signed driver installer.`r`n.NET and Windows App SDK retain their respective third-party notices.`r`n`r`n"
Set-Content "$payload\LICENSE.txt" ($notice + (Get-Content Ext4Fsd/COPYRIGHT.txt -Raw)) -Encoding utf8
dotnet run --project SoraEXT.Tests -c Release
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
dotnet publish SoraEXT.App -c Release -p:Platform=x64 -p:Version=0.1.0 -p:ReleaseChannel=release -r win-x64 --self-contained true -o "$payload\App" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Application publish failed.' }
dotnet publish SoraEXT.Agent -c Release -p:Version=0.1.0 -r win-x64 --self-contained true -o "$payload\App\Agent" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Agent publish failed.' }
Copy-Item "$payload\LICENSE.txt" "$payload\App\LICENSE.txt" -Force
dotnet publish SoraEXT.Setup -c Release -p:Platform=x64 -r win-x64 --self-contained true -o $ui --nologo
if ($LASTEXITCODE -ne 0) { throw 'Installer UI publish failed.' }
# Create the archive without deleting an existing release directory.
Add-Type -AssemblyName System.IO.Compression
$zipPath = Join-Path $stage 'setup.zip'
$zipStream = [IO.File]::Open($zipPath, [IO.FileMode]::Create)
try {
    $zip = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in Get-ChildItem $ui -Recurse -File) {
            $relative = [IO.Path]::GetRelativePath($ui, $file.FullName).Replace('\','/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $relative, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $zip.Dispose() }
} finally { $zipStream.Dispose() }
dotnet publish SoraEXT.Bootstrap -c Release -r win-x64 --self-contained true -o $output --nologo
if ($LASTEXITCODE -ne 0) { throw 'Bootstrap publish failed.' }
$exe = Join-Path $output 'SoraEXT-0.1.0-release-setup.exe'
Get-FileHash $exe -Algorithm SHA256 | Format-List
Write-Host "Ready: $exe"
