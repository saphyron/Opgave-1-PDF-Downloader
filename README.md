# PDF-Downloader – README

Robust, trådsikker og genoptagelig .NET-applikation til at **downloade PDF-rapporter ud fra metadata i Excel- eller CSV-filer**.Projektet demonstrerer **multithreading, fejlhåndtering, fil-I/O** og fleksibel **CLI-styring**.Opgaven er baseret på en **realistisk kunde-case** fra Specialisterne og er designet med fokus på **stabilitet, genkørsel og performance**.

> **Target Framework:** net9.0
> **Kerneservices:** ClosedXML, CsvHelper, System.Net.Http
> **Kørsel:** CLI / Console • Windows
> **Formål:** Stabil, hurtig og genoptagelig bulk-download af rapporter med fallback-links, resume fra status, og fleksible filtreringsmuligheder.

---

---

## Funktioner

* 📑 **Indlæser metadata** fra **Excel (.xlsx)** og **CSV** via *ClosedXML* og  *CsvHelper* .
* 🌐 **Downloader PDF-filer** fra kolonnerne `Pdf_URL` og `Pdf_URL_Alt`, med  **automatisk fallback** , hvis den primære fejler.
* ⚙️ **Konfigurerbar parallellisering** via `--max-concurrency` (styret af `SemaphoreSlim`) for balanceret netværksbelastning.
* 🔁 **Resume fra tidligere kørsel** med `--resume-from-status`, så allerede downloadede filer automatisk springes over.
* 📊 **Statusrapport i CSV** (`status.csv`) med felterne *Id, Outcome, Message, SourceUrl, OutputPath* — kan **appendes** eller **overskrives** (`--append-status`, `--overwrite-status`).
* 🎯  **Avanceret udvælgelse** : kør på et subset af metadata vha. `--first`, `--skip`, `--take`, `--from`, `--to` eller `--limit`.
* 🧠 **Change-detection og versionering** – sammenligner eksisterende PDF’er med nye via  **SHA-256 hash** , og omdøber gamle til `*.updated.pdf` hvis ændret (`--detect-changes`, `--keep-old-on-change`).
* 🔒 **Idempotent drift** – sikrer sikre genkørsler ved hjælp af `--skip-existing` og `--overwrite-downloads`.
* 🧩 **Robust fejlhåndtering** – håndterer timeouts, ugyldige links, 404-fejl og andre undtagelser uden at stoppe hele processen.
* 🧾 **CLI-interface** med mange konfigurerbare argumenter og tydelig *usage help* (`AppOptions.Usage`).
* 💾 **Automatisk navngivning** af PDF-filer efter kolonnen `BRNum`.
* 🧠 **Thread-safe design** – alle operationer udføres med korrekt asynkronitet og låsestyring for stabil performance.

---

---

## Teknologier & Arkitektur

**Runtime & CLI**

- **.NET 9 Console App** (`Program.cs` → `ApplicationRunner`)
- Command-line parser (`AppOptions.cs`) med eksplicit validering, standardværdier og hjælpe-tekst (`AppOptions.Usage`)
- Understøtter **resume**, **range-valg** (`--first`, `--skip`, `--take`, `--from`, `--to`, `--limit`) og **status-kontrol** (`--append-status`, `--overwrite-status`)
- **CancellationToken** til clean shutdown ved Ctrl + C

**Datahåndtering**

- **ClosedXML** – læser Excel-filer med headers og dynamiske kolonnenavne
- **CsvHelper** – robust parsing af CSV-filer, understøtter både læsning og skrivning
- **MetadataLoader** – vælger automatisk parser (Excel / CSV) og danner `MetadataRecord[]`
- **StatusReportReader** – kan genoptage fra eksisterende `status.csv` ved at indlæse tidligere `Downloaded`-rækker

**Multithreading & Ydelse**

- **DownloadManager** bruger `SemaphoreSlim` til at styre parallelisme (`--max-concurrency`)
- **HttpClient** genbruges per instans for at undgå socket-udmattelse
- **ConcurrentBag** anvendes til trådsikker akkumulering af resultater
- **Asynkrone IO-operationer** for høj throughput uden blokeringer
- **SHA-256 change detection**: sammenligner eksisterende filer mod ny hentede PDF’er
- Omdøber gamle filer til `<navn>.updated.pdf`, hvis ændringer registreres (`--detect-changes`, `--keep-old-on-change`)

**Fejlhåndtering & Robusthed**

- Klare udfaldstyper (`Downloaded`, `SkippedExisting`, `Failed`, `NoUrl`) via `DownloadOutcome`-enum
- Detaljerede fejlmeddelelser (HTTP-status, Content-Type, Exception message)
- **Fallback-strategi** – sekundær URL prøves automatisk, hvis den primære fejler
- Hver URL-gruppe håndteres isoleret – fejl på ét link stopper ikke de øvrige
- Intern `try/catch` sikrer fortsat kørsel, selv ved delvise fejl eller afbrud

