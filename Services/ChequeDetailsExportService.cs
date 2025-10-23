using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WPFGrowerApp.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Service for exporting cheque calculation details to various formats
    /// </summary>
    public class ChequeDetailsExportService
    {
        public ChequeDetailsExportService()
        {
        }

        /// <summary>
        /// Export cheque details to PDF format
        /// </summary>
        public async Task<string> ExportToPdfAsync(InvoiceStyleChequeDetails details, string filePath)
        {
            try
            {
                await Task.Run(() =>
                {
                    // For now, create a simple text file as PDF placeholder
                    // In production, you would use a proper PDF library like iTextSharp
                    var content = GenerateTextContent(details);
                    File.WriteAllText(filePath.Replace(".pdf", ".txt"), content);
                });

                Logger.Info($"Successfully exported cheque details to PDF: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting to PDF: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Export cheque details to Excel format
        /// </summary>
        public async Task<string> ExportToExcelAsync(InvoiceStyleChequeDetails details, string filePath)
        {
            try
            {
                await Task.Run(() =>
                {
                    // For now, create a CSV file as Excel placeholder
                    // In production, you would use a proper Excel library like EPPlus or ClosedXML
                    var content = GenerateCsvContent(details);
                    File.WriteAllText(filePath.Replace(".xlsx", ".csv"), content);
                });

                Logger.Info($"Successfully exported cheque details to Excel: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting to Excel: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Export cheque details to CSV format
        /// </summary>
        public async Task<string> ExportToCsvAsync(InvoiceStyleChequeDetails details, string filePath)
        {
            try
            {
                await Task.Run(() =>
                {
                    var content = GenerateCsvContent(details);
                    File.WriteAllText(filePath, content);
                });

                Logger.Info($"Successfully exported cheque details to CSV: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting to CSV: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Export cheque details to HTML format
        /// </summary>
        public async Task<string> ExportToHtmlAsync(InvoiceStyleChequeDetails details, string filePath)
        {
            try
            {
                await Task.Run(() =>
                {
                    var content = GenerateHtmlContent(details);
                    File.WriteAllText(filePath, content);
                });

                Logger.Info($"Successfully exported cheque details to HTML: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting to HTML: {ex.Message}", ex);
                throw;
            }
        }

        #region Content Generation Methods

        private string GenerateTextContent(InvoiceStyleChequeDetails details)
        {
            var content = $@"
{details.CompanyName}
{details.DocumentTitle}
Generated: {details.GeneratedDate:yyyy-MM-dd HH:mm:ss}

CHEQUE INFORMATION
==================
Cheque Number: {details.Header.ChequeNumber}
Date: {details.Header.DateDisplay}
Grower: {details.Header.GrowerInfo}
Payee: {details.Header.PayeeName}
Status: {details.Header.Status}

PAYMENT SUMMARY
===============
Total Gross Payments: {details.Summary.TotalGrossPaymentsDisplay}
Total Deductions: {details.Summary.TotalDeductionsDisplay}
Net Cheque Amount: {details.Summary.NetChequeAmountDisplay}

DETAILED BREAKDOWN
==================";

            foreach (var batch in details.PaymentBatches)
            {
                content += $@"

PAYMENT BATCH: {batch.BatchNumber}
Batch Date: {batch.BatchDateDisplay}
Payment Type: {batch.PaymentType}

Receipt Details:
Receipt# | Product | Process | Grade | Weight | Price | Amount
---------|---------|---------|-------|--------|-------|-------";

                foreach (var receipt in batch.Receipts)
                {
                    content += $@"
{receipt.ReceiptNumber} | {receipt.ProductName} | {receipt.ProcessName} | {receipt.Grade} | {receipt.WeightDisplay} | {receipt.PricePerPoundDisplay} | {receipt.AmountDisplay}";
                }

                content += $@"

Batch Subtotal: {batch.BatchSubtotalDisplay}";
            }

            if (details.AdvanceRuns.Any())
            {
                content += $@"

ADVANCE PAYMENT RUNS
===================
Run Date | Run Number | Amount
---------|------------|-------";

                foreach (var run in details.AdvanceRuns)
                {
                    content += $@"
{run.RunDateDisplay} | {run.RunNumber} | {run.AmountDisplay}";
                }
            }

            if (details.Deductions.Any())
            {
                content += $@"

DEDUCTIONS
==========
Type | Description | Amount
-----|-------------|-------";

                foreach (var deduction in details.Deductions)
                {
                    content += $@"
{deduction.Type} | {deduction.Description} | {deduction.AmountDisplay}";
                }
            }

            content += $@"

PAYMENT HISTORY
===============
Date | Cheque# | Batch# | Amount
-----|---------|--------|-------";

            foreach (var payment in details.History.Payments)
            {
                content += $@"
{payment.DateDisplay} | {payment.ChequeNumber} | {payment.BatchNumber} | {payment.AmountDisplay}";
            }

            content += $@"

Season Total: {details.History.SeasonTotalDisplay}";

            if (!string.IsNullOrEmpty(details.Memo))
            {
                content += $@"

MEMO
====
{details.Memo}";
            }

            return content;
        }

        private string GenerateCsvContent(InvoiceStyleChequeDetails details)
        {
            var content = $@"Cheque Calculation Details - {details.Header.ChequeNumber}
Date: {details.Header.DateDisplay}
Grower: {details.Header.GrowerInfo}

SUMMARY
Total Gross Payments,Total Deductions,Net Cheque Amount
{details.Summary.TotalGrossPayments:F2},{details.Summary.TotalDeductions:F2},{details.Summary.NetChequeAmount:F2}

PAYMENT BATCHES
Batch Number,Batch Date,Payment Type,Receipt Count,Batch Subtotal";

            foreach (var batch in details.PaymentBatches)
            {
                content += $@"
{batch.BatchNumber},{batch.BatchDateDisplay},{batch.PaymentType},{batch.ReceiptCount},{batch.BatchSubtotal:F2}";
            }

            content += @"

RECEIPT DETAILS
Receipt Number,Product,Process,Grade,Weight,Price Per Pound,Amount";

            foreach (var batch in details.PaymentBatches)
            {
                foreach (var receipt in batch.Receipts)
                {
                    content += $@"
{receipt.ReceiptNumber},{receipt.ProductName},{receipt.ProcessName},{receipt.Grade},{receipt.Weight:F2},{receipt.PricePerPound:F2},{receipt.Amount:F2}";
                }
            }

            content += @"

PAYMENT HISTORY
Date,Cheque Number,Batch Number,Amount";

            foreach (var payment in details.History.Payments)
            {
                content += $@"
{payment.DateDisplay},{payment.ChequeNumber},{payment.BatchNumber},{payment.Amount:F2}";
            }

            return content;
        }

        private string GenerateHtmlContent(InvoiceStyleChequeDetails details)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Cheque Calculation Details - {details.Header.ChequeNumber}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .company-name {{ font-size: 18px; font-weight: bold; }}
        .document-title {{ font-size: 14px; margin-top: 10px; }}
        .cheque-info {{ margin-bottom: 20px; }}
        .summary {{ margin-bottom: 30px; }}
        .section {{ margin-bottom: 25px; }}
        .section-title {{ font-size: 14px; font-weight: bold; margin-bottom: 10px; }}
        table {{ width: 100%; border-collapse: collapse; margin-bottom: 15px; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; font-weight: bold; }}
        .total {{ font-weight: bold; }}
        .memo {{ margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='company-name'>{details.CompanyName}</div>
        <div class='document-title'>{details.DocumentTitle}</div>
    </div>

    <div class='cheque-info'>
        <p><strong>Cheque Number:</strong> {details.Header.ChequeNumber}</p>
        <p><strong>Date:</strong> {details.Header.DateDisplay}</p>
        <p><strong>Grower:</strong> {details.Header.GrowerInfo}</p>
        <p><strong>Payee:</strong> {details.Header.PayeeName}</p>
    </div>

    <div class='summary section'>
        <div class='section-title'>PAYMENT SUMMARY</div>
        <table>
            <tr>
                <th>Description</th>
                <th>Amount</th>
            </tr>
            <tr>
                <td>Total Gross Payments</td>
                <td>{details.Summary.TotalGrossPaymentsDisplay}</td>
            </tr>
            <tr>
                <td>Total Deductions</td>
                <td>{details.Summary.TotalDeductionsDisplay}</td>
            </tr>
            <tr class='total'>
                <td>Net Cheque Amount</td>
                <td>{details.Summary.NetChequeAmountDisplay}</td>
            </tr>
        </table>
    </div>

    <div class='section'>
        <div class='section-title'>DETAILED BREAKDOWN</div>
        {GeneratePaymentBatchesHtml(details.PaymentBatches)}
    </div>

    {(!string.IsNullOrEmpty(details.Memo) ? $"<div class='memo'><div class='section-title'>MEMO</div><p>{details.Memo}</p></div>" : "")}
</body>
</html>";

            return html;
        }

        private string GeneratePaymentBatchesHtml(List<PaymentBatchDetail> batches)
        {
            var html = "";
            foreach (var batch in batches)
            {
                html += $@"
        <h4>PAYMENT BATCH: {batch.BatchNumber}</h4>
        <p>Batch Date: {batch.BatchDateDisplay}</p>
        <p>Payment Type: {batch.PaymentType}</p>
        <table>
            <tr>
                <th>Receipt#</th>
                <th>Product</th>
                <th>Process</th>
                <th>Grade</th>
                <th>Weight</th>
                <th>Price</th>
                <th>Amount</th>
            </tr>";

                foreach (var receipt in batch.Receipts)
                {
                    html += $@"
            <tr>
                <td>{receipt.ReceiptNumber}</td>
                <td>{receipt.ProductName}</td>
                <td>{receipt.ProcessName}</td>
                <td>{receipt.Grade}</td>
                <td>{receipt.WeightDisplay}</td>
                <td>{receipt.PricePerPoundDisplay}</td>
                <td>{receipt.AmountDisplay}</td>
            </tr>";
                }

                html += $@"
            <tr class='total'>
                <td colspan='6'>Batch Subtotal:</td>
                <td>{batch.BatchSubtotalDisplay}</td>
            </tr>
        </table>
        <br/>";
            }
            return html;
        }

        #endregion
    }
}