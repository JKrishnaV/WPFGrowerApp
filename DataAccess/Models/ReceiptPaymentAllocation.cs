using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Model class for ReceiptPaymentAllocation
    /// Matches the ReceiptPaymentAllocations table schema
    /// </summary>
    public class ReceiptPaymentAllocation
    {
        public int AllocationId { get; set; }
        public int ReceiptId { get; set; }
        public int PaymentBatchId { get; set; }
        public int PaymentTypeId { get; set; }
        public string PaymentTypeName { get; set; } // From join with PaymentTypes
        public int PriceScheduleId { get; set; }
        public decimal PricePerPound { get; set; }
        public decimal QuantityPaid { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime AllocatedAt { get; set; }
        
        // ====================================================================
        // AUDIT COLUMNS
        // ====================================================================
        
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        
        /// <summary>
        /// Returns true if the record is soft-deleted
        /// </summary>
        public bool IsDeleted => DeletedAt.HasValue;
    }
}
