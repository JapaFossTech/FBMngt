# FanPros Delta Report

Computes player movement between two FanPros ranking snapshots and outputs a TSV report sorted by largest absolute movement.

---

# 1. High-Level Architecture

```mermaid
flowchart TD
    A[FanProsDeltaReport] --> B[FanProsCoreFieldsReport - Latest]
    A --> C[FanProsCoreFieldsReport - Previous]
    B --> D[Adjusted Latest Players]
    C --> E[Adjusted Previous Players]
    D --> F[Delta Calculation]
    E --> F
    F --> G[Filter]
    G --> H[Sort]
    H --> I[TSV Writer]
```

---

# 2. Responsibility Breakdown

```mermaid
classDiagram
    class FanProsDeltaReport {
        +GenerateAsync()
        -BuildPreviousLookup()
        -ComputeMovement()
        -FilterRows()
        -SortRows()
        -WriteTsv()
    }

    class FanProsCoreFieldsReport {
        +GenerateAsync()
        -TransformAsync()
        -ComputeAdjustedRank()
    }

    class FanProsDeltaRow {
        +PlayerName
        +Team
        +Position
        +PreviousRank
        +CurrentRank
        +Movement
    }

    FanProsDeltaReport --> FanProsCoreFieldsReport
    FanProsDeltaReport --> FanProsDeltaRow
```

---

# 3. Core Processing Flow

```mermaid
sequenceDiagram
    participant User
    participant DeltaReport
    participant CoreLatest
    participant CorePrevious
    participant CsvReader
    participant TsvWriter

    User->>DeltaReport: GenerateAsync()

    DeltaReport->>CoreLatest: GenerateAsync(latestPath)
    CoreLatest->>CsvReader: Read(latest.csv)
    CoreLatest-->>DeltaReport: Latest Adjusted Players

    DeltaReport->>CorePrevious: GenerateAsync(previousPath)
    CorePrevious->>CsvReader: Read(previous.csv)
    CorePrevious-->>DeltaReport: Previous Adjusted Players

    DeltaReport->>DeltaReport: Build Lookup
    DeltaReport->>DeltaReport: Compute Movement
    DeltaReport->>DeltaReport: Filter
    DeltaReport->>DeltaReport: Sort
    DeltaReport->>TsvWriter: Write(output.tsv)
```

---

# 4. AdjustedRank Computation

Raw rank is NOT used directly for movement.

AdjustedRank is recomputed inside `FanProsCoreFieldsReport`.

```mermaid
flowchart TD
    A[Raw CSV Players] --> B[Sort by Rank - Offset]
    B --> C[ThenBy Rank]
    C --> D[Assign AdjustedRank = index + 1]
```

Implementation concept:

```
Sort by (Rank - Offset)
Then by Rank
Reassign AdjustedRank sequentially
```

⚠ Any AdjustedRank value supplied in test data will be overwritten.

---

# 5. Movement Formula

Movement is calculated using AdjustedRank:

```
Movement = PreviousAdjustedRank - CurrentAdjustedRank
```

Interpretation:

| Movement | Meaning         |
| -------- | --------------- |
| Positive | Player improved |
| Negative | Player dropped  |
| 0        | No movement     |

---

# 6. Filtering Logic

Only include rows where:

```
PreviousRank > 0
AND
(PreviousRank <= 250 OR CurrentRank <= 250)
```

```mermaid
flowchart TD
    A[Delta Row] --> B{PreviousRank > 0?}
    B -- No --> X[Discard]
    B -- Yes --> C{Prev <= 250 OR Curr <= 250?}
    C -- No --> X
    C -- Yes --> D[Keep Row]
```

Purpose:

* Excludes unranked players
* Focuses on Top 250 relevance
* Allows breakout entries

---

# 7. Sorting Strategy

Rows are ordered by:

```
OrderByDescending(Abs(Movement))
ThenBy(CurrentRank)
```

```mermaid
flowchart TD
    A[All Valid Rows] --> B["Sort by Abs(Movement) DESC"]
    B --> C[ThenBy CurrentRank ASC]
    C --> D[Final Output Order]
```

This ensures:

1. Largest movement first
2. Tie → better current rank first

---

# 8. Example Walkthrough

### Current (after AdjustedRank)

| Player | Adjusted |
| ------ | -------- |
| A      | 1        |
| C      | 2        |
| B      | 3        |

### Previous (after AdjustedRank)

| Player | Adjusted |
| ------ | -------- |
| A      | 1        |
| B      | 2        |
| C      | 3        |

### Movement

| Player | Prev | Curr | Move | Abs |
| ------ | ---- | ---- | ---- | --- |
| C      | 3    | 2    | 1    | 1   |
| B      | 2    | 3    | -1   | 1   |
| A      | 1    | 1    | 0    | 0   |

Sorted result:

```
C
B
A
```

---

# 9. Output Format

TSV Columns:

```
PlayerName
Team
Position
PreviousRank
CurrentRank
Movement
```

Output is written via injected `ITsvWriter`.

---

# 10. Key Design Decisions

```mermaid
flowchart TD
    A[Design Decision] --> B[Use AdjustedRank]
    A --> C[Sort by Absolute Movement]
    A --> D[Top 250 Filter]
    A --> E[TSV Output]

    B --> B1[Reflects System Ranking Logic]
    C --> C1[Highlights Biggest Changes]
    D --> D1[Keeps Report Actionable]
    E --> E1[Excel Friendly]
```

---

# 11. Testing Considerations

Critical test insight:

```mermaid
flowchart TD
    A[Test Sets AdjustedRank] --> B[TransformAsync Runs]
    B --> C[AdjustedRank Overwritten]
    C --> D[Test Must Account for Re-Sorting]
```

When writing tests:

* Never rely on preset AdjustedRank
* Movement must be computed after transformation
* Sorting ties must consider CurrentRank

---

# 12. Full Conceptual Pipeline

```mermaid
flowchart TD
    A[Raw CSV Rank] --> B[Apply Offset]
    B --> C[Sort]
    C --> D[Recompute AdjustedRank]
    D --> E[Compare Prev vs Current]
    E --> F[Compute Movement]
    F --> G[Filter]
    G --> H[Sort by Abs Movement]
    H --> I[Write TSV]
```

---

# Summary Mental Model

The Delta Report measures:

> Movement in the system-adjusted ranking order — not raw FanPros CSV rank.

This ensures movement reflects the same ranking logic used by the application.

---

