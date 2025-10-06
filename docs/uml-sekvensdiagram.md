| **# UML Sekvensdiagram** |
| ------------------------------ |

|  |
| - |

| **```mermaid** |
| -------------------- |

| **sequenceDiagram** |
| ------------------------- |

| **participant User** |
| -------------------------- |

| **participant CLI as Program** |
| ------------------------------------ |

| **participant Runner as ApplicationRunner** |
| ------------------------------------------------- |

| **participant Loader as MetadataLoader** |
| ---------------------------------------------- |

| **participant Manager as DownloadManager** |
| ------------------------------------------------ |

| **participant HTTP as HTTP Server** |
| ----------------------------------------- |

|  |
| - |

| **User->>CLI: dotnet run -- --input ...** |
| ----------------------------------------------- |

| **CLI->>Runner: Parse options og start** |
| ---------------------------------------------- |

| **Runner->>Loader: LoadAsync(input)** |
| ------------------------------------------- |

| **Loader-->>Runner: MetadataRecord[]** |
| -------------------------------------------- |

| **Runner->>Manager: DownloadAsync(requests)** |
| --------------------------------------------------- |

| **loop For hver rapport** |
| ------------------------------- |

| **Manager->>HTTP: GET primær URL** |
| ----------------------------------------- |

| **alt Svar OK og PDF** |
| ---------------------------- |

| **HTTP-->>Manager: PDF stream** |
| ------------------------------------- |

| **Manager->>Manager: Gem fil** |
| ------------------------------------ |

| **Manager-->>Runner: Resultat (Downloaded)** |
| -------------------------------------------------- |

| **else Fejl** |
| ------------------- |

| **Manager->>HTTP: GET fallback URL** |
| ------------------------------------------ |

| **alt Fallback OK** |
| ------------------------- |

| **HTTP-->>Manager: PDF stream** |
| ------------------------------------- |

| **Manager->>Manager: Gem fil** |
| ------------------------------------ |

| **Manager-->>Runner: Resultat (Downloaded)** |
| -------------------------------------------------- |

| **else Ingen succes** |
| --------------------------- |

| **Manager-->>Runner: Resultat (Failed/NoUrl)** |
| ---------------------------------------------------- |

| **end** |
| ------------- |

| **end** |
| ------------- |

| **end** |
| ------------- |

| **Runner->>Runner: Generér statusrapport** |
| ------------------------------------------------- |

| **Runner-->>User: Opsummering og rapportsti** |
| --------------------------------------------------- |

```

```
