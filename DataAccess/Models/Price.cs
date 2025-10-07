using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFGrowerApp.DataAccess.Models
{
    public class Price : AuditableEntity
    {
    public int PriceID { get; set; }
    public int ProductId { get; set; } // Surrogate key for Product
    public int ProcessId { get; set; } // Surrogate key for Process
    public string? Product { get; set; } // Business code (legacy)
    public string? Process { get; set; } // Business code (legacy)
        public DateTime From { get; set; }
        public bool TimePrem { get; set; }
    public string? Time { get; set; }
        public decimal CPremium { get; set; }
        public decimal UPremium { get; set; }
        
        [Column("ADV1_USED")]
        public bool Adv1Used { get; set; }
        
        [Column("ADV2_USED")]
        public bool Adv2Used { get; set; }
        
        [Column("ADV3_USED")]
        public bool Adv3Used { get; set; }
        
        [Column("FIN_USED")]
        public bool FinUsed { get; set; }

        // Canadian Prices
        public decimal CL1G1A1 { get; set; }
        public decimal CL1G1A2 { get; set; }
        public decimal CL1G1A3 { get; set; }
        public decimal CL1G1FN { get; set; }
        public decimal CL1G2A1 { get; set; }
        public decimal CL1G2A2 { get; set; }
        public decimal CL1G2A3 { get; set; }
        public decimal CL1G2FN { get; set; }
        public decimal CL1G3A1 { get; set; }
        public decimal CL1G3A2 { get; set; }
        public decimal CL1G3A3 { get; set; }
        public decimal CL1G3FN { get; set; }

        public decimal CL2G1A1 { get; set; }
        public decimal CL2G1A2 { get; set; }
        public decimal CL2G1A3 { get; set; }
        public decimal CL2G1FN { get; set; }
        public decimal CL2G2A1 { get; set; }
        public decimal CL2G2A2 { get; set; }
        public decimal CL2G2A3 { get; set; }
        public decimal CL2G2FN { get; set; }
        public decimal CL2G3A1 { get; set; }
        public decimal CL2G3A2 { get; set; }
        public decimal CL2G3A3 { get; set; }
        public decimal CL2G3FN { get; set; }

        public decimal CL3G1A1 { get; set; }
        public decimal CL3G1A2 { get; set; }
        public decimal CL3G1A3 { get; set; }
        public decimal CL3G1FN { get; set; }
        public decimal CL3G2A1 { get; set; }
        public decimal CL3G2A2 { get; set; }
        public decimal CL3G2A3 { get; set; }
        public decimal CL3G2FN { get; set; }
        public decimal CL3G3A1 { get; set; }
        public decimal CL3G3A2 { get; set; }
        public decimal CL3G3A3 { get; set; }
        public decimal CL3G3FN { get; set; }

        // US Prices
        public decimal UL1G1A1 { get; set; }
        public decimal UL1G1A2 { get; set; }
        public decimal UL1G1A3 { get; set; }
        public decimal UL1G1FN { get; set; }
        public decimal UL1G2A1 { get; set; }
        public decimal UL1G2A2 { get; set; }
        public decimal UL1G2A3 { get; set; }
        public decimal UL1G2FN { get; set; }
        public decimal UL1G3A1 { get; set; }
        public decimal UL1G3A2 { get; set; }
        public decimal UL1G3A3 { get; set; }
        public decimal UL1G3FN { get; set; }

        public decimal UL2G1A1 { get; set; }
        public decimal UL2G1A2 { get; set; }
        public decimal UL2G1A3 { get; set; }
        public decimal UL2G1FN { get; set; }
        public decimal UL2G2A1 { get; set; }
        public decimal UL2G2A2 { get; set; }
        public decimal UL2G2A3 { get; set; }
        public decimal UL2G2FN { get; set; }
        public decimal UL2G3A1 { get; set; }
        public decimal UL2G3A2 { get; set; }
        public decimal UL2G3A3 { get; set; }
        public decimal UL2G3FN { get; set; }

        public decimal UL3G1A1 { get; set; }
        public decimal UL3G1A2 { get; set; }
        public decimal UL3G1A3 { get; set; }
        public decimal UL3G1FN { get; set; }
        public decimal UL3G2A1 { get; set; }
        public decimal UL3G2A2 { get; set; }
        public decimal UL3G2A3 { get; set; }
        public decimal UL3G2FN { get; set; }
        public decimal UL3G3A1 { get; set; }
        public decimal UL3G3A2 { get; set; }
        public decimal UL3G3A3 { get; set; }
        public decimal UL3G3FN { get; set; }
    }
}
