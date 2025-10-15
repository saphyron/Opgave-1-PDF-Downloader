# Test Pack for PDF Downloader (C# / .NET 9, xUnit)

- xUnit + FluentAssertions
- **Integrationstest** med rigtige CSV/Excel-filer (ClosedXML/CsvHelper)
- **Code Coverage** via coverlet + rapport i `TestResults`
- **GitHub Actions** workflow (CI) til automatisk kørsel (Er ikke teste om det virker endnu)
- PowerShell 5.1 kompatible scripts

## Forudsætninger
- .NET SDK 9 installeret
- Windows PowerShell 5.1 (eller nyere) – scripts er skrevet til PS 5.1
- Dit app-projekt: `PDF Downloader/PDF Downloader.csproj` (target: net9.0)

## Struktur i denne pakke
```text
Legend (kort): 📁 mappe • 🧩 C#-kode  • 🪪 .sln/.csproj

📁 tests/
└─ 📁 PdfDownloader.Tests/
   ├─ 🪪 PdfDownloader.Tests.csproj
   ├─ 🧩 GlobalUsings.cs
   ├─ 📁 Fakes/
   │  ├─ 🧩 FakeHttpMessageHandler.cs
   │  └─ 🧩 ThrowingHttpMessageHandler.cs
   ├─ 📁 Integration/
   │  ├─ 🧩 ExcelIntegrationTests.cs
   │  └─ 🧩 PipelineIntegrationTests.cs
   └─ 📁 Unit/
      ├─ 🧩 AppOptionsTests.cs
      ├─ 🧩 DownloadManagerTests.cs
      ├─ 🧩 MetadataLoaderTests.cs
      ├─ 🧩 MetadataRecordTests.cs          
      ├─ 🧩 StatusReportReaderAllTests.cs
      └─ 🧩 StatusReportWriterTests.cs
```


## Hurtig start
1. **Klon/åbn dit app-projekt** (PDF Downloader).
2. Kopiér indholdet af denne `TestPack-PdfDownloader/` ind i roden af din løsning (du kan også lægge `tests/` ved siden af din app-projektmappe).
3. I app-projektet, tilføj `Properties/AssemblyInfo.cs` med `InternalsVisibleTo` (se ovenfor).
4. Kør:
   ```powershell
   powershell -ExecutionPolicy Bypass -File ".\scripts\TestReport.ps1
   ```
   Det vil:
   - Restore løsningen
   - Build app + tests
   - Køre tests
   - Lave coverage-rapport

## CI (GitHub Actions) Er ikke test om den virker endnu.
- Workflow ligger i `.github/workflows/ci.yml` – den kører restore, build, test og uploader coverage-artifacts.
- Push til `main` eller PR → CI kører automatisk.

## Noter om integrationstest
- Integrationstests bruger **rigtige filer** (ingen mocks) i `./samples`.
- Tests opretter **midlertidige mapper** og rydder op efter sig.
- Sørg for at din app’s `MetadataLoader` accepterer både `.csv` og `.xlsx` og at dine kolonnenavne matcher testene.

## Fejlfinding
- Hvis tests ikke kan se `internal` typer → tjek at `InternalsVisibleTo` er tilføjet korrekt.
- Hvis namespaces ikke matcher (`PdfDownloader.App.*`) → ret namespaces i tests så de peger på dine faktiske typer.
- Hvis HTTP-kald i enhedstests skal mockes → se `HttpMessageHandlerStub`.
