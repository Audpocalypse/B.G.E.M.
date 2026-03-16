using MaterialLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Material_Editor.Services
{
    public enum FieldCopyStatus
    {
        Success,
        Skipped,
        Failed
    }

    public sealed class FieldCopyResult
    {
        public string TargetPath { get; }
        public FieldCopyStatus Status { get; }
        public string Message { get; }

        public FieldCopyResult(string targetPath, FieldCopyStatus status, string message)
        {
            TargetPath = targetPath;
            Status = status;
            Message = message;
        }
    }

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
            if (!File.Exists(filePath))
                return new FieldCopyResult(filePath, FieldCopyStatus.Failed, "Target file does not exist.");

            if (!MaterialFilePersistence.TryLoadMaterial(filePath, out var targetMaterial, out var isJson, out var loadError))
                return new FieldCopyResult(filePath, FieldCopyStatus.Failed, loadError ?? "Failed to load material.");

            if (targetMaterial.GetType() != sourceState.GetType())
                return new FieldCopyResult(filePath, FieldCopyStatus.Skipped, "Skipped incompatible material type.");

            var supportedDescriptors = descriptors.Where(d => d.IsSupported(targetMaterial)).ToList();
            if (supportedDescriptors.Count == 0)
                return new FieldCopyResult(filePath, FieldCopyStatus.Skipped, "No compatible fields for target version.");

            foreach (var descriptor in supportedDescriptors)
            {
                descriptor.SetValue(targetMaterial, descriptor.GetValue(sourceState));
            }

            try
            {
                if (backupBeforeWrite)
                {
                    File.Copy(filePath, $"{filePath}.bak", true);
                }

                MaterialFilePersistence.SaveMaterial(filePath, targetMaterial, isJson);
            }
            catch (Exception ex)
            {
                return new FieldCopyResult(filePath, FieldCopyStatus.Failed, ex.Message);
            }

            return new FieldCopyResult(filePath, FieldCopyStatus.Success, "Updated successfully.");
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
            if (!File.Exists(filePath))
                return new FieldCopyResult(filePath, FieldCopyStatus.Failed, "Target file does not exist.");

            if (!MaterialFilePersistence.TryLoadMaterial(filePath, out var targetMaterial, out var isJson, out var loadError))
                return new FieldCopyResult(filePath, FieldCopyStatus.Failed, loadError ?? "Failed to load material.");

            if (targetMaterial.GetType() != sourceState.GetType())
                return new FieldCopyResult(filePath, FieldCopyStatus.Skipped, "Skipped incompatible material type.");

            var supportedDescriptors = descriptors.Where(d => d.IsSupported(targetMaterial)).ToList();
            if (supportedDescriptors.Count == 0)
                return new FieldCopyResult(filePath, FieldCopyStatus.Skipped, "No compatible fields for target version.");

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

            try
            {
                if (backupBeforeWrite)
                    File.Copy(filePath, $"{filePath}.bak", true);

                MaterialFilePersistence.SaveMaterial(filePath, targetMaterial, isJson);
            }
            catch (Exception ex)
            {
                return new FieldCopyResult(filePath, FieldCopyStatus.Failed, ex.Message);
            }

            return new FieldCopyResult(filePath, FieldCopyStatus.Success, "Updated successfully.");
        }

        private static object ConvertOverrideValue(MaterialFieldDescriptor descriptor, BaseMaterialFile targetMaterial, string overrideValue)
        {
            if (descriptor.TryParseTextValue(overrideValue, out object parsedValue, out string errorMessage))
                return parsedValue;

            throw new FormatException(errorMessage ?? $"Failed to parse field '{descriptor.Label}'.");
        }
    }
}
