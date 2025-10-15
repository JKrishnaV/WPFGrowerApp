using System.Collections.Generic;

namespace WPFGrowerApp.Helpers
{
    /// <summary>
    /// Helper class for province-related operations.
    /// </summary>
    public static class ProvinceHelper
    {
        /// <summary>
        /// Gets a list of Canadian provinces and territories.
        /// </summary>
        /// <returns>List of province codes</returns>
        public static List<string> GetCanadianProvinces()
        {
            return new List<string> 
            { 
                "BC", "AB", "SK", "MB", "ON", "QC", 
                "NB", "NS", "PE", "NL", "YT", "NT", "NU" 
            };
        }

        /// <summary>
        /// Gets the full name of a province code.
        /// </summary>
        /// <param name="provinceCode">The two-letter province code</param>
        /// <returns>The full province name</returns>
        public static string GetProvinceName(string provinceCode)
        {
            return provinceCode?.ToUpper() switch
            {
                "BC" => "British Columbia",
                "AB" => "Alberta",
                "SK" => "Saskatchewan",
                "MB" => "Manitoba",
                "ON" => "Ontario",
                "QC" => "Quebec",
                "NB" => "New Brunswick",
                "NS" => "Nova Scotia",
                "PE" => "Prince Edward Island",
                "NL" => "Newfoundland and Labrador",
                "YT" => "Yukon Territory",
                "NT" => "Northwest Territories",
                "NU" => "Nunavut",
                _ => provinceCode ?? string.Empty
            };
        }

        /// <summary>
        /// Validates if a province code is valid.
        /// </summary>
        /// <param name="provinceCode">The province code to validate</param>
        /// <returns>True if valid</returns>
        public static bool IsValidProvince(string provinceCode)
        {
            if (string.IsNullOrWhiteSpace(provinceCode))
                return false;

            return GetCanadianProvinces().Contains(provinceCode.ToUpper());
        }
    }
}
