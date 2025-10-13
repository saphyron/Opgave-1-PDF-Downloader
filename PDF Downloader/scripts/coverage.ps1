Write-Host "== Coverage report paths ==" -ForegroundColor Cyan
Get-ChildItem -Recurse .\TestResults\ -Filter coverage.cobertura.xml | ForEach-Object {
  Write-Host $_.FullName
}
