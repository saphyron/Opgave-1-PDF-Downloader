# .\PDF Downloader\scripts\test.ps1
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

# Gå til repo-roden (scripts ligger i ".../PDF Downloader/scripts")
Set-Location -LiteralPath (Join-Path $PSScriptRoot '..')

# 1) Restore
& dotnet restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 2) Build
& dotnet build --configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 3) Test (gem TRX)
& dotnet test --configuration $Configuration --logger "trx;LogFileName=tests.trx"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Succeskode
exit 0
