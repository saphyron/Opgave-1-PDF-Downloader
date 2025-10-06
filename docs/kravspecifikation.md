
---

### 📋 **`docs/kravspecifikation.md`**
```markdown
# Kravspecifikation

## Formål
Udvikle et pålideligt .NET-program, der downloader PDF-rapporter fra en metadataliste
med **primære og sekundære URL’er**, og registrerer downloadstatus for hver rapport.

---

## Funktionelle krav

1. Systemet skal kunne læse metadata fra både **Excel (.xlsx)** og **CSV-filer**.  
2. Systemet skal downloade PDF-filer ved hjælp af **primær URL** og en **fallback-URL**, hvis den første fejler.  
3. Systemet skal kunne **begrænse antallet af samtidige downloads**.  
4. Systemet skal generere en **statusrapport i CSV-format** med mindst felterne  
   `Id`, `Status`, `Message`, `SourceUrl` og `OutputPath`.  
5. Downloadede filer skal **navngives efter en konfigurerbar identifikator** (standard: `BRnum`).  
6. Systemet skal kunne **springe allerede downloadede filer over** for at understøtte genstart.

---

## Ikke-funktionelle krav

- Programmet skal være **robust over for netværksfejl** og fortsætte med de næste downloads.  
- Kodebasen skal følge **principperne om Separation of Concerns** og være veldokumenteret.  
- Downloadprocessen skal som standard køre i en **prototype-tilstand**, hvor højst 10 filer hentes.  
- Løsningen skal være **nem at distribuere og køre via kommandolinjen (CLI)**.  

---

## Brugere og interessenter

- **Dataanalytikere hos kunden**, der skal bruge PDF-rapporterne i videre analyser.  
- **IT-administratorer**, der kører værktøjet periodisk eller efter behov.  

---

## Acceptkriterier

- Værktøjet downloader mindst én PDF med succes fra en angivet metadatafil.  
- Værktøjet håndterer ugyldige eller utilgængelige URL’er uden at stoppe hele processen, og forsøger fallback-URL’en.  
- Statusrapporten angiver korrekt, hvilke downloads der lykkedes, blev skippet eller fejlede.  
- Programmet kan genkøre uden fejl, selv hvis outputmappen allerede indeholder enkelte filer.

---

## Afgrænsninger

- Der udføres ikke automatisk genforsøg ud over den konfigurerede fallback-URL.  
- Løsningen forudsætter adgang til internettet og at metadata indeholder gyldige URL’er.
