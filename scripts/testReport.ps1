param(
  [string]$SolutionPath = ".\PdfDownloader.sln",
  [string]$TestsProject = ".\tests\PdfDownloader.Tests\PdfDownloader.Tests.csproj",
  [string]$OutDir       = ".\docs\test-reports",
  [string]$AppProject   = ".\PDF Downloader\PDF Downloader.csproj",
  [string]$SampleExcel  = ".\samples\GRI_2017_2020 (1).xlsx",
  [int]$LiveLimit       = 20
)

$ErrorActionPreference = "Stop"

# --- Ensartet UTF-8 (konsol + default fil-encodings) ---
try { chcp 65001 > $null } catch {}
$OutputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new()
$PSDefaultParameterValues['Out-File:Encoding']    = 'utf8BOM'
$PSDefaultParameterValues['Set-Content:Encoding'] = 'utf8BOM'
$PSDefaultParameterValues['Add-Content:Encoding'] = 'utf8BOM'
$Utf8BOM = [Text.UTF8Encoding]::new($true)

# ================  Forberedelse  ================
$stamp      = Get-Date -Format "yyyy-MM-dd_HHmmss"
$sessionDir = Join-Path $env:TEMP ("pdfd-tests-" + $stamp)

if (-not (Test-Path -LiteralPath $OutDir)) { New-Item -ItemType Directory -Force -Path $OutDir | Out-Null }
$runDir = Join-Path $OutDir $stamp
if (-not (Test-Path -LiteralPath $runDir)) { New-Item -ItemType Directory -Force -Path $runDir | Out-Null }

$newReport = Join-Path $runDir "TestReport.md"
if (-not (Test-Path -LiteralPath $sessionDir)) { New-Item -ItemType Directory -Force -Path $sessionDir | Out-Null }

# --- Transcript i run-mappen (PS 5.1 → UTF-16; konverteres til sidst) ---
$RawTranscript = Join-Path $runDir "session.transcript.log"
$FinalLog      = Join-Path $runDir "Run.log"
Start-Transcript -Path $RawTranscript -Force | Out-Null

# =========================
#  Hjælpefunktioner (PS 5.1)
# =========================

function Get-Expectation-And-Description {
  param([string]$fullName)
  if ($null -eq $fullName) { $fullName = "" }
  $n = [string]$fullName
  $n = $n.ToLowerInvariant()

  # Kendte tests
  if ($n -like "*statusreporttests*write*read*roundtrip*") {
    return @{ Expected="Rapportfilen skrives og kan læses igen uden datatab."
              Description="Roundtrip af status-CSV (resume + revision)." }
  }
  if ($n -like "*excelintegrationtests*metadata*reads*real*excel*") {
    return @{ Expected="Excel-filen læses korrekt; ID- og URL-kolonner mappes korrekt."
              Description="Indlæsning af 'rigtig' Excel og mapping af BRnum/Pdf_URL." }
  }
  if ($n -like "*metadataloader*excel*" -or $n -like "*metadataloader*csv*") {
    return @{ Expected="Kolonner identificeres; poster med ID + primær/fallback-URL returneres."
              Description="CSV/Excel kolonne-mapping og rækkefølge." }
  }
  if ($n -like "*downloadmanagertests*concurrency*" -or $n -like "*downloadmanagertests*parallel*") {
    return @{ Expected="Samtidighed respekteres (max samtidige downloads)."
              Description="SemaphoreSlim-begrænsning beskytter netværk/system." }
  }
  if ($n -like "*downloadmanagertests*fallback*" -or $n -like "*downloadmanagertests*secondary*") {
    return @{ Expected="Fallback-URL bruges hvis primær fejler; PDF hentes."
              Description="Fallback-logikken mellem primær og sekundær URL." }
  }
  if ($n -like "*downloadmanagertests*skip*existing*" -or $n -like "*downloadmanagertests*detect*changes*") {
    return @{ Expected="Eksisterende filer springes over / overskrives kun ved ændring/valg."
              Description="Undgår unødige downloads via hash/overwrite-flags." }
  }
  if ($n -like "*appoptionstests*") {
    return @{ Expected="CLI-parametre parses korrekt; ugyldige kombinationer afvises."
              Description="Validering af --input/--output/--status m.fl." }
  }
  if ($n -like "*pipelineintegrationtests*") {
    return @{ Expected="End-to-end: metadata læses, nogle PDF'er hentes, status-CSV skrives."
              Description="Hele ruten fra metadata til statusrapport." }
  }

  # Ekstra dæknings-patterns (nye/helpere)
  if ($n -like "*metadatarecord_extrastests*hasanyurl*orderedurls*") {
    return @{ Expected="Detekterer korrekt om der findes en URL og returnerer (primær, fallback) i rækkefølge."
              Description="Unit test af MetadataRecord.HasAnyUrl og GetOrderedUrls()." }
  }
  if ($n -like "*downloadmanager_nourltests*nourl*yields*nourl*") {
    return @{ Expected="Rækker uden URL markeres som NoUrl med beskeden 'No URL'."
              Description="Validerer korrekt outcome ved manglende URL." }
  }
  if ($n -like "*downloadmanager_timeouttests*timeout*reported*") {
    return @{ Expected="HTTP-timeout rapporteres som 'Timeout' og outcome=Failed."
              Description="Mapning af TaskCanceledException til Timeout." }
  }
  if ($n -like "*downloadmanager_keepoldonchangetests*changed*renames*") {
    return @{ Expected="Ved ændret indhold + keep-old-on-change oprettes *.updated*.pdf."
              Description="Bevarer tidligere version ved overskrivning." }
  }
  if ($n -like "*downloadmanager_keepoldonchangetests*overwrite*without*keep*") {
    return @{ Expected="Ved overskrivning uden 'keep' oprettes ingen *.updated*.pdf."
              Description="Kun ny fil eksisterer." }
  }
  if ($n -like "*statusreportreader_readalltests*readall*handles*reordered*") {
    return @{ Expected="ReadAll kan læse status-CSV uanset kolonne-rækkefølge og manglende felter."
              Description="Robust CSV-parsing via navne/indeks fallback." }
  }
  if ($n -like "*downloadmanager_slotstatstests*slotstats*counts*all*jobs*") {
    return @{ Expected="Slot-statistik summerer alle jobs; antal slots matcher maxConcurrency."
              Description="Verificerer optælling og gennemsnit pr. slot." }
  }

  return @{ Expected="Testen forventes at lykkes uden fejl."
            Description="Tester en del af PDF Downloaderen." }
}