**Rapportering & Status**

- **StatusReportWriter** genererer CSV-fil med felterne:`Id, Outcome, Message, SourceUrl, SavedFile`
- Understøtter **append** og **overwrite**-mode afhængigt af CLI-argumenter
- Opretter automatisk mappestruktur og skriver header ved ny fil
- **Resume-venligt design** – rapporten kan genbruges i efterfølgende kørsel for at skippe hentede filer
- Filnavne renses via `SanitizeFileName()` for at sikre gyldige OS-filnavne
- Output og status kan skrives parallelt uden låsekonflikter

--

## Arkitektur (forenklet)

```mermaid
flowchart TB
  subgraph CLI["Command Line Interface"]
    CMD["dotnet run -- ..."]
  end

  CMD -->|AppOptions| RUN["ApplicationRunner"]

  %% Resume from status.csv
  RUN -->|optional --resume-from-status| READ["StatusReportReader"]
  READ --> DONE["Completed ID set"]

  %% Load metadata
  RUN --> LOAD["MetadataLoader (Excel/CSV)"]
  LOAD --> RECS["Records"]

  %% Selection / filtering
  RECS --> FILTER["Select: first / skip / take / from / to / limit"]
  DONE -->|exclude IDs| FILTER

  FILTER --> REQ["DownloadRequest list"]

  %% Download manager with throttling
  RUN --> MGR["DownloadManager (SemaphoreSlim: --max-concurrency)"]

  %% Existence / overwrite / change detection
  MGR --> EXIST{"File exists?"}
  EXIST -- "No" --> FETCH["HTTP GET (primary then fallback)"]
  EXIST -- "Yes" --> OVER{"Overwrite or DetectChanges?"}

  OVER -- "No" --> SKIP["Result: SkippedExisting"]
  OVER -- "Yes" --> CHK{"Changed (SHA-256)?"}
  CHK -- "No" --> SKIP
  CHK -- "Yes" --> RENAME["Rename old to *.updated"]

  RENAME --> FETCH
  FETCH --> SAVE["Save PDF"]

  SAVE --> RESULTS["Results (Downloaded / Failed / NoUrl / Skipped)"]
  SKIP --> RESULTS

  %% Reporting
  RUN --> REPORT["StatusReportWriter"]
  RESULTS --> REPORT
  REPORT -->|append or overwrite| CSV["status.csv"]

```

---

## Projektstruktur

```text
Legend (kort): 📁 mappe • 🧩 C#-kode • ⚙️ config/json • 🪪 .sln/.csproj • 🧾 README/MD • 📑 CSV • 📊 Excel

📁 Opgave 1 PDF Downloader/
├─ 🪪 PDF Downloader.slnx
├─ 🧾 README.md
├─ 📁 PDF Downloader/
│  ├─ 🧩 Program.cs
│  ├─ ⚙️ appsettings.json
│  ├─ ⚙️ appsettings.Development.json
│  ├─ 🪪 PDF Downloader.csproj
│  ├─ 📁 src/
│  │  └─ 📁 PdfDownloader.App/
│  │     ├─ 🧩 ApplicationRunner.cs
│  │     ├─ 🧩 AppOptions.cs 
│  │     ├─ 📁 Middleware/
│  │     │  ├─ 🧩 MetadataLoader.cs
│  │     │  └─ 🧩 MetadataRecord.cs
│  │     ├─ 📁 Downloads/
│  │     │  ├─ 🧩 DownloadManager.cs  
│  │     │  ├─ 🧩 DownloadRequest.cs
│  │     │  ├─ 🧩 DownloadResult.cs
│  │     │  └─ 🧩 DownloadOutcome.cs
│  │     └─ 📁 Reporting/
│  │        ├─ 🧩 StatusReportWriter.cs  
│  │        └─ 🧩 StatusReportReader.cs  
│  ├─ 📁 Downloads/
│  │  ├─ 📑 status.csv
│  │  └─ 📑 status_GRI.csv
│  └─ 📁 samples/
│     ├─ 📊 Metadata2006_2016.xlsx
│     └─ 📊 GRI_2017_2020 (1).xlsx
└─ 📁 docs/
   ├─ 🧾 kravspecifikation.md  
   └─ 🧾 uml-sekvensdiagram.md  

```

---

## Kørsel & CLI

### Standard eksekvering

```bash
dotnet run --   --input "..\samples\Metadata2006_2016.xlsx"   --output ".\Downloads"   --status ".\Downloads\status.csv"   --id-column "BRnum"   --url-column "Pdf_URL"   --fallback-url-column "Pdf_URL_Alt"   --limit 10   --max-concurrency 5
```

