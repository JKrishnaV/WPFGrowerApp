using System;
using System.ComponentModel;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a payment allocation for a receipt
    /// </summary>
    public class ReceiptPaymentAllocation : INotifyPropertyChanged
    {
        public int AllocationId { get; set; }
        public int ReceiptId { get; set; }
        public int PaymentBatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime BatchDate { get; set; }
        public string PaymentTypeName { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal AllocatedWeight { get; set; }
        public decimal PricePerPound { get; set; }
        public DateTime AllocatedAt { get; set; }
        public string AllocatedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Active, Voided, etc.
        public string? Notes { get; set; }
        
        // Additional properties for compatibility
        public int PaymentTypeId { get; set; }
        public int PriceScheduleId { get; set; }
        public decimal QuantityPaid { get; set; }
        public int GrowerId { get; set; }
        public string GrowerName { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        
        // Product and Process information
        public string ProductName { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        
        // Grade and Price Class information
        public int Grade { get; set; }
        public string PriceClassName { get; set; } = string.Empty;

        // Display Properties
        public string AmountPaidDisplay => $"{AmountPaid:C2}";
        public string AllocatedWeightDisplay => $"{AllocatedWeight:N2} lbs";
        public string PricePerPoundDisplay => $"{PricePerPound:C4}/lb";
        public string AllocatedAtDisplay => $"{AllocatedAt:yyyy-MM-dd HH:mm}";
        public string BatchInfoDisplay => $"{BatchNumber} ({PaymentTypeDisplay})";
        public string StatusDisplay => Status;
        public string PaymentTypeDisplay => GetPaymentTypeDisplay();

        // Helper Properties
        public bool IsActive => Status == "Active";
        public bool IsVoided => Status == "Voided";
        public bool IsRecent => (DateTime.Now - AllocatedAt).TotalDays <= 30;

        private string GetPaymentTypeDisplay()
        {
            // Map PaymentTypeId to display names
            return PaymentTypeId switch
            {
                1 => "Advance 1",
                2 => "Advance 2", 
                3 => "Advance 3",
                4 => "Final Payment",
                _ => $"Payment Type {PaymentTypeId}"
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}