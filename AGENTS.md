# Agent Instructions

This repository contains **B.G.E.M.**, a WinForms desktop application for viewing and editing Bethesda material files (`BGSM`, `BGEM`). It also includes bulk overwrite and variation-generation workflows, shared material serialization code, and a lightweight executable test harness.

## High-Value Repo Guidance

- Reuse existing WinForms patterns already present in the app.
- Keep business logic and other reusable logic out of dialogs when possible.
- Prefer extending an existing helper, service, or model before creating a parallel one.
- Prefer the smallest valid change over introducing new abstractions.
- Keep UI additions practical and consistent with the current app.
- Use ASCII unless a file already requires otherwise.
- Avoid destructive git operations unless explicitly requested.

## Solution Map

- `Forms/`
  - main application window and menu-driven workflows
  - `Forms/Main.cs` is the central app entry point for most user actions
- `Dialogs/`
  - supporting dialogs for selection, generation, and summaries
- `Controls/`
  - custom WinForms controls used across the app
- `Services/`
  - shared backend workflow logic such as `FieldOverwriteTool` and `MaterialVariationGenerator`
- `AdvancedVariant/`
  - shared Advanced Variant models, rule resolution, and expansion logic
- `Theming/`
  - dialog and control theme helpers
- `MaterialLib/`
  - lower-level BGSM/BGEM models and serialization
- `MaterialEditor.Tests/`
  - lightweight executable test harness for app-level behavior

## Workflow Entry Points

Start by locating the real entry points in `Forms/Main.cs`.

### Bulk overwrite

1. `Forms/Main.cs`
2. `Dialogs/FieldSelectionDialog.cs`
3. `Dialogs/TargetFileSelectionDialog.cs`
4. `Services/FieldOverwriteTool.cs`
5. `Dialogs/OverwriteSummaryDialog.cs`

### Generate variations

1. `Forms/Main.cs`
2. `Dialogs/VariationGeneratorDialog.cs`
3. `Services/MaterialVariationGenerator.cs`
4. `Dialogs/OverwriteSummaryDialog.cs`

Reuse existing preview, clone, backup, save, and summary logic before adding a new path.

## Advanced Variant

When changing or extending Advanced Variant behavior:

- keep business logic in shared backend code, not in the dialog
- preserve predictable wildcard/default matching from blank layer cells
- preserve specificity-based override behavior
- preserve row-order tie breaking when two rules are equally specific
- treat `IndexNN` as derived output, not primary authored input
- avoid duplicating logic between generate and future bulk-edit paths
- prefer small concrete models and services over speculative abstractions

Current supported behavior:

- up to 4 logical layers
- wildcard/default rule matching via blank layer cells
- specificity-based overrides
- row-order tie breaking for equally specific rules
- derived combined indices: `{index}`, `{indexNN}`
- layer-specific numeric index tokens: `{indexLayer1}` to `{indexLayer4}`, `{indexNNLayer1}` to `{indexNNLayer4}`
- a single authored naming token: `{indexTok}`

`{indexTok}` is currently the only authored Advanced Variant naming token in the rules UI. If a resolved combination has no explicit `indexTok`, it falls back to the raw combined numeric index.

## Legacy Reference Material

Archived legacy reference material exists under `reference/advanced-variant/`.

Do not use it as the default implementation guide. Consult it only for unresolved behavior that is not already represented in the current C# codebase.

If legacy reference material must be consulted:

- use it as behavioral reference only
- do not port Pascal directly
- do not copy FO4Edit/xEdit/JvInterpreter architecture
- do not import script-environment limitations unless they are still logically useful here

## File and Path Behavior

This app stores material and texture paths as game-relative style paths when possible.

- texture selections should prefer the path after `Textures\`
- material selections should prefer the path after `Materials\`
- if those roots are missing, warn but allow the user to proceed

## Verification

For app-level changes, prefer verifying with:

- `dotnet build "Material Editor.csproj" -p:RestoreIgnoreFailedSources=true -p:NuGetAudit=false`
- `dotnet build "MaterialEditor.Tests\\MaterialEditor.Tests.csproj" --no-restore`
- `dotnet "MaterialEditor.Tests\\bin\\Debug\\net8.0-windows10.0.22621.0\\MaterialEditor.Tests.dll"`

Helpful environment settings in this repo:

- `DOTNET_CLI_HOME` set to the repo-local `.dotnet-home`
- `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`

Example:

```powershell
$env:DOTNET_CLI_HOME='c:\Users\Scott\source\repos\materialeditor\Material-Editor\.dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
dotnet build "Material Editor.csproj" -p:RestoreIgnoreFailedSources=true -p:NuGetAudit=false
```

Important:

- run verification commands sequentially, not in parallel
- parallel verification can hit the expected CS2012 file-lock on `obj\...\bgem.dll`
- sequential verification is the correct validation approach for this repo

## Approach for Substantial Changes

Before making major changes, especially to bulk workflows:

1. Identify the actual entry points in `Forms/Main.cs`.
2. Identify existing dialogs and shared services that already cover part of the workflow.
3. Reuse existing naming, preview, clone, backup, save, and summary logic first.
4. If Advanced Variant behavior is involved, prefer the current C# implementation first; consult legacy reference material only for unresolved behavior that is not already captured in the codebase.

## Keeping This File Useful

This file should describe the repository's stable conventions, not only the latest task.

When updating `AGENTS.md` in the future:

- keep repo-wide guidance intact
- add or revise major current initiatives only as a section of the file
- avoid turning the whole document into a single-task brief
- prefer durable repo-specific constraints over broad repo narration
