using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MaterialEditor.Tests
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                bool preRelease = HasArgument(args, "--pre-release");
                bool stressOnly = HasArgument(args, "--stress-only");
                bool soakOnly = HasArgument(args, "--soak-only");
                bool validationReport = HasArgument(args, "--validation-report");
                bool soak = HasArgument(args, "--soak");
                bool stress = HasArgument(args, "--stress");
                bool hasAnyArguments = args != null && args.Length > 0;
                bool runSoak = preRelease || soak || soakOnly;
                bool runStress = preRelease || stress || stressOnly || (runSoak && !soakOnly);
                bool runCore = preRelease
                    || stress
                    || soak
                    || (!hasAnyArguments && !validationReport)
                    || (!stressOnly && !soakOnly && !validationReport && !runStress && !runSoak);
                var completedSuites = new List<SuiteRun>();
                ValidationReportSummary validationSummary = null;

                if (runCore)
                {
                    completedSuites.Add(RunSuite("core", () =>
                    {
                        AdvancedVariantTests.RunAll();
                        AppearanceTests.RunAll();
                        BulkMaterialEditSessionTests.RunAll();
                        FieldOverwriteAdvancedTests.RunAll();
                        MaterialBackupServiceTests.RunAll();
                        ThemeTests.RunAll();
                    }));
                }

                if (runStress)
                    completedSuites.Add(RunSuite("stress", StressTests.RunAll));

                if (runSoak)
                    completedSuites.Add(RunSuite("soak", StressTests.RunSoakAll));

                if (validationReport)
                {
                    string reportDirectory = GetOptionValue(args, "--report-dir");
                    validationSummary = ValidationReport.Generate(reportDirectory);
                    if (validationSummary.FailedCount > 0)
                        throw new InvalidOperationException($"Validation report generated with {validationSummary.FailedCount} failing case(s).");
                }

                Console.WriteLine(GetSuccessMessage(runCore, runStress, runSoak, preRelease, validationReport));
                Console.WriteLine(GetSummaryLine(completedSuites, validationSummary));
                if (validationSummary != null)
                    Console.WriteLine($"Validation summary: {validationSummary.PassedCount}/{validationSummary.TotalCount} cases passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static bool HasArgument(string[] args, string expected)
        {
            foreach (string arg in args ?? Array.Empty<string>())
            {
                if (string.Equals(arg, expected, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string GetOptionValue(string[] args, string optionName)
        {
            if (args == null || string.IsNullOrWhiteSpace(optionName))
                return string.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i] ?? string.Empty;
                if (string.Equals(arg, optionName, StringComparison.OrdinalIgnoreCase))
                    return i + 1 < args.Length ? args[i + 1] ?? string.Empty : string.Empty;

                string prefix = optionName + "=";
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return arg.Substring(prefix.Length);
            }

            return string.Empty;
        }

        private static SuiteRun RunSuite(string name, Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return new SuiteRun(name, stopwatch.Elapsed);
        }

        private static string GetSuccessMessage(bool runCore, bool runStress, bool runSoak, bool preRelease, bool validationReport)
        {
            if (preRelease)
                return "MaterialEditor.Tests pre-release suite passed.";

            if (validationReport && !runCore && !runStress && !runSoak)
                return "MaterialEditor.Tests validation report generated.";

            if (runCore && runStress && runSoak)
                return "MaterialEditor.Tests core, stress, and soak suites passed.";

            if (runStress && runSoak)
                return "MaterialEditor.Tests stress and soak suites passed.";

            if (runSoak)
                return "MaterialEditor.Tests soak suite passed.";

            if (runCore && runStress)
                return "MaterialEditor.Tests core and stress suites passed.";

            if (runStress)
                return "MaterialEditor.Tests stress suite passed.";

            return "MaterialEditor.Tests passed.";
        }

        private static string GetSummaryLine(IReadOnlyList<SuiteRun> completedSuites, ValidationReportSummary validationSummary)
        {
            if (completedSuites == null || completedSuites.Count == 0)
            {
                if (validationSummary != null)
                    return $"Summary: validation-report={validationSummary.PassedCount}/{validationSummary.TotalCount} passed.";

                return "Summary: no suites were selected.";
            }

            TimeSpan total = TimeSpan.Zero;
            foreach (SuiteRun suite in completedSuites)
                total += suite.Elapsed;

            string suites = string.Join(", ", completedSuites.Select(suite => $"{suite.Name}={FormatDuration(suite.Elapsed)}"));
            return $"Summary: {suites}; total={FormatDuration(total)}.";
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalSeconds < 1)
                return $"{duration.TotalMilliseconds:0}ms";

            if (duration.TotalMinutes < 1)
                return $"{duration.TotalSeconds:0.0}s";

            return $"{(int)duration.TotalMinutes}m {duration.Seconds:00}s";
        }

        private readonly record struct SuiteRun(string Name, TimeSpan Elapsed);
    }
}
