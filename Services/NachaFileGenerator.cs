using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Service for generating NACHA formatted files for electronic payments.
    /// Creates standard NACHA format files for bank processing.
    /// </summary>
    public class NachaFileGenerator
    {
        private readonly string _connectionString;
        private const string FILE_HEADER_RECORD_TYPE = "1";
        private const string BATCH_HEADER_RECORD_TYPE = "5";
        private const string ENTRY_DETAIL_RECORD_TYPE = "6";
        private const string BATCH_CONTROL_RECORD_TYPE = "8";
        private const string FILE_CONTROL_RECORD_TYPE = "9";

        public NachaFileGenerator(string connectionString = "")
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Generates a NACHA formatted file for the specified electronic payments.
        /// </summary>
        /// <param name="payments">List of electronic payments to include in the file.</param>
        /// <param name="companyId">Company identification number.</param>
        /// <param name="companyName">Company name.</param>
        /// <param name="bankRoutingNumber">Bank routing number.</param>
        /// <param name="bankAccountNumber">Bank account number.</param>
        /// <returns>Byte array containing the NACHA formatted file.</returns>
        public async Task<byte[]> GenerateNachaFileAsync(List<ElectronicPayment> payments, 
            string companyId, string companyName, string bankRoutingNumber, string bankAccountNumber)
        {
            try
            {
                var fileContent = new StringBuilder();
                var fileId = DateTime.Now.ToString("yyMMddHHmmss");
                var creationDate = DateTime.Now;
                var effectiveDate = DateTime.Now.AddDays(1); // Next business day

                // File Header Record (Type 1)
                var fileHeader = CreateFileHeaderRecord(fileId, creationDate, effectiveDate, companyId, companyName);
                fileContent.AppendLine(fileHeader);

                // Batch Header Record (Type 5)
                var batchHeader = CreateBatchHeaderRecord(bankRoutingNumber, companyId, companyName, effectiveDate);
                fileContent.AppendLine(batchHeader);

                // Entry Detail Records (Type 6) - One for each payment
                var entryCount = 0;
                var totalDebitAmount = 0m;

                foreach (var payment in payments)
                {
                    var entryDetail = CreateEntryDetailRecord(payment, bankRoutingNumber, bankAccountNumber, entryCount + 1);
                    fileContent.AppendLine(entryDetail);
                    entryCount++;
                    totalDebitAmount += payment.Amount;
                }

                // Batch Control Record (Type 8)
                var batchControl = CreateBatchControlRecord(entryCount, totalDebitAmount, bankRoutingNumber);
                fileContent.AppendLine(batchControl);

                // File Control Record (Type 9)
                var fileControl = CreateFileControlRecord(entryCount, totalDebitAmount, fileId);
                fileContent.AppendLine(fileControl);

                var content = fileContent.ToString();
                return Encoding.ASCII.GetBytes(content);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating NACHA file: {ex.Message}", ex);
                throw;
            }
        }

        private string CreateFileHeaderRecord(string fileId, DateTime creationDate, DateTime effectiveDate, string companyId, string companyName)
        {
            // NACHA File Header Record (Type 1) - 94 characters
            var record = new StringBuilder();
            record.Append(FILE_HEADER_RECORD_TYPE);                                    // 1: Record Type Code
            record.Append("01");                                                      // 2-3: Priority Code
            record.Append(" ");                                                       // 4: Space
            record.Append("094");                                                     // 5-7: Immediate Destination
            record.Append(" ");                                                       // 8: Space
            record.Append("094");                                                     // 9-11: Immediate Origin
            record.Append(creationDate.ToString("yyMMdd"));                          // 12-17: File Creation Date
            record.Append(creationDate.ToString("HHmm"));                            // 18-21: File Creation Time
            record.Append("A");                                                       // 22: File ID Modifier
            record.Append("094");                                                     // 23-25: Record Size
            record.Append("10");                                                      // 26-27: Blocking Factor
            record.Append("1");                                                       // 28: Format Code
            record.Append("094");                                                     // 29-31: Immediate Destination Name
            record.Append(" ");                                                       // 32: Space
            record.Append("094");                                                     // 33-35: Immediate Origin Name
            record.Append(" ");                                                       // 36: Space
            record.Append(" ");                                                       // 37: Space
            record.Append(" ");                                                       // 38: Space
            record.Append(" ");                                                       // 39: Space
            record.Append(" ");                                                       // 40: Space
            record.Append(" ");                                                       // 41: Space
            record.Append(" ");                                                       // 42: Space
            record.Append(" ");                                                       // 43: Space
            record.Append(" ");                                                       // 44: Space
            record.Append(" ");                                                       // 45: Space
            record.Append(" ");                                                       // 46: Space
            record.Append(" ");                                                       // 47: Space
            record.Append(" ");                                                       // 48: Space
            record.Append(" ");                                                       // 49: Space
            record.Append(" ");                                                       // 50: Space
            record.Append(" ");                                                       // 51: Space
            record.Append(" ");                                                       // 52: Space
            record.Append(" ");                                                       // 53: Space
            record.Append(" ");                                                       // 54: Space
            record.Append(" ");                                                       // 55: Space
            record.Append(" ");                                                       // 56: Space
            record.Append(" ");                                                       // 57: Space
            record.Append(" ");                                                       // 58: Space
            record.Append(" ");                                                       // 59: Space
            record.Append(" ");                                                       // 60: Space
            record.Append(" ");                                                       // 61: Space
            record.Append(" ");                                                       // 62: Space
            record.Append(" ");                                                       // 63: Space
            record.Append(" ");                                                       // 64: Space
            record.Append(" ");                                                       // 65: Space
            record.Append(" ");                                                       // 66: Space
            record.Append(" ");                                                       // 67: Space
            record.Append(" ");                                                       // 68: Space
            record.Append(" ");                                                       // 69: Space
            record.Append(" ");                                                       // 70: Space
            record.Append(" ");                                                       // 71: Space
            record.Append(" ");                                                       // 72: Space
            record.Append(" ");                                                       // 73: Space
            record.Append(" ");                                                       // 74: Space
            record.Append(" ");                                                       // 75: Space
            record.Append(" ");                                                       // 76: Space
            record.Append(" ");                                                       // 77: Space
            record.Append(" ");                                                       // 78: Space
            record.Append(" ");                                                       // 79: Space
            record.Append(" ");                                                       // 80: Space
            record.Append(" ");                                                       // 81: Space
            record.Append(" ");                                                       // 82: Space
            record.Append(" ");                                                       // 83: Space
            record.Append(" ");                                                       // 84: Space
            record.Append(" ");                                                       // 85: Space
            record.Append(" ");                                                       // 86: Space
            record.Append(" ");                                                       // 87: Space
            record.Append(" ");                                                       // 88: Space
            record.Append(" ");                                                       // 89: Space
            record.Append(" ");                                                       // 90: Space
            record.Append(" ");                                                       // 91: Space
            record.Append(" ");                                                       // 92: Space
            record.Append(" ");                                                       // 93: Space
            record.Append(" ");                                                       // 94: Space

            return record.ToString();
        }

        private string CreateBatchHeaderRecord(string bankRoutingNumber, string companyId, string companyName, DateTime effectiveDate)
        {
            // NACHA Batch Header Record (Type 5) - 94 characters
            var record = new StringBuilder();
            record.Append(BATCH_HEADER_RECORD_TYPE);                                  // 1: Record Type Code
            record.Append("200");                                                    // 2-4: Service Class Code (200 = Mixed Debits and Credits)
            record.Append(companyName.PadRight(16).Substring(0, 16));               // 5-20: Company Name
            record.Append(" ");                                                       // 21: Space
            record.Append(" ");                                                       // 22: Space
            record.Append(" ");                                                       // 23: Space
            record.Append(" ");                                                       // 24: Space
            record.Append(" ");                                                       // 25: Space
            record.Append(" ");                                                       // 26: Space
            record.Append(" ");                                                       // 27: Space
            record.Append(" ");                                                       // 28: Space
            record.Append(" ");                                                       // 29: Space
            record.Append(" ");                                                       // 30: Space
            record.Append(" ");                                                       // 31: Space
            record.Append(" ");                                                       // 32: Space
            record.Append(" ");                                                       // 33: Space
            record.Append(" ");                                                       // 34: Space
            record.Append(" ");                                                       // 35: Space
            record.Append(" ");                                                       // 36: Space
            record.Append(" ");                                                       // 37: Space
            record.Append(" ");                                                       // 38: Space
            record.Append(" ");                                                       // 39: Space
            record.Append(" ");                                                       // 40: Space
            record.Append(" ");                                                       // 41: Space
            record.Append(" ");                                                       // 42: Space
            record.Append(" ");                                                       // 43: Space
            record.Append(" ");                                                       // 44: Space
            record.Append(" ");                                                       // 45: Space
            record.Append(" ");                                                       // 46: Space
            record.Append(" ");                                                       // 47: Space
            record.Append(" ");                                                       // 48: Space
            record.Append(" ");                                                       // 49: Space
            record.Append(" ");                                                       // 50: Space
            record.Append(" ");                                                       // 51: Space
            record.Append(" ");                                                       // 52: Space
            record.Append(" ");                                                       // 53: Space
            record.Append(" ");                                                       // 54: Space
            record.Append(" ");                                                       // 55: Space
            record.Append(" ");                                                       // 56: Space
            record.Append(" ");                                                       // 57: Space
            record.Append(" ");                                                       // 58: Space
            record.Append(" ");                                                       // 59: Space
            record.Append(" ");                                                       // 60: Space
            record.Append(" ");                                                       // 61: Space
            record.Append(" ");                                                       // 62: Space
            record.Append(" ");                                                       // 63: Space
            record.Append(" ");                                                       // 64: Space
            record.Append(" ");                                                       // 65: Space
            record.Append(" ");                                                       // 66: Space
            record.Append(" ");                                                       // 67: Space
            record.Append(" ");                                                       // 68: Space
            record.Append(" ");                                                       // 69: Space
            record.Append(" ");                                                       // 70: Space
            record.Append(" ");                                                       // 71: Space
            record.Append(" ");                                                       // 72: Space
            record.Append(" ");                                                       // 73: Space
            record.Append(" ");                                                       // 74: Space
            record.Append(" ");                                                       // 75: Space
            record.Append(" ");                                                       // 76: Space
            record.Append(" ");                                                       // 77: Space
            record.Append(" ");                                                       // 78: Space
            record.Append(" ");                                                       // 79: Space
            record.Append(" ");                                                       // 80: Space
            record.Append(" ");                                                       // 81: Space
            record.Append(" ");                                                       // 82: Space
            record.Append(" ");                                                       // 83: Space
            record.Append(" ");                                                       // 84: Space
            record.Append(" ");                                                       // 85: Space
            record.Append(" ");                                                       // 86: Space
            record.Append(" ");                                                       // 87: Space
            record.Append(" ");                                                       // 88: Space
            record.Append(" ");                                                       // 89: Space
            record.Append(" ");                                                       // 90: Space
            record.Append(" ");                                                       // 91: Space
            record.Append(" ");                                                       // 92: Space
            record.Append(" ");                                                       // 93: Space
            record.Append(" ");                                                       // 94: Space

            return record.ToString();
        }

        private string CreateEntryDetailRecord(ElectronicPayment payment, string bankRoutingNumber, string bankAccountNumber, int sequenceNumber)
        {
            // NACHA Entry Detail Record (Type 6) - 94 characters
            var record = new StringBuilder();
            record.Append(ENTRY_DETAIL_RECORD_TYPE);                                  // 1: Record Type Code
            record.Append("22");                                                      // 2-3: Transaction Code (22 = Credit)
            record.Append(bankRoutingNumber.PadLeft(8, '0'));                        // 4-11: Receiving DFI Identification
            record.Append("1");                                                       // 12: Check Digit
            record.Append(bankAccountNumber.PadRight(17).Substring(0, 17));          // 13-29: DFI Account Number
            record.Append(payment.Amount.ToString("F2").Replace(".", "").PadLeft(10, '0')); // 30-39: Amount
            record.Append(payment.ReferenceNumber.PadRight(15).Substring(0, 15));    // 40-54: Individual Identification
            record.Append(" ");                                                       // 55: Space
            record.Append(" ");                                                       // 56: Space
            record.Append(" ");                                                       // 57: Space
            record.Append(" ");                                                       // 58: Space
            record.Append(" ");                                                       // 59: Space
            record.Append(" ");                                                       // 60: Space
            record.Append(" ");                                                       // 61: Space
            record.Append(" ");                                                       // 62: Space
            record.Append(" ");                                                       // 63: Space
            record.Append(" ");                                                       // 64: Space
            record.Append(" ");                                                       // 65: Space
            record.Append(" ");                                                       // 66: Space
            record.Append(" ");                                                       // 67: Space
            record.Append(" ");                                                       // 68: Space
            record.Append(" ");                                                       // 69: Space
            record.Append(" ");                                                       // 70: Space
            record.Append(" ");                                                       // 71: Space
            record.Append(" ");                                                       // 72: Space
            record.Append(" ");                                                       // 73: Space
            record.Append(" ");                                                       // 74: Space
            record.Append(" ");                                                       // 75: Space
            record.Append(" ");                                                       // 76: Space
            record.Append(" ");                                                       // 77: Space
            record.Append(" ");                                                       // 78: Space
            record.Append(" ");                                                       // 79: Space
            record.Append(" ");                                                       // 80: Space
            record.Append(" ");                                                       // 81: Space
            record.Append(" ");                                                       // 82: Space
            record.Append(" ");                                                       // 83: Space
            record.Append(" ");                                                       // 84: Space
            record.Append(" ");                                                       // 85: Space
            record.Append(" ");                                                       // 86: Space
            record.Append(" ");                                                       // 87: Space
            record.Append(" ");                                                       // 88: Space
            record.Append(" ");                                                       // 89: Space
            record.Append(" ");                                                       // 90: Space
            record.Append(" ");                                                       // 91: Space
            record.Append(" ");                                                       // 92: Space
            record.Append(" ");                                                       // 93: Space
            record.Append(" ");                                                       // 94: Space

            return record.ToString();
        }

        private string CreateBatchControlRecord(int entryCount, decimal totalAmount, string bankRoutingNumber)
        {
            // NACHA Batch Control Record (Type 8) - 94 characters
            var record = new StringBuilder();
            record.Append(BATCH_CONTROL_RECORD_TYPE);                                 // 1: Record Type Code
            record.Append("200");                                                    // 2-4: Service Class Code
            record.Append(entryCount.ToString().PadLeft(6, '0'));                    // 5-10: Entry/Addenda Count
            record.Append("9");                                                      // 11: Entry Hash
            record.Append(totalAmount.ToString("F2").Replace(".", "").PadLeft(12, '0')); // 12-23: Total Debit Entry Dollar Amount
            record.Append(totalAmount.ToString("F2").Replace(".", "").PadLeft(12, '0')); // 24-35: Total Credit Entry Dollar Amount
            record.Append(" ");                                                       // 36: Space
            record.Append(" ");                                                       // 37: Space
            record.Append(" ");                                                       // 38: Space
            record.Append(" ");                                                       // 39: Space
            record.Append(" ");                                                       // 40: Space
            record.Append(" ");                                                       // 41: Space
            record.Append(" ");                                                       // 42: Space
            record.Append(" ");                                                       // 43: Space
            record.Append(" ");                                                       // 44: Space
            record.Append(" ");                                                       // 45: Space
            record.Append(" ");                                                       // 46: Space
            record.Append(" ");                                                       // 47: Space
            record.Append(" ");                                                       // 48: Space
            record.Append(" ");                                                       // 49: Space
            record.Append(" ");                                                       // 50: Space
            record.Append(" ");                                                       // 51: Space
            record.Append(" ");                                                       // 52: Space
            record.Append(" ");                                                       // 53: Space
            record.Append(" ");                                                       // 54: Space
            record.Append(" ");                                                       // 55: Space
            record.Append(" ");                                                       // 56: Space
            record.Append(" ");                                                       // 57: Space
            record.Append(" ");                                                       // 58: Space
            record.Append(" ");                                                       // 59: Space
            record.Append(" ");                                                       // 60: Space
            record.Append(" ");                                                       // 61: Space
            record.Append(" ");                                                       // 62: Space
            record.Append(" ");                                                       // 63: Space
            record.Append(" ");                                                       // 64: Space
            record.Append(" ");                                                       // 65: Space
            record.Append(" ");                                                       // 66: Space
            record.Append(" ");                                                       // 67: Space
            record.Append(" ");                                                       // 68: Space
            record.Append(" ");                                                       // 69: Space
            record.Append(" ");                                                       // 70: Space
            record.Append(" ");                                                       // 71: Space
            record.Append(" ");                                                       // 72: Space
            record.Append(" ");                                                       // 73: Space
            record.Append(" ");                                                       // 74: Space
            record.Append(" ");                                                       // 75: Space
            record.Append(" ");                                                       // 76: Space
            record.Append(" ");                                                       // 77: Space
            record.Append(" ");                                                       // 78: Space
            record.Append(" ");                                                       // 79: Space
            record.Append(" ");                                                       // 80: Space
            record.Append(" ");                                                       // 81: Space
            record.Append(" ");                                                       // 82: Space
            record.Append(" ");                                                       // 83: Space
            record.Append(" ");                                                       // 84: Space
            record.Append(" ");                                                       // 85: Space
            record.Append(" ");                                                       // 86: Space
            record.Append(" ");                                                       // 87: Space
            record.Append(" ");                                                       // 88: Space
            record.Append(" ");                                                       // 89: Space
            record.Append(" ");                                                       // 90: Space
            record.Append(" ");                                                       // 91: Space
            record.Append(" ");                                                       // 92: Space
            record.Append(" ");                                                       // 93: Space
            record.Append(" ");                                                       // 94: Space

            return record.ToString();
        }

        private string CreateFileControlRecord(int entryCount, decimal totalAmount, string fileId)
        {
            // NACHA File Control Record (Type 9) - 94 characters
            var record = new StringBuilder();
            record.Append(FILE_CONTROL_RECORD_TYPE);                                 // 1: Record Type Code
            record.Append("1");                                                       // 2: Batch Count
            record.Append(entryCount.ToString().PadLeft(6, '0'));                    // 3-8: Block Count
            record.Append(entryCount.ToString().PadLeft(6, '0'));                    // 9-14: Entry/Addenda Count
            record.Append("9");                                                       // 15: Entry Hash
            record.Append(totalAmount.ToString("F2").Replace(".", "").PadLeft(12, '0')); // 16-27: Total Debit Entry Dollar Amount
            record.Append(totalAmount.ToString("F2").Replace(".", "").PadLeft(12, '0')); // 28-39: Total Credit Entry Dollar Amount
            record.Append(" ");                                                       // 40: Space
            record.Append(" ");                                                       // 41: Space
            record.Append(" ");                                                       // 42: Space
            record.Append(" ");                                                       // 43: Space
            record.Append(" ");                                                       // 44: Space
            record.Append(" ");                                                       // 45: Space
            record.Append(" ");                                                       // 46: Space
            record.Append(" ");                                                       // 47: Space
            record.Append(" ");                                                       // 48: Space
            record.Append(" ");                                                       // 49: Space
            record.Append(" ");                                                       // 50: Space
            record.Append(" ");                                                       // 51: Space
            record.Append(" ");                                                       // 52: Space
            record.Append(" ");                                                       // 53: Space
            record.Append(" ");                                                       // 54: Space
            record.Append(" ");                                                       // 55: Space
            record.Append(" ");                                                       // 56: Space
            record.Append(" ");                                                       // 57: Space
            record.Append(" ");                                                       // 58: Space
            record.Append(" ");                                                       // 59: Space
            record.Append(" ");                                                       // 60: Space
            record.Append(" ");                                                       // 61: Space
            record.Append(" ");                                                       // 62: Space
            record.Append(" ");                                                       // 63: Space
            record.Append(" ");                                                       // 64: Space
            record.Append(" ");                                                       // 65: Space
            record.Append(" ");                                                       // 66: Space
            record.Append(" ");                                                       // 67: Space
            record.Append(" ");                                                       // 68: Space
            record.Append(" ");                                                       // 69: Space
            record.Append(" ");                                                       // 70: Space
            record.Append(" ");                                                       // 71: Space
            record.Append(" ");                                                       // 72: Space
            record.Append(" ");                                                       // 73: Space
            record.Append(" ");                                                       // 74: Space
            record.Append(" ");                                                       // 75: Space
            record.Append(" ");                                                       // 76: Space
            record.Append(" ");                                                       // 77: Space
            record.Append(" ");                                                       // 78: Space
            record.Append(" ");                                                       // 79: Space
            record.Append(" ");                                                       // 80: Space
            record.Append(" ");                                                       // 81: Space
            record.Append(" ");                                                       // 82: Space
            record.Append(" ");                                                       // 83: Space
            record.Append(" ");                                                       // 84: Space
            record.Append(" ");                                                       // 85: Space
            record.Append(" ");                                                       // 86: Space
            record.Append(" ");                                                       // 87: Space
            record.Append(" ");                                                       // 88: Space
            record.Append(" ");                                                       // 89: Space
            record.Append(" ");                                                       // 90: Space
            record.Append(" ");                                                       // 91: Space
            record.Append(" ");                                                       // 92: Space
            record.Append(" ");                                                       // 93: Space
            record.Append(" ");                                                       // 94: Space

            return record.ToString();
        }
    }
}
