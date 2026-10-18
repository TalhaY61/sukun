param([ValidateSet('Build','Run','Test')][string]$Action = 'Run', [string]$RepositoryUrl = '')
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$taskRoot = Split-Path $PSScriptRoot -Parent
$candidates = @()
if ($env:DOTNET_ROOT) { $candidates += Join-Path $env:DOTNET_ROOT 'dotnet.exe' }
$systemDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($systemDotnet) { $candidates += $systemDotnet.Source }
$candidates += Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'
$candidates += 'C:\dev\sdk\dotnet\dotnet.exe'
$sdk = $null
foreach ($candidate in ($candidates | Select-Object -Unique)) {
    if ((Test-Path -LiteralPath $candidate) -and (@(& $candidate --list-sdks) -match '^10\.').Count -gt 0) { $sdk = $candidate; break }
}
if (-not $sdk) { throw '.NET 10 SDK bulunamadi. .NET Desktop Runtime tek basina derleme icin yeterli degildir.' }
$repositoryArg = @()
if ($RepositoryUrl) { $repositoryArg += "-p:RepositoryUrl=$RepositoryUrl" }
switch ($Action) {
    'Build' { & $sdk build (Join-Path $taskRoot 'Sukun.sln') -c Debug @repositoryArg }
    'Run' { & $sdk run --project (Join-Path $taskRoot 'src/Sukun.Windows') -c Debug @repositoryArg }
    'Test' { & $sdk run --project (Join-Path $taskRoot 'tests/Sukun.Tests') -c Debug }
}
if ($LASTEXITCODE -ne 0) { throw "$Action basarisiz. Windows Application Control hatasi varsa guvenlik ayarlarini degistirmeyin; imzali yayin gerekir." }
