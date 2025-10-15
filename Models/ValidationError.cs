using System;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Represents a single validation error or warning
    /// </summary>
    public class ValidationError
    {
        public string FieldName { get; set; }
        public string Message { get; set; }
        public ValidationSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }

        public ValidationError(string fieldName, string message, ValidationSeverity severity = ValidationSeverity.Error)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Severity = severity;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{FieldName}: {Message}";
        }
    }

    /// <summary>
    /// Severity levels for validation errors
    /// </summary>
    public enum ValidationSeverity
    {
        Error,
        Warning,
        Info
    }
}
