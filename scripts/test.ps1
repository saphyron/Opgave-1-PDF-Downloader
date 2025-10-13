param(
  [string]$SolutionPath = ".\PdfDownloader.sln",
  [string]$TestsProject = ".\tests\PdfDownloader.Tests\PdfDownloader.Tests.csproj",
  [string]$OutDir       = ".\docs\test-reports"
)

$ErrorActionPreference = "Stop"

# --- Prep ---
$stamp      = Get-Date -Format "yyyy-MM-dd_HHmmss"
$sessionDir = Join-Path $env:TEMP ("pdfd-tests-" + $stamp)
$newReport  = Join-Path $OutDir ("TestReport_" + $stamp + ".md")

if (-not (Test-Path -LiteralPath $sessionDir)) { New-Item -ItemType Directory -Force -Path $sessionDir | Out-Null }
if (-not (Test-Path -LiteralPath $OutDir))     { New-Item -ItemType Directory -Force -Path $OutDir     | Out-Null }

Write-Host "== Restore =="
dotnet restore $SolutionPath | Out-Host

Write-Host "== Build =="
dotnet build $SolutionPath -c Release --nologo | Out-Host

Write-Host "== Test + Coverage =="

# Skriv midlertidig .runsettings til XPlat Code Coverage (Cobertura + exclude test-libs)
$runSettings = @"
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura</Format>
          <Exclude>[xunit.*]*</Exclude>
          <Exclude>[FluentAssertions*]*</Exclude>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
"@

$runSettingsPath = Join-Path $sessionDir "coverlet.runsettings"
Set-Content -LiteralPath $runSettingsPath -Value $runSettings -Encoding UTF8

$trxName = "test.trx"
dotnet test $TestsProject -c Release `
  --results-directory $sessionDir `
  --logger "trx;LogFileName=$trxName" `
  --collect:"XPlat Code Coverage" `
  --settings $runSettingsPath | Out-Host

# --- Find artefakter ---
$trxFile = Join-Path $sessionDir $trxName
if (-not (Test-Path -LiteralPath $trxFile)) {
  $trxFound = Get-ChildItem -Recurse -Path $sessionDir -Filter *.trx | Select-Object -First 1
  if ($trxFound) { $trxFile = $trxFound.FullName } else { $trxFile = $null }
}
$covFound = Get-ChildItem -Recurse -Path $sessionDir -Filter "coverage.cobertura.xml" | Select-Object -First 1
$covFile  = if ($covFound) { $covFound.FullName } else { $null }

# --- Parse TRX ---
[int]$total = 0
[int]$passed = 0
[int]$failed = 0
[int]$skipped = 0
$failedTests = New-Object System.Collections.ArrayList

if ($trxFile) {
  [xml]$trx = Get-Content -LiteralPath $trxFile
  $resultsNodes = $trx.TestRun.Results.UnitTestResult
  if ($resultsNodes) {
    $resultsArray = @($resultsNodes)
    $total   = $resultsArray.Count
    $passed  = @($resultsArray | Where-Object { $_.outcome -eq "Passed" }).Count
    $failed  = @($resultsArray | Where-Object { $_.outcome -eq "Failed" }).Count
    $skipped = @($resultsArray | Where-Object { $_.outcome -eq "NotExecuted" -or $_.outcome -eq "Skipped" }).Count

    foreach ($r in ($resultsArray | Where-Object { $_.outcome -eq "Failed" })) {
      $testName = $r.testName
      $msg   = $null
      $stack = $null
      if ($r.Output -and $r.Output.ErrorInfo -and $r.Output.ErrorInfo.Message)    { $msg   = ($r.Output.ErrorInfo.Message -join "`n") }
      if ($r.Output -and $r.Output.ErrorInfo -and $r.Output.ErrorInfo.StackTrace) { $stack = ($r.Output.ErrorInfo.StackTrace -join "`n") }
      $null = $failedTests.Add([pscustomobject]@{
        Name  = $testName
        Error = $msg
        Stack = $stack
      })
    }
  }
}

# --- Parse Cobertura coverage ---
$linePctText = "N/A"
$perPackage  = New-Object System.Collections.ArrayList

if ($covFile) {
  [xml]$cov = Get-Content -LiteralPath $covFile

  $globalRate = $cov.coverage.'line-rate'
  if (($null -ne $globalRate) -and ($globalRate -ne "")) {
    $lineRate = [double]::Parse($globalRate, [System.Globalization.CultureInfo]::InvariantCulture)
    $linePct  = [Math]::Round($lineRate * 100, 1)
    $linePctText = "$linePct%"
  }

  $pkgs = $cov.coverage.packages.package
  if ($pkgs) {
    foreach ($p in $pkgs) {
      $classes = $p.classes.class
      if (-not $classes) { continue }
      $linesValid = 0
      $linesCovered = 0
      foreach ($c in $classes) {
        $cLines = $c.lines.line
        if ($cLines) {
          $cLinesArray = @($cLines)
          $countAll = $cLinesArray.Count
          $countHit = @($cLinesArray | Where-Object { ([int]$_.hits) -gt 0 }).Count
          $linesValid   += $countAll
          $linesCovered += $countHit
        }
      }
      if ($linesValid -gt 0) {
        $pkgPct = [Math]::Round(($linesCovered / [double]$linesValid) * 100, 1)
        $null = $perPackage.Add([pscustomobject]@{
          Package      = $p.name
          Coverage     = ($pkgPct.ToString() + "%")
          LinesCovered = $linesCovered
          LinesValid   = $linesValid
        })
      }
    }
    $perPackage = $perPackage | Sort-Object LinesValid -Descending
  }
}

# --- Byg Markdown rapport (ASCII only) ---
$sb = New-Object System.Text.StringBuilder

