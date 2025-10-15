# PDF Downloader - Testrapport

**Dato:** 2025-10-15 10:13:42
**Loesning:** .\PdfDownloader.sln
**Testprojekt:** .\tests\PdfDownloader.Tests\PdfDownloader.Tests.csproj

## Overblik
- Total tests: **18**  |  Passed: **18**  |  Failed: **0**  |  Skipped: **0**
- Code coverage (line): **84%**
- Cobertura XML: .\docs\test-reports\2025-10-15_101300\coverage.cobertura.xml
- TRX: .\docs\test-reports\2025-10-15_101300\test.trx

## Testcases (forklaret for alle)
* **PdfDownloader.Tests.Integration.ExcelIntegrationTests.MetadataLoader_reads_real_excel_and_maps_BRnum_and_PdfURL**
  - Hvad tester den: Indlæsning af 'rigtig' Excel og mapping af BRnum/Pdf_URL.
  - Forventet resultat: Excel-filen læses korrekt; ID- og URL-kolonner mappes korrekt.
  - Resultat: Bestod (00:00:07.6531416)

* **PdfDownloader.Tests.Integration.PipelineIntegrationTests.Csv_to_statusfile_roundtrip_smoke**
  - Hvad tester den: Hele ruten fra metadata til statusrapport.
  - Forventet resultat: End-to-end: metadata læses, nogle PDF'er hentes, status-CSV skrives.
  - Resultat: Bestod (00:00:10.9445128)

* **PdfDownloader.Tests.Unit.AppOptionsTests.KeepOldOnChange_Is_Implied_By_OverwriteDownloads**
  - Hvad tester den: Validering af --input/--output/--status m.fl.
  - Forventet resultat: CLI-parametre parses korrekt; ugyldige kombinationer afvises.
  - Resultat: Bestod (00:00:00.0001624)

* **PdfDownloader.Tests.Unit.AppOptionsTests.NoSkipExisting_Flag_Disables_Skip**
  - Hvad tester den: Validering af --input/--output/--status m.fl.
  - Forventet resultat: CLI-parametre parses korrekt; ugyldige kombinationer afvises.
  - Resultat: Bestod (00:00:00.0001157)

* **PdfDownloader.Tests.Unit.AppOptionsTests.Parse_Defaults_And_Basics**
  - Hvad tester den: Validering af --input/--output/--status m.fl.
  - Forventet resultat: CLI-parametre parses korrekt; ugyldige kombinationer afvises.
  - Resultat: Bestod (00:00:00.0027271)

* **PdfDownloader.Tests.Unit.AppOptionsTests.Parse_MutuallyExclusive_StatusFlags**
  - Hvad tester den: Validering af --input/--output/--status m.fl.
  - Forventet resultat: CLI-parametre parses korrekt; ugyldige kombinationer afvises.
  - Resultat: Bestod (00:00:00.0070320)

* **PdfDownloader.Tests.Unit.AppOptionsTests.Unknown_Extension_Throws**
  - Hvad tester den: Validering af --input/--output/--status m.fl.
  - Forventet resultat: CLI-parametre parses korrekt; ugyldige kombinationer afvises.
  - Resultat: Bestod (00:00:00.0115880)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.DetectChanges_Overwrites_When_Changed_Even_Without_OverwriteDownloads**
  - Hvad tester den: Undgår unødige downloads via hash/overwrite-flags.
  - Forventet resultat: Eksisterende filer springes over / overskrives kun ved ændring/valg.
  - Resultat: Bestod (00:00:00.0110074)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.KeepOldOnChange_Renames_Old_File**
  - Hvad tester den: Tester en del af PDF Downloaderen.
  - Forventet resultat: Testen forventes at lykkes uden fejl.
  - Resultat: Bestod (00:00:00.0145634)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.NonPdf_ContentType_Surfaces_As_ContentType_Reason**
  - Hvad tester den: Tester en del af PDF Downloaderen.
  - Forventet resultat: Testen forventes at lykkes uden fejl.
  - Resultat: Bestod (00:00:00.0012408)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.NoUrl_Results_In_NoUrl_Outcome**
  - Hvad tester den: Tester en del af PDF Downloaderen.
  - Forventet resultat: Testen forventes at lykkes uden fejl.
  - Resultat: Bestod (00:00:00.0022585)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.OverwriteDownloads_True_With_KeepOldOnChange_Renames_Old_File**
  - Hvad tester den: Tester en del af PDF Downloaderen.
  - Forventet resultat: Testen forventes at lykkes uden fejl.
  - Resultat: Bestod (00:00:00.0515423)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.Respects_MaxConcurrency**
  - Hvad tester den: SemaphoreSlim-begrænsning beskytter netværk/system.
  - Forventet resultat: Samtidighed respekteres (max samtidige downloads).
  - Resultat: Bestod (00:00:00.6655219)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.SkipExisting_When_NoOverwrite_And_NoChange**
  - Hvad tester den: Undgår unødige downloads via hash/overwrite-flags.
  - Forventet resultat: Eksisterende filer springes over / overskrives kun ved ændring/valg.
  - Resultat: Bestod (00:00:00.0076082)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.Skips_NonPdf_ContentType_Except_OctetStream**
  - Hvad tester den: Tester en del af PDF Downloaderen.
  - Forventet resultat: Testen forventes at lykkes uden fejl.
  - Resultat: Bestod (00:00:00.0316931)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.Timeout_Is_Reported_As_Failed_With_Timeout_Message**
  - Hvad tester den: Tester en del af PDF Downloaderen.
  - Forventet resultat: Testen forventes at lykkes uden fejl.
  - Resultat: Bestod (00:00:00.0022715)

