using System;
using System.ComponentModel;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Extended receipt model with joined data for detail views
    /// </summary>
    public class ReceiptDetailDto : INotifyPropertyChanged
    {
        // Core Receipt Properties
        public int ReceiptId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public TimeSpan ReceiptTime { get; set; }
        public int GrowerId { get; set; }
        public int ProductId { get; set; }
        public int ProcessId { get; set; }
        public int? ProcessTypeId { get; set; }
        public int? VarietyId { get; set; }
        public int DepotId { get; set; }
        public int Grade { get; set; }
        public int PriceClassId { get; set; }
        public decimal GrossWeight { get; set; }
        public decimal TareWeight { get; set; }
        public decimal NetWeight { get; set; }
        public decimal DockPercentage { get; set; }
        public decimal DockWeight { get; set; }
        public decimal FinalWeight { get; set; }
        public bool IsVoided { get; set; }
        public string? VoidedReason { get; set; }
        public DateTime? VoidedAt { get; set; }
        public string? VoidedBy { get; set; }
        public int? ImportBatchId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? QualityCheckedAt { get; set; }
        public string? QualityCheckedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        // Joined Data Properties
        public string GrowerName { get; set; } = string.Empty;
        public string GrowerNumber { get; set; } = string.Empty;
        public string GrowerAddress { get; set; } = string.Empty;
        public string GrowerPhone { get; set; } = string.Empty;
        public string GrowerEmail { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string ProcessDescription { get; set; } = string.Empty;
        public string? ProcessTypeName { get; set; }
        public string? VarietyName { get; set; }
        public string DepotName { get; set; } = string.Empty;
        public string DepotAddress { get; set; } = string.Empty;
        public string PriceClassName { get; set; } = string.Empty;
        public decimal PricePerPound { get; set; }
        public int PaymentTypeId { get; set; }
        public string PaymentTypeName { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;

        // Payment Allocation Summary
        public bool IsPaid { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public int PaymentBatchCount { get; set; }
        public string? LastPaymentBatchNumber { get; set; }
        public DateTime? LastPaymentDate { get; set; }

        // Audit Trail Summary
        public int ChangeCount { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string? LastModifiedBy { get; set; }

        // Helper Properties
        public string StatusDisplay => IsVoided ? "Voided" : "Active";
        public string QualityStatusDisplay => QualityCheckedAt.HasValue ? "Quality Checked" : "Pending";
        public string PaymentStatusDisplay => IsPaid ? "Paid" : "Unpaid";
        public string AdvancePaymentDisplay => GetAdvancePaymentDisplay();
        public string FullGrowerDisplay => $"{GrowerNumber} - {GrowerName}";
        public string FullProductDisplay => $"{ProductName} ({ProductDescription})";
        public string FullProcessDisplay => $"{ProcessName} ({ProcessDescription})";
        public string FullDepotDisplay => $"{DepotName} - {DepotAddress}";
        public string GradeDisplay => $"Grade {Grade}";
        public string WeightSummary => $"Gross: {GrossWeight:N2} | Net: {NetWeight:N2} | Final: {FinalWeight:N2}";
        public string DockSummary => $"Dock: {DockPercentage:N2}% ({DockWeight:N2} lbs)";

        // Computed Properties for Display
        public string ReceiptDateTimeDisplay => $"{ReceiptDate:yyyy-MM-dd} {ReceiptTime:HH:mm}";
        public string CreatedDisplay => $"{CreatedAt:yyyy-MM-dd HH:mm} by {CreatedBy}";
        public string ModifiedDisplay => ModifiedAt.HasValue ? $"{ModifiedAt:yyyy-MM-dd HH:mm} by {ModifiedBy}" : "Never";
        public string QualityCheckedDisplay => QualityCheckedAt.HasValue ? $"{QualityCheckedAt:yyyy-MM-dd HH:mm} by {QualityCheckedBy}" : "Not checked";
        public string VoidedDisplay => IsVoided ? $"{VoidedAt:yyyy-MM-dd HH:mm} by {VoidedBy}" : "Not voided";

        // Business Logic Properties
        public bool CanEdit => !IsVoided && DeletedAt == null;
        public bool CanVoid => !IsVoided && DeletedAt == null;
        public bool CanDelete => !IsVoided && DeletedAt == null;
        public bool CanQualityCheck => !IsVoided && QualityCheckedAt == null;
        public bool IsRecentlyCreated => (DateTime.Now - CreatedAt).TotalDays <= 7;
        public bool IsRecentlyModified => ModifiedAt.HasValue && (DateTime.Now - ModifiedAt.Value).TotalDays <= 7;

        private string GetAdvancePaymentDisplay()
        {
            // Use the actual PaymentTypeName from database if available, otherwise fall back to mapping
            if (!string.IsNullOrEmpty(PaymentTypeName))
            {
                return PaymentTypeName;
            }
            
            // Fallback mapping for PaymentTypeId
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
