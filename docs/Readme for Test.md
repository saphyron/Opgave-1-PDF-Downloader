# Test Pack for PDF Downloader (C# / .NET 9, xUnit)

- xUnit + FluentAssertions + Moq til **enhedstest**
- **Integrationstest** med rigtige CSV/Excel-filer (ClosedXML/CsvHelper)
- **Code Coverage** via coverlet + rapport i `TestResults`
- **GitHub Actions** workflow (CI) til automatisk kørsel
- PowerShell 5.1 kompatible scripts

## Forudsætninger
- .NET SDK 9 installeret
- Windows PowerShell 5.1 (eller nyere) – scripts er skrevet til PS 5.1
- Dit app-projekt: `PDF Downloader/PDF Downloader.csproj` (target: net9.0)

## Struktur i denne pakke
```
TestPack-PdfDownloader/
  .github/workflows/ci.yml
  samples/
    sample-input.csv
    sample-input.xlsx
  scripts/
    test.ps1
    coverage.ps1
  tests/PdfDownloader.Tests/
    PdfDownloader.Tests.csproj
    Unit/
      MetadataLoaderTests.cs
      DownloadManagerTests.cs
    Integration/
      PipelineIntegrationTests.cs
      StatusReportTests.cs
    TestHelpers/
      TempDir.cs
      HttpMessageHandlerStub.cs
```

## Hurtig start
1. **Klon/åbn dit app-projekt** (PDF Downloader).
2. Kopiér indholdet af denne `TestPack-PdfDownloader/` ind i roden af din løsning (du kan også lægge `tests/` ved siden af din app-projektmappe).
3. I app-projektet, tilføj `Properties/AssemblyInfo.cs` med `InternalsVisibleTo` (se ovenfor).
4. Kør:
   ```powershell
   ./scripts/test.ps1
   ```
   Det vil:
   - Restore løsningen
   - Build app + tests
   - Køre tests
   - Lave coverage-rapport

## Kør tests manuelt
```powershell
dotnet restore
dotnet build --configuration Release
dotnet test ./tests/PdfDownloader.Tests/PdfDownloader.Tests.csproj `
  --configuration Release `
  --collect:"XPlat Code Coverage" `
  --results-directory ./TestResults
```

Coverage-rapport (Cobertura) findes i `TestResults/**/coverage.cobertura.xml`.

## CI (GitHub Actions)
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