* **PdfDownloader.Tests.Unit.MetadataLoaderTests.Load_From_Csv_With_Fallback**
  - Hvad tester den: CSV/Excel kolonne-mapping og rækkefølge.
  - Forventet resultat: Kolonner identificeres; poster med ID + primær/fallback-URL returneres.
  - Resultat: Bestod (00:00:00.0284828)

* **PdfDownloader.Tests.Unit.MetadataLoaderTests.Load_From_Excel_Maps_Headers_CaseInsensitive**
  - Hvad tester den: CSV/Excel kolonne-mapping og rækkefølge.
  - Forventet resultat: Kolonner identificeres; poster med ID + primær/fallback-URL returneres.
  - Resultat: Bestod (00:00:00.9051162)


## Live prøve: 20 downloads med og uden samtidighed

**Kørsler**
- Seriel (max-concurrency=1): 00:00:14.8683544
- Parallel (max-concurrency=5): 00:00:09.2294262
- Effekt af samtidighed: Parallel vs. Seriel (ca. 1.61x hurtigere)

**Resultater (parallel kørsel, første 20 rækker)**
- Downloaded: **10**  |  Skipped: **0**  |  Failed: **10**  |  NoUrl: **0**

### Detaljer pr. PDF (parallel)
* BR50060 - Downloaded - PDF gemt korrekt
* BR50058 - Downloaded - PDF gemt korrekt
* BR50057 - Downloaded - PDF gemt korrekt
* BR50042 - Downloaded - PDF gemt korrekt
* BR50055 - Downloaded - PDF gemt korrekt
* BR50054 - Downloaded - PDF gemt korrekt
* BR50061 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 404
* BR50052 - Downloaded - PDF gemt korrekt
* BR50059 - Downloaded - PDF gemt korrekt
* BR50056 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 404
* BR50047 - Downloaded - PDF gemt korrekt
* BR50051 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - Content-Type: text/html
* BR50045 - Downloaded - PDF gemt korrekt
* BR50053 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 404
* BR50050 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 404
* BR50049 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 406
* BR50041 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - Content-Type: text/html
* BR50048 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 404
* BR50043 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 403
* BR50044 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - Exception: NotSupportedException

## Dækning pr. package (top 10)
_Ingen per-package detaljer tilgængelige (afhænger af Cobertura layout)._

## Hvordan håndterer løsningen fejl og mangler?
- HTTP-fejl / Ikke-200: forsøger næste URL (fallback).
- Content-Type: accepterer application/pdf og application/octet-stream, ignorerer andre.
- Manglende URL: markeres som NoUrl i status.
- Netværksfejl (ikke-cancel): markeres som Failed, men pipeline fortsætter.
- Uændret indhold: hvis --detect-changes og hash er identisk, markeres som SkippedExisting (ingen overskrivning).
- Overwrite + bevar gammel: med --keep-old-on-change omdøbes gammel fil til *.updated[-timestamp].pdf før overskrivning.
- Status-CSV: alle forsøg logges (Id, Outcome, Message, SourceUrl, SavedFile) til resume og revision.

## Refleksion over kodekvalitet
- Styrker: klar pipeline (load -> filtrering -> download -> status), robust fallback mellem URL'er, hashing for ændringsdetektion, fortsætter trods enkeltfejl.
- Testbarhed: injektion af HttpMessageHandler gør DownloadManager velegnet til enhedstest inkl. concurrency-måling.
- Ydelse: SemaphoreSlim begrænser samtidige downloads; HttpClient genbruges.

## Forbedringsforslag
- Retry-policy med eksponentiel backoff pr. URL (fx 2-3 forsøg) og separat timeout for forbindelses- vs. læsetid.
- Struktureret logging (ILogger) til bedre diagnose og metrics (evt. EventId pr. udfald).
- Konfigurerbar content-type whitelist og evt. sniff af de første bytes (PDF header %PDF-).
- Checksum i status (ny kolonne) for hurtig detektion af uændrede filer uden at åbne eksisterende filer hver gang.
- Parallel I/O-tuning: bufferstørrelser og CopyToAsync med cancellationToken + evt. temp-fil i outputdir for lavere cross-disk I/O.
- Robust kolonne-mapping: valgfri alias-liste for Id/Pdf_URL i MetadataLoader for endnu mere 'live data'-tolerance.

