using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.Models
{
    public class PriceDisplayItem
    {
        public Price Price { get; set; }
        public string ProductName { get; set; }
        public string ProcessName { get; set; }
        
        /// <summary>
        /// Returns true if any payment type has been used (locked)
        /// </summary>
        public bool IsAnyLocked => Price != null && 
            (Price.Adv1Used || Price.Adv2Used || Price.Adv3Used || Price.FinUsed);
        
        /// <summary>
        /// Returns a tooltip showing which payment types are locked
        /// </summary>
        public string LockStatusTooltip
        {
            get
            {
                if (Price == null || !IsAnyLocked)
                    return "Not used - can be edited";
                
                var locked = new System.Collections.Generic.List<string>();
                if (Price.Adv1Used) locked.Add("Advance 1");
                if (Price.Adv2Used) locked.Add("Advance 2");
                if (Price.Adv3Used) locked.Add("Advance 3");
                if (Price.FinUsed) locked.Add("Final");
                
                return "🔒 Locked - Used for: " + string.Join(", ", locked);
            }
        }
    }
}
