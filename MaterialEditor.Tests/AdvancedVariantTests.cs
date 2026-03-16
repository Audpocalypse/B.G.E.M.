using Material_Editor.AdvancedVariant;
using Material_Editor.Models;
using Material_Editor.Services;
using MaterialLib;
using System;
using System.IO;
using System.Linq;

namespace MaterialEditor.Tests
{
    internal static class AdvancedVariantTests
    {
        public static void RunAll()
        {
            Resolve_ExpandsFourLayersInStableOrder();
            Resolve_AppliesDefaultAndSpecificOverrides();
            Resolve_SingleLayerRulesPopulateLayerIndexTokens();
            Resolve_LaterRuleWinsWithinEqualSpecificity();
            Resolve_UnnamedIndexTokenFallsBackToRawIndex();
            Resolve_ExactRuleCanDisableRow();
            PreviewFieldValue_ExpandsAdvancedTokens();
            Generate_AdvancedPathWritesResolvedFieldValues();
            Generate_AdvancedPathSkipsDisabledRows();
            Generate_AdvancedPathRejectsDuplicateOutputPaths();
            Generate_AdvancedPathRejectsInvalidGeneratedName();
            Generate_LegacyPathStillSupportsIndexPlaceholders();
        }

        private static void Resolve_ExpandsFourLayersInStableOrder()
        {
            var options = new AdvancedVariantOptions
            {
                Layers = new[]
                {
                    new AdvancedVariantLayerDefinition("Type", new[] { 1, 2 }),
                    new AdvancedVariantLayerDefinition("Name", new[] { 3 }),
                    new AdvancedVariantLayerDefinition("Slot", new[] { 4, 5 }),
                    new AdvancedVariantLayerDefinition("Opt", new[] { 6 }),
                }
            };

            var contexts = AdvancedVariantEngine.Resolve(options);

            AssertEqual(4, contexts.Count, nameof(Resolve_ExpandsFourLayersInStableOrder));
            AssertSequenceEqual(new[] { "1346", "1356", "2346", "2356" }, contexts.Select(context => context.Index).ToArray(), nameof(Resolve_ExpandsFourLayersInStableOrder));
            AssertSequenceEqual(new[] { "01030406", "01030506", "02030406", "02030506" }, contexts.Select(context => context.IndexNN).ToArray(), nameof(Resolve_ExpandsFourLayersInStableOrder));
        }

        private static void Resolve_AppliesDefaultAndSpecificOverrides()
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

            var contexts = AdvancedVariantEngine.Resolve(options);

            AssertSequenceEqual(new[] { "base", "base", "exact", "layer1" }, contexts.Select(context => context.IndexTok).ToArray(), nameof(Resolve_AppliesDefaultAndSpecificOverrides));
        }

        private static void Resolve_LaterRuleWinsWithinEqualSpecificity()
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

            var context = AdvancedVariantEngine.Resolve(options).Single();

