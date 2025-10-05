using System;
using System.Collections.Generic;

namespace WPFGrowerApp.DataAccess.Exceptions
{
    /// <summary>
    /// Exception thrown when required reference data (Container, PriceClass, PriceArea) is missing for a receipt.
    /// This allows the import process to skip the receipt and log detailed information about what's missing.
    /// </summary>
    public class MissingReferenceDataException : Exception
    {
        public List<string> MissingItems { get; }
        public string ReceiptIdentifier { get; }

        public MissingReferenceDataException(string receiptIdentifier, List<string> missingItems)
            : base($"Receipt {receiptIdentifier} is missing required reference data: {string.Join(", ", missingItems)}")
        {
            ReceiptIdentifier = receiptIdentifier;
            MissingItems = missingItems ?? new List<string>();
        }

        public MissingReferenceDataException(string receiptIdentifier, string missingItem)
            : this(receiptIdentifier, new List<string> { missingItem })
        {
        }
    }
}