function Format-Outcome {
  param([string]$o)
  switch ($o) {
    "Passed"      { "Bestod"; break }
    "Failed"      { "Fejlede"; break }
    "NotExecuted" { "Skippet"; break }
    "Skipped"     { "Skippet"; break }
    default       { $o }
  }
}

function GetOutcomeExplanation {
  param([string]$Outcome)
  switch ($Outcome) {
    "Downloaded"      { "PDF gemt korrekt" }
    "SkippedExisting" { "Filen findes allerede" }
    "Failed"          { "Fejl (HTTP, IO, timeout, forkert content-type)" }
    "NoUrl"           { "Mangler gyldig URL i metadata" }
    default           { "" }
  }
}

Write-Host "== Restore =="; dotnet restore $SolutionPath | Out-Host
Write-Host "== Build ==";   dotnet build $SolutionPath -c Release --nologo | Out-Host

Write-Host "== Test + Coverage =="

# Runsettings til XPlat Code Coverage
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

# ===================  Find artefakter  ===================
$trxFile = Join-Path $sessionDir $trxName
if (-not (Test-Path -LiteralPath $trxFile)) {
  $trxFound = Get-ChildItem -Recurse -Path $sessionDir -Filter *.trx | Select-Object -First 1
  if ($trxFound) { $trxFile = $trxFound.FullName } else { $trxFile = $null }
}
$covFound = Get-ChildItem -Recurse -Path $sessionDir -Filter "coverage.cobertura.xml" | Select-Object -First 1
if ($covFound) { $covFile = $covFound.FullName } else { $covFile = $null }

# ==========  Parse TRX → totaler + test-liste  ==========
[int]$total = 0; [int]$passed = 0; [int]$failed = 0; [int]$skipped = 0
$allTests = @()