### “Oh-shit moment” (fuld kørsel hjemme)

```bash
dotnet run --   --input "..\samples\GRI_2017_2020 (1).xlsx"   --output ".\Downloads"   --status ".\Downloads\status_full.csv"   --id-column "BRnum"   --url-column "Pdf_URL"   --fallback-url-column "Pdf_URL_Alt"   --limit 0   --max-concurrency 50
```

> 💡 Brug `Ctrl+C` for at afbryde. DownloadManager lukker trådsikkert ned via `CancellationToken`.

---

## Fejlhåndtering & Stabilitet

| Udfald                    | Forklaring                                     |
| ------------------------- | ---------------------------------------------- |
| **Downloaded**      | PDF gemt korrekt                               |
| **SkippedExisting** | Filen findes allerede                          |
| **Failed**          | Fejl (HTTP, IO, timeout, forkert content-type) |
| **NoUrl**           | Mangler gyldig URL i metadata                  |

- Hver fejl logges med årsag i `status.csv`
- Timeout = 120 sekunder pr. request
- Gyldige filnavne sikres via `SanitizeFileName`
- Ugyldige eller tomme URL’er springes over

---

## Concurrency og “Oh-shit moment”

Ved første test forsøgte systemet at starte **50 samtidige downloads** 😱
→ Resulterede i CPU Temperatur steg drastisk

**Løsningen**

- Introduceret **SemaphoreSlim** for max N samtidige downloads
- CLI-parameter `--max-concurrency` (1-32 standard)
- Stabilitet ved store dataset
- 50 samtidige tråde = ~100x hastighedsforbedring uden overload

---

## Statusrapport (CSV)

Eksempel (`status.csv`):

| Id    | Outcome         | Message            | SourceUrl   | OutputPath            |
| ----- | --------------- | ------------------ | ----------- | --------------------- |
| BR001 | Downloaded      |                    | https://... | ./Downloads/BR001.pdf |
| BR002 | Failed          | HTTP 404 Not Found | https://... |                       |
| BR003 | SkippedExisting | Allerede hentet    |             | ./Downloads/BR003.pdf |

---

## Versionshistorik

### 2025-10-06

- Første stabile version med fuld concurrency-styring
- Tilføjet statusrapport + fallback URL-logik
- Dokumenteret “oh-shit moment”
- MetadataLoader implementeret (Excel/CSV)
- CLI parser (AppOptions) tilføjet
- Implementeret DownloadManager med HttpClient
- Projektstruktur og kravspecifikation etableret
- UML-diagram oprettet
- Udvidet funktionalitet til at kunne append til CSV,
  - Fortsætte hvor man var kommet til
  - Overskrive gamle link med opdateret hvis de er.
  - Vælge specifikke rækket i datasættet.

---

## Designbeskrivelse & Begrundelser

- **Separation of Concerns** `MetadataLoader`, `DownloadManager`, `StatusReportReader` og `StatusReportWriter` har hver deres tydelige ansvar for hhv. indlæsning, behandling, status-analyse og rapportering.`ApplicationRunner` fungerer som pipeline-orkestrator.
- **Concurrent design**Download-processen styres med `SemaphoreSlim`, der begrænser antallet af samtidige downloads (`--max-concurrency`).Det giver optimal balance mellem hastighed og systembelastning – og kan skaleres efter netværk eller maskinkraft.
- **HttpClient reuse**Hele applikationen genbruger én delt `HttpClient` for at undgå socket-udmattelse og unødige forbindelser, hvilket øger stabilitet og performance.
- **Resume og filtrering**Applikationen kan genoptage tidligere kørsel via `--resume-from-status`, hvor allerede downloadede filer læses fra status-CSV og springes over.Bruger kan kombinere `--first`, `--skip`, `--take`, `--from`, `--to` og `--limit` for præcis kontrol over hvilke rækker, der behandles.
- **Statusrapportering (append / overwrite)**Status-CSV kan oprettes, appendes eller overskrives efter behov (`--append-status`, `--overwrite-status`).Rapporten muliggør audit-trail, fejlsøgning og sikker genstart uden datatab.
- **Change detection & versionering**Ved brug af `--detect-changes` sammenlignes eksisterende og ny PDF via SHA-256 hash.Hvis indholdet er ændret og `--keep-old-on-change` er angivet, gemmes den gamle version som `<filnavn>.updated.pdf` før ny download.Dette bevarer historik og understøtter versionssporing.
- **Fallback-logik**Hvis primær URL fejler, forsøges fallback-URL (kolonnen `Pdf_URL_Alt`) automatisk – uden at stoppe hele processen.
- **Idempotent drift** `--skip-existing` sikrer, at allerede hentede filer ikke gendownloades, medmindre man eksplicit vælger `--overwrite-downloads`.Det giver stabile, reproducerbare kørsler uden datakollisioner.
- **Robusthed og fejl-tolerance**Hver URL behandles individuelt – fejl, timeouts eller manglende PDF’er påvirker ikke de øvrige.`CancellationToken` giver clean shutdown ved Ctrl+C.
- **Udvidelsesmuligheder**
  Designet er modulært, så komponenter som logging (Serilog), GUI eller database-backends kan tilføjes uden at ændre kernearkitekturen.

