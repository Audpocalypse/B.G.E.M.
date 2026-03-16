using Material_Editor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Material_Editor.Services
{
    internal static class BulkEditorPreferencesService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false
        };

        public static string SerializePresets(IReadOnlyList<BulkFieldPreset> presets)
        {
            if (presets == null || presets.Count == 0)
                return string.Empty;

            try
            {
                return JsonSerializer.Serialize(presets, JsonOptions);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static List<BulkFieldPreset> DeserializePresets(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<BulkFieldPreset>();

            try
            {
                return JsonSerializer.Deserialize<List<BulkFieldPreset>>(json, JsonOptions)
                    ?.Where(preset => !string.IsNullOrWhiteSpace(preset?.Name))
                    .Select(NormalizePreset)
                    .ToList()
                    ?? new List<BulkFieldPreset>();
            }
            catch
            {
                return new List<BulkFieldPreset>();
            }
        }

        public static IReadOnlyList<BulkFieldPreset> GetPresets(Config config, MaterialType materialType)
        {
            return (config?.BulkFieldPresets ?? new List<BulkFieldPreset>())
                .Where(preset => preset.MaterialType == materialType)
                .OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static BulkFieldPreset SavePreset(Config config, MaterialType materialType, string name, IEnumerable<MaterialFieldDescriptor> descriptors)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            string normalizedName = NormalizeName(name);
            if (string.IsNullOrWhiteSpace(normalizedName))
                throw new ArgumentException("Preset name is required.", nameof(name));

            List<string> fieldLabels = (descriptors ?? Array.Empty<MaterialFieldDescriptor>())
                .Select(descriptor => descriptor?.Label)
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(label => label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (fieldLabels.Count == 0)
                throw new InvalidOperationException("Choose at least one field before saving a preset.");

            config.BulkFieldPresets ??= new List<BulkFieldPreset>();

            BulkFieldPreset existingPreset = config.BulkFieldPresets.FirstOrDefault(preset =>
                preset.MaterialType == materialType
                && string.Equals(preset.Name, normalizedName, StringComparison.OrdinalIgnoreCase));

            if (existingPreset == null)
            {
                existingPreset = new BulkFieldPreset
                {
                    Name = normalizedName,
                    MaterialType = materialType
                };
                config.BulkFieldPresets.Add(existingPreset);
            }

            existingPreset.Name = normalizedName;
            existingPreset.MaterialType = materialType;
            existingPreset.FieldLabels = fieldLabels;
            return existingPreset;
        }

        public static bool DeletePreset(Config config, MaterialType materialType, string name)
        {
            if (config?.BulkFieldPresets == null || string.IsNullOrWhiteSpace(name))
                return false;

            BulkFieldPreset existingPreset = config.BulkFieldPresets.FirstOrDefault(preset =>
                preset.MaterialType == materialType
                && string.Equals(preset.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (existingPreset == null)
                return false;

            return config.BulkFieldPresets.Remove(existingPreset);
        }

        public static IReadOnlyList<MaterialFieldDescriptor> ResolvePresetFields(
            IReadOnlyList<MaterialFieldDescriptor> availableDescriptors,
            BulkFieldPreset preset)
        {
            if (availableDescriptors == null || preset?.FieldLabels == null)
                return Array.Empty<MaterialFieldDescriptor>();

            var descriptorLookup = availableDescriptors.ToDictionary(descriptor => descriptor.Label, StringComparer.OrdinalIgnoreCase);
            var resolved = new List<MaterialFieldDescriptor>();
            foreach (string label in preset.FieldLabels)
            {
                if (descriptorLookup.TryGetValue(label, out MaterialFieldDescriptor descriptor))
                    resolved.Add(descriptor);
            }

            return resolved;
        }

        private static BulkFieldPreset NormalizePreset(BulkFieldPreset preset)
        {
            return new BulkFieldPreset
            {
                Name = NormalizeName(preset.Name),
                MaterialType = preset.MaterialType,
                FieldLabels = (preset.FieldLabels ?? new List<string>())
                    .Where(label => !string.IsNullOrWhiteSpace(label))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(label => label, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }

        private static string NormalizeName(string name)
        {
            return (name ?? string.Empty).Trim();
        }
    }
}