$null = $sb.AppendLine("# PDF Downloader - Testrapport")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("**Dato:** " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
$null = $sb.AppendLine("**Loesning:** " + $SolutionPath)
$null = $sb.AppendLine("**Testprojekt:** " + $TestsProject)
$null = $sb.AppendLine("")
$null = $sb.AppendLine("## Overblik")
$null = $sb.AppendLine("- Total tests: **" + $total + "**  |  Passed: **" + $passed + "**  |  Failed: **" + $failed + "**  |  Skipped: **" + $skipped + "**")
$null = $sb.AppendLine("- Code coverage (line): **" + $linePctText + "**")
if ($covFile) { $null = $sb.AppendLine("- Cobertura XML: " + $covFile) }
if ($trxFile) { $null = $sb.AppendLine("- TRX: " + $trxFile) }
$null = $sb.AppendLine("")
$null = $sb.AppendLine("## Daekningsdetaljer pr. package (top 10)")
if ($perPackage.Count -gt 0) {
  $top = $perPackage | Select-Object -First 10
  foreach ($row in $top) {
    $null = $sb.AppendLine("- " + $row.Package + " - **" + $row.Coverage + "** (" + $row.LinesCovered + "/" + $row.LinesValid + " lines)")
  }
} else {
  $null = $sb.AppendLine("_Ingen per-package detaljer tilgaengelige (afhaenger af Cobertura layout)._")
}
$null = $sb.AppendLine("")
$null = $sb.AppendLine("## Fejlede tests")
if ($failedTests.Count -gt 0) {
  foreach ($f in $failedTests) {
    $shortMsg = if ($f.Error) { ($f.Error -replace "`r","" -replace "`n"," " ) } else { "(ingen fejltekst)" }
    if ($shortMsg.Length -gt 300) { $shortMsg = $shortMsg.Substring(0,300) + " ..." }
    $null = $sb.AppendLine("* **" + $f.Name + "**")
    $null = $sb.AppendLine("  - Fejl: " + $shortMsg)
  }
} else {
  $null = $sb.AppendLine("Ingen.")
}
$null = $sb.AppendLine("")
$null = $sb.AppendLine("## Hvordan haandterer loesningen fejl og mangler?")
$null = $sb.AppendLine("- HTTP-fejl / Ikke-200: forsoeger naeste URL (fallback).")
$null = $sb.AppendLine("- Content-Type: accepterer application/pdf og application/octet-stream, ignorerer andre.")
$null = $sb.AppendLine("- Manglende URL: markeres som NoUrl i status.")
$null = $sb.AppendLine("- Netvaerksfejl (ikke-cancel): markeres som Failed, men pipeline fortsaetter.")
$null = $sb.AppendLine("- Uaendret indhold: hvis --detect-changes og hash er identisk, markeres som SkippedExisting (ingen overskrivning).")
$null = $sb.AppendLine("- Overwrite + bevar gammel: med --keep-old-on-change omdoebes gammel fil til *.updated[-timestamp].pdf foer overskrivning.")
$null = $sb.AppendLine("- Status-CSV: alle forsoeg logges (Id, Outcome, Message, SourceUrl, SavedFile) til resume og revision.")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("## Refleksion over kodekvalitet")
$null = $sb.AppendLine("- Styrker: klar pipeline (load -> filtrering -> download -> status), robust fallback mellem URL'er, hashing for aendringsdetektion, fortsaetter trods enkeltfejl.")
$null = $sb.AppendLine("- Testbarhed: injektion af HttpMessageHandler goer DownloadManager velegnet til enhedstest inkl. concurrency-maaling.")
$null = $sb.AppendLine("- Ydelse: SemaphoreSlim begraenser samtidige downloads; HttpClient genbruges.")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("## Forbedringsforslag")
$null = $sb.AppendLine("- Retry-policy med eksponentiel backoff pr. URL (fx 2-3 forsoeg) og separat timeout for forbindelses- vs. laesetid.")
$null = $sb.AppendLine("- Struktureret logging (ILogger) til bedre diagnose og metrics (evt. EventId pr. udfald).")
$null = $sb.AppendLine("- Konfigurerbar content-type whitelist og evt. sniff af de foerste bytes (PDF header %PDF-).")
$null = $sb.AppendLine("- Checksum i status (ny kolonne) for hurtig detektion af uaendrede filer uden at aabne eksisterende filer hver gang.")
$null = $sb.AppendLine("- Parallel I/O-tuning: bufferstoerrelser og CopyToAsync med cancellationToken + evt. temp-fil i outputdir for lavere cross-disk I/O.")
$null = $sb.AppendLine("- Robust kolonne-mapping: valgfri alias-liste for Id/Pdf_URL i MetadataLoader for endnu mere 'live data'-tolerance.")
$null = $sb.AppendLine("")

# --- Skriv rapport ---
Set-Content -LiteralPath $newReport -Value $sb.ToString() -Encoding UTF8

# --- Kopier artefakter ---
$trxOut = Join-Path $OutDir ("TRX_" + $stamp + ".trx")
$covOut = Join-Path $OutDir ("COVERAGE_" + $stamp + ".xml")
if ($trxFile) { Copy-Item -LiteralPath $trxFile -Destination $trxOut -Force }
if ($covFile) { Copy-Item -LiteralPath $covFile -Destination $covOut -Force }

Write-Host ""
Write-Host "Testrapport genereret:"
Write-Host (" - " + $newReport)
if ($trxFile -and (Test-Path -LiteralPath $trxOut)) { Write-Host (" - TRX:  " + $trxOut) }
if ($covFile -and (Test-Path -LiteralPath $covOut)) { Write-Host (" - COV:  " + $covOut) }
