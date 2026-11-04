param(
    [string]$VpkDirectory = (Join-Path $PSScriptRoot '..\tools\vpk'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\dist')
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$buildTool = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
if ($buildTool) {
    $msbuildPath = $buildTool.Source
} else {
    $vswherePath = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswherePath)) { throw 'MSBuild / Visual Studio 2022 was not found.' }
    $msbuildPath = & $vswherePath -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}
if (-not $msbuildPath) { throw 'MSBuild was not found.' }

& $msbuildPath (Join-Path $projectRoot 'L4NIconPatcher.csproj') /t:Build /p:Configuration=Release /nologo /verbosity:minimal /p:CopyRetryCount=0
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }

$requiredVpkFiles = @('vpk.exe', 'filesystem_stdio.dll', 'tier0.dll', 'tier0_s.dll', 'vstdlib.dll', 'vstdlib_s.dll')
foreach ($file in $requiredVpkFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $VpkDirectory $file))) { throw "Missing VPK dependency: $file" }
}

$application = Join-Path $projectRoot 'bin\Release\L4NIconPatcher.exe'
$fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($application)
$version = '{0}.{1}.{2}' -f $fileVersion.FileMajorPart, $fileVersion.FileMinorPart, $fileVersion.FileBuildPart
$packageName = "L4NIconPatcher-v$version-win"
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$packageDirectory = Join-Path $OutputDirectory $packageName
if (Test-Path -LiteralPath $packageDirectory) { throw "Package staging folder already exists: $packageDirectory" }
$null = New-Item -ItemType Directory -Path (Join-Path $packageDirectory 'bin')
Copy-Item -LiteralPath $application, "$application.config" -Destination $packageDirectory
foreach ($file in @('README.md', 'README.en.md', 'THIRD-PARTY-NOTICES.md', 'RELEASE-NOTES.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination $packageDirectory
}
foreach ($file in $requiredVpkFiles) {
    Copy-Item -LiteralPath (Join-Path $VpkDirectory $file) -Destination (Join-Path $packageDirectory 'bin')
}

$archivePath = Join-Path $OutputDirectory "$packageName.zip"
Compress-Archive -LiteralPath $packageDirectory -DestinationPath $archivePath -CompressionLevel Optimal -Force
$checksum = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$archivePath.sha256" -Value "$checksum  $packageName.zip" -Encoding ASCII
Write-Output $archivePath
Write-Output "$archivePath.sha256"
