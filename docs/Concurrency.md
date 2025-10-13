# Concurrency-resultater for PDF Downloader

**Maskine:** AMD Ryzen 7 5825U (8 kerner / 16 tråde) @ ~2.0 GHz • 16 GB RAM • 512 GB SSD  
**Datasæt:** 100 rækker (2006–2016)  
**Timeout:** 60 s pr. anmodning  
**HTTP-klient:** Genbrugt `HttpClient`, parallelisering via `SemaphoreSlim`

---

## Målte køretider

- Concurrency reducerer testtiden kraftigt: fra **4:06** (1×) til **~1:09** ved 16–32 samtidige (ca. **3,5–3,6×** hurtigere).
- Den bedste “bang-for-buck” ligger omkring **8–16** samtidige downloads. Herefter flader gevinsten ud.


| Samtidige downloads | Tid (mm:ss.mmm) | Tid (sekunder) | Speedup vs. 1× | **Effektivitet ift. 1×** |
|---:|:---:|---:|---:|---:|
| 1  | 04:06.562 | 246,562 | 1,00× | **-** |
| 2  | 02:11.942 | 131,942 | 1,87× | **46.5 %** |
| 4  | 01:32.303 | 92,303  | 2,67× | **62.6 %** |
| 8  | 01:17.300 | 77,300  | 3,19× | **68.7 %** |
| 16 | 01:09.794 | 69,794  | 3,53× | **71.7 %** |
| 32 | 01:08.781 | 68,781  | 3,59× | **72.1 %** |
| 50 | 01:08.999 | 68,999  | 3,57× | **72.0 %**  |
| 100| 01:09.255 | 69,255  | 3,56× | **71.9 %**  |


**Observationer:**

- Markant fald fra 1 → 8 tråde (≈ 246 s → 77 s).  
- Herefter flader kurven ud: omkring **16–32** tråde rammer vi en “bund” på **~69 s**.  
- Højere concurrency (50–100) giver ingen reel forbedring og kan endda svinge en smule.

---

## Hvorfor er speedup ikke lineær?

> Kort svar: fordi arbejdet ikke er 100 % parallelt, og fordi den “langsomme hale” (timeouts/fejl) sætter et gulv for total-tiden.

**1) Amdahl’s Law / ikke-parallel del**  
Selv om mange downloads kan køre samtidigt, er der sektioner der ikke kan paralleliseres: læsning af metadata, log-/statusskrivning, planlægning mv. Den ikke-parallelle andel begrænser maksimal speedup.

**2) “Lang hale” i netværket**  
En del links fejler hurtigt (404/403/301/HTML osv.), men nogle hænger i op til **60 s timeout**. Selvom 90 % af opgaverne bliver færdige hurtigt med høj concurrency, skal total-kørslen stadig vente på de langsomste resterende opgaver. Det skaber et naturligt **gulv ≈ timeout + overhead** (her ~69 s).

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
Ud fra målingerne ligger et fornuftigt valg omkring **8–16 samtidige downloads**.  
- 8 tråde: ~77 s (god balance).  
- 16 tråde: ~70 s (tæt på bundgrænsen).  
Over 16 tråde får du marginal eller ingen gevinst, men mere støj i loggen og potentielt flere fejl fra fjernservere.

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
   - Start ved 8–12 og **justér op/ned** baseret på aktuel fejlrate/latens (f.eks. glidende middel).  
   - Hvis andel 404/HTML er høj, er flere tråde sjældent hjælpsomt.

5) **Redirect-håndtering og content-sniff:**  
   - Følg sikre redirects hvor det giver mening; lav evt. **content sniff** af de første bytes (`%PDF-`) som supplement til `Content-Type`.

6) **Mål på per-host og tail-latency:**  
   - Rapporter 50./90./99.-percentiler og **per-host** fejl/latens for at se, hvor “halen” kommer fra.

---

## Anbefaling (kort) | Anbefalet af AI |

- Brug **8–16** samtidige downloads som standard på denne maskine.  
- Sæt **hedged requests + begrænsning pr. host** for at tøjle hale og throttling.  
- Behold **timeout 60 s** (eller lavere for “aggressiv” kørsel), kombineret med **korte retrier** på transiente fejl.  
- Mål og log **per-host** og **tail percentiler** – optimér mod halen, ikke gennemsnittet.