if ($trxFile) {
  [xml]$trx = Get-Content -LiteralPath $trxFile
  $resultsNodes = $trx.TestRun.Results.UnitTestResult
  if ($resultsNodes) {
    $resultsArray = @($resultsNodes)
    $total   = $resultsArray.Count
    $passed  = @($resultsArray | Where-Object { $_.outcome -eq "Passed" }).Count
    $failed  = @($resultsArray | Where-Object { $_.outcome -eq "Failed" }).Count
    $skipped = @($resultsArray | Where-Object { $_.outcome -eq "NotExecuted" -or $_.outcome -eq "Skipped" }).Count

    foreach ($r in $resultsArray) {
      $name    = $r.testName
      $outcome = $r.outcome
      $duration= $r.duration
      $msg     = $null
      if ($r.Output -and $r.Output.ErrorInfo -and $r.Output.ErrorInfo.Message) {
        $msg = ($r.Output.ErrorInfo.Message -join " ")
      }

      # Fuld klasse.metode
      $full = $name
      if ($trx.TestRun.TestDefinitions -and $trx.TestRun.TestDefinitions.UnitTest) {
        $match = @($trx.TestRun.TestDefinitions.UnitTest | Where-Object { $_.name -eq $name }) | Select-Object -First 1
        if ($match -and $match.TestMethod) {
          $className = $match.TestMethod.className
          if ($className) { $full = "$className.$name" }
        }
      }

      $ex = Get-Expectation-And-Description -fullName $full
      $sortKey = 0; if ($outcome -eq 'Passed') { $sortKey = 1 }  # Fejl/skips først
      $t = [pscustomobject]@{
        Name        = $name
        FullName    = $full
        Outcome     = $outcome
        OutcomeText = (Format-Outcome -o $outcome)
        Duration    = $duration
        Message     = $msg
        Expected    = $ex.Expected
        Description = $ex.Description
        SortKey     = $sortKey
      }
      $allTests += $t
    }
    $allTests = $allTests | Sort-Object -Property SortKey, Name
  }
}

# ==========  Parse Cobertura coverage XML  ==========
$linePctText = "N/A"; $perPackage  = @()
if ($covFile) {
  [xml]$cov = Get-Content -LiteralPath $covFile
  $globalRate = $cov.coverage.'line-rate'
  if ($null -ne $globalRate -and $globalRate -ne "") {
    $lineRate = [double]::Parse($globalRate, [System.Globalization.CultureInfo]::InvariantCulture)
    $linePct  = [Math]::Round($lineRate * 100, 1)
    $linePctText = "$linePct%"
  }
  $pkgs = $cov.coverage.packages.package
  if ($pkgs) {
    foreach ($p in $pkgs) {
      $classes = $p.classes.class
      if (-not $classes) { continue }
      $linesValid = 0; $linesCovered = 0
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
        $perPackage += [pscustomobject]@{
          Package      = $p.name
          Coverage     = ($pkgPct.ToString() + "%")
          LinesCovered = $linesCovered
          LinesValid   = $linesValid
        }
      }
    }
    $perPackage = $perPackage | Sort-Object -Property LinesValid -Descending
  }
}

# ==========  LIVE PRØVE: 20 rækker, concurrency test  ==========
Write-Host "== Live prøve (20 rækker) =="
$samplePath = Resolve-Path $SampleExcel
$serialOut  = Join-Path $sessionDir "dl-serial"
$parallelOut= Join-Path $sessionDir "dl-parallel"
$statusSer  = Join-Path $sessionDir "status-serial.csv"
$statusPar  = Join-Path $sessionDir "status-parallel.csv"

foreach ($p in @($serialOut,$parallelOut)) { if (-not (Test-Path $p)) { New-Item -ItemType Directory -Path $p | Out-Null } }

function Invoke-AppRun {
  param([int]$MaxConc,[string]$OutDir,[string]$StatusFile)
  $args = @(
    "--input", $samplePath.Path,
    "--output", (Resolve-Path $OutDir).Path,
    "--status", $StatusFile,
    "--limit",  [string]$LiveLimit,
    "--max-concurrency", [string]$MaxConc,
    "--overwrite-status"
  )
  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  dotnet run --project $AppProject -- $args | Out-Host
  $sw.Stop()
  return $sw.Elapsed
}

Write-Host "Kører SERIEL (max-concurrency=1)..."
$durSer = Invoke-AppRun -MaxConc 1 -OutDir $serialOut -StatusFile $statusSer

Write-Host "Kører PARALLEL (max-concurrency=5)..."
$durPar = Invoke-AppRun -MaxConc 5 -OutDir $parallelOut -StatusFile $statusPar

function Read-Status {
  param([string]$file)
  $rows = @()
  if (Test-Path -LiteralPath $file) { $rows = Import-Csv -LiteralPath $file }
  return ,$rows
}

$serRows = Read-Status -file $statusSer
$parRows = Read-Status -file $statusPar

