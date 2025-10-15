using System;
using System.ComponentModel;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents an audit trail entry for receipt changes
    /// </summary>
    public class ReceiptAuditEntry : INotifyPropertyChanged
    {
        public int AuditId { get; set; }
        public int ReceiptId { get; set; }
        public string ChangeType { get; set; } = string.Empty; // Created, Updated, Voided, Deleted, QualityChecked, etc.
        public string FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public string? ChangeReason { get; set; }
        public string? UserRole { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        // Display Properties
        public string ChangeTypeDisplay
        {
            get
            {
                return ChangeType switch
                {
                    "Created" => "Created",
                    "Updated" => "Modified",
                    "Voided" => "Voided",
                    "Deleted" => "Deleted",
                    "QualityChecked" => "Quality Checked",
                    "Restored" => "Restored",
                    _ => ChangeType
                };
            }
        }

        public string FieldDisplayName
        {
            get
            {
                return FieldName switch
                {
                    "ReceiptNumber" => "Receipt Number",
                    "ReceiptDate" => "Receipt Date",
                    "ReceiptTime" => "Receipt Time",
                    "GrowerId" => "Grower",
                    "ProductId" => "Product",
                    "ProcessId" => "Process",
                    "DepotId" => "Depot",
                    "Grade" => "Grade",
                    "GrossWeight" => "Gross Weight",
                    "TareWeight" => "Tare Weight",
                    "DockPercentage" => "Dock Percentage",
                    "IsVoided" => "Void Status",
                    _ => FieldName
                };
            }
        }

        public string ChangedByDisplay => $"{ChangedBy} ({UserRole})";
        public string ChangedAtDisplay => $"{ChangedAt:yyyy-MM-dd HH:mm:ss}";
        public string ChangeDescription => $"{ChangeTypeDisplay} {FieldDisplayName}";
        
        public string ValueChangeDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(OldValue) && string.IsNullOrEmpty(NewValue))
                    return "No change";
                
                if (string.IsNullOrEmpty(OldValue))
                    return $"Set to: {NewValue}";
                
                if (string.IsNullOrEmpty(NewValue))
                    return $"Removed: {OldValue}";
                
                return $"Changed from '{OldValue}' to '{NewValue}'";
            }
        }

        public string FullDescription => $"{ChangeDescription} - {ValueChangeDisplay}";

        // Helper Properties
        public bool IsRecentChange => (DateTime.Now - ChangedAt).TotalHours <= 24;
        public bool IsSignificantChange => ChangeType is "Voided" or "Deleted" or "Restored";
        public bool HasValueChange => !string.IsNullOrEmpty(OldValue) || !string.IsNullOrEmpty(NewValue);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
