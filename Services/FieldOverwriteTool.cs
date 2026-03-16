using MaterialLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Material_Editor.Services
{
    public sealed class FieldOverwriteTool
    {
        public IReadOnlyList<FieldCopyResult> Run(BaseMaterialFile sourceState, IReadOnlyList<MaterialFieldDescriptor> descriptors, IReadOnlyList<string> targetFiles, bool backupBeforeWrite)
        {
            return Run(sourceState, new FieldOverwriteOptions
            {
                Descriptors = descriptors ?? Array.Empty<MaterialFieldDescriptor>(),
                TargetFiles = targetFiles ?? Array.Empty<string>(),
                BackupBeforeWrite = backupBeforeWrite
            });
        }

        public IReadOnlyList<FieldCopyResult> Run(BaseMaterialFile sourceState, FieldOverwriteOptions options)
        {
            var results = new List<FieldCopyResult>();
            if (sourceState == null || options == null)
                return new[] { new FieldCopyResult(string.Empty, FieldCopyStatus.Failed, "Source material or overwrite options are missing.") };

            if (options.IterativeOptions == null)
            {
                foreach (var target in options.TargetFiles ?? Array.Empty<string>())
                {
                    var result = ProcessTarget(sourceState, options.Descriptors, target, options.BackupBeforeWrite);
                    results.Add(result);
                }

                return results;
            }

            var iterativeContexts = IterativeFieldOverwritePlanner.BuildContexts(
                MaterialFileTypeHelper.GetMaterialType(sourceState),
                options.TargetFiles,
                options.IterativeOptions);

            var iterativeAssignments = (options.IterativeOptions.Assignments ?? Array.Empty<IterativeFieldAssignment>())
                .GroupBy(assignment => assignment.Descriptor?.Label ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
            var fieldValueOverrides = (options.IterativeOptions.FieldValueOverrides ?? Array.Empty<IterativeFieldValueOverride>())
                .GroupBy(overrideValue => (MaterialFilePersistence.NormalizePath(overrideValue.TargetPath), overrideValue.FieldLabel ?? string.Empty))
                .ToDictionary(group => group.Key, group => group.Last().Value ?? string.Empty);

            foreach (var context in iterativeContexts)
            {
                if (!context.IsCompatibleType)
                {
                    results.Add(new FieldCopyResult(context.TargetPath, FieldCopyStatus.Skipped, "Skipped incompatible material type."));
                    continue;
                }

                if (!context.WillApply)
                {
                    results.Add(new FieldCopyResult(context.TargetPath, FieldCopyStatus.Skipped, "Skipped by iteration settings."));
                    continue;
                }

                var result = ProcessTargetIterative(sourceState, options.Descriptors, iterativeAssignments, fieldValueOverrides, context, options.BackupBeforeWrite);
                results.Add(result);
            }

            return results;
        }

        private FieldCopyResult ProcessTarget(BaseMaterialFile sourceState, IReadOnlyList<MaterialFieldDescriptor> descriptors, string filePath, bool backupBeforeWrite)
        {
            if (!TryPrepareTargetMaterial(sourceState, descriptors, filePath, out BaseMaterialFile targetMaterial, out bool isJson, out IReadOnlyList<MaterialFieldDescriptor> supportedDescriptors, out FieldCopyResult failureResult))
                return failureResult;

            foreach (var descriptor in supportedDescriptors)
                descriptor.SetValue(targetMaterial, descriptor.GetValue(sourceState));

            return SaveTargetMaterial(filePath, targetMaterial, isJson, backupBeforeWrite);
        }

        private FieldCopyResult ProcessTargetIterative(
            BaseMaterialFile sourceState,
            IReadOnlyList<MaterialFieldDescriptor> descriptors,
            IReadOnlyDictionary<string, IterativeFieldAssignment> iterativeAssignments,
            IReadOnlyDictionary<(string TargetPath, string FieldLabel), string> fieldValueOverrides,
            IterativeTargetContext context,
            bool backupBeforeWrite)
        {
            string filePath = context.TargetPath;
            if (!TryPrepareTargetMaterial(sourceState, descriptors, filePath, out BaseMaterialFile targetMaterial, out bool isJson, out IReadOnlyList<MaterialFieldDescriptor> supportedDescriptors, out FieldCopyResult failureResult))
                return failureResult;

            foreach (var descriptor in supportedDescriptors)
            {
                try
                {
                    if (!iterativeAssignments.TryGetValue(descriptor.Label, out var assignment))
                    {
                        descriptor.SetValue(targetMaterial, descriptor.GetValue(sourceState));
                        continue;
                    }

                    if (fieldValueOverrides.TryGetValue((MaterialFilePersistence.NormalizePath(filePath), descriptor.Label ?? string.Empty), out string overrideValue))
                    {
                        descriptor.SetValue(targetMaterial, ConvertOverrideValue(descriptor, targetMaterial, overrideValue));
                        continue;
                    }

                    if (assignment.IsNumericSequence)
                    {
                        float value = assignment.NumericStartValue.Value + assignment.NumericStepValue.Value * (context.AppliedOrdinal ?? 0);
                        descriptor.SetValue(targetMaterial, value);
                        continue;
                    }

                    string valueText = MaterialVariationTokenExpander.Expand(assignment.Pattern, context.AdvancedContext == null ? context.IndexValue : null, context);
                    descriptor.SetValue(targetMaterial, valueText);
                }
                catch (Exception ex)
                {
                    return new FieldCopyResult(filePath, FieldCopyStatus.Failed, $"Failed to apply field '{descriptor.Label}': {ex.Message}");
                }
            }

            return SaveTargetMaterial(filePath, targetMaterial, isJson, backupBeforeWrite);
        }

        private static object ConvertOverrideValue(MaterialFieldDescriptor descriptor, BaseMaterialFile targetMaterial, string overrideValue)
        {
            if (descriptor.TryParseTextValue(overrideValue, out object parsedValue, out string errorMessage))
                return parsedValue;

            throw new FormatException(errorMessage ?? $"Failed to parse field '{descriptor.Label}'.");
        }

        private static bool TryPrepareTargetMaterial(
            BaseMaterialFile sourceState,
            IReadOnlyList<MaterialFieldDescriptor> descriptors,
            string filePath,
            out BaseMaterialFile targetMaterial,
            out bool isJson,
            out IReadOnlyList<MaterialFieldDescriptor> supportedDescriptors,
            out FieldCopyResult failureResult)
        {
            targetMaterial = null;
            isJson = false;
            supportedDescriptors = Array.Empty<MaterialFieldDescriptor>();
            failureResult = null;

            if (!File.Exists(filePath))
            {
                failureResult = new FieldCopyResult(filePath, FieldCopyStatus.Failed, "Target file does not exist.");
                return false;
            }

            if (!MaterialFilePersistence.TryLoadMaterial(filePath, out BaseMaterialFile loadedMaterial, out isJson, out string loadError))
            {
                failureResult = new FieldCopyResult(filePath, FieldCopyStatus.Failed, loadError ?? "Failed to load material.");
                return false;
            }

            targetMaterial = loadedMaterial;
            if (targetMaterial.GetType() != sourceState.GetType())
            {
                failureResult = new FieldCopyResult(filePath, FieldCopyStatus.Skipped, "Skipped incompatible material type.");
                return false;
            }

            BaseMaterialFile preparedMaterial = targetMaterial;
            supportedDescriptors = (descriptors ?? Array.Empty<MaterialFieldDescriptor>())
                .Where(descriptor => descriptor.IsSupported(preparedMaterial))
                .ToArray();
            if (supportedDescriptors.Count == 0)
            {
                failureResult = new FieldCopyResult(filePath, FieldCopyStatus.Skipped, "No compatible fields for target version.");
                return false;
            }

            return true;
        }

        private static FieldCopyResult SaveTargetMaterial(string filePath, BaseMaterialFile targetMaterial, bool isJson, bool backupBeforeWrite)
        {
            return MaterialFilePersistence.SaveMaterialResult(
                filePath,
                targetMaterial,
                isJson,
                "Updated successfully.",
                backupBeforeWrite);
        }
    }
}
