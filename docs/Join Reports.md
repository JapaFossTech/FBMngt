# Joining Reports
### Acceptance Criteria

*Given* multiple reports that output `StringLines`  
*When* I generate a combined report  
*Then* reports are appended horizontally  
*And* each report retains its own columns  
*And* exactly one tab separates reports  
*And* no columns are merged or normalized  
*And* the output opens correctly in Excel


## Architecture Diagram
```mermaid
graph TD
    A[CLI Command] --> B[Report A Generator]
    A --> C[Report B Generator]
    A --> D[Report C Generator]

    B --> E[ReportResult A<br/>StringLines]
    C --> F[ReportResult B<br/>StringLines]
    D --> G[ReportResult C<br/>StringLines]

    E --> H[Horizontal Report Appender]
    F --> H
    G --> H

    H --> I[Final StringLines]
    I --> J[Excel / CSV Output]
```
## Join Flow
```mermaid
sequenceDiagram
    participant U as User
    participant CLI as CLI Command
    participant R1 as Report A
    participant R2 as Report B
    participant H as Horizontal Appender

    U->>CLI: run combined-report
    CLI->>R1: Generate report
    CLI->>R2: Generate report
    R1-->>CLI: ReportResult<StringLines>
    R2-->>CLI: ReportResult<StringLines>
    CLI->>H: Append horizontally
    H-->>CLI: Final StringLines
    CLI-->>U: Output to file
```
## Class Diagram
```mermaid
classDiagram
    direction LR

    class ReportCommand {
        +ExecuteAsync(string[] args)
    }

    class ReportService {
        +GenerateZScoreReportsAsync()
        +GenerateFanProsCoreFieldsReportAsync(int rows)
        +GenerateCombinedReportAsync(IEnumerable~string~ reportNames)
        -GetReportBuilders(IEnumerable~string~ reportNames)
    }

    class IReportBuilder {
        <<interface>>
        +GenerateAsync() ReportResult~object~
    }

    class FanProsCoreFieldsReportBuilder {
        +GenerateAsync() ReportResult~object~
    }

    class ZscoresReportBuilder {
        +GenerateAsync() ReportResult~object~
    }

    class IHorizontalReportAppender {
        <<interface>>
        +Append(List~ReportResult~object~~) List~string~
    }

    class HorizontalReportAppender {
        +Append(List~ReportResult~object~~) List~string~
    }

    class ReportResult~T~ {
        +List~T~ ReportRows
        +List~string~ StringLines
    }

    ReportCommand --> ReportService
    ReportService --> IReportBuilder
    FanProsCoreFieldsReportBuilder ..|> IReportBuilder
    ZscoresReportBuilder ..|> IReportBuilder

    ReportService --> IHorizontalReportAppender
    HorizontalReportAppender ..|> IHorizontalReportAppender

    ReportService --> ReportResult~object~


```
## CLI Flow Diagram — `report --combine`
```mermaid
sequenceDiagram
    participant CLI as CLI
    participant RC as ReportCommand
    participant RS as ReportService
    participant RB as IReportBuilder
    participant HA as HorizontalReportAppender
    participant FS as FileSystem

    CLI->>RC: report --combine FanProsCoreFields,zscores
    RC->>RS: GenerateCombinedReportAsync(reportNames)

    loop for each reportName
        RS->>RS: GetReportBuilders()
        RS->>RB: GenerateAsync()
        RB-->>RS: ReportResult<object>
    end

    RS->>HA: Append(reportResults)
    HA-->>RS: List<string> combinedLines

    RS->>FS: WriteAllLinesAsync(.tsv)

```
## CLI Flow (High-Level Control)
```mermaid
graph TD
    A[CLI: report --combine] --> B[CombinedReportService]
    B --> C[IReportRegistry]
    C --> D[Report Generators]
    D --> E[ReportResult.StringLines]
    E --> F[HorizontalReportAppender]
    F --> G[Final StringLines]
    G --> H[Excel / CSV Output]
```
## Horizontal Combination Logic Diagram (Data-Oriented)

Explains why Excel opens this correctly.
```mermaid
flowchart LR
    A[Report A StringLines] --> C[HorizontalReportAppender]
    B[Report B StringLines] --> C
    D[Report C StringLines] --> C

    C --> E[Combined TSV Lines]

    N[Preserve line index<br/>Join with exactly one tab<br/>No column merging<br/>Empty string for missing rows]

    C -.-> N

```
## Builder Selection Diagram (Factory via switch)
```mermaid
flowchart TD
    A[reportNames] --> B[GetReportBuilders]

    B -->|FanProsCoreFields| C[FanProsCoreFieldsReportBuilder]
    B -->|zscores| D[ZscoresReportBuilder]

    C --> E[GenerateAsync]
    D --> E

    E --> F[ReportResult<object>]
```