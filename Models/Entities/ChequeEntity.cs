using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFGrowerApp.Models.Entities
{
    [Table("Cheque")]
    public class ChequeEntity
    {
        [Column("SERIES")]
        [StringLength(2)]
        public string Series { get; set; }

        [Column("CHEQUE")]
        public decimal Cheque { get; set; }

        [Column("NUMBER")]
        public decimal Number { get; set; }

        [Column("DATE")]
        public DateTime? Date { get; set; }

        [Column("AMOUNT")]
        public decimal? Amount { get; set; }

        [Column("YEAR")]
        public decimal? Year { get; set; }

        [Column("CHEQTYPE")]
        [StringLength(1)]
        public string ChequeType { get; set; }

        [Column("VOID")]
        public bool? Void { get; set; }

        [Column("DATECLEAR")]
        public DateTime? DateClear { get; set; }

        [Column("ISCLEARED")]
        public bool? IsCleared { get; set; }

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
    }
}
