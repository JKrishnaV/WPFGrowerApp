using System;

namespace WPFGrowerApp.Models
{
    public class Account
    {
        public int AccountID { get; set; }
        public decimal GrowerNumber { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public DateTime? LastActivity { get; set; }
    }
}