            AssertEqual("second", context.IndexTok, nameof(Resolve_LaterRuleWinsWithinEqualSpecificity));
        }

        private static void Resolve_SingleLayerRulesPopulateLayerIndexTokens()
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

            var contexts = AdvancedVariantEngine.Resolve(options).ToArray();

            AssertEqual("primary", contexts[0].Layer1IndexToken, nameof(Resolve_SingleLayerRulesPopulateLayerIndexTokens));
            AssertEqual("red", contexts[0].Layer2IndexToken, nameof(Resolve_SingleLayerRulesPopulateLayerIndexTokens));
            AssertEqual("pads", contexts[3].Layer1IndexToken, nameof(Resolve_SingleLayerRulesPopulateLayerIndexTokens));
            AssertEqual("green", contexts[3].Layer2IndexToken, nameof(Resolve_SingleLayerRulesPopulateLayerIndexTokens));
        }

        private static void Resolve_UnnamedIndexTokenFallsBackToRawIndex()
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

            var contexts = AdvancedVariantEngine.Resolve(options).ToArray();

            AssertEqual("81", contexts[0].IndexTok, nameof(Resolve_UnnamedIndexTokenFallsBackToRawIndex));
            AssertEqual("vaultpants", contexts[1].IndexTok, nameof(Resolve_UnnamedIndexTokenFallsBackToRawIndex));
            AssertTrue(contexts[0].IndexTokFallback, nameof(Resolve_UnnamedIndexTokenFallsBackToRawIndex));
            AssertTrue(!contexts[1].IndexTokFallback, nameof(Resolve_UnnamedIndexTokenFallsBackToRawIndex));
        }

        private static void Resolve_ExactRuleCanDisableRow()
        {
            var options = new AdvancedVariantOptions
            {
                Layers = new[]
                {
                    new AdvancedVariantLayerDefinition("Layer1", new[] { 1, 2 }),
                },
                Rules = new[]
                {
                    new AdvancedVariantRule { Layer1Index = 2, Enabled = false, SourceOrder = 0 },
                }
            };

            var contexts = AdvancedVariantEngine.Resolve(options).ToArray();

            AssertTrue(contexts[0].Enabled, nameof(Resolve_ExactRuleCanDisableRow));
            AssertTrue(!contexts[1].Enabled, nameof(Resolve_ExactRuleCanDisableRow));
        }

        private static void PreviewFieldValue_ExpandsAdvancedTokens()
        {
            var context = AdvancedVariantEngine.Resolve(new AdvancedVariantOptions
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

            string value = MaterialVariationGenerator.PreviewFieldValue("name_{index}_{indexNN}_{indexTok}_{indexLayer1}_{indexNNLayer2}_{indexTokLayer1}_{indexTokLayer2}", context);

            AssertEqual("name_112_0112_red_1_12_primary_red", value, nameof(PreviewFieldValue_ExpandsAdvancedTokens));
        }

        private static void Generate_AdvancedPathWritesResolvedFieldValues()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string outputPath = outputDirectory.Path;
            var template = new BGSM
            {
                DiffuseTexture = "textures\\base.dds",
                NormalTexture = "textures\\normal.dds",
                GrayscaleToPaletteScale = 1.0f
            };

            var diffuseDescriptor = GetDescriptor(ControlNames.Diffuse);
            var options = new MaterialVariationOptions
            {
                OutputPattern = Path.Combine(outputPath, "variant_{indexNN}_{indexTok}.bgsm"),
                Fields = new[]
                {
                    new MaterialVariationFieldAssignment(diffuseDescriptor, "textures\\variant_{indexLayer1}_{indexTok}.dds")
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

            var results = MaterialVariationGenerator.Generate(template, options, serializeAsJson: true);

            AssertTrue(results.All(result => result.Status == FieldCopyStatus.Success), nameof(Generate_AdvancedPathWritesResolvedFieldValues));
            AssertEqual(2, results.Count, nameof(Generate_AdvancedPathWritesResolvedFieldValues));

            string firstText = File.ReadAllText(Path.Combine(outputPath, "variant_01_base.bgsm"));
            string secondText = File.ReadAllText(Path.Combine(outputPath, "variant_02_special.bgsm"));

            AssertContains(firstText, "textures\\\\variant_1_base.dds", nameof(Generate_AdvancedPathWritesResolvedFieldValues));
            AssertContains(secondText, "textures\\\\variant_2_special.dds", nameof(Generate_AdvancedPathWritesResolvedFieldValues));
        }

        private static void Generate_AdvancedPathSkipsDisabledRows()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
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

            var results = MaterialVariationGenerator.Generate(new BGSM(), options, serializeAsJson: true);
            string[] files = Directory.GetFiles(outputPath, "*.bgsm").Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal).ToArray();

            AssertEqual(2, results.Count, nameof(Generate_AdvancedPathSkipsDisabledRows));
            AssertTrue(results.All(result => result.Status == FieldCopyStatus.Success), nameof(Generate_AdvancedPathSkipsDisabledRows));
            AssertSequenceEqual(new[] { "variant_01.bgsm", "variant_03.bgsm" }, files, nameof(Generate_AdvancedPathSkipsDisabledRows));
        }

        private static void Generate_AdvancedPathRejectsDuplicateOutputPaths()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
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

            var results = MaterialVariationGenerator.Generate(new BGSM(), options, serializeAsJson: true);

            AssertEqual(2, results.Count, nameof(Generate_AdvancedPathRejectsDuplicateOutputPaths));
            AssertTrue(results.All(result => result.Status == FieldCopyStatus.Failed && result.Message.Contains("Duplicate target path", StringComparison.Ordinal)), nameof(Generate_AdvancedPathRejectsDuplicateOutputPaths));
        }

        private static void Generate_AdvancedPathRejectsInvalidGeneratedName()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string outputPath = outputDirectory.Path;
            var options = new MaterialVariationOptions
            {
                OutputPattern = Path.Combine(outputPath, "bad|{index}.bgsm"),
                AdvancedVariant = new AdvancedVariantOptions
                {
                    Layers = new[]
                    {
                        new AdvancedVariantLayerDefinition("Layer1", new[] { 1 }),
                    }
                }
            };

            var result = MaterialVariationGenerator.Generate(new BGSM(), options, serializeAsJson: true).Single();

            AssertEqual(FieldCopyStatus.Failed, result.Status, nameof(Generate_AdvancedPathRejectsInvalidGeneratedName));
            AssertContains(result.Message, "invalid characters", nameof(Generate_AdvancedPathRejectsInvalidGeneratedName));
        }

        private static void Generate_LegacyPathStillSupportsIndexPlaceholders()
        {
            using var outputDirectory = TestFileSupport.CreateTempDirectoryScope();
            string outputPath = outputDirectory.Path;
            var template = new BGSM
            {
                DiffuseTexture = "textures\\base.dds",
                NormalTexture = "textures\\normal.dds"
            };

            var diffuseDescriptor = GetDescriptor(ControlNames.Diffuse);
            var options = new MaterialVariationOptions
            {
                StartIndex = 1,
                Count = 2,
                Step = 1,
                OutputPattern = Path.Combine(outputPath, "legacy_{index:00}.bgsm"),
                Fields = new[]
                {
                    new MaterialVariationFieldAssignment(diffuseDescriptor, "textures\\legacy_{index:00}.dds")
                }
            };

            var results = MaterialVariationGenerator.Generate(template, options, serializeAsJson: true);

            AssertTrue(results.All(result => result.Status == FieldCopyStatus.Success), nameof(Generate_LegacyPathStillSupportsIndexPlaceholders));
            AssertContains(File.ReadAllText(Path.Combine(outputPath, "legacy_01.bgsm")), "textures\\\\legacy_01.dds", nameof(Generate_LegacyPathStillSupportsIndexPlaceholders));
            AssertContains(File.ReadAllText(Path.Combine(outputPath, "legacy_02.bgsm")), "textures\\\\legacy_02.dds", nameof(Generate_LegacyPathStillSupportsIndexPlaceholders));
        }

        private static MaterialFieldDescriptor GetDescriptor(string label)
        {
            return MaterialFieldRegistry.GetDescriptors(new BGSM()).Single(descriptor => descriptor.Label == label);
        }

        private static void AssertContains(string value, string expectedSubstring, string testName)
        {
            if (value == null || value.IndexOf(expectedSubstring, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException($"{testName} failed: expected '{expectedSubstring}' to be present.");
        }

        private static void AssertEqual<T>(T expected, T actual, string testName)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException($"{testName} failed: expected '{expected}', got '{actual}'.");
        }

        private static void AssertSequenceEqual(string[] expected, string[] actual, string testName)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidOperationException($"{testName} failed: sequences did not match.");
        }

        private static void AssertTrue(bool value, string testName)
        {
            if (!value)
                throw new InvalidOperationException($"{testName} failed.");
        }
    }
}
