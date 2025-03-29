using System;
using System.Collections.Generic;

namespace WPFGrowerApp.DataAccess.Exceptions
{
    public class ImportValidationException : Exception
    {
        public List<string> ValidationErrors { get; }

        public ImportValidationException(string message) : base(message)
        {
            ValidationErrors = new List<string> { message };
        }

        public ImportValidationException(string message, List<string> validationErrors) : base(message)
        {
            ValidationErrors = validationErrors;
        }

        public ImportValidationException(string message, Exception innerException) 
            : base(message, innerException)
        {
            ValidationErrors = new List<string> { message };
        }
    }

    public class ImportProcessingException : Exception
    {
        public ImportProcessingException(string message) : base(message) { }
        public ImportProcessingException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    public class ImportBatchException : Exception
    {
        public ImportBatchException(string message) : base(message) { }
        public ImportBatchException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
} 