---

## Fremtidige forbedringer

### Observability & Drift

- **Fil- og konsol-logging** med rullende logs (Serilog File + Console sinks)`--log-level Information|Debug` • `--log-file .\logs\run.log`
- **Struktureret logging** (JSON) for bedre fejlsøgning/ELK/Grafana Loki
- **Metrics** (Prometheus-format) for antal/sek, fejlrate, gennemsnitlig hentetidEvt. lille indbygget `/metrics` http-listener (`--metrics-port 9090`)
- **Audit-tagging** af run (`--run-id`), som også inkluderes i status.csv

### Robusthed & Netværk

- **Eksponentiel backoff + jitter** pr. host (Polly)`--retry-count 3 --retry-base-ms 500 --retry-max-ms 10000`
- **Per-host rate-limit** for at undgå blokeringer `--rate-per-host 5/s`
- **Smart fallback-strategi** (parallelt race mod primær/fallback med cancel-on-first-success)

### Integritet & Versionering

- **ETag/If-None-Match** og **If-Modified-Since** for at undgå unødige downloads `--honor-etag --honor-last-modified`
- **Checksum-manifest** (`checksums.json`) ved siden af status.csv for hurtig change-detektion
- **Versionsmappe pr. ID** når der findes flere versioner
  `Downloads/BR001/BR001_2025-10-06.pdf`

### Ydelse & Concurrency

- **Channel-baseret pipeline** (System.Threading.Channels) for lavere overhead end N×Tasks
- **Batch-flush** af status til CSV (f.eks. hver 100. post) for færre FS-writes `--status-batch 100`
- **Adaptive concurrency**: justér automatisk `--max-concurrency` ud fra fejlrater og latenser

### Datastrømme & Kilder

- **Flere inputkilder**: HTTP/REST, SQL, S3-manifest, Google Sheets `--input-kind xlsx|csv|api|sql`
- **Filtre på metadata**: år, domæne, land, kategori
  `--filter "year>=2017 AND domain LIKE '%.gov%'"`

### Resume & Status (udvidet)

- **Resume-politik** konfigurerbar: skip kun `Downloaded`, eller også nylige `Failed--resume-policy Downloaded|DownloadedOrRecentlyFailed --failed-window 2d`
- **”Retry-kurv”** til fejlede, der køres til sidst i en separat bølge

### Brugervenlighed

- **TUI/CLI-UI** (Spectre.Console) med live-progress (per ID, total, hastighed)
- **Interaktiv “dry-run”**: vis hvor mange der vil blive hentet/overskrevet, uden at downloade
  `--dry-run`

### Sikkerhed

- **Domænehvidliste/sortliste** `--allow-host *.example.com --deny-host *.unknown.tld`
- **Timeouts per fase**: DNS, connect, headers, body `--timeout-connect 5s --timeout-body 120s`
- **Proxy-support** og systemcertifikater
  `--proxy http://user:pass@host:port`

### Packaging & Deployment

- **Single-file publish** + self-contained runtime for nem distribution `dotnet publish -r win-x64 -p:PublishSingleFile=true`
- **Docker-image** (alpine) med bind mounts for input/output/status
- **GitHub Actions** med CI (build, test, publish artefakter) og nightly smoketest

### Testbarhed & Kvalitet

- **Unit/integration tests** med HttpMessageHandler-mocks (WireMock.Net)
- **Contract tests** for CSV/Excel-parsing (edge-cases: tomme celler, BOM, semikolon/komma)
- **Load-tests** (NBomber/k6) på 1k/10k/50k rækker
- **Static analysis** (Roslyn analyzers, StyleCop) og kodecov

### Testing


Krav: .NET 9 SDK.


```powershell
powershell -ExecutionPolicy Bypass -File ".\scripts\TestReport.ps1
```

#### Rapport skabes i docs/test-reports/<datatime>/
- Der skabes 2 filer der bruges af Rapporten.
  - coverage.cobertura.xml
  - test.trx
- Der skabes 2 status.csv filer, der afspejler normalt drift, en for serial og en for parallel
- Der skabes en Run.log, der viser processen programmet tog.
- Der skabes en TestReport.md fil der afspejler test. 

---

## Licens

Se `LICENSE` i roden af repoet.
