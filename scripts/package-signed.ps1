param([Parameter(Mandatory)][string]$Directory)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$signedDirectory = (Resolve-Path -LiteralPath $Directory).Path
$exe = Join-Path $signedDirectory 'Sukun-win-x64.exe'
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw 'Signed EXE is missing.' }
$signature = Get-AuthenticodeSignature -LiteralPath $exe
if ($signature.Status -ne 'Valid' -or $null -eq $signature.SignerCertificate) {
    throw 'Trusted Authenticode verification failed. Do not publish this package.'
}
if ($null -eq $signature.TimeStamperCertificate) {
    throw 'The signature has no verified timestamp. Do not publish this package.'
}
$notices = @('LICENSE','PRIVACY.md','NOTICE.md','THIRD_PARTY_NOTICES.md') | ForEach-Object {
    $source = Join-Path $taskRoot $_
    Copy-Item -LiteralPath $source -Destination $signedDirectory -Force
    Join-Path $signedDirectory $_
}
$zip = Join-Path $signedDirectory 'Sukun-win-x64.zip'
Compress-Archive -LiteralPath (@($exe) + $notices) -DestinationPath $zip -Force
$lines = @($exe,$zip) | ForEach-Object {
    (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($_)
}
[IO.File]::WriteAllLines((Join-Path $signedDirectory 'SHA256SUMS.txt'),$lines,[Text.UTF8Encoding]::new($false))
Write-Host 'Trusted signature and timestamp verified. Signed ZIP and checksums are ready.'
