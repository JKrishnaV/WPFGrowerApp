using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFGrowerApp.Models.Entities
{
    [Table("Account")]
    public class AccountEntity
    {
        [Key]
        [Column("NUMBER")]
        public decimal Number { get; set; }

        [Column("DATE")]
        public DateTime? Date { get; set; }

        [Column("TYPE")]
        [StringLength(3)]
        public string Type { get; set; }

        [Column("CLASS")]
        [StringLength(8)]
        public string Class { get; set; }

        [Column("PRODUCT")]
        [StringLength(2)]
        public string Product { get; set; }

        [Column("PROCESS")]
        [StringLength(2)]
        public string Process { get; set; }

        [Column("GRADE")]
        public decimal? Grade { get; set; }

        [Column("LBS")]
        public decimal? Lbs { get; set; }

        [Column("U_PRICE")]
        public decimal? UnitPrice { get; set; }

        [Column("DOLLARS")]
        public decimal? Dollars { get; set; }

        [Column("Description")]
        [StringLength(30)]
        public string Description { get; set; }

        [Column("SERIES")]
        [StringLength(2)]
        public string Series { get; set; }

        [Column("CHEQUE")]
        public decimal? Cheque { get; set; }

        [Column("T_SER")]
        [StringLength(2)]
        public string TSeries { get; set; }

        [Column("T_CHEQ")]
        public decimal? TCheque { get; set; }

        [Column("YEAR")]
        public decimal? Year { get; set; }

        [Column("ACCT_UNIQ")]
        public decimal? AccountUnique { get; set; }

        [Column("CURRENCY")]
        [StringLength(1)]
        public string Currency { get; set; }

        [Column("QADD_DATE")]
        public DateTime? AddDate { get; set; }

        [Column("QADD_TIME")]
        [StringLength(8)]
        public string AddTime { get; set; }

        [Column("QADD_OP")]
        [StringLength(10)]
        public string AddOperator { get; set; }

        [Column("QED_DATE")]
        public DateTime? EditDate { get; set; }

        [Column("QED_TIME")]
        [StringLength(8)]
        public string EditTime { get; set; }

        [Column("QED_OP")]
        [StringLength(10)]
        public string EditOperator { get; set; }

        [Column("QDEL_DATE")]
        public DateTime? DeleteDate { get; set; }

        [Column("QDEL_TIME")]
        [StringLength(8)]
        public string DeleteTime { get; set; }

        [Column("QDEL_OP")]
        [StringLength(10)]
        public string DeleteOperator { get; set; }

        [Column("GST_EST")]
        public decimal? GstEstimate { get; set; }

        [Column("CHG_GST")]
        public bool? ChargeGst { get; set; }

        [Column("GST_RATE")]
        public decimal? GstRate { get; set; }

        [Column("NONGST_EST")]
        public decimal? NonGstEstimate { get; set; }

        [Column("ADV_NO")]
        public decimal? AdvanceNumber { get; set; }

        [Column("ADV_BAT")]
        public decimal? AdvanceBatch { get; set; }

        [Column("FIN_BAT")]
        public decimal? FinanceBatch { get; set; }
    }
}
