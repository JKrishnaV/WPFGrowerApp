using System;

namespace WPFGrowerApp.Models
{
    public class Cheque
    {
        public int ChequeID { get; set; }
        public decimal GrowerNumber { get; set; }
        public string ChequeNumber { get; set; }
        public DateTime ChequeDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }
}
