# Concurrency-resultater for PDF Downloader

**Maskine:** AMD Ryzen 7 5825U (8 kerner / 16 tråde) @ ~2.0 GHz • 16 GB RAM • 512 GB SSD  
**Datasæt:** 2000 rækker (2006–2016) 
**Server Timeout** 60 s pr. anmodning 
**Idle Timeout:** 60 s pr. anmodning  
**Connect Timeout** 10 s pr. anmodning
**Download Timeout** 60 s pr. anmodning
**HTTP-klient:** Genbrugt `HttpClient`, parallelisering via `SemaphoreSlim`

---

## Målte køretider

Test parameter
```bash
dotnet run --   --input "..\samples\Metadata2006_2016.xlsx"   --output ".\Downloads"   --status ".\Downloads\status_test2000.csv"   --id-column "BRnum"   --url-column "Pdf_URL"   --fallback-url-column "Pdf_URL_Alt"   --limit 2000   --max-concurrency x    --download-timeout 00:01:00   --idle-timeout 00:01:00
```

- Concurrency reducerer testtiden kraftigt: fra **-** (-×) til **~-** ved - samtidige (ca. **-** hurtigere).
- Den bedste “bang-for-buck” ligger omkring **-** samtidige downloads. Herefter flader gevinsten ud.


| Samtidige downloads | Tid (mm:ss.mmm) | Tid (sekunder) | Speedup vs. 1× | **Effektivitet ift. 1×** |
|---:|:---:|---:|---:|---:|
| 1  | - | - | 1,00× | **-** |
| 2  | - | - | -× | **- %** |
| 4  | - | - | -× | **- %** |
| 8  | 10:31:292 | - | -× | **- %** |
| 16 | 05:41:356 | - | -× | **- %** |
| 32 | 03:16:299 | - | -× | **- %** |
| 50 | 02:29:727 | - | -× | **- %**  |
| 100| 01:40:852 | - | -× | **- %**  |


**Observationer:**

- Markant fald fra - → - tråde (≈ - s → - s).  
- Herefter flader kurven ud: omkring **-** tråde rammer vi en “bund” på **~- s**.  
- Højere concurrency (-) giver ingen reel forbedring og kan endda svinge en smule.

---

## Hvorfor er speedup ikke lineær?

> Kort svar: fordi arbejdet ikke er 100 % parallelt, og fordi den “langsomme hale” (timeouts/fejl) sætter et gulv for total-tiden.

**1) Amdahl’s Law / ikke-parallel del**  
Selv om mange downloads kan køre samtidigt, er der sektioner der ikke kan paralleliseres: læsning af metadata, log-/statusskrivning, planlægning mv. Den ikke-parallelle andel begrænser maksimal speedup.

**2) “Lang hale” i netværket**  
En del links fejler hurtigt (404/403/301/HTML osv.), men nogle hænger i op til **60 s timeout**. Selvom 90 % af opgaverne bliver færdige hurtigt med høj concurrency, skal total-kørslen stadig vente på de langsomste resterende opgaver. Det skaber et naturligt **gulv ≈ timeout + overhead** (her ~- s).

**3) Server-throttling og fejlkoder**  
Mange links går til få domæner, som kan begrænse forbindelser (rate limits) eller svare med fejlkoder. Mere concurrency lokalt hjælper ikke mod fjernservernes grænser – det kan endda udløse flere 403/429/5xx.

**4) Overhead og contention**  
Flere samtidige forbindelser betyder mere TLS-håndtryk, flere sockets og flere kontekstskift. Det koster CPU/IO-overhead. På min maskine er CPU ikke flaskehalsen, men overheaden æder alligevel en del af gevinsten ved meget høj concurrency.

**5) Disk/IO er lille, men ikke nul**  
PDF’erne er relativt små, men skrivning/logging/status-CSV er stadig delt ressourcer. Ved meget høj concurrency øges lock- og buffertrykket lidt.

---

## Hvad betyder det for test maskinen?

- **CPU:** Ryzen 7 5825U (8C/16T) har masser af tråde, men workloaden er **netværks- og serverbegrænset**, ikke CPU-begrænset.  
- **RAM/SSD:** 16 GB og SSD er rigeligt; hver download bruger lidt buffer, men langt under systemgrænserne.  
- **Netværk:** Den reelle begrænsning er fjernservernes respons og fejlkoder, samt min timeout på 60 s.

**Praktisk sweet spot:**  
Ud fra målingerne ligger et fornuftigt valg omkring **- samtidige downloads**.  
- - tråde: ~- s (god balance).  
- - tråde: ~- s (tæt på bundgrænsen).  
Over - tråde får du marginal eller ingen gevinst, men mere støj i loggen og potentielt flere fejl fra fjernservere.

---

## Styrker ved concurrency

- **Klar begrænsning** via `SemaphoreSlim` – let at justere (`--max-concurrency`).  
- **God overlapning af ventetid** – netværkssvar udnyttes bedre end 1×.  
- **Genbrug af `HttpClient`** – undgår ekstra socket-oprettelser.  
- **Live-telemetri** – du kan se aktive jobs, tider og fejlkategorier.

---

## Svagheder / forbedringsmuligheder | Anbefalet af AI |

1) **Lang hale dominerer:**  
   - Overvej **hedged requests** (start fallback-URL efter f.eks. 3–5 s i stedet for først efter fejl).  
   - Overvej **tidlig abort** af primær, hvis fallback lykkes (annullér slæbende kald).

2) **Per-værts “fairness”:**  
   - Indfør **per-host køer/lofter** (f.eks. max 2–4 samtidige pr. domæne) for at undgå at spamme en enkelt server og udløse 403/429.

3) **Retrier med backoff:**  
   - Giv kort, begrænset retry på transiente fejl (5xx, timeouts) med **exponential backoff + jitter**.

4) **Adaptiv concurrency:**  
   - Start ved - og **justér op/ned** baseret på aktuel fejlrate/latens (f.eks. glidende middel).  
   - Hvis andel 404/HTML er høj, er flere tråde sjældent hjælpsomt.

5) **Redirect-håndtering og content-sniff:**  
   - Følg sikre redirects hvor det giver mening; lav evt. **content sniff** af de første bytes (`%PDF-`) som supplement til `Content-Type`.

6) **Mål på per-host og tail-latency:**  
   - Rapporter 50./90./99.-percentiler og **per-host** fejl/latens for at se, hvor “halen” kommer fra.

---

## Anbefaling (kort) | Anbefalet af AI |

- Brug **-** samtidige downloads som standard på denne maskine.  
- Sæt **hedged requests + begrænsning pr. host** for at tøjle hale og throttling.  
- Behold **timeout 60 s** (eller lavere for “aggressiv” kørsel), kombineret med **korte retrier** på transiente fejl.  
- Mål og log **per-host** og **tail percentiler** – optimér mod halen, ikke gennemsnittet.

