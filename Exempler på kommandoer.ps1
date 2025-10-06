## Exempel 1 - Læs status, spring hentede over, tag de næste 10 (append til CSV):

dotnet run -- `
  --input "..\samples\GRI_2017_2020 (1).xlsx" `
  --output ".\Downloads" `
  --status ".\Downloads\status.csv" `
  --resume-from-status ".\Downloads\status.csv" `
  --append-status `
  --id-column "BRnum" `
  --url-column "Pdf_URL" `
  --fallback-url-column "Pdf_URL_Alt" `
  --first 10 `
  --max-concurrency 10

  ## Exempel 2 - Tag interval fra 101 til 200 (1-baseret), append til status:

  dotnet run -- `
  --input "..\samples\GRI_2017_2020 (1).xlsx" `
  --output ".\Downloads" `
  --status ".\Downloads\status.csv" `
  --resume-from-status ".\Downloads\status.csv" `
  --append-status `
  --from 101 `
  --to 200 `
  --max-concurrency 20

  ## Exempel 3 - Brug skip/take (spring 500 over, tag 250):

  dotnet run -- `
  --input "..\samples\GRI_2017_2020 (1).xlsx" `
  --output ".\Downloads" `
  --status ".\Downloads\status.csv" `
  --resume-from-status ".\Downloads\status.csv" `
  --append-status `
  --skip 500 `
  --take 250 `
  --max-concurrency 25


  ## Exempel 4 - Overwrite downloads + detekter ændringer, behold gammel som .updated, og overskriv statusfilen:

  dotnet run -- `
  --input "..\samples\GRI_2017_2020 (1).xlsx" `
  --output ".\Downloads" `
  --status ".\Downloads\status.csv" `
  --overwrite-status `
  --id-column "BRnum" `
  --url-column "Pdf_URL" `
  --fallback-url-column "Pdf_URL_Alt" `
  --overwrite-downloads `
  --detect-changes `
  --keep-old-on-change `
  --limit 0 `
  --max-concurrency 50
