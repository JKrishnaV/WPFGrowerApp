using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WPFGrowerApp.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Service for persisting filter presets to JSON files
    /// </summary>
    public class FilterPresetService
    {
        private readonly string _presetsFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public FilterPresetService()
        {
            // Store presets in the application's AppData folder
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "BerryFarms", "WPFGrowerApp");
            
            // Ensure directory exists
            Directory.CreateDirectory(appFolder);
            
            _presetsFilePath = Path.Combine(appFolder, "FilterPresets.json");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Load filter presets from JSON file
        /// </summary>
        /// <returns>List of loaded presets</returns>
        public async Task<List<FilterPreset>> LoadPresetsAsync()
        {
            try
            {
                if (!File.Exists(_presetsFilePath))
                {
                    return new List<FilterPreset>();
                }

                var jsonContent = await File.ReadAllTextAsync(_presetsFilePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return new List<FilterPreset>();
                }

                var presets = JsonSerializer.Deserialize<List<FilterPreset>>(jsonContent, _jsonOptions);
                return presets ?? new List<FilterPreset>();
            }
            catch (Exception ex)
            {
                // Log error and return empty list
                Logger.Error($"Error loading filter presets: {ex.Message}", ex);
                return new List<FilterPreset>();
            }
        }

        /// <summary>
        /// Save filter presets to JSON file
        /// </summary>
        /// <param name="presets">List of presets to save</param>
        public async Task SavePresetsAsync(List<FilterPreset> presets)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(presets, _jsonOptions);
                await File.WriteAllTextAsync(_presetsFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                // Log error
                Logger.Error($"Error saving filter presets: {ex.Message}", ex);
                throw new InvalidOperationException($"Failed to save filter presets: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Add a new preset and save to file
        /// </summary>
        /// <param name="preset">Preset to add</param>
        /// <param name="existingPresets">Current list of presets</param>
        public async Task AddPresetAsync(FilterPreset preset, List<FilterPreset> existingPresets)
        {
            existingPresets.Add(preset);
            await SavePresetsAsync(existingPresets);
        }

        /// <summary>
        /// Remove a preset and save to file
        /// </summary>
        /// <param name="preset">Preset to remove</param>
        /// <param name="existingPresets">Current list of presets</param>
        public async Task RemovePresetAsync(FilterPreset preset, List<FilterPreset> existingPresets)
        {
            existingPresets.Remove(preset);
            await SavePresetsAsync(existingPresets);
        }

        /// <summary>
        /// Update an existing preset and save to file
        /// </summary>
        /// <param name="preset">Preset to update</param>
        /// <param name="existingPresets">Current list of presets</param>
        public async Task UpdatePresetAsync(FilterPreset preset, List<FilterPreset> existingPresets)
        {
            var index = existingPresets.FindIndex(p => p.Name == preset.Name);
            if (index >= 0)
            {
                existingPresets[index] = preset;
                await SavePresetsAsync(existingPresets);
            }
        }
    }
}

