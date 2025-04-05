using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents basic Grower information for lists/ComboBoxes.
    /// </summary>
    public class GrowerInfo
    {
        // Corresponds to NUMBER DECIMAL(4, 0)
        public decimal Number { get; set; }

        // Corresponds to NAME NVARCHAR(30)
        public string Name { get; set; }

        // Optional: Combine Name and Number for display
        public string DisplayName => $"{Name} ({Number})";
    }
}
