# Release Checklist

Use this checklist when preparing a release build of B.G.E.M.

## Automated

Run these commands sequentially from the repo root:

```powershell
$env:DOTNET_CLI_HOME='c:\Users\Scott\source\repos\materialeditor\Material-Editor\.dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
dotnet build "Material Editor.csproj" -p:RestoreIgnoreFailedSources=true -p:NuGetAudit=false
dotnet build "MaterialEditor.Tests\MaterialEditor.Tests.csproj" --no-restore
dotnet "MaterialEditor.Tests\bin\Debug\net8.0-windows10.0.22621.0\MaterialEditor.Tests.dll"
dotnet "MaterialEditor.Tests\bin\Debug\net8.0-windows10.0.22621.0\MaterialEditor.Tests.dll" --stress-only
dotnet "MaterialEditor.Tests\bin\Debug\net8.0-windows10.0.22621.0\MaterialEditor.Tests.dll" --soak-only
dotnet "MaterialEditor.Tests\bin\Debug\net8.0-windows10.0.22621.0\MaterialEditor.Tests.dll" --pre-release
dotnet "MaterialEditor.Tests\bin\Debug\net8.0-windows10.0.22621.0\MaterialEditor.Tests.dll" --validation-report --report-dir ".\artifacts\pre-release"
.\scripts\Run-PreRelease.ps1 -Configuration Debug
```

Expected outcome:

- the normal harness passes
- the optional stress suite passes
- the optional soak suite passes
- the pre-release aggregate run passes and prints a compact suite timing summary
- the wrapper script writes `artifacts\pre-release\pre-release-summary.txt` and `artifacts\pre-release\pre-release.log`
- the validation report writes `artifacts\pre-release\validation-summary.txt` and `artifacts\pre-release\validation-report.json`
- no unexpected warnings or crashes appear during either run

If you want one command that reruns the normal suite before the stress suite, use `--stress` instead of `--stress-only`.
If you want one command that runs core, stress, and soak together, use `--soak`.
If you want the same full run with an explicit pre-release summary line, use `--pre-release`.

## Manual UI

Run these scenarios against a representative set of `BGSM` and `BGEM` files:

1. Single-file editing: open both material types, edit a few fields, save, reload, and confirm values persist without changing unrelated fields.
2. Theme and settings: change theme, font, splash preference, and backup settings; reopen the app and confirm the settings persist and the UI still lays out cleanly.
3. Bulk editor loading: open a folder with at least 100 files, confirm the grid populates, paths trim after `Materials\` when available, and mixed/incompatible files are clearly surfaced.
4. Bulk editor interactions: verify row selection, cell selection, right-click behavior, fill up/down, replace-all, undo, redo, sort, filter, and folder grouping all still behave correctly.
5. Bulk apply workflow: dirty a subset of rows, use both apply-selected and apply-all, then confirm only intended files changed and saved rows are no longer marked dirty.
6. Backup and recovery: run a save with backups enabled, confirm backup files are created, then exercise recover newest, recover oldest, and browse backups.
7. Field overwrite workflow: run a legacy overwrite and an iterative overwrite, confirm preview/index behavior is correct, and verify manually disabled targets remain skipped.
8. Variation generator: test a legacy index pattern and an advanced variant pattern, confirm previews match generated file names, and verify duplicate-output and invalid-path validation.
9. Path handling: pick textures under `Textures\` and materials under `Materials\`, confirm stored paths are trimmed to game-relative form, and confirm missing-root warnings still allow proceeding.
10. Regression smoke test: close and reopen the app after each major workflow to confirm no crash-on-start, stale dirty state, or broken menu state remains.

## Suggested Data Sets

- a small set of 5 to 10 files for quick smoke testing
- a medium set of 50 to 100 files for bulk UI interaction checks
- a larger set of 100 to 250 files for release-time bulk workflow validation