function Summarize {
  param($rows)
  $sum = @{ Downloaded=0; SkippedExisting=0; Failed=0; NoUrl=0; Other=0 }
  foreach ($r in $rows) {
    $o = [string]$r.Outcome
    if     ($o -eq "Downloaded")      { $sum.Downloaded++ }
    elseif ($o -eq "SkippedExisting") { $sum.SkippedExisting++ }
    elseif ($o -eq "Failed")          { $sum.Failed++ }
    elseif ($o -eq "NoUrl")           { $sum.NoUrl++ }
    else                               { $sum.Other++ }
  }
  return $sum
}

$serSum = Summarize -rows $serRows
$parSum = Summarize -rows $parRows

# =======================  Byg Markdown rapport  =======================
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

$trxOut = Join-Path $runDir "test.trx"
$covOut = Join-Path $runDir "coverage.cobertura.xml"
$serOut = Join-Path $runDir "status-serial.csv"
$parOut = Join-Path $runDir "status-parallel.csv"

$null = $sb.AppendLine("- Cobertura XML: " + $covOut)
$null = $sb.AppendLine("- TRX: " + $trxOut)
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## Testcases (forklaret for alle)")
if ($allTests.Count -gt 0) {
  foreach ($t in $allTests) {
    $shortMsg = $t.Message
    if ($shortMsg) {
      $shortMsg = $shortMsg -replace "`r",""
      $shortMsg = $shortMsg -replace "`n"," "
      if ($shortMsg.Length -gt 300) { $shortMsg = $shortMsg.Substring(0,300) + " ..." }
    }
    $durText = ""; if ($t.Duration) { $durText = " (" + $t.Duration + ")" }

    $null = $sb.AppendLine("* **" + $t.Name + "**")
    $null = $sb.AppendLine("  - Hvad tester den: " + $t.Description)
    $null = $sb.AppendLine("  - Forventet resultat: " + $t.Expected)
    $null = $sb.AppendLine("  - Resultat: " + $t.OutcomeText + $durText)
    if ($t.Outcome -eq "Failed" -and $shortMsg) {
      $null = $sb.AppendLine("  - Fejlbesked (kort): " + $shortMsg)
    }
    $null = $sb.AppendLine("")
  }
} else {
  $null = $sb.AppendLine("_Ingen tests blev registreret i TRX._")
}
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## Live prøve: 20 downloads med og uden samtidighed")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("**Kørsler**")
$null = $sb.AppendLine("- Seriel (max-concurrency=1): " + $durSer.ToString())
$null = $sb.AppendLine("- Parallel (max-concurrency=5): " + $durPar.ToString())
$speedup = ""
if ($durPar.TotalMilliseconds -gt 0) {
  $ratio = [Math]::Round(($durSer.TotalMilliseconds / $durPar.TotalMilliseconds), 2)
  $speedup = " (ca. " + $ratio + "x hurtigere)"
}
$null = $sb.AppendLine("- Effekt af samtidighed: Parallel vs. Seriel" + $speedup)
$null = $sb.AppendLine("")
$null = $sb.AppendLine("**Resultater (parallel kørsel, første " + $LiveLimit + " rækker)**")
$null = $sb.AppendLine("- Downloaded: **" + $parSum.Downloaded + "**  |  Skipped: **" + $parSum.SkippedExisting + "**  |  Failed: **" + $parSum.Failed + "**  |  NoUrl: **" + $parSum.NoUrl + "**")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("### Detaljer pr. PDF (parallel)")
if ($parRows.Count -gt 0) {
  foreach ($r in $parRows) {
    $msg = [string]$r.Message
    if ($msg) {
      $msg = $msg -replace "`r",""
      $msg = $msg -replace "`n"," "
      if ($msg.Length -gt 200) { $msg = $msg.Substring(0,200) + " ..." }
    }
    $explain = GetOutcomeExplanation -Outcome ([string]$r.Outcome)
    $line = "* " + $r.Id + " - " + $r.Outcome
    if ($explain -and $explain -ne "") { $line = $line + " - " + $explain }
    if ($msg) { $line = $line + " - " + $msg }
    $null = $sb.AppendLine($line)
  }
} else {
  $null = $sb.AppendLine("_Ingen rækker fundet i status-filen._")
}
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## Dækning pr. package (top 10)")
if ($perPackage.Count -gt 0) {
  $top = $perPackage | Select-Object -First 10
  foreach ($row in $top) {
    $null = $sb.AppendLine("- " + $row.Package + " - **" + $row.Coverage + "** (" + $row.LinesCovered + "/" + $row.LinesValid + " lines)")
  }
} else {
  $null = $sb.AppendLine("_Ingen per-package detaljer tilgængelige (afhænger af Cobertura layout)._")
}
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## Hvordan håndterer løsningen fejl og mangler?")
$null = $sb.AppendLine("- HTTP-fejl / Ikke-200: forsøger næste URL (fallback).")
$null = $sb.AppendLine("- Content-Type: accepterer application/pdf og application/octet-stream, ignorerer andre.")
$null = $sb.AppendLine("- Manglende URL: markeres som NoUrl i status.")
$null = $sb.AppendLine("- Netværksfejl (ikke-cancel): markeres som Failed, men pipeline fortsætter.")
$null = $sb.AppendLine("- Uændret indhold: hvis --detect-changes og hash er identisk, markeres som SkippedExisting (ingen overskrivning).")
$null = $sb.AppendLine("- Overwrite + bevar gammel: med --keep-old-on-change omdøbes gammel fil til *.updated[-timestamp].pdf før overskrivning.")
$null = $sb.AppendLine("- Status-CSV: alle forsøg logges (Id, Outcome, Message, SourceUrl, SavedFile) til resume og revision.")
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## Refleksion over kodekvalitet")
$null = $sb.AppendLine("- Styrker: klar pipeline (load -> filtrering -> download -> status), robust fallback mellem URL'er, hashing for ændringsdetektion, fortsætter trods enkeltfejl.")
$null = $sb.AppendLine("- Testbarhed: injektion af HttpMessageHandler gør DownloadManager velegnet til enhedstest inkl. concurrency-måling.")
$null = $sb.AppendLine("- Ydelse: SemaphoreSlim begrænser samtidige downloads; HttpClient genbruges.")
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## Forbedringsforslag")
$null = $sb.AppendLine("- Retry-policy med eksponentiel backoff pr. URL (fx 2-3 forsøg) og separat timeout for forbindelses- vs. læsetid.")
$null = $sb.AppendLine("- Struktureret logging (ILogger) til bedre diagnose og metrics (evt. EventId pr. udfald).")
$null = $sb.AppendLine("- Konfigurerbar content-type whitelist og evt. sniff af de første bytes (PDF header %PDF-).")
$null = $sb.AppendLine("- Checksum i status (ny kolonne) for hurtig detektion af uændrede filer uden at åbne eksisterende filer hver gang.")
$null = $sb.AppendLine("- Parallel I/O-tuning: bufferstørrelser og CopyToAsync med cancellationToken + evt. temp-fil i outputdir for lavere cross-disk I/O.")
$null = $sb.AppendLine("- Robust kolonne-mapping: valgfri alias-liste for Id/Pdf_URL i MetadataLoader for endnu mere 'live data'-tolerance.")
$null = $sb.AppendLine("")

