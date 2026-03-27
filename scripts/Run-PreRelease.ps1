param(
    [string]$Configuration = "Debug",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDirectory

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\pre-release"
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$env:DOTNET_CLI_HOME = Join-Path $repoRoot ".dotnet-home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

$projectPath = Join-Path $repoRoot "Material Editor.csproj"
$testsProjectPath = Join-Path $repoRoot "MaterialEditor.Tests\MaterialEditor.Tests.csproj"
$testsDllPath = Join-Path $repoRoot "MaterialEditor.Tests\bin\$Configuration\net8.0-windows10.0.22621.0\MaterialEditor.Tests.dll"
$logPath = Join-Path $OutputDirectory "pre-release.log"
$summaryPath = Join-Path $OutputDirectory "pre-release-summary.txt"
$validationSummaryPath = Join-Path $OutputDirectory "validation-summary.txt"
$validationReportPath = Join-Path $OutputDirectory "validation-report.json"

dotnet build $projectPath -p:RestoreIgnoreFailedSources=true -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) {
    throw "Material Editor build failed with exit code $LASTEXITCODE."
}

dotnet build $testsProjectPath --no-restore -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "MaterialEditor.Tests build failed with exit code $LASTEXITCODE."
}

if (Test-Path $logPath) {
    Remove-Item -LiteralPath $logPath -Force
}

dotnet $testsDllPath --pre-release 2>&1 | Tee-Object -FilePath $logPath
if ($LASTEXITCODE -ne 0) {
    throw "Pre-release test run failed with exit code $LASTEXITCODE."
}

$summaryLine = Select-String -Path $logPath -Pattern "^Summary:" | Select-Object -Last 1
if ($null -eq $summaryLine) {
    throw "Pre-release summary line was not found in '$logPath'."
}

Set-Content -LiteralPath $summaryPath -Value $summaryLine.Line -NoNewline
dotnet $testsDllPath --validation-report --report-dir $OutputDirectory
if ($LASTEXITCODE -ne 0) {
    throw "Validation report generation failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path $validationSummaryPath)) {
    throw "Validation summary file was not created at '$validationSummaryPath'."
}

if (-not (Test-Path $validationReportPath)) {
    throw "Validation report file was not created at '$validationReportPath'."
}

Write-Host "Pre-release summary saved to $summaryPath"
