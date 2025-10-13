# Testrapport – PDF Downloader


**Dato:** <indsæt>
**Commit:** <SHA>
**Miljø:** Windows 11 / .NET 9.0


## Omfang
- Unit tests (AppOptions, MetadataLoader, StatusReport*, DownloadManager logik)
- Integration tests (Excel/CSV → Download → status.csv) m. WireMock


## Dækning (kort)
- Statement: <xx>% | Branch: <yy>% | Lines: <zz>%


## Væsentlige scenarier
- Ugyldige CLI‑argumenter afvises (manglende input, for højt concurrency)
- Loader accepterer CSV/XLSX og kræver `BRnum` + `Pdf_URL`
- Download fallback ved 404 på primær
- Timeout håndteres (markeret som `Failed`)
- Skip existing/overwrite fungerer
- Status CSV oprettes med header, kan appendes og genoptages


## Fund / rettelser
- [ID] Beskrivelse, påvirket klasse, reproduktion, fix og test‑evidens (link til testnavn)
- …


## Forbedringsforslag
- Polly retries m. jitter
- ETag/If‑Modified‑Since
- Per‑host rate limiting
- Serilog (rolling file + console)


## Bilag
- Testresultater (TRX) artefakter fra Actions
- Coverage (lcov/opencover) artefakter