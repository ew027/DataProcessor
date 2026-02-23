# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build the full solution
dotnet build DataProcessor.sln

# Build individual projects
dotnet build DataProcessor.Engine/DataProcessor.Engine.csproj
dotnet build DataProcessor.UI/DataProcessor.UI.csproj

# Run the UI (Windows only, WinForms)
dotnet run --project DataProcessor.UI/DataProcessor.UI.csproj

# Build release
dotnet build DataProcessor.sln -c Release
```

There are no automated tests in this codebase.

## Architecture Overview

The solution contains two projects:

- **DataProcessor.Engine** (`net8.0`) — Core data processing library
- **DataProcessor.UI** (`net8.0-windows`) — MDI WinForms frontend

### Engine: Script Execution Model

`Engine.cs` is the central orchestrator. It maintains:
- A dictionary of named `DataArray` instances (the datasets)
- A dictionary of named operation lists (the scripts)
- A log of execution output

Scripts are loaded from pipe-delimited text files. Each line has the format:
```
OperationName|param1|param2|...
```

Lines starting with `#` are treated as comments. When `Engine.Execute(operationName)` is called, it uses **reflection** to:
1. Instantiate the corresponding `DataProcessor.Engine.Operations.{OperationName}Operation` class
2. Call the method named by `operation.GetExecuteMethod()` on the `Engine` instance

Each `Execute*` method on `Engine` receives the operation object, performs the work on the named `DataArray`, and returns a log string.

### DataArray: The Columnar Data Store

`DataArray` stores data column-major as `List<List<object>>` where the outer list is columns and each inner list holds that column's row values. Column metadata is in `List<Column>` and name-to-index lookup is in `Dictionary<string, int> ColumnPositions`.

**Missing value conventions:**
- Integer missing: `Int32.MinValue`
- Double missing: `Double.NaN`

`DataArray` can export to CSV (`Export`) or SPSS `.sav` format (`ExportToSpss`) using the `Spss` library.

### Operations Pattern

All operation classes in `DataProcessor.Engine/Operations/` follow this pattern:
- Inherit from `Operation` (abstract base)
- Constructor accepts a pipe-delimited argument string and parses it into properties
- Override `GetExecuteMethod()` to return the corresponding `Execute*` method name on `Engine`

Available operations: `AboveBelow`, `Aggregate`, `AddValueLabels`, `AddVariableLabel`, `BlankSurvey`, `Comment`, `ComputeExpr`, `ComputeSum`, `CreateIntVar`, `Export`, `GenderRecode`, `IPCheck`, `KeepVars`, `Recode`, `RecodeInto`, `RecodeRange`, `RemoveRecords`, `RenameVars`, `SetRemoveFlag`, `SigTest`, `Sort`, `SplitVar`.

### Expression Evaluation

`Eval.cs` (based on Jonathan Wood's CPOL-licensed code) evaluates arithmetic expressions used by `ComputeExpr`. Variable references in expressions are resolved via the `ProcessSymbol` event, which the `Engine` hooks to look up values from the active `DataArray`.

### Newer Type System (In Progress)

`DataTable.cs` and `Column<T> : IColumn` represent a newer, strongly-typed columnar data model intended to replace `DataArray` and the legacy non-generic `Column` class used in `Engine.cs`. The legacy code in `Engine.cs`/`DataArray.cs` still uses the old non-generic `Column` class with a `ColumnType` enum property. The two systems coexist but are not yet integrated.

### UI

`DataProcessor.UI` is a WinForms MDI application. `MainForm` is the MDI container; child forms are:
- `CodeEditorForm` — text editor for writing/loading scripts
- `WebViewForm` — HTML output viewer (uses `Westermo.HtmlRenderer`, included as local DLLs in `DataProcessor.UI/lib/`)
- `DataGridForm` — tabular data viewer

The UI does not yet wire the editor to the engine; it is a shell.
