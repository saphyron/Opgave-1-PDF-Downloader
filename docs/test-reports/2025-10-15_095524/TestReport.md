# PDF Downloader - Testrapport

**Dato:** 2025-10-15 09:56:26
**Loesning:** .\PdfDownloader.sln
**Testprojekt:** .\tests\PdfDownloader.Tests\PdfDownloader.Tests.csproj

## Overblik
- Total tests: **18**  |  Passed: **18**  |  Failed: **0**  |  Skipped: **0**
- Code coverage (line): **84%**
- Cobertura XML: .\docs\test-reports\2025-10-15_095524\coverage.cobertura.xml
- TRX: .\docs\test-reports\2025-10-15_095524\test.trx

## Testcases (forklaret for alle)
* **PdfDownloader.Tests.Integration.ExcelIntegrationTests.MetadataLoader_reads_real_excel_and_maps_BRnum_and_PdfURL**
  - Hvad tester den: IndlÃ¦sning af 'rigtig' Excel og mapping af BRnum/Pdf_URL.
  - Forventet resultat: Excel-filen lÃ¦ses korrekt; ID- og URL-kolonner mappes korrekt.
  - Resultat: Bestod (00:00:08.2343519)

* **PdfDownloader.Tests.Integration.PipelineIntegrationTests.Csv_to_statusfile_roundtrip_smoke**
  - Hvad tester den: Hele ruten fra metadata til statusrapport.
  - Forventet resultat: End-to-end: metadata lÃ¦ses, nogle PDF'er hentes, status-CSV skrives.
  - Resultat: Bestod (00:00:11.1613775)

* **PdfDownloader.Tests.Unit.AppOptionsTests.KeepOldOnChange_Is_Implied_By_OverwriteDownloads**
  - Hvad tester den: Validering af --input/--output/--status m.fl.
  - Forventet resultat: CLI-parametre parses korrekt; ugyldige kombinationer afvises.
  - Resultat: Bestod (00:00:00.0002099)

* **PdfDownloader.Tests.Unit.AppOptionsTests.NoSkipExisting_Flag_Disables_Skip**
  - Hvad tester den: Validering af --input/--output/--status m.fl.
  - Forventet resultat: CLI-parametre parses korrekt; ugyldige kombinationer afvises.
  - Resultat: Bestod (00:00:00.0001549)

* **PdfDownloader.Tests.Unit.AppOptionsTests.Parse_Defaults_And_Basics**
  - Hvad tester den: Validering af --input/--output/--status m.fl.
  - Forventet resultat: CLI-parametre parses korrekt; ugyldige kombinationer afvises.
  - Resultat: Bestod (00:00:00.0030976)

* **PdfDownloader.Tests.Unit.AppOptionsTests.Parse_MutuallyExclusive_StatusFlags**
  - Hvad tester den: Validering af --input/--output/--status m.fl.
  - Forventet resultat: CLI-parametre parses korrekt; ugyldige kombinationer afvises.
  - Resultat: Bestod (00:00:00.0079922)

* **PdfDownloader.Tests.Unit.AppOptionsTests.Unknown_Extension_Throws**
  - Hvad tester den: Validering af --input/--output/--status m.fl.
  - Forventet resultat: CLI-parametre parses korrekt; ugyldige kombinationer afvises.
  - Resultat: Bestod (00:00:00.0129949)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.DetectChanges_Overwrites_When_Changed_Even_Without_OverwriteDownloads**
  - Hvad tester den: UndgÃ¥r unÃ¸dige downloads via hash/overwrite-flags.
  - Forventet resultat: Eksisterende filer springes over / overskrives kun ved Ã¦ndring/valg.
  - Resultat: Bestod (00:00:00.0081627)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.KeepOldOnChange_Renames_Old_File**
  - Hvad tester den: Tester en del af PDF Downloaderen.
  - Forventet resultat: Testen forventes at lykkes uden fejl.
  - Resultat: Bestod (00:00:00.0196279)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.NonPdf_ContentType_Surfaces_As_ContentType_Reason**
  - Hvad tester den: Tester en del af PDF Downloaderen.
  - Forventet resultat: Testen forventes at lykkes uden fejl.
  - Resultat: Bestod (00:00:00.0019695)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.NoUrl_Results_In_NoUrl_Outcome**
  - Hvad tester den: Tester en del af PDF Downloaderen.
  - Forventet resultat: Testen forventes at lykkes uden fejl.
  - Resultat: Bestod (00:00:00.0025704)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.OverwriteDownloads_True_With_KeepOldOnChange_Renames_Old_File**
  - Hvad tester den: Tester en del af PDF Downloaderen.
  - Forventet resultat: Testen forventes at lykkes uden fejl.
  - Resultat: Bestod (00:00:00.0407435)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.Respects_MaxConcurrency**
  - Hvad tester den: SemaphoreSlim-begrÃ¦nsning beskytter netvÃ¦rk/system.
  - Forventet resultat: Samtidighed respekteres (max samtidige downloads).
  - Resultat: Bestod (00:00:00.6492163)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.SkipExisting_When_NoOverwrite_And_NoChange**
  - Hvad tester den: UndgÃ¥r unÃ¸dige downloads via hash/overwrite-flags.
  - Forventet resultat: Eksisterende filer springes over / overskrives kun ved Ã¦ndring/valg.
  - Resultat: Bestod (00:00:00.0076404)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.Skips_NonPdf_ContentType_Except_OctetStream**
  - Hvad tester den: Tester en del af PDF Downloaderen.
  - Forventet resultat: Testen forventes at lykkes uden fejl.
  - Resultat: Bestod (00:00:00.0327119)

