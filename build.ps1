param(
    [string]$SdkVersion = '10.0.28000.0',
    [string]$Toolset = 'v145'
)

$ErrorActionPreference = 'Stop'
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) {
    throw 'MSBuild was not found. Install Visual Studio C++ tools, MFC, SDK and WDK.'
}

$common = @(
    '/m', '/nologo', '/verbosity:minimal',
    '/p:Configuration=Release', '/p:Platform=x64',
    "/p:WindowsTargetPlatformVersion=$SdkVersion",
    "/p:SolutionDir=$PSScriptRoot\"
)

foreach ($project in @('Ext2Srv', 'Ext2Mgr', 'Ext4Fsd')) {
    [string[]]$options = if ($project -eq 'Ext4Fsd') {
        # This legacy filesystem driver targets desktop Windows.
        # Produce an unsigned binary without installing or loading it.
        @('/p:DriverTargetPlatform=Desktop', '/p:SignMode=Off')
    } else {
        @("/p:PlatformToolset=$Toolset")
    }
    & $msbuild "$PSScriptRoot\$project\$project.vcxproj" @common @options
    if ($LASTEXITCODE -ne 0) {
        throw "$project build failed (exit code $LASTEXITCODE)."
    }
}

Write-Host 'x64 Release build complete. Driver binary is unsigned.'
