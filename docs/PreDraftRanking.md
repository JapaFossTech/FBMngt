# Pre Draft Ranking
Overview

This feature provides a **pre-draft ranking adjustment layer** on top of base FanPros rankings.
It allows:
- Default system offsets for catchers and closers.
- User-defined offsets via batch commands.
- Deterministic and unique AdjustedRank for reports.

Integration into combined report generation.
## Class / Dependency Diagram
```mermaid
classDiagram
    class ReportBase {
        +GenerateAndWriteAsync()
        #ReadAsync()
        #TransformAsync()
        #FormatReport()
        #WriteAsync()
    }

    class PlayerResolver {
        +ResolvePlayerIDAsync(players)
    }

    class IPlayerOffsetProvider {
        +GetOffset(playerId)
    }

    class PreDraftAdjustRepository {
        +GetAllAsync()
        +UpsertAsync(playerId, offset)
    }

    class PlayerOffsetService {
        +InitialConfigurationAsync()
        +AdjustAsync(batch)
    }

    ReportBase --> PlayerResolver
    ReportBase --> IPlayerOffsetProvider
    IPlayerOffsetProvider --> PreDraftAdjustRepository
    PlayerOffsetService --> IPlayerOffsetProvider
```
## Report Generation Sequence
```mermaid
sequenceDiagram
    participant CLI
    participant Report
    participant Provider
    participant Repo

    CLI->>Report: GenerateAndWriteAsync(rows)

    Report->>Report: ReadAsync()
    Report->>PlayerResolver: ResolvePlayerIDAsync(players)

    loop for each player
        Report->>Provider: GetOffset(PlayerID)
        Provider->>Repo: Fetch offset
        Repo-->>Provider: offset | 0
        Provider-->>Report: offset
        Report->>Report: Compute AdjustedRank (unique, sequential)
    end

    Report->>Report: FormatReport()
    Report->>Report: WriteAsync()
```
**Note**: AdjustedRank is computed **after sorting** and guarantees **unique sequential values**, even when multiple players share the same `Rank - Offset` value.
## Responsibility Flow
```mermaid
flowchart TD
    A[FanPros CSV] --> B[Base Rank]
    B --> C[TransformAsync seam]
    C --> D[AdjustedRank computed sequentially]
    D --> E[Reports / Combined]
```
## playerOffset CLI Commands
### playerOffset --initialConfiguration [--doCreateReport]
Initializes pre-draft offsets for the top 300 players.
### playerOffset --adjust playerID,12|playerID2,-12 [--doCreateReport]
Updates offsets for individual players in batch.
```mermaid
flowchart TD
    P[Program] --> C[PlayerOffsetCommand]

    C -->|--initialConfiguration| IC[InitialConfigurationAsync]
    C -->|--adjust| ADJ[AdjustAsync]

    ADJ --> PB[Parse batch]
    PB --> DB[Upsert offsets]

    IC --> DBINIT[Offsets written by system]

    DB --> ASK{--doCreateReport?}
    DBINIT --> ASK

    ASK -->|yes| RS[ReportService.GenerateCombined]
    ASK -->|no| END[Finish]
```
### Default offset rules (`--initialConfiguration`)
```mermaid
flowchart TD
    A[Load players] --> B[Delete table]
    B --> C[Loop players]
    C --> D{Catcher?}
    D -->|yes| E[Upsert +12]
    D -->|no| F{Closer?}
    F -->|yes| G[Upsert +24]
    F -->|no| H[Skip]
```
Neutral players (non-catchers, non-closers) are **not stored**, saving database space.
## Compact top-to-bottom flow summary
```mermaid
flowchart TD
    %% CLI
    CLI[Program.cs / CLI args] --> CMD[PlayerOffsetCommand]

    %% Commands
    CMD -->|--initialConfiguration| IC[InitialConfigurationAsync]
    CMD -->|--adjust| ADJ[AdjustAsync]

    %% Initial Configuration path
    IC --> DBINIT[Delete table & Upsert default offsets]
    DBINIT --> CHECK1{--doCreateReport?}
    CHECK1 -->|yes| RS1[ReportService.GenerateCombinedReport]
    CHECK1 -->|no| END1[Finish]

    %% Adjust path
    ADJ --> PB[Parse batch]
    PB --> DB[Upsert offsets]
    DB --> CHECK2{--doCreateReport?}
    CHECK2 -->|yes| RS2[ReportService.GenerateCombinedReport]
    CHECK2 -->|no| END2[Finish]

    %% Reuse repository
    DBINIT --> Repo[PreDraftAdjustRepository]
    DB --> Repo
    RS1 --> Report[FanProsCoreFieldsReport + Combined]
    RS2 --> Report
```