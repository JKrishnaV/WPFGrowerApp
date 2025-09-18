namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Defines methods for managing UI-related user settings.
    /// </summary>
    public interface IUISettingsService
    {
        /// <summary>
        /// Gets the currently saved font size scaling factor.
        /// Defaults to 1.0 if not set.
        /// </summary>
        /// <returns>The font size scaling factor (e.g., 1.0, 1.5, 2.0).</returns>
        double GetFontScalingFactor();

        /// <summary>
        /// Saves the specified font size scaling factor.
        /// </summary>
        /// <param name="scalingFactor">The scaling factor to save.</param>
        void SaveFontScalingFactor(double scalingFactor);
    }
}
