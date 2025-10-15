using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.XlsIO;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for exporting payment batch data to Excel and PDF formats
    /// </summary>
    public class PaymentBatchExportService : IPaymentBatchExportService
    {
        private readonly IPaymentService _paymentService;
        private readonly IPaymentBatchManagementService _batchService;
        private readonly IChequeGenerationService _chequeService;

        public PaymentBatchExportService(
            IPaymentService paymentService,
            IPaymentBatchManagementService batchService,
            IChequeGenerationService chequeService)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
            _chequeService = chequeService ?? throw new ArgumentNullException(nameof(chequeService));
        }

        /// <summary>
        /// Export grower payments to Excel
        /// </summary>
        public async Task<bool> ExportGrowerPaymentsToExcelAsync(int batchId, string filePath)
        {
            try
            {
                Logger.Info($"Starting Excel export of grower payments for batch {batchId}");

                // Get data
                var summary = await _batchService.GetBatchSummaryAsync(batchId);
                var growerPayments = await _paymentService.GetGrowerPaymentsForBatchAsync(batchId);

                if (summary == null)
                {
                    Logger.Error($"Batch summary {batchId} not found");
                    return false;
                }

                // Create Excel workbook
                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Excel2016;
                    IWorkbook workbook = application.Workbooks.Create(1);
                    IWorksheet sheet = workbook.Worksheets[0];
                    sheet.Name = "Grower Payments";

                    // Add report header
                    sheet.Range["A1"].Text = "Berry Farm Management System";
                    sheet.Range["A1"].CellStyle.Font.Bold = true;
                    sheet.Range["A1"].CellStyle.Font.Size = 16;
                    sheet.Range["A1"].CellStyle.Font.Color = ExcelKnownColors.Blue;

                    sheet.Range["A2"].Text = $"Grower Payments - Batch {summary.BatchNumber}";
                    sheet.Range["A2"].CellStyle.Font.Bold = true;
                    sheet.Range["A2"].CellStyle.Font.Size = 14;

                    sheet.Range["A3"].Text = $"Payment Type: {summary.PaymentTypeName} | Date: {summary.BatchDate:MMM dd, yyyy} | Status: {summary.Status}";
                    sheet.Range["A3"].CellStyle.Font.Size = 11;

                    sheet.Range["A4"].Text = $"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm:ss}";
                    sheet.Range["A4"].CellStyle.Font.Size = 10;
                    sheet.Range["A4"].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;

                    // Column headers (Row 6)
                    int headerRow = 6;
                    sheet.Range[$"A{headerRow}"].Text = "Grower #";
                    sheet.Range[$"B{headerRow}"].Text = "Grower Name";
                    sheet.Range[$"C{headerRow}"].Text = "Receipts";
                    sheet.Range[$"D{headerRow}"].Text = "Weight (lbs)";
                    sheet.Range[$"E{headerRow}"].Text = "Amount";
                    sheet.Range[$"F{headerRow}"].Text = "Cheque #";
                    sheet.Range[$"G{headerRow}"].Text = "Payment Method";
                    sheet.Range[$"H{headerRow}"].Text = "Status";

                    // Format header row
                    var headerRange = sheet.Range[$"A{headerRow}:H{headerRow}"];
                    headerRange.CellStyle.Font.Bold = true;
                    headerRange.CellStyle.Color = Color.FromArgb(79, 129, 189);
                    headerRange.CellStyle.Font.Color = ExcelKnownColors.White;
                    headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                    // Add data rows
                    int currentRow = headerRow + 1;
                    decimal totalAmount = 0;
                    decimal totalWeight = 0;
                    int totalReceipts = 0;

                    foreach (var payment in growerPayments.OrderBy(p => p.GrowerNumber))
                    {
                        sheet.Range[$"A{currentRow}"].Text = payment.GrowerNumber;
                        sheet.Range[$"B{currentRow}"].Text = payment.GrowerName;
                        sheet.Range[$"C{currentRow}"].Number = payment.ReceiptCount;
                        sheet.Range[$"D{currentRow}"].Number = (double)payment.TotalWeight;
                        sheet.Range[$"E{currentRow}"].Number = (double)payment.TotalAmount;
                        sheet.Range[$"E{currentRow}"].NumberFormat = "$#,##0.00";
                        sheet.Range[$"F{currentRow}"].Text = payment.ChequeNumber;
                        sheet.Range[$"G{currentRow}"].Text = payment.PaymentMethodName;
                        sheet.Range[$"H{currentRow}"].Text = payment.IsOnHold ? "On-Hold" : "Active";

                        // Color code status
                        if (payment.IsOnHold)
                        {
                            sheet.Range[$"H{currentRow}"].CellStyle.Color = Color.FromArgb(255, 235, 59);
                        }

                        totalAmount += payment.TotalAmount;
                        totalWeight += payment.TotalWeight;
                        totalReceipts += payment.ReceiptCount;

                        currentRow++;
                    }

                    // Add totals row
                    int totalsRow = currentRow + 1;
                    sheet.Range[$"A{totalsRow}"].Text = "TOTALS";
                    sheet.Range[$"A{totalsRow}"].CellStyle.Font.Bold = true;
                    sheet.Range[$"C{totalsRow}"].Number = totalReceipts;
                    sheet.Range[$"C{totalsRow}"].CellStyle.Font.Bold = true;
                    sheet.Range[$"D{totalsRow}"].Number = (double)totalWeight;
                    sheet.Range[$"D{totalsRow}"].CellStyle.Font.Bold = true;
                    sheet.Range[$"E{totalsRow}"].Number = (double)totalAmount;
                    sheet.Range[$"E{totalsRow}"].NumberFormat = "$#,##0.00";
                    sheet.Range[$"E{totalsRow}"].CellStyle.Font.Bold = true;
                    sheet.Range[$"E{totalsRow}"].CellStyle.Color = Color.FromArgb(146, 208, 80);

                    // Auto-fit columns
                    sheet.UsedRange.AutofitColumns();

                    // Freeze header row
                    sheet.Range[$"A{headerRow + 1}"].FreezePanes();

                    // Save workbook
                    workbook.SaveAs(filePath);

                    Logger.Info($"Successfully exported {growerPayments.Count} grower payments to {filePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting grower payments for batch {batchId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Export receipt allocations to Excel
        /// </summary>
        public async Task<bool> ExportReceiptAllocationsToExcelAsync(int batchId, string filePath)
        {
            try
            {
                Logger.Info($"Starting Excel export of receipt allocations for batch {batchId}");

                // Get data
                var summary = await _batchService.GetBatchSummaryAsync(batchId);
                var allocations = await _paymentService.GetBatchAllocationsAsync(batchId);

                if (summary == null)
                {
                    Logger.Error($"Batch summary {batchId} not found");
                    return false;
                }

                // Create Excel workbook
                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Excel2016;
                    IWorkbook workbook = application.Workbooks.Create(1);
                    IWorksheet sheet = workbook.Worksheets[0];
                    sheet.Name = "Receipt Allocations";

                    // Add report header
                    sheet.Range["A1"].Text = "Berry Farm Management System";
                    sheet.Range["A1"].CellStyle.Font.Bold = true;
                    sheet.Range["A1"].CellStyle.Font.Size = 16;
                    sheet.Range["A1"].CellStyle.Font.Color = ExcelKnownColors.Blue;

                    sheet.Range["A2"].Text = $"Receipt Allocations - Batch {summary.BatchNumber}";
                    sheet.Range["A2"].CellStyle.Font.Bold = true;
                    sheet.Range["A2"].CellStyle.Font.Size = 14;

                    sheet.Range["A3"].Text = $"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm:ss}";
                    sheet.Range["A3"].CellStyle.Font.Size = 10;
                    sheet.Range["A3"].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;

                    // Column headers (Row 5)
                    int headerRow = 5;
                    sheet.Range[$"A{headerRow}"].Text = "Receipt #";
                    sheet.Range[$"B{headerRow}"].Text = "Date";
                    sheet.Range[$"C{headerRow}"].Text = "Grower";
                    sheet.Range[$"D{headerRow}"].Text = "Weight (lbs)";
                    sheet.Range[$"E{headerRow}"].Text = "Price/lb";
                    sheet.Range[$"F{headerRow}"].Text = "Amount";
                    sheet.Range[$"G{headerRow}"].Text = "Payment Type";

                    // Format header row
                    var headerRange = sheet.Range[$"A{headerRow}:G{headerRow}"];
                    headerRange.CellStyle.Font.Bold = true;
                    headerRange.CellStyle.Color = Color.FromArgb(79, 129, 189);
                    headerRange.CellStyle.Font.Color = ExcelKnownColors.White;
                    headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                    // Add data rows
                    int currentRow = headerRow + 1;
                    decimal totalAmount = 0;
                    decimal totalWeight = 0;

                    foreach (var allocation in allocations.OrderBy(a => a.GrowerName).ThenBy(a => a.ReceiptNumber))
                    {
                        sheet.Range[$"A{currentRow}"].Text = allocation.ReceiptNumber ?? "";
                        sheet.Range[$"B{currentRow}"].DateTime = allocation.AllocatedAt;
                        sheet.Range[$"B{currentRow}"].NumberFormat = "MMM dd, yyyy";
                        sheet.Range[$"C{currentRow}"].Text = allocation.GrowerName ?? "";
                        sheet.Range[$"D{currentRow}"].Number = (double)allocation.QuantityPaid;
                        sheet.Range[$"E{currentRow}"].Number = (double)allocation.PricePerPound;
                        sheet.Range[$"E{currentRow}"].NumberFormat = "$#,##0.0000";
                        sheet.Range[$"F{currentRow}"].Number = (double)allocation.AmountPaid;
                        sheet.Range[$"F{currentRow}"].NumberFormat = "$#,##0.00";
                        sheet.Range[$"G{currentRow}"].Text = allocation.PaymentTypeName ?? "";

                        totalAmount += allocation.AmountPaid;
                        totalWeight += allocation.QuantityPaid;

                        currentRow++;
                    }

                    // Add totals row
                    int totalsRow = currentRow + 1;
                    sheet.Range[$"A{totalsRow}"].Text = "TOTALS";
                    sheet.Range[$"A{totalsRow}"].CellStyle.Font.Bold = true;
                    sheet.Range[$"D{totalsRow}"].Number = (double)totalWeight;
                    sheet.Range[$"D{totalsRow}"].CellStyle.Font.Bold = true;
                    sheet.Range[$"F{totalsRow}"].Number = (double)totalAmount;
                    sheet.Range[$"F{totalsRow}"].NumberFormat = "$#,##0.00";
                    sheet.Range[$"F{totalsRow}"].CellStyle.Font.Bold = true;
                    sheet.Range[$"F{totalsRow}"].CellStyle.Color = Color.FromArgb(146, 208, 80);

                    // Auto-fit columns
                    sheet.UsedRange.AutofitColumns();

                    // Freeze header row
                    sheet.Range[$"A{headerRow + 1}"].FreezePanes();

                    // Save workbook
                    workbook.SaveAs(filePath);

                    Logger.Info($"Successfully exported {allocations.Count} receipt allocations to {filePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting receipt allocations for batch {batchId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Export cheques to Excel
        /// </summary>
        public async Task<bool> ExportChequesToExcelAsync(int batchId, string filePath)
        {
            try
            {
                Logger.Info($"Starting Excel export of cheques for batch {batchId}");

                // Get data
                var summary = await _batchService.GetBatchSummaryAsync(batchId);
                var cheques = await _chequeService.GetBatchChequesAsync(batchId);

                if (summary == null)
                {
                    Logger.Error($"Batch summary {batchId} not found");
                    return false;
                }

                // Create Excel workbook
                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Excel2016;
                    IWorkbook workbook = application.Workbooks.Create(1);
                    IWorksheet sheet = workbook.Worksheets[0];
                    sheet.Name = "Cheques";

                    // Add report header
                    sheet.Range["A1"].Text = "Berry Farm Management System";
                    sheet.Range["A1"].CellStyle.Font.Bold = true;
                    sheet.Range["A1"].CellStyle.Font.Size = 16;
                    sheet.Range["A1"].CellStyle.Font.Color = ExcelKnownColors.Blue;

                    sheet.Range["A2"].Text = $"Cheques - Batch {summary.BatchNumber}";
                    sheet.Range["A2"].CellStyle.Font.Bold = true;
                    sheet.Range["A2"].CellStyle.Font.Size = 14;

                    sheet.Range["A3"].Text = $"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm:ss}";
                    sheet.Range["A3"].CellStyle.Font.Size = 10;
                    sheet.Range["A3"].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;

                    // Column headers (Row 5)
                    int headerRow = 5;
                    sheet.Range[$"A{headerRow}"].Text = "Cheque #";
                    sheet.Range[$"B{headerRow}"].Text = "Grower Name";
                    sheet.Range[$"C{headerRow}"].Text = "Amount";
                    sheet.Range[$"D{headerRow}"].Text = "Status";
                    sheet.Range[$"E{headerRow}"].Text = "Created Date";

                    // Format header row
                    var headerRange = sheet.Range[$"A{headerRow}:E{headerRow}"];
                    headerRange.CellStyle.Font.Bold = true;
                    headerRange.CellStyle.Color = Color.FromArgb(79, 129, 189);
                    headerRange.CellStyle.Font.Color = ExcelKnownColors.White;
                    headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                    // Add data rows
                    int currentRow = headerRow + 1;
                    decimal totalAmount = 0;

                    foreach (var cheque in cheques.OrderBy(c => c.ChequeNumber))
                    {
                        // Format cheque number with series
                        string chequeNum = cheque.SeriesCode != null ? 
                            $"{cheque.SeriesCode}-{cheque.ChequeNumber}" : 
                            cheque.ChequeNumber.ToString();
                            
                        sheet.Range[$"A{currentRow}"].Text = chequeNum;
                        sheet.Range[$"B{currentRow}"].Text = cheque.GrowerName ?? "";
                        sheet.Range[$"C{currentRow}"].Number = (double)cheque.ChequeAmount;
                        sheet.Range[$"C{currentRow}"].NumberFormat = "$#,##0.00";
                        sheet.Range[$"D{currentRow}"].Text = cheque.Status ?? "Issued";

                        // Color code by status
                        var status = cheque.Status ?? "Issued";
                        switch (status)
                        {
                            case "Issued":
                                sheet.Range[$"D{currentRow}"].CellStyle.Color = Color.FromArgb(76, 175, 80);
                                break;
                            case "Cleared":
                                sheet.Range[$"D{currentRow}"].CellStyle.Color = Color.FromArgb(33, 150, 243);
                                break;
                            case "Voided":
                                sheet.Range[$"D{currentRow}"].CellStyle.Color = Color.FromArgb(244, 67, 54);
                                sheet.Range[$"D{currentRow}"].CellStyle.Font.Color = ExcelKnownColors.White;
                                break;
                        }

                        sheet.Range[$"E{currentRow}"].DateTime = cheque.CreatedAt;
                        sheet.Range[$"E{currentRow}"].NumberFormat = "MMM dd, yyyy";

                        if (cheque.Status != "Voided")
                        {
                            totalAmount += cheque.ChequeAmount;
                        }

                        currentRow++;
                    }

                    // Add totals row
                    int totalsRow = currentRow + 1;
                    sheet.Range[$"A{totalsRow}"].Text = "TOTAL (Excluding Voided)";
                    sheet.Range[$"A{totalsRow}"].CellStyle.Font.Bold = true;
                    sheet.Range[$"C{totalsRow}"].Number = (double)totalAmount;
                    sheet.Range[$"C{totalsRow}"].NumberFormat = "$#,##0.00";
                    sheet.Range[$"C{totalsRow}"].CellStyle.Font.Bold = true;
                    sheet.Range[$"C{totalsRow}"].CellStyle.Color = Color.FromArgb(146, 208, 80);

                    // Auto-fit columns
                    sheet.UsedRange.AutofitColumns();

                    // Freeze header row
                    sheet.Range[$"A{headerRow + 1}"].FreezePanes();

                    // Save workbook
                    workbook.SaveAs(filePath);

                    Logger.Info($"Successfully exported {cheques.Count} cheques to {filePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting cheques for batch {batchId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Export complete batch to multi-sheet Excel workbook
        /// </summary>
        public async Task<bool> ExportCompleteBatchToExcelAsync(int batchId, string filePath)
        {
            try
            {
                Logger.Info($"Starting complete Excel export for batch {batchId}");

                // Get all data
                var summary = await _batchService.GetBatchSummaryAsync(batchId);
                var growerPayments = await _paymentService.GetGrowerPaymentsForBatchAsync(batchId);
                var allocations = await _paymentService.GetBatchAllocationsAsync(batchId);
                var cheques = await _chequeService.GetBatchChequesAsync(batchId);
                var analytics = await _paymentService.CalculateBatchAnalyticsAsync(batchId);

                if (summary == null)
                {
                    Logger.Error($"Batch summary {batchId} not found");
                    return false;
                }

                // Create Excel workbook with multiple sheets
                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Excel2016;
                    IWorkbook workbook = application.Workbooks.Create(5);

                    // Sheet 1: Batch Summary
                    CreateBatchSummarySheet(workbook.Worksheets[0], summary, analytics);

                    // Sheet 2: Grower Payments
                    CreateGrowerPaymentsSheet(workbook.Worksheets[1], summary, growerPayments);

                    // Sheet 3: Receipt Allocations
                    CreateReceiptAllocationsSheet(workbook.Worksheets[2], summary, allocations);

                    // Sheet 4: Cheques
                    CreateChequesSheet(workbook.Worksheets[3], summary, cheques);

                    // Sheet 5: Analytics
                    CreateAnalyticsSheet(workbook.Worksheets[4], summary, analytics);

                    // Save workbook
                    workbook.SaveAs(filePath);

                    Logger.Info($"Successfully exported complete batch to {filePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting complete batch {batchId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Export batch summary to PDF
        /// </summary>
        public async Task<bool> ExportBatchSummaryToPdfAsync(int batchId, string filePath)
        {
            try
            {
                Logger.Info($"Starting PDF export for batch {batchId}");

                // Get data
                var summary = await _batchService.GetBatchSummaryAsync(batchId);
                var growerPayments = await _paymentService.GetGrowerPaymentsForBatchAsync(batchId);
                var analytics = await _paymentService.CalculateBatchAnalyticsAsync(batchId);

                if (summary == null)
                {
                    Logger.Error($"Batch summary {batchId} not found");
                    return false;
                }

                // Create PDF document
                using (PdfDocument document = new PdfDocument())
                {
                    // Page 1: Batch Summary
                    PdfPage page1 = document.Pages.Add();
                    PdfGraphics graphics1 = page1.Graphics;

                    // Fonts
                    PdfFont titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 20, PdfFontStyle.Bold);
                    PdfFont headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
                    PdfFont subHeaderFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
                    PdfFont normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);

                    float yPosition = 0;

                    // Title
                    graphics1.DrawString("Berry Farm Management System", titleFont, PdfBrushes.DarkBlue, new PointF(0, yPosition));
                    yPosition += 30;

                    graphics1.DrawString($"Payment Batch Summary Report", headerFont, PdfBrushes.Black, new PointF(0, yPosition));
                    yPosition += 25;

                    // Batch Info
                    graphics1.DrawString($"Batch Number: {summary.BatchNumber}", subHeaderFont, PdfBrushes.Black, new PointF(0, yPosition));
                    yPosition += 20;
                    graphics1.DrawString($"Payment Type: {summary.PaymentTypeName}", normalFont, PdfBrushes.Black, new PointF(0, yPosition));
                    yPosition += 15;
                    graphics1.DrawString($"Batch Date: {summary.BatchDate:MMMM dd, yyyy}", normalFont, PdfBrushes.Black, new PointF(0, yPosition));
                    yPosition += 15;
                    graphics1.DrawString($"Crop Year: {summary.CropYear}", normalFont, PdfBrushes.Black, new PointF(0, yPosition));
                    yPosition += 15;
                    graphics1.DrawString($"Status: {summary.Status}", normalFont, PdfBrushes.Black, new PointF(0, yPosition));
                    yPosition += 30;

                    // Financial Summary
                    graphics1.DrawString("Financial Summary", subHeaderFont, PdfBrushes.Black, new PointF(0, yPosition));
                    yPosition += 20;

                    graphics1.DrawString($"Total Amount: {summary.TotalAmount:C2}", normalFont, PdfBrushes.Black, new PointF(20, yPosition));
                    yPosition += 15;
                    graphics1.DrawString($"Total Growers: {summary.TotalGrowers}", normalFont, PdfBrushes.Black, new PointF(20, yPosition));
                    yPosition += 15;
                    graphics1.DrawString($"Total Receipts: {summary.TotalReceipts}", normalFont, PdfBrushes.Black, new PointF(20, yPosition));
                    yPosition += 30;

                    // Analytics Summary
                    if (analytics != null)
                    {
                        graphics1.DrawString("Analytics Overview", subHeaderFont, PdfBrushes.Black, new PointF(0, yPosition));
                        yPosition += 20;

                        graphics1.DrawString($"Average Payment per Grower: {analytics.AveragePaymentPerGrower:C2}", normalFont, PdfBrushes.Black, new PointF(20, yPosition));
                        yPosition += 15;
                        graphics1.DrawString($"Largest Payment: {analytics.LargestPayment:C2}", normalFont, PdfBrushes.Black, new PointF(20, yPosition));
                        yPosition += 15;
                        graphics1.DrawString($"Payment Range: {analytics.PaymentRange}", normalFont, PdfBrushes.Black, new PointF(20, yPosition));
                        yPosition += 15;
                        graphics1.DrawString($"Anomalies Detected: {analytics.AnomalyCount}", normalFont, PdfBrushes.Black, new PointF(20, yPosition));
                        yPosition += 30;
                    }

                    // Page 2: Grower Payments Table
                    PdfPage page2 = document.Pages.Add();
                    PdfGraphics graphics2 = page2.Graphics;

                    yPosition = 0;
                    graphics2.DrawString("Grower Payments", headerFont, PdfBrushes.Black, new PointF(0, yPosition));
                    yPosition += 25;

                    // Create PdfGrid for grower payments
                    PdfGrid paymentGrid = new PdfGrid();
                    
                    // Add columns
                    paymentGrid.Columns.Add(8);
                    paymentGrid.Columns[0].Width = 60;  // Grower #
                    paymentGrid.Columns[1].Width = 120; // Name
                    paymentGrid.Columns[2].Width = 50;  // Receipts
                    paymentGrid.Columns[3].Width = 70;  // Weight
                    paymentGrid.Columns[4].Width = 70;  // Amount
                    paymentGrid.Columns[5].Width = 80;  // Cheque #
                    paymentGrid.Columns[6].Width = 80;  // Payment Method
                    paymentGrid.Columns[7].Width = 60;  // Status

                    // Add header row
                    PdfGridRow headerRow1 = paymentGrid.Headers.Add(1)[0];
                    headerRow1.Cells[0].Value = "Grower #";
                    headerRow1.Cells[1].Value = "Name";
                    headerRow1.Cells[2].Value = "Receipts";
                    headerRow1.Cells[3].Value = "Weight";
                    headerRow1.Cells[4].Value = "Amount";
                    headerRow1.Cells[5].Value = "Cheque #";
                    headerRow1.Cells[6].Value = "Payment Method";
                    headerRow1.Cells[7].Value = "Status";

                    // Format header
                    PdfGridCellStyle headerStyle = new PdfGridCellStyle();
                    headerStyle.BackgroundBrush = new PdfSolidBrush(new PdfColor(79, 129, 189));
                    headerStyle.TextBrush = PdfBrushes.White;
                    headerStyle.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
                    
                    for (int i = 0; i < headerRow1.Cells.Count; i++)
                    {
                        headerRow1.Cells[i].Style = headerStyle;
                    }

                    // Add data rows
                    foreach (var payment in growerPayments)
                    {
                        PdfGridRow row = paymentGrid.Rows.Add();
                        row.Cells[0].Value = payment.GrowerNumber;
                        row.Cells[1].Value = payment.GrowerName;
                        row.Cells[2].Value = payment.ReceiptCount.ToString();
                        row.Cells[3].Value = $"{payment.TotalWeight:N2}";
                        row.Cells[4].Value = $"{payment.TotalAmount:C2}";
                        row.Cells[5].Value = payment.ChequeNumber;
                        row.Cells[6].Value = payment.PaymentMethodName;
                        row.Cells[7].Value = payment.IsOnHold ? "On-Hold" : "Active";
                    }

                    // Draw grid
                    paymentGrid.Draw(page2, new PointF(0, yPosition));

                    // Add footer with timestamp
                    graphics2.DrawString($"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm:ss}", 
                        new PdfStandardFont(PdfFontFamily.Helvetica, 8), 
                        PdfBrushes.Gray, 
                        new PointF(0, page2.Graphics.ClientSize.Height - 20));

                    // Save PDF
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        document.Save(stream);
                    }

                    Logger.Info($"Successfully exported batch summary to PDF: {filePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting batch {batchId} to PDF", ex);
                return false;
            }
        }

        #region Helper Methods for Multi-Sheet Excel Export

        private void CreateBatchSummarySheet(IWorksheet sheet, PaymentBatchSummary summary, PaymentBatchAnalytics analytics)
        {
            sheet.Name = "Batch Summary";

            // Header
            sheet.Range["A1"].Text = "Berry Farm Management System";
            sheet.Range["A1"].CellStyle.Font.Bold = true;
            sheet.Range["A1"].CellStyle.Font.Size = 18;
            sheet.Range["A1"].CellStyle.Font.Color = ExcelKnownColors.Blue;

            sheet.Range["A2"].Text = "Payment Batch Summary";
            sheet.Range["A2"].CellStyle.Font.Bold = true;
            sheet.Range["A2"].CellStyle.Font.Size = 14;

            // Batch Information
            int row = 4;
            sheet.Range[$"A{row}"].Text = "Batch Number:";
            sheet.Range[$"B{row}"].Text = summary.BatchNumber;
            sheet.Range[$"B{row}"].CellStyle.Font.Bold = true;
            row++;

            sheet.Range[$"A{row}"].Text = "Payment Type:";
            sheet.Range[$"B{row}"].Text = summary.PaymentTypeName;
            row++;

            sheet.Range[$"A{row}"].Text = "Batch Date:";
            sheet.Range[$"B{row}"].DateTime = summary.BatchDate;
            sheet.Range[$"B{row}"].NumberFormat = "MMMM dd, yyyy";
            row++;

            sheet.Range[$"A{row}"].Text = "Crop Year:";
            sheet.Range[$"B{row}"].Number = summary.CropYear;
            row++;

            sheet.Range[$"A{row}"].Text = "Status:";
            sheet.Range[$"B{row}"].Text = summary.Status;
            row += 2;

            // Financial Summary
            sheet.Range[$"A{row}"].Text = "Financial Summary";
            sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}"].CellStyle.Font.Size = 12;
            row++;

            sheet.Range[$"A{row}"].Text = "Total Amount:";
            sheet.Range[$"B{row}"].Number = (double)summary.TotalAmount;
            sheet.Range[$"B{row}"].NumberFormat = "$#,##0.00";
            row++;

            sheet.Range[$"A{row}"].Text = "Total Growers:";
            sheet.Range[$"B{row}"].Number = summary.TotalGrowers;
            row++;

            sheet.Range[$"A{row}"].Text = "Total Receipts:";
            sheet.Range[$"B{row}"].Number = summary.TotalReceipts;
            row += 2;

            // Analytics Summary
            if (analytics != null)
            {
                sheet.Range[$"A{row}"].Text = "Analytics";
                sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
                sheet.Range[$"A{row}"].CellStyle.Font.Size = 12;
                row++;

                sheet.Range[$"A{row}"].Text = "Avg Payment/Grower:";
                sheet.Range[$"B{row}"].Number = (double)analytics.AveragePaymentPerGrower;
                sheet.Range[$"B{row}"].NumberFormat = "$#,##0.00";
                row++;

                sheet.Range[$"A{row}"].Text = "Largest Payment:";
                sheet.Range[$"B{row}"].Number = (double)analytics.LargestPayment;
                sheet.Range[$"B{row}"].NumberFormat = "$#,##0.00";
                row++;

                sheet.Range[$"A{row}"].Text = "Payment Range:";
                sheet.Range[$"B{row}"].Text = analytics.PaymentRange;
                row++;

                sheet.Range[$"A{row}"].Text = "Total Weight:";
                sheet.Range[$"B{row}"].Number = (double)analytics.TotalWeight;
                sheet.Range[$"B{row}"].NumberFormat = "#,##0.00";
                row++;

                sheet.Range[$"A{row}"].Text = "Anomalies:";
                sheet.Range[$"B{row}"].Number = analytics.AnomalyCount;
                row++;
            }

            // Auto-fit columns
            sheet.UsedRange.AutofitColumns();
        }

        private void CreateGrowerPaymentsSheet(IWorksheet sheet, PaymentBatchSummary summary, List<GrowerPaymentSummary> growerPayments)
        {
            sheet.Name = "Grower Payments";

            // Header
            sheet.Range["A1"].Text = $"Grower Payments - {summary.BatchNumber}";
            sheet.Range["A1"].CellStyle.Font.Bold = true;
            sheet.Range["A1"].CellStyle.Font.Size = 14;

            // Column headers
            int headerRow = 3;
            sheet.Range[$"A{headerRow}"].Text = "Grower #";
            sheet.Range[$"B{headerRow}"].Text = "Name";
            sheet.Range[$"C{headerRow}"].Text = "Receipts";
            sheet.Range[$"D{headerRow}"].Text = "Weight (lbs)";
            sheet.Range[$"E{headerRow}"].Text = "Amount";
            sheet.Range[$"F{headerRow}"].Text = "Cheque #";
            sheet.Range[$"G{headerRow}"].Text = "Payment Method";
            sheet.Range[$"H{headerRow}"].Text = "Status";

            // Format headers
            var headerRange = sheet.Range[$"A{headerRow}:H{headerRow}"];
            headerRange.CellStyle.Font.Bold = true;
            headerRange.CellStyle.Color = Color.FromArgb(79, 129, 189);
            headerRange.CellStyle.Font.Color = ExcelKnownColors.White;

            // Data rows
            int currentRow = headerRow + 1;
            foreach (var payment in growerPayments)
            {
                sheet.Range[$"A{currentRow}"].Text = payment.GrowerNumber;
                sheet.Range[$"B{currentRow}"].Text = payment.GrowerName;
                sheet.Range[$"C{currentRow}"].Number = payment.ReceiptCount;
                sheet.Range[$"D{currentRow}"].Number = (double)payment.TotalWeight;
                sheet.Range[$"E{currentRow}"].Number = (double)payment.TotalAmount;
                sheet.Range[$"E{currentRow}"].NumberFormat = "$#,##0.00";
                sheet.Range[$"F{currentRow}"].Text = payment.ChequeNumber;
                sheet.Range[$"G{currentRow}"].Text = payment.PaymentMethodName;
                sheet.Range[$"H{currentRow}"].Text = payment.IsOnHold ? "On-Hold" : "Active";
                currentRow++;
            }

            sheet.UsedRange.AutofitColumns();
            sheet.Range[$"A{headerRow + 1}"].FreezePanes();
        }

        private void CreateReceiptAllocationsSheet(IWorksheet sheet, PaymentBatchSummary summary, List<ReceiptPaymentAllocation> allocations)
        {
            sheet.Name = "Receipt Allocations";

            // Header
            sheet.Range["A1"].Text = $"Receipt Allocations - {summary.BatchNumber}";
            sheet.Range["A1"].CellStyle.Font.Bold = true;
            sheet.Range["A1"].CellStyle.Font.Size = 14;

            // Column headers
            int headerRow = 3;
            sheet.Range[$"A{headerRow}"].Text = "Receipt #";
            sheet.Range[$"B{headerRow}"].Text = "Date";
            sheet.Range[$"C{headerRow}"].Text = "Grower";
            sheet.Range[$"D{headerRow}"].Text = "Weight (lbs)";
            sheet.Range[$"E{headerRow}"].Text = "Price/lb";
            sheet.Range[$"F{headerRow}"].Text = "Amount";
            sheet.Range[$"G{headerRow}"].Text = "Payment Type";

            // Format headers
            var headerRange = sheet.Range[$"A{headerRow}:G{headerRow}"];
            headerRange.CellStyle.Font.Bold = true;
            headerRange.CellStyle.Color = Color.FromArgb(79, 129, 189);
            headerRange.CellStyle.Font.Color = ExcelKnownColors.White;

            // Data rows
            int currentRow = headerRow + 1;
            foreach (var allocation in allocations)
            {
                sheet.Range[$"A{currentRow}"].Text = allocation.ReceiptNumber ?? "";
                sheet.Range[$"B{currentRow}"].DateTime = allocation.AllocatedAt;
                sheet.Range[$"B{currentRow}"].NumberFormat = "MMM dd, yyyy";
                sheet.Range[$"C{currentRow}"].Text = allocation.GrowerName ?? "";
                sheet.Range[$"D{currentRow}"].Number = (double)allocation.QuantityPaid;
                sheet.Range[$"E{currentRow}"].Number = (double)allocation.PricePerPound;
                sheet.Range[$"E{currentRow}"].NumberFormat = "$#,##0.0000";
                sheet.Range[$"F{currentRow}"].Number = (double)allocation.AmountPaid;
                sheet.Range[$"F{currentRow}"].NumberFormat = "$#,##0.00";
                sheet.Range[$"G{currentRow}"].Text = allocation.PaymentTypeName ?? "";
                currentRow++;
            }

            sheet.UsedRange.AutofitColumns();
            sheet.Range[$"A{headerRow + 1}"].FreezePanes();
        }

        private void CreateChequesSheet(IWorksheet sheet, PaymentBatchSummary summary, List<Cheque> cheques)
        {
            sheet.Name = "Cheques";

            // Header
            sheet.Range["A1"].Text = $"Cheques - {summary.BatchNumber}";
            sheet.Range["A1"].CellStyle.Font.Bold = true;
            sheet.Range["A1"].CellStyle.Font.Size = 14;

            // Column headers
            int headerRow = 3;
            sheet.Range[$"A{headerRow}"].Text = "Cheque #";
            sheet.Range[$"B{headerRow}"].Text = "Grower";
            sheet.Range[$"C{headerRow}"].Text = "Amount";
            sheet.Range[$"D{headerRow}"].Text = "Status";
            sheet.Range[$"E{headerRow}"].Text = "Created Date";

            // Format headers
            var headerRange = sheet.Range[$"A{headerRow}:E{headerRow}"];
            headerRange.CellStyle.Font.Bold = true;
            headerRange.CellStyle.Color = Color.FromArgb(79, 129, 189);
            headerRange.CellStyle.Font.Color = ExcelKnownColors.White;

            // Data rows
            int currentRow = headerRow + 1;
            foreach (var cheque in cheques)
            {
                // Format cheque number with series
                string chequeNum = cheque.SeriesCode != null ? 
                    $"{cheque.SeriesCode}-{cheque.ChequeNumber}" : 
                    cheque.ChequeNumber.ToString();
                    
                sheet.Range[$"A{currentRow}"].Text = chequeNum;
                sheet.Range[$"B{currentRow}"].Text = cheque.GrowerName ?? "";
                sheet.Range[$"C{currentRow}"].Number = (double)cheque.ChequeAmount;
                sheet.Range[$"C{currentRow}"].NumberFormat = "$#,##0.00";
                sheet.Range[$"D{currentRow}"].Text = cheque.Status ?? "Issued";
                sheet.Range[$"E{currentRow}"].DateTime = cheque.CreatedAt;
                sheet.Range[$"E{currentRow}"].NumberFormat = "MMM dd, yyyy";

                currentRow++;
            }

            sheet.UsedRange.AutofitColumns();
            sheet.Range[$"A{headerRow + 1}"].FreezePanes();
        }

        private void CreateAnalyticsSheet(IWorksheet sheet, PaymentBatchSummary summary, PaymentBatchAnalytics analytics)
        {
            sheet.Name = "Analytics";

            if (analytics == null)
            {
                sheet.Range["A1"].Text = "No analytics data available";
                return;
            }

            // Header
            sheet.Range["A1"].Text = $"Analytics - {summary.BatchNumber}";
            sheet.Range["A1"].CellStyle.Font.Bold = true;
            sheet.Range["A1"].CellStyle.Font.Size = 14;

            int row = 3;

            // Quick Stats
            sheet.Range[$"A{row}"].Text = "Quick Statistics";
            sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}"].CellStyle.Font.Size = 12;
            row++;

            sheet.Range[$"A{row}"].Text = "Average Payment/Grower:";
            sheet.Range[$"B{row}"].Number = (double)analytics.AveragePaymentPerGrower;
            sheet.Range[$"B{row}"].NumberFormat = "$#,##0.00";
            row++;

            sheet.Range[$"A{row}"].Text = "Largest Payment:";
            sheet.Range[$"B{row}"].Number = (double)analytics.LargestPayment;
            sheet.Range[$"B{row}"].NumberFormat = "$#,##0.00";
            row++;

            sheet.Range[$"A{row}"].Text = "Payment Range:";
            sheet.Range[$"B{row}"].Text = analytics.PaymentRange;
            row++;

            sheet.Range[$"A{row}"].Text = "Total Weight:";
            sheet.Range[$"B{row}"].Number = (double)analytics.TotalWeight;
            sheet.Range[$"B{row}"].NumberFormat = "#,##0.00";
            row++;

            sheet.Range[$"A{row}"].Text = "Anomalies Detected:";
            sheet.Range[$"B{row}"].Number = analytics.AnomalyCount;
            row += 2;

            // Payment Distribution
            if (analytics.PaymentDistribution != null && analytics.PaymentDistribution.Count > 0)
            {
                sheet.Range[$"A{row}"].Text = "Payment Distribution";
                sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
                sheet.Range[$"A{row}"].CellStyle.Font.Size = 12;
                row++;

                sheet.Range[$"A{row}"].Text = "Range";
                sheet.Range[$"B{row}"].Text = "Growers";
                sheet.Range[$"C{row}"].Text = "Total Amount";
                sheet.Range[$"D{row}"].Text = "Percentage";
                
                var distHeaderRange = sheet.Range[$"A{row}:D{row}"];
                distHeaderRange.CellStyle.Font.Bold = true;
                distHeaderRange.CellStyle.Color = Color.LightGray;
                row++;

                foreach (var dist in analytics.PaymentDistribution)
                {
                    sheet.Range[$"A{row}"].Text = dist.Range;
                    sheet.Range[$"B{row}"].Number = dist.GrowerCount;
                    sheet.Range[$"C{row}"].Number = (double)dist.TotalAmount;
                    sheet.Range[$"C{row}"].NumberFormat = "$#,##0.00";
                    sheet.Range[$"D{row}"].Number = (double)dist.Percentage;
                    sheet.Range[$"D{row}"].NumberFormat = "0.0%";
                    row++;
                }
                row++;
            }

            // Product Breakdown
            if (analytics.ProductBreakdown != null && analytics.ProductBreakdown.Count > 0)
            {
                sheet.Range[$"A{row}"].Text = "Product Breakdown";
                sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
                sheet.Range[$"A{row}"].CellStyle.Font.Size = 12;
                row++;

                sheet.Range[$"A{row}"].Text = "Product";
                sheet.Range[$"B{row}"].Text = "Weight";
                sheet.Range[$"C{row}"].Text = "Amount";
                sheet.Range[$"D{row}"].Text = "Receipts";
                sheet.Range[$"E{row}"].Text = "Percentage";
                
                var prodHeaderRange = sheet.Range[$"A{row}:E{row}"];
                prodHeaderRange.CellStyle.Font.Bold = true;
                prodHeaderRange.CellStyle.Color = Color.LightGray;
                row++;

                foreach (var product in analytics.ProductBreakdown)
                {
                    sheet.Range[$"A{row}"].Text = product.ProductName;
                    sheet.Range[$"B{row}"].Number = (double)product.TotalWeight;
                    sheet.Range[$"B{row}"].NumberFormat = "#,##0.00";
                    sheet.Range[$"C{row}"].Number = (double)product.Amount;
                    sheet.Range[$"C{row}"].NumberFormat = "$#,##0.00";
                    sheet.Range[$"D{row}"].Number = product.ReceiptCount;
                    sheet.Range[$"E{row}"].Number = (double)product.Percentage;
                    sheet.Range[$"E{row}"].NumberFormat = "0.0%";
                    row++;
                }
            }

            // Auto-fit columns
            sheet.UsedRange.AutofitColumns();
        }

        #endregion
    }
}