# ===================  Skriv/kopier filer  ===================
[IO.File]::WriteAllText($newReport, $sb.ToString(), $Utf8BOM)

# Kopiér artefakter fra temp-session til run-mappe
if ($trxFile) { Copy-Item -LiteralPath $trxFile -Destination $trxOut -Force }
if ($covFile) { Copy-Item -LiteralPath $covFile -Destination $covOut -Force }

# Live-run status-CSV'er
if (Test-Path $statusSer) { Copy-Item -LiteralPath $statusSer -Destination $serOut -Force }
if (Test-Path $statusPar) { Copy-Item -LiteralPath $statusPar -Destination $parOut -Force }

Write-Host ""
Write-Host "Testrapport genereret i run-mappe:"
Write-Host (" - " + $newReport)
if (Test-Path $trxOut) { Write-Host (" - TRX:  " + $trxOut) }
if (Test-Path $covOut) { Write-Host (" - COV:  " + $covOut) }
if (Test-Path $serOut) { Write-Host (" - STATUS (seriel):    " + $serOut) }
if (Test-Path $parOut) { Write-Host (" - STATUS (parallel):  " + $parOut) }

# ----- Stop transcript og konverter til UTF-8 med BOM -----
try { Stop-Transcript | Out-Null } catch {}

if (Test-Path -LiteralPath $RawTranscript) {
  try {
    $rawText = Get-Content -LiteralPath $RawTranscript -Raw -Encoding Unicode
    [IO.File]::WriteAllText($FinalLog, $rawText, $Utf8BOM)
  } catch {
    Write-Warning "Kunne ikke konvertere transcript til UTF-8: $_"
  } finally {
    Remove-Item -LiteralPath $RawTranscript -Force -ErrorAction SilentlyContinue
  }
}

Write-Host (" - LOG:  " + $FinalLog)
