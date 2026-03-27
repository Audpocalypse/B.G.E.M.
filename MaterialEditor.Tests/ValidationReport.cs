using Material_Editor.AdvancedVariant;
using Material_Editor.Models;
using Material_Editor.Services;
using MaterialLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace MaterialEditor.Tests
{
    internal static class ValidationReport
    {
        public static ValidationReportSummary Generate(string outputDirectory)
        {
            string targetDirectory = ResolveOutputDirectory(outputDirectory);
            Directory.CreateDirectory(targetDirectory);

            var cases = new List<ValidationCaseResult>
            {
                RunCase("legacy_index_basic", "legacy-token", "legacy_7", () => MaterialVariationGenerator.PreviewFieldValue("legacy_{index}", 7)),
                RunCase("legacy_index_padded", "legacy-token", "legacy_007", () => MaterialVariationGenerator.PreviewFieldValue("legacy_{index:000}", 7)),
                RunCase("legacy_pattern_normalization", "legacy-token", "variant_{indexNN}.bgsm", () => MaterialVariationGenerator.NormalizeLegacyOutputPatternForAdvancedMode("variant_{index:00}.bgsm")),
                RunCase("wildcard_default_specific_resolution", "advanced-wildcard", "base,base,exact,layer1", ResolveWildcardPriorityCase),
                RunCase("later_rule_tie_break", "advanced-wildcard", "second", ResolveLaterRuleTieBreakCase),
                RunCase("layer_token_resolution", "advanced-token", "primary|red;pads|green", ResolveLayerTokenCase),
                RunCase("indexTok_fallback", "advanced-token", "81|True;vaultpants|False", ResolveIndexTokenFallbackCase),
                RunCase("advanced_preview_full_pattern", "advanced-token", "name_112_112_red_1_12_primary_red", ResolveAdvancedPreviewCase),
                RunCase("advanced_generation_paths_and_fields", "advanced-generation", "variant_1_base.bgsm=>textures\\\\variant_1_base.dds;variant_2_special.bgsm=>textures\\\\variant_2_special.dds", ResolveAdvancedGenerationCase),
                RunCase("template_token_expansion", "advanced-generation", "variant_11.bgsm=>vaultsuitNumber1_d.dds|vaultsuitNumber1_n.dds;variant_12.bgsm=>vaultsuitNumber2_d.dds|vaultsuitNumber2_n.dds", ResolveTemplateTokenExpansionCase),
                RunCase("disabled_rows_skip", "advanced-wildcard", "variant_1.bgsm,variant_3.bgsm", ResolveDisabledRowsCase),
                RunCase("duplicate_output_validation", "advanced-generation", "Failed,Failed", ResolveDuplicateOutputCase),
                RunCase("iterative_indexTok_resolution", "iterative-token", "3|True;named|False", ResolveIterativeIndexTokenCase),
                RunCase("iterative_pattern_expansion", "iterative-token", "textures\\\\iter_5_05.dds;textures\\\\iter_named_06.dds", ResolveIterativePatternExpansionCase),
                RunCase("iterative_advanced_token_expansion", "iterative-token", "adv_1_base_1_1;adv_2_special_2_special", ResolveIterativeAdvancedTokenCase),
                RunCase("advanced_parentheses_token_syntax", "future-pattern", "future_112_1_red", ResolveParenthesesTokenSyntaxCase),
                RunCase("advanced_explicit_layer_token_override", "future-pattern", "armor|blue;base|blue", ResolveExplicitLayerTokenOverrideCase),
                RunCase("advanced_preview_output_paths_realistic", "future-pattern", "Armor\\Raider\\raider_utility_0101.bgsm;Armor\\Raider\\raider_utility_0102.bgsm;Armor\\Raider\\raider_heavy_0201.bgsm;Armor\\Raider\\raider_heavy_0202.bgsm", ResolveRealisticPreviewOutputPathsCase),
                RunCase("advanced_layer_padding_preview", "future-pattern", "Armor\\Set\\piece_01_01_001.bgsm", ResolveLayerPaddingPreviewCase),
                RunCase("advanced_layer_token_fallback_preview", "future-pattern", "Boots\\vault_4_09.bgsm", ResolveLayerTokenFallbackPreviewCase),
                RunCase("iterative_mixed_target_selection_preview", "future-pattern", "a.bgsm|True|future_10_10.dds;b.bgsm|False|;c.bgsm|True|future_12_named.dds;wrong.bgem|False|", ResolveIterativeMixedTargetSelectionCase),
                RunCase("backup_retained_original_sequence", "backup", "2|1|textures\\\\original.dds|textures\\\\first.dds", ResolveRetainedOriginalBackupCase),
                RunCase("backup_restore_round_trip", "backup", "Success|textures\\\\original.dds;Success|textures\\\\first.dds", ResolveBackupRestoreRoundTripCase),
                RunCase("find_replace_request_clone_isolated", "find-replace", "True|False|Diffuse;False|True|Diffuse,Normal", ResolveFindReplaceRequestCloneCase)
            };

            var summary = new ValidationReportSummary(
                DateTime.UtcNow,
                cases.Count,
                cases.Count(result => result.Passed),
                cases.Count(result => !result.Passed),
                cases);

            WriteOutputs(targetDirectory, summary);
            return summary;
        }

        private static ValidationCaseResult RunCase(string name, string category, string expected, Func<string> actualFactory)
        {
            string actual;
            string errorMessage = string.Empty;

            try
            {
                actual = actualFactory() ?? string.Empty;
            }
            catch (Exception ex)
            {
                actual = string.Empty;
                errorMessage = ex.ToString();
            }

            bool passed = string.IsNullOrEmpty(errorMessage) && string.Equals(expected, actual, StringComparison.Ordinal);
            return new ValidationCaseResult(name, category, expected, actual, passed, errorMessage);
        }

        private static string ResolveWildcardPriorityCase()
        {
            var options = new AdvancedVariantOptions
            {
                Layers = new[]
                {
                    new AdvancedVariantLayerDefinition("Layer1", new[] { 0, 1 }),
                    new AdvancedVariantLayerDefinition("Layer2", new[] { 0, 1 }),
                },
                Rules = new[]
                {
                    new AdvancedVariantRule { IndexToken = "base", SourceOrder = 0 },
                    new AdvancedVariantRule { Layer1Index = 1, IndexToken = "layer1", SourceOrder = 1 },
                    new AdvancedVariantRule { Layer1Index = 1, Layer2Index = 0, IndexToken = "exact", SourceOrder = 2 },
                }
            };

            return string.Join(",", AdvancedVariantEngine.Resolve(options).Select(context => context.IndexTok));
        }

        private static string ResolveLaterRuleTieBreakCase()
        {
            var options = new AdvancedVariantOptions
            {
                Layers = new[]
                {
                    new AdvancedVariantLayerDefinition("Only", new[] { 7 }),
                },
                Rules = new[]
                {
                    new AdvancedVariantRule { Layer1Index = 7, IndexToken = "first", SourceOrder = 0 },
                    new AdvancedVariantRule { Layer1Index = 7, IndexToken = "second", SourceOrder = 1 },
                }
            };

            return AdvancedVariantEngine.Resolve(options).Single().IndexTok;
        }

        private static string ResolveLayerTokenCase()
        {
            var options = new AdvancedVariantOptions
            {
                Layers = new[]
                {
                    new AdvancedVariantLayerDefinition("Layer1", new[] { 1, 6 }),
                    new AdvancedVariantLayerDefinition("Layer2", new[] { 1, 28 }),
                },
                Rules = new[]
                {
                    new AdvancedVariantRule { Layer1Index = 1, IndexToken = "primary", SourceOrder = 0 },
                    new AdvancedVariantRule { Layer1Index = 6, IndexToken = "pads", SourceOrder = 1 },
                    new AdvancedVariantRule { Layer2Index = 1, IndexToken = "red", SourceOrder = 2 },
                    new AdvancedVariantRule { Layer2Index = 28, IndexToken = "green", SourceOrder = 3 },
                }
            };

            AdvancedVariantResolvedContext[] contexts = AdvancedVariantEngine.Resolve(options).ToArray();
            return $"{contexts[0].Layer1IndexToken}|{contexts[0].Layer2IndexToken};{contexts[3].Layer1IndexToken}|{contexts[3].Layer2IndexToken}";
        }

        private static string ResolveIndexTokenFallbackCase()
        {
            var options = new AdvancedVariantOptions
            {
                Layers = new[]
                {
                    new AdvancedVariantLayerDefinition("Layer1", new[] { 8 }),
                    new AdvancedVariantLayerDefinition("Layer2", new[] { 1, 2 }),
                },
                Rules = new[]
                {
                    new AdvancedVariantRule { Layer2Index = 2, IndexToken = "vaultpants", SourceOrder = 1 },
                }
            };

            AdvancedVariantResolvedContext[] contexts = AdvancedVariantEngine.Resolve(options).ToArray();
            return $"{contexts[0].IndexTok}|{contexts[0].IndexTokFallback};{contexts[1].IndexTok}|{contexts[1].IndexTokFallback}";
        }

        private static string ResolveAdvancedPreviewCase()
        {
            AdvancedVariantResolvedContext context = AdvancedVariantEngine.Resolve(new AdvancedVariantOptions
            {
                Layers = new[]
                {
                    new AdvancedVariantLayerDefinition("Layer1", new[] { 1 }),
                    new AdvancedVariantLayerDefinition("Layer2", new[] { 12 }),
                },
                Rules = new[]
                {
                    new AdvancedVariantRule { Layer1Index = 1, IndexToken = "primary" },
                    new AdvancedVariantRule { Layer2Index = 12, IndexToken = "red" },
                    new AdvancedVariantRule { IndexToken = "token" }
                }
            }).Single();

            return MaterialVariationGenerator.PreviewFieldValue("name_{index}_{indexNN}_{indexTok}_{indexLayer1}_{indexNNLayer2}_{indexTokLayer1}_{indexTokLayer2}", context);
        }

        private static string ResolveAdvancedGenerationCase()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope("MaterialEditorValidation");
            string outputPath = outputDirectory.Path;
            var template = new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\base.dds",
                NormalTexture = "textures\\normal.dds"
            };

            var options = new MaterialVariationOptions
            {
                OutputPattern = Path.Combine(outputPath, "variant_{indexNN}_{indexTok}.bgsm"),
                Fields = new[]
                {
                    new MaterialVariationFieldAssignment(GetDescriptor(ControlNames.Diffuse), "textures\\variant_{indexLayer1}_{indexTok}.dds")
                },
                AdvancedVariant = new AdvancedVariantOptions
                {
                    Layers = new[]
                    {
                        new AdvancedVariantLayerDefinition("Layer1", new[] { 1, 2 }),
                    },
                    Rules = new[]
                    {
                        new AdvancedVariantRule { IndexToken = "base", SourceOrder = 0 },
                        new AdvancedVariantRule { Layer1Index = 2, IndexToken = "special", SourceOrder = 1 },
                    }
                }
            };

            MaterialVariationGenerator.Generate(template, options, serializeAsJson: true);

            string firstPath = Path.Combine(outputPath, "variant_1_base.bgsm");
            string secondPath = Path.Combine(outputPath, "variant_2_special.bgsm");
            return $"variant_1_base.bgsm=>{ExtractEscapedSubstring(File.ReadAllText(firstPath), "textures\\\\variant_1_base.dds")};variant_2_special.bgsm=>{ExtractEscapedSubstring(File.ReadAllText(secondPath), "textures\\\\variant_2_special.dds")}";
        }

        private static string ResolveTemplateTokenExpansionCase()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope("MaterialEditorValidation");
            string outputPath = outputDirectory.Path;
            var template = new BGSM
            {
                Version = 2,
                DiffuseTexture = "Clothes\\VaultSuit\\vaultsuitNumber{indexLayer2}_d.dds",
                NormalTexture = "Clothes\\VaultSuit\\vaultsuitNumber{indexLayer2}_n.dds"
            };

            var options = new MaterialVariationOptions
            {
                OutputPattern = Path.Combine(outputPath, "variant_{indexNN}.bgsm"),
                AdvancedVariant = new AdvancedVariantOptions
                {
                    Layers = new[]
                    {
                        new AdvancedVariantLayerDefinition("Layer1", new[] { 1 }),
                        new AdvancedVariantLayerDefinition("Layer2", new[] { 1, 2 }),
                    }
                }
            };

            MaterialVariationGenerator.Generate(template, options, serializeAsJson: true);

            string firstText = File.ReadAllText(Path.Combine(outputPath, "variant_11.bgsm"));
            string secondText = File.ReadAllText(Path.Combine(outputPath, "variant_12.bgsm"));
            return $"variant_11.bgsm=>{ExtractEscapedSubstring(firstText, "vaultsuitNumber1_d.dds")}|{ExtractEscapedSubstring(firstText, "vaultsuitNumber1_n.dds")};variant_12.bgsm=>{ExtractEscapedSubstring(secondText, "vaultsuitNumber2_d.dds")}|{ExtractEscapedSubstring(secondText, "vaultsuitNumber2_n.dds")}";
        }

        private static string ResolveDisabledRowsCase()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope("MaterialEditorValidation");
            string outputPath = outputDirectory.Path;
            var options = new MaterialVariationOptions
            {
                OutputPattern = Path.Combine(outputPath, "variant_{indexNN}.bgsm"),
                AdvancedVariant = new AdvancedVariantOptions
                {
                    Layers = new[]
                    {
                        new AdvancedVariantLayerDefinition("Layer1", new[] { 1, 2, 3 }),
                    },
                    Rules = new[]
                    {
                        new AdvancedVariantRule { Layer1Index = 2, Enabled = false, SourceOrder = 0 },
                    }
                }
            };

            MaterialVariationGenerator.Generate(new BGSM { Version = 2 }, options, serializeAsJson: true);
            return string.Join(",", Directory.GetFiles(outputPath, "*.bgsm").Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal));
        }

        private static string ResolveDuplicateOutputCase()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope("MaterialEditorValidation");
            string outputPath = outputDirectory.Path;
            var options = new MaterialVariationOptions
            {
                OutputPattern = Path.Combine(outputPath, "same.bgsm"),
                AdvancedVariant = new AdvancedVariantOptions
                {
                    Layers = new[]
                    {
                        new AdvancedVariantLayerDefinition("Layer1", new[] { 1, 2 }),
                    }
                }
            };

            IReadOnlyList<FieldCopyResult> results = MaterialVariationGenerator.Generate(new BGSM { Version = 2 }, options, serializeAsJson: true);
            return string.Join(",", results.Select(result => result.Status.ToString()));
        }

        private static string ResolveIterativeIndexTokenCase()
        {
            IReadOnlyList<IterativeTargetContext> contexts = IterativeFieldOverwritePlanner.BuildContexts(
                MaterialType.Material,
                new[] { @"C:\temp\a.bgsm", @"C:\temp\b.bgsm" },
                new IterativeFieldOverwriteOptions
                {
                    StartIndex = 3,
                    Count = 2,
                    Step = 1,
                    Targets = new[]
                    {
                        new IterativeTargetOverride(@"C:\temp\b.bgsm", "named")
                    }
                });

            return $"{contexts[0].IndexTok}|{contexts[0].IndexTokFallback};{contexts[1].IndexTok}|{contexts[1].IndexTokFallback}";
        }

        private static string ResolveIterativePatternExpansionCase()
        {
            IReadOnlyList<IterativeTargetContext> contexts = IterativeFieldOverwritePlanner.BuildContexts(
                MaterialType.Material,
                new[] { @"C:\temp\a.bgsm", @"C:\temp\b.bgsm" },
                new IterativeFieldOverwriteOptions
                {
                    StartIndex = 5,
                    Count = 2,
                    Step = 1,
                    Targets = new[]
                    {
                        new IterativeTargetOverride(@"C:\temp\b.bgsm", "named")
                    }
                });

            string first = MaterialVariationTokenExpander.Expand("textures\\iter_{indexTok}_{index:00}.dds", contexts[0].IndexValue, contexts[0]);
            string second = MaterialVariationTokenExpander.Expand("textures\\iter_{indexTok}_{index:00}.dds", contexts[1].IndexValue, contexts[1]);
            return $"{EscapeForLog(first)};{EscapeForLog(second)}";
        }

        private static string ResolveIterativeAdvancedTokenCase()
        {
            string[] targets = { @"C:\temp\a.bgsm", @"C:\temp\b.bgsm" };
            IReadOnlyList<IterativeTargetContext> contexts = IterativeFieldOverwritePlanner.BuildContexts(
                MaterialType.Material,
                targets,
                new IterativeFieldOverwriteOptions
                {
                    StartIndex = 1,
                    Count = 2,
                    Step = 1,
                    AdvancedVariant = new AdvancedVariantOptions
                    {
                        Layers = new[]
                        {
                            new AdvancedVariantLayerDefinition("Layer1", new[] { 1, 2 }),
                        },
                        Rules = new[]
                        {
                            new AdvancedVariantRule { IndexToken = "base", SourceOrder = 0 },
                            new AdvancedVariantRule { Layer1Index = 2, IndexToken = "special", SourceOrder = 1 },
                        }
                    }
                });

            string first = MaterialVariationTokenExpander.Expand("adv_{indexNN}_{indexTok}_{indexLayer1}_{indexTokLayer1}", null, contexts[0]);
            string second = MaterialVariationTokenExpander.Expand("adv_{indexNN}_{indexTok}_{indexLayer1}_{indexTokLayer1}", null, contexts[1]);
            return $"{first};{second}";
        }

        private static string ResolveParenthesesTokenSyntaxCase()
        {
            AdvancedVariantResolvedContext context = AdvancedVariantEngine.Resolve(new AdvancedVariantOptions
            {
                Layers = new[]
                {
                    new AdvancedVariantLayerDefinition("Layer1", new[] { 1 }),
                    new AdvancedVariantLayerDefinition("Layer2", new[] { 12 }),
                },
                Rules = new[]
                {
                    new AdvancedVariantRule { Layer2Index = 12, IndexToken = "red", SourceOrder = 0 }
                }
            }).Single();

            return MaterialVariationGenerator.PreviewFieldValue("future_(indexNN)_(indexLayer1)_(indexTok)", context);
        }

        private static string ResolveExplicitLayerTokenOverrideCase()
        {
            var options = new AdvancedVariantOptions
            {
                Layers = new[]
                {
                    new AdvancedVariantLayerDefinition("ArmorClass", new[] { 1, 2 }),
                    new AdvancedVariantLayerDefinition("Color", new[] { 7 }),
                },
                Rules = new[]
                {
                    new AdvancedVariantRule { Layer1Index = 2, IndexToken = "heavy", Layer1Token = "armor", SourceOrder = 0 },
                    new AdvancedVariantRule { Layer2Index = 7, IndexToken = "blue", Layer2Token = "blue", SourceOrder = 1 },
                    new AdvancedVariantRule { Layer1Index = 1, IndexToken = "base", SourceOrder = 2 },
                }
            };

            AdvancedVariantResolvedContext[] contexts = AdvancedVariantEngine.Resolve(options).ToArray();
            return $"{contexts[1].Layer1IndexToken}|{contexts[1].Layer2IndexToken};{contexts[0].Layer1IndexToken}|{contexts[0].Layer2IndexToken}";
        }

        private static string ResolveRealisticPreviewOutputPathsCase()
        {
            var options = new MaterialVariationOptions
            {
                OutputPattern = @"Armor\Raider\raider_{indexTok}_{indexNNLayer1}{indexNNLayer2}.bgsm",
                AdvancedVariant = new AdvancedVariantOptions
                {
                    Layers = new[]
                    {
                        new AdvancedVariantLayerDefinition("Type", new[] { 1, 2, 99 }),
                        new AdvancedVariantLayerDefinition("Color", new[] { 1, 2, 99 }),
                    },
                    Rules = new[]
                    {
                        new AdvancedVariantRule { Layer1Index = 1, IndexToken = "utility", SourceOrder = 0 },
                        new AdvancedVariantRule { Layer1Index = 2, IndexToken = "heavy", SourceOrder = 1 },
                    }
                }
            };

            return string.Join(";", MaterialVariationGenerator.PreviewOutputPaths(options)
                .Where(path => !path.Contains("99", StringComparison.Ordinal))
                .Take(4));
        }

        private static string ResolveLayerPaddingPreviewCase()
        {
            AdvancedVariantResolvedContext context = AdvancedVariantEngine.Resolve(new AdvancedVariantOptions
            {
                Layers = new[]
                {
                    new AdvancedVariantLayerDefinition("Layer1", new[] { 1, 12 }),
                    new AdvancedVariantLayerDefinition("Layer2", new[] { 1, 12 }),
                    new AdvancedVariantLayerDefinition("Layer3", new[] { 1, 123 }),
                }
            }).First();

            return MaterialVariationGenerator.PreviewFieldValue(@"Armor\Set\piece_{indexNNLayer1}_{indexNNLayer2}_{indexNNLayer3}.bgsm", context);
        }

        private static string ResolveLayerTokenFallbackPreviewCase()
        {
            AdvancedVariantResolvedContext context = AdvancedVariantEngine.Resolve(new AdvancedVariantOptions
            {
                Layers = new[]
                {
                    new AdvancedVariantLayerDefinition("Style", new[] { 4 }),
                    new AdvancedVariantLayerDefinition("Color", new[] { 9, 99 }),
                }
            }).First();

            return MaterialVariationGenerator.PreviewFieldValue(@"Boots\vault_{indexTokLayer1}_{indexNNLayer2}.bgsm", context);
        }

        private static string ResolveIterativeMixedTargetSelectionCase()
        {
            IReadOnlyList<IterativeTargetContext> contexts = IterativeFieldOverwritePlanner.BuildContexts(
                MaterialType.Material,
                new[]
                {
                    @"C:\temp\c.bgsm",
                    @"C:\temp\b.bgsm",
                    @"C:\temp\a.bgsm",
                    @"C:\temp\wrong.bgem",
                },
                new IterativeFieldOverwriteOptions
                {
                    StartIndex = 10,
                    Count = 3,
                    Step = 1,
                    Targets = new[]
                    {
                        new IterativeTargetOverride(@"C:\temp\b.bgsm", string.Empty, isEnabled: false),
                        new IterativeTargetOverride(@"C:\temp\c.bgsm", "named", isEnabled: true),
                    }
                });

            return string.Join(";", contexts.Select(context =>
            {
                string expanded = context.WillApply
                    ? Path.GetFileName(MaterialVariationTokenExpander.Expand(@"textures\future_{index}_{indexTok}.dds", context.IndexValue, context))
                    : string.Empty;
                return $"{Path.GetFileName(context.TargetPath)}|{context.WillApply}|{expanded}";
            }));
        }

        private static string ResolveRetainedOriginalBackupCase()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope("MaterialEditorValidation");
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "retain-validation.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\original.dds";
            });

            var config = new Config
            {
                CreateBackupsByDefault = true,
                RetainOriginalBackup = true,
                MaxBackupsPerFile = 0,
                MaxBackupFolderMegabytes = 0
            };

            MaterialFilePersistence.SaveMaterial(path, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\first.dds"
            }, asJson: false, backupExisting: true, config);

            MaterialFilePersistence.SaveMaterial(path, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\second.dds"
            }, asJson: false, backupExisting: true, config);

            MaterialBackupEntry[] backups = MaterialBackupService.GetAvailableBackups(path)
                .OrderBy(entry => entry.CreatedUtc)
                .ThenBy(entry => entry.DisplayLabel, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            string oldestTexture = TestFileSupport.LoadBgsm(backups[0].BackupPath).DiffuseTexture;
            string newestTexture = TestFileSupport.LoadBgsm(backups[^1].BackupPath).DiffuseTexture;
            return $"{backups.Length}|{backups.Count(entry => entry.IsRetainedOriginal)}|{EscapeForLog(oldestTexture)}|{EscapeForLog(newestTexture)}";
        }

        private static string ResolveBackupRestoreRoundTripCase()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope("MaterialEditorValidation");
            string path = TestFileSupport.CreateBgsm(outputDirectory.Path, "restore-validation.bgsm", material =>
            {
                material.Version = 2;
                material.DiffuseTexture = "textures\\original.dds";
            });

            var config = new Config
            {
                CreateBackupsByDefault = true,
                MaxBackupsPerFile = 0,
                MaxBackupFolderMegabytes = 0
            };

            MaterialFilePersistence.SaveMaterial(path, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\first.dds"
            }, asJson: false, backupExisting: true, config);

            MaterialFilePersistence.SaveMaterial(path, new BGSM
            {
                Version = 2,
                DiffuseTexture = "textures\\second.dds"
            }, asJson: false, backupExisting: true, config);

            FieldCopyResult oldest = MaterialBackupService.RestoreOldest(path, config, backupCurrentBeforeRestore: false);
            string afterOldest = TestFileSupport.LoadBgsm(path).DiffuseTexture;
            FieldCopyResult newest = MaterialBackupService.RestoreNewest(path, config, backupCurrentBeforeRestore: false);
            string afterNewest = TestFileSupport.LoadBgsm(path).DiffuseTexture;

            return $"{oldest.Status}|{EscapeForLog(afterOldest)};{newest.Status}|{EscapeForLog(afterNewest)}";
        }

        private static string ResolveFindReplaceRequestCloneCase()
        {
            var request = new BulkFindReplaceRequest
            {
                Scope = BulkFindScope.ByField,
                ValueType = BulkFindValueType.Boolean,
                FindBooleanValue = true,
                ReplaceBooleanValue = false,
                FieldLabels = new List<string> { ControlNames.Diffuse }
            };

            BulkFindReplaceRequest clone = request.Clone();
            clone.FindBooleanValue = false;
            clone.ReplaceBooleanValue = true;
            clone.FieldLabels.Add(ControlNames.Normal);

            return $"{request.GetFindValueText()}|{request.GetReplaceValueText()}|{string.Join(",", request.FieldLabels)};{clone.GetFindValueText()}|{clone.GetReplaceValueText()}|{string.Join(",", clone.FieldLabels)}";
        }

        private static string ResolveOutputDirectory(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                return Path.Combine(Environment.CurrentDirectory, "artifacts", "validation-report");

            return Path.GetFullPath(outputDirectory);
        }

        private static MaterialFieldDescriptor GetDescriptor(string label)
        {
            return MaterialFieldRegistry.GetDescriptors(new BGSM()).Single(descriptor => descriptor.Label == label);
        }

        private static void WriteOutputs(string targetDirectory, ValidationReportSummary summary)
        {
            string reportPath = Path.Combine(targetDirectory, "validation-report.json");
            string summaryPath = Path.Combine(targetDirectory, "validation-summary.txt");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            File.WriteAllText(reportPath, JsonSerializer.Serialize(summary, options), Encoding.UTF8);

            var builder = new StringBuilder();
            builder.AppendLine($"Validation summary generated at {summary.GeneratedUtc:O}");
            builder.AppendLine($"Cases passed: {summary.PassedCount}/{summary.TotalCount}");
            builder.AppendLine($"Cases failed: {summary.FailedCount}");
            builder.AppendLine();
            foreach (ValidationCaseResult result in summary.Cases)
            {
                builder.AppendLine($"{(result.Passed ? "PASS" : "FAIL")} [{result.Category}] {result.Name}");
                builder.AppendLine($"  Expected: {result.Expected}");
                builder.AppendLine($"  Actual:   {result.Actual}");
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    builder.AppendLine($"  Error:    {result.ErrorMessage}");
                builder.AppendLine();
            }

            File.WriteAllText(summaryPath, builder.ToString(), Encoding.UTF8);
        }

        private static string EscapeForLog(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\");
        }

        private static string ExtractEscapedSubstring(string text, string expectedSubstring)
        {
            return text != null && text.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase)
                ? expectedSubstring
                : string.Empty;
        }
    }

    internal sealed class ValidationReportSummary
    {
        public ValidationReportSummary(DateTime generatedUtc, int totalCount, int passedCount, int failedCount, IReadOnlyList<ValidationCaseResult> cases)
        {
            GeneratedUtc = generatedUtc;
            TotalCount = totalCount;
            PassedCount = passedCount;
            FailedCount = failedCount;
            Cases = cases ?? Array.Empty<ValidationCaseResult>();
        }

        public DateTime GeneratedUtc { get; }
        public int TotalCount { get; }
        public int PassedCount { get; }
        public int FailedCount { get; }
        public IReadOnlyList<ValidationCaseResult> Cases { get; }
    }

    internal sealed class ValidationCaseResult
    {
        public ValidationCaseResult(string name, string category, string expected, string actual, bool passed, string errorMessage)
        {
            Name = name ?? string.Empty;
            Category = category ?? string.Empty;
            Expected = expected ?? string.Empty;
            Actual = actual ?? string.Empty;
            Passed = passed;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string Name { get; }
        public string Category { get; }
        public string Expected { get; }
        public string Actual { get; }
        public bool Passed { get; }
        public string ErrorMessage { get; }
    }
}
