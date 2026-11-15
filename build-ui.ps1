param(
    [switch]$Publish,
    [ValidatePattern('^0\.[1-9][0-9]*\.0$')]
    [string]$Version
)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    if (!$Version) {
        $previous = Get-ChildItem (Join-Path $PSScriptRoot 'artifacts') -Directory -ErrorAction SilentlyContinue |
            Where-Object Name -Match '^SoraEXT-Dev-0\.[1-9][0-9]*\.0$' |
            ForEach-Object { [version]($_.Name -replace '^SoraEXT-Dev-', '') } |
            Sort-Object -Descending | Select-Object -First 1
        $minor = if ($previous) { $previous.Minor + 1 } else { 1 }
        $Version = "0.$minor.0"
    }
    $publishDirectory = "artifacts/SoraEXT-Dev-$Version"
    if ($Publish -and (Test-Path $publishDirectory)) {
        throw "Release directory already exists: $publishDirectory. Choose the next version."
    }
    dotnet build SoraEXT.App/SoraEXT.App.csproj -c Release -p:Platform=x64 --nologo
    if ($LASTEXITCODE -ne 0) { throw 'WinUI build failed.' }
    dotnet build SoraEXT.Agent/SoraEXT.Agent.csproj -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Agent build failed.' }
    dotnet run --project SoraEXT.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Protocol verification failed.' }
    if ($Publish) {
        dotnet publish SoraEXT.App/SoraEXT.App.csproj -c Release -p:Platform=x64 "-p:Version=$Version" -r win-x64 --self-contained true -o $publishDirectory --nologo
        if ($LASTEXITCODE -ne 0) { throw 'UI publish failed.' }
        dotnet publish SoraEXT.Agent/SoraEXT.Agent.csproj -c Release "-p:Version=$Version" -r win-x64 --self-contained true -o "$publishDirectory/Agent" --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Agent publish failed.' }
        Write-Host "Ready: $PSScriptRoot\$publishDirectory\SoraEXT.exe"
    }
} finally {
    Pop-Location
}
