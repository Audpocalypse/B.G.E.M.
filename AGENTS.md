# Agent Instructions

## Repository overview

This repository contains **B.G.E.M.**, a WinForms desktop application for viewing and editing Bethesda material files:

- `BGSM`
- `BGEM`

The app is primarily a GUI editor, but it also includes bulk workflows such as:

- overwriting selected fields across many files
- generating material variations from a template

The solution also contains a small shared library for material serialization and a lightweight local test harness.

## Solution layout

- `Forms/`
  - main application window and menu-driven workflows
  - `Forms/Main.cs` is the central app entry point for most user actions
- `Dialogs/`
  - supporting dialogs such as field selection, target selection, output summaries, and variation generation
- `Controls/`
  - custom WinForms controls used throughout the editor, especially for file/path fields and typed property editors
- `Services/`
  - shared backend workflow logic such as:
    - `FieldOverwriteTool`
    - `MaterialVariationGenerator`
    - `MaterialVariationTokenExpander`
    - `MaterialFileCloner`
- `AdvancedVariant/`
  - shared advanced-variant expansion, rule resolution, and related models
- `Theming/`
  - dialog and control theme helpers
- `MaterialLib/`
  - lower-level BGSM/BGEM models and serialization
- `MaterialEditor.Tests/`
  - lightweight executable test harness for app-level behavior

## Important application workflows

### Main editor workflow

The core editor UI is driven from `Forms/Main.cs`.

Important responsibilities there include:

- loading and saving BGSM/BGEM files
- building the main property UI
- wiring file/path controls to material fields
- launching bulk tools and summaries

### Bulk overwrite workflow

Current bulk overwrite flow:

1. `Forms/Main.cs`
2. `Dialogs/FieldSelectionDialog.cs`
3. `Dialogs/TargetFileSelectionDialog.cs`
4. `Services/FieldOverwriteTool.cs`
5. `Dialogs/OverwriteSummaryDialog.cs`

This path is the main existing bulk-edit style workflow.

### Generate variations workflow

Current variation generation flow:

1. `Forms/Main.cs`
2. `Dialogs/VariationGeneratorDialog.cs`
3. `Services/MaterialVariationGenerator.cs`
4. `Dialogs/OverwriteSummaryDialog.cs`

This path now supports:

- legacy indexed generation
- Advanced Variant rule-based generation

## Current Advanced Variant state

Advanced Variant work is no longer backend-only. The repository now includes:

- shared Advanced Variant backend models and resolution logic under `AdvancedVariant/`
- token expansion support in `MaterialVariationTokenExpander`
- integration into `MaterialVariationGenerator`
- a first-pass UI inside `VariationGeneratorDialog`

### Current Advanced Variant behavior

The current Advanced Variant implementation supports:

- up to 4 logical layers
- wildcard/default rule matching via blank layer cells
- specificity-based override behavior
- row-order tie breaking for equally specific rules
- derived combined indices:
  - `{index}`
  - `{indexNN}`
- layer-specific numeric index tokens:
  - `{indexLayer1}` to `{indexLayer4}`
  - `{indexNNLayer1}` to `{indexNNLayer4}`
- a single authored naming token:
  - `{indexTok}`

`{indexTok}` is currently the only authored Advanced naming token in the rules UI.
If a resolved combination has no explicit `indexTok`, it falls back to the raw combined numeric index.

### Advanced Variant implementation guidance

When extending Advanced Variant behavior:

- keep business logic in shared backend code, not in the dialog
- prefer small concrete models and services over speculative abstractions
- preserve predictable wildcard/default and specificity behavior
- treat `IndexNN` as derived from resolved layer indices, not primary authored input
- avoid duplicating logic between generate and future bulk-edit paths

### Reference material

Reference material lives under:

- `reference/advanced-variant/FO4 Iterative Record Generators MS v200.pas`
- `reference/advanced-variant/cobjrecipes.csv`
- `reference/advanced-variant/README.md`

Use these as **behavioral reference only**.

Do not:

- port Pascal directly
- copy FO4Edit/xEdit/JvInterpreter architecture
- import script-environment limitations unless still logically useful here

Use the references mainly for:

- layered expansion behavior
- wildcard/default semantics
- specificity and override ordering
- inferred index formatting
- naming/token concepts

## Coding guidance for this repository

- Reuse existing WinForms patterns already present in the app.
- Keep reusable logic out of dialogs when possible.
- Prefer extending existing shared helpers before creating parallel ones.
- Use ASCII unless the file already requires otherwise.
- Keep UI additions practical and consistent with the app instead of introducing speculative frameworks.
- Avoid destructive git operations unless explicitly requested.

### File/path behavior

This app stores material and texture paths as game-relative style paths when possible.

Current expectations:

- texture selections should prefer the path after `Textures\`
- material selections should prefer the path after `Materials\`
- if those roots are missing, warn but allow the user to proceed

## Verification expectations

For app-level changes, prefer verifying with:

- `dotnet build "Material Editor.csproj" -p:RestoreIgnoreFailedSources=true -p:NuGetAudit=false`
- `dotnet build "MaterialEditor.Tests\\MaterialEditor.Tests.csproj" --no-restore`
- `dotnet "MaterialEditor.Tests\\bin\\Debug\\net8.0-windows10.0.22621.0\\MaterialEditor.Tests.dll"`

In this environment, it is often helpful to set:

- `DOTNET_CLI_HOME` to the repo-local `.dotnet-home`
- `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`

Example:

```powershell
$env:DOTNET_CLI_HOME='c:\Users\Scott\source\repos\materialeditor\Material-Editor\.dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
dotnet build "Material Editor.csproj" -p:RestoreIgnoreFailedSources=true -p:NuGetAudit=false
```

## When starting a substantial change

Before making major changes, especially to bulk workflows:

1. Identify the actual entry points in `Forms/Main.cs`.
2. Identify existing dialogs and shared services that already cover part of the workflow.
3. Look for reusable naming, preview, clone, backup, save, and summary logic first.
4. If Advanced Variant behavior is involved, inspect only the relevant reference sections and translate behavior into idiomatic C#.

## Durable rule for future tasks

This file should describe the **repository and its stable conventions**, not only the latest task.

When updating `AGENTS.md` in the future:

- keep repo-wide guidance intact
- add or revise major current initiatives only as a section of the file
- avoid turning the whole document into a single-task brief


Important Note:
verification commands in parallel, the test-project build hit the expected CS2012 file-lock on obj\...\bgem.dll. Sequential verification is clean and remains the right way to validate this repo.