* **PdfDownloader.Tests.Unit.DownloadManagerTests.Timeout_Is_Reported_As_Failed_With_Timeout_Message**
  - Hvad tester den: Tester en del af PDF Downloaderen.
  - Forventet resultat: Testen forventes at lykkes uden fejl.
  - Resultat: Bestod (00:00:00.0020012)

* **PdfDownloader.Tests.Unit.MetadataLoaderTests.Load_From_Csv_With_Fallback**
  - Hvad tester den: CSV/Excel kolonne-mapping og rÃ¦kkefÃ¸lge.
  - Forventet resultat: Kolonner identificeres; poster med ID + primÃ¦r/fallback-URL returneres.
  - Resultat: Bestod (00:00:00.0325217)

* **PdfDownloader.Tests.Unit.MetadataLoaderTests.Load_From_Excel_Maps_Headers_CaseInsensitive**
  - Hvad tester den: CSV/Excel kolonne-mapping og rÃ¦kkefÃ¸lge.
  - Forventet resultat: Kolonner identificeres; poster med ID + primÃ¦r/fallback-URL returneres.
  - Resultat: Bestod (00:00:01.0524885)


## Live prÃ¸ve: 20 downloads med og uden samtidighed

**KÃ¸rsler**
- Seriel (max-concurrency=1): 00:00:30.5809236
- Parallel (max-concurrency=5): 00:00:09.7328485
- Effekt af samtidighed: Parallel vs. Seriel (ca. 3.14x hurtigere)

**Resultater (parallel kÃ¸rsel, fÃ¸rste 20 rÃ¦kker)**
- Downloaded: **10**  |  Skipped: **0**  |  Failed: **10**  |  NoUrl: **0**

### Detaljer pr. PDF (parallel)
* BR50052 - Downloaded - PDF gemt korrekt
* BR50061 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 404
* BR50060 - Downloaded - PDF gemt korrekt
* BR50058 - Downloaded - PDF gemt korrekt
* BR50051 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - Content-Type: text/html
* BR50049 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 406
* BR50047 - Downloaded - PDF gemt korrekt
* BR50059 - Downloaded - PDF gemt korrekt
* BR50057 - Downloaded - PDF gemt korrekt
* BR50055 - Downloaded - PDF gemt korrekt
* BR50045 - Downloaded - PDF gemt korrekt
* BR50050 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 404
* BR50041 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - Content-Type: text/html
* BR50054 - Downloaded - PDF gemt korrekt
* BR50056 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 404
* BR50042 - Downloaded - PDF gemt korrekt
* BR50053 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 404
* BR50048 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 404
* BR50043 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - HTTP 403
* BR50044 - Failed - Fejl (HTTP, IO, timeout, forkert content-type) - Exception: NotSupportedException

## DÃ¦kning pr. package (top 10)
_Ingen per-package detaljer tilgÃ¦ngelige (afhÃ¦nger af Cobertura layout)._

## Hvordan hÃ¥ndterer lÃ¸sningen fejl og mangler?
- HTTP-fejl / Ikke-200: forsÃ¸ger nÃ¦ste URL (fallback).
- Content-Type: accepterer application/pdf og application/octet-stream, ignorerer andre.
- Manglende URL: markeres som NoUrl i status.
- NetvÃ¦rksfejl (ikke-cancel): markeres som Failed, men pipeline fortsÃ¦tter.
- UÃ¦ndret indhold: hvis --detect-changes og hash er identisk, markeres som SkippedExisting (ingen overskrivning).
- Overwrite + bevar gammel: med --keep-old-on-change omdÃ¸bes gammel fil til *.updated[-timestamp].pdf fÃ¸r overskrivning.
- Status-CSV: alle forsÃ¸g logges (Id, Outcome, Message, SourceUrl, SavedFile) til resume og revision.

## Refleksion over kodekvalitet
- Styrker: klar pipeline (load -> filtrering -> download -> status), robust fallback mellem URL'er, hashing for Ã¦ndringsdetektion, fortsÃ¦tter trods enkeltfejl.
- Testbarhed: injektion af HttpMessageHandler gÃ¸r DownloadManager velegnet til enhedstest inkl. concurrency-mÃ¥ling.
- Ydelse: SemaphoreSlim begrÃ¦nser samtidige downloads; HttpClient genbruges.

## Forbedringsforslag
- Retry-policy med eksponentiel backoff pr. URL (fx 2-3 forsÃ¸g) og separat timeout for forbindelses- vs. lÃ¦setid.
- Struktureret logging (ILogger) til bedre diagnose og metrics (evt. EventId pr. udfald).
- Konfigurerbar content-type whitelist og evt. sniff af de fÃ¸rste bytes (PDF header %PDF-).
- Checksum i status (ny kolonne) for hurtig detektion af uÃ¦ndrede filer uden at Ã¥bne eksisterende filer hver gang.
- Parallel I/O-tuning: bufferstÃ¸rrelser og CopyToAsync med cancellationToken + evt. temp-fil i outputdir for lavere cross-disk I/O.
- Robust kolonne-mapping: valgfri alias-liste for Id/Pdf_URL i MetadataLoader for endnu mere 'live data'-tolerance.


