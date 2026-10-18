param([string]$Runtime = 'win-x64', [switch]$Lite)
$ErrorActionPreference = 'Stop'
if ($Runtime -ne 'win-x64') { throw 'Official beta target: Windows 10/11 x64 (win-x64).' }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$taskRoot = Split-Path $PSScriptRoot -Parent
$output = Join-Path $taskRoot $(if ($Lite) { 'artifacts/lite' } else { 'artifacts/portable' })
$publishArgs = @('publish', (Join-Path $taskRoot 'src/Sukun.Windows/Sukun.Windows.csproj'), '-c', 'Release', '-r', $Runtime,
    '--self-contained', $(if ($Lite) { 'false' } else { 'true' }), '-p:PublishSingleFile=true', '-p:EnableCompressionInSingleFile=true', '-p:PublishTrimmed=false', '-p:PublishAot=false', '-p:DebugType=embedded', '-o', $output)
if (-not $Lite) { $publishArgs += '-p:IncludeNativeLibrariesForSelfExtract=true' }
dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
Copy-Item -LiteralPath (Join-Path $taskRoot 'LICENSE'),(Join-Path $taskRoot 'PRIVACY.md'),(Join-Path $taskRoot 'NOTICE.md'),(Join-Path $taskRoot 'THIRD_PARTY_NOTICES.md') -Destination $output
$stem = "Sukun-$Runtime" + $(if ($Lite) { '-lite' } else { '' })
$exe = Join-Path $taskRoot "artifacts/$stem.exe"
Copy-Item -LiteralPath (Join-Path $output 'Sukun.exe') -Destination $exe -Force
if (-not $Lite) {
    $zip = Join-Path $taskRoot "artifacts/$stem.zip"
    Compress-Archive -LiteralPath $exe,(Join-Path $output 'LICENSE'),(Join-Path $output 'PRIVACY.md'),(Join-Path $output 'NOTICE.md'),(Join-Path $output 'THIRD_PARTY_NOTICES.md') -DestinationPath $zip -Force
    $lines = @($exe,$zip) | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($_) }
    [IO.File]::WriteAllLines((Join-Path $taskRoot 'artifacts/SHA256SUMS.txt'),$lines,[Text.UTF8Encoding]::new($false))
}
Write-Host "Ready: $exe"
