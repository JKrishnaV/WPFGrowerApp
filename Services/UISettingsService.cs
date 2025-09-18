using WPFGrowerApp.Properties; // Required for accessing Settings

namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Implements methods for managing UI-related user settings using application properties.
    /// </summary>
    public class UISettingsService : IUISettingsService
    {
        private const string FontScalingFactorSettingName = "FontScalingFactor";
        private const double DefaultFontScalingFactor = 1.0;

        /// <summary>
        /// Gets the currently saved font size scaling factor from application settings.
        /// Defaults to 1.0 if not set or invalid.
        /// </summary>
        /// <returns>The font size scaling factor (e.g., 1.0, 1.5, 2.0).</returns>
        public double GetFontScalingFactor()
        {
            try
            {
                // Check if the setting exists and is of the correct type
                var setting = Settings.Default[FontScalingFactorSettingName];
                if (setting is double factor)
                {
                    // Basic validation to ensure it's a reasonable positive number
                    return factor > 0 ? factor : DefaultFontScalingFactor;
                }
            }
            catch (System.Configuration.SettingsPropertyNotFoundException)
            {
                // Setting doesn't exist yet, return default
                return DefaultFontScalingFactor;
            }
            catch
            {
                // Catch other potential errors (e.g., type mismatch) and return default
                return DefaultFontScalingFactor;
            }

            // Fallback if setting exists but isn't a double
            return DefaultFontScalingFactor;
        }

        /// <summary>
        /// Saves the specified font size scaling factor to application settings.
        /// </summary>
        /// <param name="scalingFactor">The scaling factor to save.</param>
        public void SaveFontScalingFactor(double scalingFactor)
        {
            // Basic validation before saving
            if (scalingFactor <= 0)
            {
                scalingFactor = DefaultFontScalingFactor;
            }

            Settings.Default[FontScalingFactorSettingName] = scalingFactor;
            Settings.Default.Save(); // Persist the changes
        }
    }
}
