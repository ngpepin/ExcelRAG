# ExcelRAG Architecture (Preliminary Scaffolding)

This document reflects the **current** implementation scaffolding in this repo (not the full planned scope in `README.md`).

## Goals (Current Phase)

- Target `.NET Framework 4.8` and remain compatible with **C# 7.3**.
- Follow the Excel-DNA patterns described in `AGENTS.md`:
  - identity UDFs are caller-scoped and idempotent
  - heavy work is trigger-guarded and async
  - observers are push-based (no forced recalculation)

## Current Solution Structure

- Solution root
  - `README.md` – product vision and planned UDF surface
  - `AGENTS.md` – operational rules for Excel-DNA UDFs
  - `ARCHITECTURE.md` – this file

- Project: `ExcelRAG/ExcelRAG.csproj`
  - Target: `net48`
  - Packages: `ExcelDna.AddIn`

## Implemented Scaffolding

### Add-in entry point

- `ExcelRAG/AddIn.cs`
  - Implements `ExcelDna.Integration.IExcelAddIn`
  - `AutoOpen()` / `AutoClose()` are present as the lifecycle hooks (currently empty)

### Infrastructure

- `ExcelRAG/Infrastructure/TriggerKey.cs`
  - `TriggerKey` is a small value type used to normalize and compare trigger inputs.
  - Used to ensure: **same trigger ? no-op**, **different trigger ? new run**.

- `ExcelRAG/Infrastructure/CallerKey.cs`
  - `CallerKey.Current()` derives a caller identity from `xlfCaller`.
  - Used to scope cache entries to a specific calling cell.

- `ExcelRAG/Infrastructure/CallerScopedCache.cs`
  - A minimal caller-scoped cache storing:
    - last `TriggerKey`
    - cached value
  - Used to make identity + triggered-work UDFs idempotent across recalculation storms.

- `ExcelRAG/Infrastructure/RagProgressHub.cs`
  - A very small publish/subscribe hub keyed by topic string.
  - Observers subscribe to topics; publishers push status updates.

### Domain models

- `ExcelRAG/Domain/RagSource.cs`
  - `RagSource` POCO representing a source document row (scaffold only).

- `ExcelRAG/Domain/RagChunk.cs`
  - `RagChunk` POCO representing a chunk record (scaffold only).

### Services

- `ExcelRAG/Services/RagChunker.cs`
  - `ChunkBySentence(...)` implements a simple sentence split + max-char split.
  - Also includes a naive token estimator.
  - This is placeholder logic to validate the Excel/UDF execution patterns.

### Excel UDF surface (preliminary)

- `ExcelRAG/Udfs/RagUdfs.cs`

Implemented UDFs:

- `RAG_SOURCE_ID(key, [trigger])`
  - **Category:** Identity (caller-scoped, cached)
  - Returns a stable id (per calling cell) unless trigger changes.

- `RAG_CHUNK(sourceId, content, [maxChars], [trigger])`
  - **Category:** Triggered work (async)
  - Uses `ExcelAsyncUtil.RunTask` and caches result per caller + trigger.
  - Returns a 2D table with headers:
    - `ChunkId`, `SourceId`, `ChunkText`, `TokenCount`, `OffsetStart`, `OffsetEnd`

- `RAG_STATUS([topic])`
  - **Category:** Observer
  - Uses `ExcelAsyncUtil.Observe` and `RagProgressHub` to push status strings.
  - Current topics used by scaffolding: `chunk`.

## Data/Control Flow (Current)

1. Excel cell calls `RAG_CHUNK(...)` with a trigger value
2. `TriggerKey` is computed and compared against `CallerScopedCache`
3. If trigger is new:
   - work runs via `ExcelAsyncUtil.RunTask`
   - status is published to `RagProgressHub` (`Running` ? `Done`)
   - result table is cached and returned
4. Cells calling `RAG_STATUS(...)` receive pushed updates

## What is intentionally missing (Next scaffolding waves)

This repo does **not** yet implement the planned items in `README.md`, including:

- ingestion (`RAG.INGEST`, file/URL parsing)
- metadata templating (`RAG.METADATA`)
- validation & dedup (`RAG.VALIDATE`, `RAG.DEDUP`)
- export (`RAG.EXPORT_JSONL`, manifests)
- ribbon UI

Those should be added by extending the same patterns already scaffolded: trigger-guarded async work + observer hub + caller-scoped caching.
