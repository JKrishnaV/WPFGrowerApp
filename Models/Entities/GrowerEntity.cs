using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFGrowerApp.Models.Entities
{
    [Table("Grower")]
    public class GrowerEntity
    {
        [Key]
        [Column("NUMBER")]
        public decimal GrowerNumber { get; set; }

        [Column("STATUS")]
        public decimal? Status { get; set; }

        [Column("CHEQNAME")]
        [StringLength(30)]
        public string ChequeName { get; set; }

        [Column("NAME")]
        [StringLength(30)]
        public string GrowerName { get; set; }

        [Column("STREET")]
        [StringLength(30)]
        public string Address { get; set; }

        [Column("CITY")]
        [StringLength(25)]
        public string City { get; set; }

        [Column("PROV")]
        [StringLength(2)]
        public string Province { get; set; }

        [Column("PCODE")]
        [StringLength(10)]
        public string Postal { get; set; }

        [Column("PHONE")]
        [StringLength(13)]
        public string Phone { get; set; }

        [Column("DEPOT")]
        [StringLength(1)]
        public string Depot { get; set; }

        [Column("ACRES")]
        public decimal? Acres { get; set; }

        [Column("NOTES")]
        [StringLength(60)]
        public string Notes { get; set; }

        [Column("CONTRACT")]
        [StringLength(1)]
        public string Contract { get; set; }

        [Column("CURRENCY")]
        [StringLength(1)]
        public string Currency { get; set; }

        [Column("CONTLIM")]
        public decimal? ContractLimit { get; set; }

        [Column("PAYGRP")]
        [StringLength(1)]
        public string PayGroup { get; set; }

        [Column("ONHOLD")]
        public bool? OnHold { get; set; }

        [Column("PHONE2")]
        [StringLength(13)]
        public string PhoneAdditional1 { get; set; }

        [Column("STREET2")]
        [StringLength(30)]
        public string AddressLine2 { get; set; }

        [Column("ALT_NAME1")]
        [StringLength(30)]
        public string OtherNames { get; set; }

        [Column("ALT_PHONE1")]
        [StringLength(13)]
        public string AltPhone1 { get; set; }

        [Column("ALT_NAME2")]
        [StringLength(30)]
        public string AltName2 { get; set; }

        [Column("ALT_PHONE2")]
        [StringLength(13)]
        public string PhoneAdditional2 { get; set; }

        [Column("NOTE2")]
        [StringLength(60)]
        public string Note2 { get; set; }

        [Column("LY_FRESH")]
        public int? LYFresh { get; set; }

        [Column("LY_OTHER")]
        public int? LYOther { get; set; }

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

        [Column("CERTIFIED")]
        [StringLength(15)]
        public string Certified { get; set; }

        [Column("FAX")]
        [StringLength(13)]
        public string Fax { get; set; }

        [Column("CHG_GST")]
        public bool? ChargeGST { get; set; }
    }
}
