| **# PDF Downloader** |
| -------------------------- |

|  |
| - |

| Dette repository indeholder en .NET-baseret løsning, som downloader PDF-rapporter ud fra en |
| -------------------------------------------------------------------------------------------- |

| Excel- eller CSV-metadatafil. Værktøjet håndterer alternative URL'er, kører downloads parallelt |
| --------------------------------------------------------------------------------------------------- |

| og udskriver en statusrapport med resultatet for hver række. |
| ------------------------------------------------------------- |

|  |
| - |

| **## Funktioner** |
| ----------------------- |

|  |
| - |

| **-**Understøtter både Excel (**`.xlsx`**) og CSV som inputkilde. |
| --------------------------------------------------------------------------------- |

| **-**Kan bruge en fallback-URL, hvis det primære link fejler. |
| -------------------------------------------------------------- |

| **-**Parallelle downloads med begrænsning på antal samtidige forbindelser. |
| ---------------------------------------------------------------------------- |

| **-**Prototypetilstand henter som udgangspunkt højst 10 rapporter. |
| ------------------------------------------------------------------- |

| **-**Rapporterer status (Downloadet, Skippet, Fejlet, Ingen URL) i en CSV-fil. |
| ------------------------------------------------------------------------------ |

|  |
| - |

| **## Kom godt i gang** |
| ---------------------------- |

|  |
| - |

| **### Krav** |
| ------------------ |

|  |
| - |

| **-**.NET 8 SDK |
| --------------------- |

| **-**Adgang til de metadatafiler, der skal behandles (Excel eller CSV) |
| ---------------------------------------------------------------------- |

|  |
| - |

| **### Installation** |
| -------------------------- |

|  |
| - |

| **1.**Gendan NuGet-pakker i Visual Studio eller via** `dotnet restore`**. |
| --------------------------------------------------------------------------------------- |

| **2.**Byg løsningen med** `dotnet build`**. |
| ---------------------------------------------------------- |

|  |
| - |

| **### Kørsel** |
| --------------------- |

|  |
| - |

| Kør programmet fra projektmappen**`src/PdfDownloader.App`**: |
| --------------------------------------------------------------- |

|  |
| - |

| **```bash** |
| ----------------- |

| **dotnet run -- \** |
| --------------------- |

| **--input /sti/til/GRI_2017_2020.xlsx \** |
| ------------------------------------------- |

| **--output ./Downloads \** |
| ---------------------------- |

| **--status ./Downloads/status.csv \** |
| --------------------------------------- |

| **--id-column BRnum \** |
| ------------------------- |

| **--url-column Pdf_URL \** |
| ---------------------------- |

| **--fallback-url-column Pdf_URL_Alt** |
| ------------------------------------------- |

| **```** |
| ------------- |

|  |
| - |

| Vigtige argumenter: |
| ------------------- |

|  |
| - |

| **-**`--input`(påkrævet): Sti til metadatafilen (.xlsx eller .csv). |
| ----------------------------------------------------------------------------- |

| **-**`--output`: Mappe hvor PDF'er gemmes (standard:**`./Downloads`**). |
| --------------------------------------------------------------------------------------- |

| **-**`--status`: Sti til en CSV-statusrapport (valgfrit). |
| ----------------------------------------------------------------- |

| **-**`--id-column`: Kolonnenavn der indeholder id'et, som bruges til filnavne (standard:**`BRnum`**). |
| ------------------------------------------------------------------------------------------------------------------------ |

| **-**`--url-column`: Kolonnenavn med den primære URL (standard:**`Pdf_URL`**). |
| ----------------------------------------------------------------------------------------------- |

| **-**`--fallback-url-column`: Kolonnenavn med alternativ URL (standard:**`Pdf_URL_Alt`**). |
| ---------------------------------------------------------------------------------------------------------- |

| **-**`--limit`: Maksimalt antal rækker der behandles (standard:**`10`**). |
| ------------------------------------------------------------------------------------------ |

| **-**`--max-concurrency`: Antal samtidige downloads (standard:**`4`**). |
| --------------------------------------------------------------------------------------- |

| **-**`--no-skip-existing`: Medtag for at overskrive allerede hentede filer. |
| ----------------------------------------------------------------------------------- |

|  |
| - |

| Tryk**`Ctrl+C`**for at annullere under kørsel. |
| ------------------------------------------------- |

|  |
| - |

| **## Output** |
| ------------------- |

|  |
| - |

| **-**PDF-filer gemmes i den angivne outputmappe og navngives** `<ID>.pdf`**. |
| ------------------------------------------------------------------------------------------ |

| **-**Hvis** `--status`**er angivet, skrives en CSV-rapport med kolonnerne** `Id`**,**`Status`**,**`Message`**, |
| ---------------------------------------------------------------------------------------------------------------------------------------------------- |

| **`SourceUrl`**og** `OutputPath`**. |
| --------------------------------------------------- |

|  |
| - |

| **## Projektstruktur** |
| ---------------------------- |

|  |
| - |

| **```** |
| ------------- |

| **PdfDownloader.sln** |
| --------------------------- |

| **└── src/** |
| --------------------- |

| **└── PdfDownloader.App/** |
| ----------------------------------- |

| **├── Downloads/        # Downloadlogik og resultater** |
| ---------------------------------------------------------------- |

| **├── Metadata/         # Indlæsning af Excel/CSV-data** |
| ------------------------------------------------------------------ |

| **├── Reporting/        # Generering af statusrapporter** |
| ------------------------------------------------------------------ |

| **├── AppOptions.cs     # Kommandolinjehåndtering** |
| ------------------------------------------------------------- |

| **├── ApplicationRunner.cs** |
| ------------------------------------- |

| **└── Program.cs** |
| --------------------------- |

| **```** |
| ------------- |

|  |
| - |

| **## Udvidelser** |
| ----------------------- |

|  |
| - |

| **-**Justér** `--limit`**når løsningen skal skaleres ud over prototypetilstanden. |
| -------------------------------------------------------------------------------------------- |

| **-**Planlagt videreudvikling kan inkludere logging til filer og en GUI til batchovervågning. |
| ---------------------------------------------------------------------------------------------- |

|  |
| - |

| **## Dokumentation** |
| -------------------------- |

|  |
| - |

| **-**[**Kravspecifikation**](**docs/kravspecifikation.md**) |
| -------------------------------------------------------------------- |

- [**UML sekvensdiagram**](**docs/uml-sekvensdiagram.md**)
## Samples
- `samples/Metadata2006_2016.xlsx`: Example metadata covering 2006-2016, including id column `BRnum` og URL felterne `Pdf_URL` og `Pdf_URL_Alt`.
- `samples/GRI_2017_2020 (1).xlsx`: Additional dataset that matches de samme kolonnenavne og kan bruges til at teste fallback URL logik.
- Begge filer bliver nu markeret som projektindhold og kopieres til output ved build, saa de altid er tilgaengelige sammen med binarierne.
- Hurtig test i projektroden: `dotnet run -- --input "..\samples\Metadata2006_2016.xlsx" --output .\Downloads --status .\Downloads\status.csv`.


