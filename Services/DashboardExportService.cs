using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.XlsIO;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using WPFGrowerApp.DataAccess.Models;
using System.Drawing;

namespace WPFGrowerApp.Services
{
    public class DashboardExportService
    {
        public async Task ExportDashboardAsync(
            DashboardSummary summary,
            IEnumerable<ChartDataPoint> monthlyTrend,
            IEnumerable<ChartDataPoint> growerPerformance,
            IEnumerable<ChartDataPoint> paymentMethodDistribution,
            IEnumerable<ChartDataPoint> provinceDistribution,
            IEnumerable<ChartDataPoint> priceLevelDistribution,
            IEnumerable<Payment> recentPayments,
            string format,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                string defaultFileName = $"Dashboard_Analytics_{DateTime.Now:yyyyMMdd_HHmmss}";
                string filter;
                string fileExtension;

                // Set up file dialog based on format
                switch (format.ToUpper())
                {
                    case "PDF":
                        filter = "PDF files (*.pdf)|*.pdf";
                        fileExtension = "pdf";
                        break;
                    case "EXCEL":
                        filter = "Excel files (*.xlsx)|*.xlsx";
                        fileExtension = "xlsx";
                        break;
                    case "WORD":
                        filter = "Word files (*.docx)|*.docx";
                        fileExtension = "docx";
                        break;
                    case "CSV":
                        filter = "CSV files (*.csv)|*.csv";
                        fileExtension = "csv";
                        break;
                    default:
                        throw new ArgumentException($"Unsupported export format: {format}");
                }

                // Show save dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = filter,
                    DefaultExt = fileExtension,
                    FileName = $"{defaultFileName}.{fileExtension}",
                    Title = $"Save Dashboard Analytics as {format.ToUpper()}"
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    return; // User cancelled
                }

                string filePath = saveFileDialog.FileName;

                // Export based on format
                bool success = format.ToUpper() switch
                {
                    "PDF" => await ExportToPdfAsync(summary, monthlyTrend, growerPerformance, 
                        paymentMethodDistribution, provinceDistribution, priceLevelDistribution, 
                        recentPayments, filePath, startDate, endDate),
                    "EXCEL" => await ExportToExcelAsync(summary, monthlyTrend, growerPerformance, 
                        paymentMethodDistribution, provinceDistribution, priceLevelDistribution, 
                        recentPayments, filePath, startDate, endDate),
                    "WORD" => await ExportToWordAsync(summary, monthlyTrend, growerPerformance, 
                        paymentMethodDistribution, provinceDistribution, priceLevelDistribution, 
                        recentPayments, filePath, startDate, endDate),
                    "CSV" => await ExportToCsvAsync(summary, monthlyTrend, growerPerformance, 
                        paymentMethodDistribution, provinceDistribution, priceLevelDistribution, 
                        recentPayments, filePath, startDate, endDate),
                    _ => false
                };

                if (success)
                {
                    // Open the exported file
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });

                    MessageBox.Show($"Dashboard analytics successfully exported to {Path.GetFileName(filePath)}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting dashboard: {ex.Message}", "Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> ExportToPdfAsync(
            DashboardSummary summary,
            IEnumerable<ChartDataPoint> monthlyTrend,
            IEnumerable<ChartDataPoint> growerPerformance,
            IEnumerable<ChartDataPoint> paymentMethodDistribution,
            IEnumerable<ChartDataPoint> provinceDistribution,
            IEnumerable<ChartDataPoint> priceLevelDistribution,
            IEnumerable<Payment> recentPayments,
            string filePath,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                using (PdfDocument document = new PdfDocument())
                {
                    // Add title page
                    PdfPage titlePage = document.Pages.Add();
                    DrawTitlePage(titlePage, summary, startDate, endDate);

                    // Add summary page
                    PdfPage summaryPage = document.Pages.Add();
                    DrawSummaryPage(summaryPage, summary, startDate, endDate);

                    // Add charts page
                    PdfPage chartsPage = document.Pages.Add();
                    DrawChartsPage(chartsPage, monthlyTrend, growerPerformance, 
                        paymentMethodDistribution, provinceDistribution, priceLevelDistribution);

                    // Add recent activity page
                    PdfPage activityPage = document.Pages.Add();
                    DrawRecentActivityPage(activityPage, recentPayments);

                    // Save the document
                    document.Save(filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating PDF: {ex.Message}", "PDF Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async Task<bool> ExportToExcelAsync(
            DashboardSummary summary,
            IEnumerable<ChartDataPoint> monthlyTrend,
            IEnumerable<ChartDataPoint> growerPerformance,
            IEnumerable<ChartDataPoint> paymentMethodDistribution,
            IEnumerable<ChartDataPoint> provinceDistribution,
            IEnumerable<ChartDataPoint> priceLevelDistribution,
            IEnumerable<Payment> recentPayments,
            string filePath,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Excel2016;
                    IWorkbook workbook = application.Workbooks.Create(1);

                    // Summary sheet
                    IWorksheet summarySheet = workbook.Worksheets[0];
                    summarySheet.Name = "Summary";
                    CreateSummarySheet(summarySheet, summary, startDate, endDate);

                    // Monthly trend sheet
                    IWorksheet trendSheet = workbook.Worksheets.Create("Monthly Trends");
                    CreateTrendSheet(trendSheet, monthlyTrend);

                    // Grower performance sheet
                    IWorksheet performanceSheet = workbook.Worksheets.Create("Grower Performance");
                    CreatePerformanceSheet(performanceSheet, growerPerformance);

                    // Distribution sheets
                    IWorksheet distributionSheet = workbook.Worksheets.Create("Distributions");
                    CreateDistributionSheet(distributionSheet, paymentMethodDistribution, 
                        provinceDistribution, priceLevelDistribution);

                    // Recent activity sheet
                    IWorksheet activitySheet = workbook.Worksheets.Create("Recent Activity");
                    CreateActivitySheet(activitySheet, recentPayments);

                    // Save the workbook
                    workbook.SaveAs(filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating Excel file: {ex.Message}", "Excel Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async Task<bool> ExportToWordAsync(
            DashboardSummary summary,
            IEnumerable<ChartDataPoint> monthlyTrend,
            IEnumerable<ChartDataPoint> growerPerformance,
            IEnumerable<ChartDataPoint> paymentMethodDistribution,
            IEnumerable<ChartDataPoint> provinceDistribution,
            IEnumerable<ChartDataPoint> priceLevelDistribution,
            IEnumerable<Payment> recentPayments,
            string filePath,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                using (WordDocument document = new WordDocument())
                {
                    IWSection section = document.AddSection();

                    // Title
                    IWParagraph titlePara = section.AddParagraph();
                    titlePara.AppendText("Berry Farm Management System - Dashboard Analytics");
                    titlePara.ApplyStyle(BuiltinStyle.Heading1);

                    // Date range
                    IWParagraph datePara = section.AddParagraph();
                    datePara.AppendText($"Report Period: {startDate:MMMM dd, yyyy} to {endDate:MMMM dd, yyyy}");
                    datePara.ApplyStyle(BuiltinStyle.Heading2);

                    // Summary section
                    CreateWordSummarySection(section, summary);

                    // Charts section
                    CreateWordChartsSection(section, monthlyTrend, growerPerformance, 
                        paymentMethodDistribution, provinceDistribution, priceLevelDistribution);

                    // Recent activity section
                    CreateWordActivitySection(section, recentPayments);

                    // Save the document
                    document.Save(filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating Word document: {ex.Message}", "Word Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async Task<bool> ExportToCsvAsync(
            DashboardSummary summary,
            IEnumerable<ChartDataPoint> monthlyTrend,
            IEnumerable<ChartDataPoint> growerPerformance,
            IEnumerable<ChartDataPoint> paymentMethodDistribution,
            IEnumerable<ChartDataPoint> provinceDistribution,
            IEnumerable<ChartDataPoint> priceLevelDistribution,
            IEnumerable<Payment> recentPayments,
            string filePath,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Header
                    writer.WriteLine("Berry Farm Management System - Dashboard Analytics");
                    writer.WriteLine($"Report Period: {startDate:MMMM dd, yyyy} to {endDate:MMMM dd, yyyy}");
                    writer.WriteLine($"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm}");
                    writer.WriteLine();

                    // Summary
                    writer.WriteLine("SUMMARY");
                    writer.WriteLine("Metric,Value");
                    writer.WriteLine($"Total Growers,{summary.TotalGrowers}");
                    writer.WriteLine($"Active Growers,{summary.ActiveGrowers}");
                    writer.WriteLine($"On Hold Growers,{summary.OnHoldGrowers}");
                    writer.WriteLine($"Total Payments,{summary.TotalPayments}");
                    writer.WriteLine($"Total Payment Amount,{summary.TotalPaymentAmount:C}");
                    writer.WriteLine($"Average Payment Amount,{summary.AveragePaymentAmount:C}");
                    writer.WriteLine($"Total Batches,{summary.TotalBatches}");
                    writer.WriteLine($"Completed Batches,{summary.CompletedBatches}");
                    writer.WriteLine($"Pending Batches,{summary.PendingBatches}");
                    writer.WriteLine();

                    // Monthly trend
                    writer.WriteLine("MONTHLY PAYMENT TREND");
                    writer.WriteLine("Month,Amount");
                    foreach (var trend in monthlyTrend)
                    {
                        writer.WriteLine($"{trend.Category},{trend.Value:C}");
                    }
                    writer.WriteLine();

                    // Grower performance
                    writer.WriteLine("TOP GROWER PERFORMANCE");
                    writer.WriteLine("Grower,Total Amount");
                    foreach (var performance in growerPerformance)
                    {
                        writer.WriteLine($"{performance.Category},{performance.Value:C}");
                    }
                    writer.WriteLine();

                    // Payment method distribution
                    writer.WriteLine("PAYMENT METHOD DISTRIBUTION");
                    writer.WriteLine("Method,Count");
                    foreach (var method in paymentMethodDistribution)
                    {
                        writer.WriteLine($"{method.Category},{method.Value}");
                    }
                    writer.WriteLine();

                    // Province distribution
                    writer.WriteLine("PROVINCE DISTRIBUTION");
                    writer.WriteLine("Province,Count");
                    foreach (var province in provinceDistribution)
                    {
                        writer.WriteLine($"{province.Category},{province.Value}");
                    }
                    writer.WriteLine();

                    // Price level distribution
                    writer.WriteLine("PRICE LEVEL DISTRIBUTION");
                    writer.WriteLine("Price Level,Count");
                    foreach (var priceLevel in priceLevelDistribution)
                    {
                        writer.WriteLine($"{priceLevel.Category},{priceLevel.Value}");
                    }
                    writer.WriteLine();

                    // Recent payments
                    writer.WriteLine("RECENT PAYMENTS");
                    writer.WriteLine("Date,Grower,Amount,Type,Status");
                    foreach (var payment in recentPayments)
                    {
                        writer.WriteLine($"{payment.PaymentDate:yyyy-MM-dd},{payment.GrowerId},{payment.Amount:C},{payment.PaymentType},{payment.Status}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating CSV file: {ex.Message}", "CSV Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        #region PDF Helper Methods

        private void DrawTitlePage(PdfPage page, DashboardSummary summary, DateTime startDate, DateTime endDate)
        {
            PdfGraphics graphics = page.Graphics;
            PdfFont titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 24, PdfFontStyle.Bold);
            PdfFont subtitleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16);
            PdfFont normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12);

            float yPosition = 100;

            // Title
            graphics.DrawString("Berry Farm Management System", titleFont, PdfBrushes.DarkBlue, 
                new PointF(50, yPosition));
            yPosition += 40;

            graphics.DrawString("Dashboard Analytics Report", subtitleFont, PdfBrushes.DarkBlue, 
                new PointF(50, yPosition));
            yPosition += 60;

            // Report period
            graphics.DrawString($"Report Period: {startDate:MMMM dd, yyyy} to {endDate:MMMM dd, yyyy}", 
                normalFont, PdfBrushes.Black, new PointF(50, yPosition));
            yPosition += 30;

            graphics.DrawString($"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm}", 
                normalFont, PdfBrushes.Black, new PointF(50, yPosition));
            yPosition += 60;

            // Key metrics
            graphics.DrawString("Key Metrics:", normalFont, PdfBrushes.Black, new PointF(50, yPosition));
            yPosition += 25;

            graphics.DrawString($"• Total Growers: {summary.TotalGrowers}", 
                normalFont, PdfBrushes.Black, new PointF(70, yPosition));
            yPosition += 20;

            graphics.DrawString($"• Total Payments: {summary.TotalPayments}", 
                normalFont, PdfBrushes.Black, new PointF(70, yPosition));
            yPosition += 20;

            graphics.DrawString($"• Total Payment Amount: {summary.TotalPaymentAmount:C}", 
                normalFont, PdfBrushes.Black, new PointF(70, yPosition));
            yPosition += 20;

            graphics.DrawString($"• Completion Rate: {summary.CompletionRate:F1}%", 
                normalFont, PdfBrushes.Black, new PointF(70, yPosition));
        }

        private void DrawSummaryPage(PdfPage page, DashboardSummary summary, DateTime startDate, DateTime endDate)
        {
            PdfGraphics graphics = page.Graphics;
            PdfFont headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
            PdfFont normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12);

            float yPosition = 50;

            // Summary title
            graphics.DrawString("Executive Summary", headerFont, PdfBrushes.DarkBlue, new PointF(50, yPosition));
            yPosition += 40;

            // Create summary table
            PdfGrid summaryGrid = new PdfGrid();
            summaryGrid.Columns.Add(2);
            summaryGrid.Style.Font = normalFont;

            // Add summary data
            AddSummaryRow(summaryGrid, "Total Growers", summary.TotalGrowers.ToString());
            AddSummaryRow(summaryGrid, "Active Growers", summary.ActiveGrowers.ToString());
            AddSummaryRow(summaryGrid, "On Hold Growers", summary.OnHoldGrowers.ToString());
            AddSummaryRow(summaryGrid, "Total Payments", summary.TotalPayments.ToString());
            AddSummaryRow(summaryGrid, "Total Payment Amount", summary.TotalPaymentAmount.ToString("C"));
            AddSummaryRow(summaryGrid, "Average Payment Amount", summary.AveragePaymentAmount.ToString("C"));
            AddSummaryRow(summaryGrid, "Total Batches", summary.TotalBatches.ToString());
            AddSummaryRow(summaryGrid, "Completed Batches", summary.CompletedBatches.ToString());
            AddSummaryRow(summaryGrid, "Pending Batches", summary.PendingBatches.ToString());
            AddSummaryRow(summaryGrid, "Completion Rate", $"{summary.CompletionRate:F1}%");

            // Draw the grid
            summaryGrid.Draw(page, new PointF(50, yPosition));
        }

        private void DrawChartsPage(PdfPage page, IEnumerable<ChartDataPoint> monthlyTrend, 
            IEnumerable<ChartDataPoint> growerPerformance, IEnumerable<ChartDataPoint> paymentMethodDistribution,
            IEnumerable<ChartDataPoint> provinceDistribution, IEnumerable<ChartDataPoint> priceLevelDistribution)
        {
            PdfGraphics graphics = page.Graphics;
            PdfFont headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
            PdfFont normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);

            float yPosition = 50;

            // Charts title
            graphics.DrawString("Analytics & Trends", headerFont, PdfBrushes.DarkBlue, new PointF(50, yPosition));
            yPosition += 40;

            // Monthly trend table
            graphics.DrawString("Monthly Payment Trend:", normalFont, PdfBrushes.Black, new PointF(50, yPosition));
            yPosition += 20;

            PdfGrid trendGrid = new PdfGrid();
            trendGrid.Columns.Add(2);
            trendGrid.Style.Font = normalFont;

            PdfGridRow headerRow = trendGrid.Headers.Add(1)[0];
            headerRow.Cells[0].Value = "Month";
            headerRow.Cells[1].Value = "Amount";

            foreach (var trend in monthlyTrend)
            {
                PdfGridRow row = trendGrid.Rows.Add();
                row.Cells[0].Value = trend.Category;
                row.Cells[1].Value = trend.Value.ToString("C");
            }

            trendGrid.Draw(page, new PointF(50, yPosition));
            yPosition += trendGrid.Rows.Count * 20 + 50;

            // Grower performance table
            graphics.DrawString("Top Grower Performance:", normalFont, PdfBrushes.Black, new PointF(50, yPosition));
            yPosition += 20;

            PdfGrid performanceGrid = new PdfGrid();
            performanceGrid.Columns.Add(2);
            performanceGrid.Style.Font = normalFont;

            PdfGridRow perfHeaderRow = performanceGrid.Headers.Add(1)[0];
            perfHeaderRow.Cells[0].Value = "Grower";
            perfHeaderRow.Cells[1].Value = "Total Amount";

            foreach (var performance in growerPerformance.Take(10))
            {
                PdfGridRow row = performanceGrid.Rows.Add();
                row.Cells[0].Value = performance.Category;
                row.Cells[1].Value = performance.Value.ToString("C");
            }

            performanceGrid.Draw(page, new PointF(50, yPosition));
        }

        private void DrawRecentActivityPage(PdfPage page, IEnumerable<Payment> recentPayments)
        {
            PdfGraphics graphics = page.Graphics;
            PdfFont headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
            PdfFont normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);

            float yPosition = 50;

            // Recent activity title
            graphics.DrawString("Recent Payment Activity", headerFont, PdfBrushes.DarkBlue, new PointF(50, yPosition));
            yPosition += 40;

            // Create recent payments table
            PdfGrid activityGrid = new PdfGrid();
            activityGrid.Columns.Add(5);
            activityGrid.Style.Font = normalFont;

            // Add headers
            PdfGridRow headerRow = activityGrid.Headers.Add(1)[0];
            headerRow.Cells[0].Value = "Date";
            headerRow.Cells[1].Value = "Grower ID";
            headerRow.Cells[2].Value = "Amount";
            headerRow.Cells[3].Value = "Type";
            headerRow.Cells[4].Value = "Status";

            // Add data rows
            foreach (var payment in recentPayments.Take(20))
            {
                PdfGridRow row = activityGrid.Rows.Add();
                row.Cells[0].Value = payment.PaymentDate.ToString("MMM dd, yyyy");
                row.Cells[1].Value = payment.GrowerId.ToString();
                row.Cells[2].Value = payment.Amount.ToString("C");
                row.Cells[3].Value = payment.PaymentType;
                row.Cells[4].Value = payment.Status;
            }

            // Draw the grid
            activityGrid.Draw(page, new PointF(50, yPosition));
        }

        private void AddSummaryRow(PdfGrid grid, string label, string value)
        {
            PdfGridRow row = grid.Rows.Add();
            row.Cells[0].Value = label;
            row.Cells[1].Value = value;
        }

        #endregion

        #region Excel Helper Methods

        private void CreateSummarySheet(IWorksheet sheet, DashboardSummary summary, DateTime startDate, DateTime endDate)
        {
            int row = 1;

            // Title
            sheet.Range[$"A{row}"].Text = "Berry Farm Management System - Dashboard Analytics";
            sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}"].CellStyle.Font.Size = 16;
            row += 2;

            // Date range
            sheet.Range[$"A{row}"].Text = $"Report Period: {startDate:MMMM dd, yyyy} to {endDate:MMMM dd, yyyy}";
            sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
            row += 2;

            // Summary data
            sheet.Range[$"A{row}"].Text = "Summary Metrics";
            sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}"].CellStyle.Font.Size = 14;
            row += 2;

            // Create summary table
            sheet.Range[$"A{row}"].Text = "Metric";
            sheet.Range[$"B{row}"].Text = "Value";
            sheet.Range[$"A{row}:B{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}:B{row}"].CellStyle.Color = Color.LightGray;
            row++;

            sheet.Range[$"A{row}"].Text = "Total Growers";
            sheet.Range[$"B{row}"].Number = summary.TotalGrowers;
            row++;

            sheet.Range[$"A{row}"].Text = "Active Growers";
            sheet.Range[$"B{row}"].Number = summary.ActiveGrowers;
            row++;

            sheet.Range[$"A{row}"].Text = "On Hold Growers";
            sheet.Range[$"B{row}"].Number = summary.OnHoldGrowers;
            row++;

            sheet.Range[$"A{row}"].Text = "Total Payments";
            sheet.Range[$"B{row}"].Number = summary.TotalPayments;
            row++;

            sheet.Range[$"A{row}"].Text = "Total Payment Amount";
            sheet.Range[$"B{row}"].Number = (double)summary.TotalPaymentAmount;
            sheet.Range[$"B{row}"].NumberFormat = "$#,##0.00";
            row++;

            sheet.Range[$"A{row}"].Text = "Average Payment Amount";
            sheet.Range[$"B{row}"].Number = (double)summary.AveragePaymentAmount;
            sheet.Range[$"B{row}"].NumberFormat = "$#,##0.00";
            row++;

            sheet.Range[$"A{row}"].Text = "Total Batches";
            sheet.Range[$"B{row}"].Number = summary.TotalBatches;
            row++;

            sheet.Range[$"A{row}"].Text = "Completed Batches";
            sheet.Range[$"B{row}"].Number = summary.CompletedBatches;
            row++;

            sheet.Range[$"A{row}"].Text = "Pending Batches";
            sheet.Range[$"B{row}"].Number = summary.PendingBatches;
            row++;

            sheet.Range[$"A{row}"].Text = "Completion Rate";
            sheet.Range[$"B{row}"].Number = summary.CompletionRate;
            sheet.Range[$"B{row}"].NumberFormat = "0.0%";
            row++;

            // Auto-fit columns
            sheet.UsedRange.AutofitColumns();
        }

        private void CreateTrendSheet(IWorksheet sheet, IEnumerable<ChartDataPoint> monthlyTrend)
        {
            int row = 1;

            sheet.Range[$"A{row}"].Text = "Monthly Payment Trend";
            sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}"].CellStyle.Font.Size = 14;
            row += 2;

            sheet.Range[$"A{row}"].Text = "Month";
            sheet.Range[$"B{row}"].Text = "Amount";
            sheet.Range[$"A{row}:B{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}:B{row}"].CellStyle.Color = Color.LightGray;
            row++;

            foreach (var trend in monthlyTrend)
            {
                sheet.Range[$"A{row}"].Text = trend.Category;
                sheet.Range[$"B{row}"].Number = trend.Value;
                sheet.Range[$"B{row}"].NumberFormat = "$#,##0.00";
                row++;
            }

            sheet.UsedRange.AutofitColumns();
        }

        private void CreatePerformanceSheet(IWorksheet sheet, IEnumerable<ChartDataPoint> growerPerformance)
        {
            int row = 1;

            sheet.Range[$"A{row}"].Text = "Top Grower Performance";
            sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}"].CellStyle.Font.Size = 14;
            row += 2;

            sheet.Range[$"A{row}"].Text = "Grower";
            sheet.Range[$"B{row}"].Text = "Total Amount";
            sheet.Range[$"A{row}:B{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}:B{row}"].CellStyle.Color = Color.LightGray;
            row++;

            foreach (var performance in growerPerformance)
            {
                sheet.Range[$"A{row}"].Text = performance.Category;
                sheet.Range[$"B{row}"].Number = performance.Value;
                sheet.Range[$"B{row}"].NumberFormat = "$#,##0.00";
                row++;
            }

            sheet.UsedRange.AutofitColumns();
        }

        private void CreateDistributionSheet(IWorksheet sheet, IEnumerable<ChartDataPoint> paymentMethodDistribution,
            IEnumerable<ChartDataPoint> provinceDistribution, IEnumerable<ChartDataPoint> priceLevelDistribution)
        {
            int row = 1;

            // Payment method distribution
            sheet.Range[$"A{row}"].Text = "Payment Method Distribution";
            sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}"].CellStyle.Font.Size = 14;
            row += 2;

            sheet.Range[$"A{row}"].Text = "Method";
            sheet.Range[$"B{row}"].Text = "Count";
            sheet.Range[$"A{row}:B{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}:B{row}"].CellStyle.Color = Color.LightGray;
            row++;

            foreach (var method in paymentMethodDistribution)
            {
                sheet.Range[$"A{row}"].Text = method.Category;
                sheet.Range[$"B{row}"].Number = method.Value;
                row++;
            }

            row += 3;

            // Province distribution
            sheet.Range[$"A{row}"].Text = "Province Distribution";
            sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}"].CellStyle.Font.Size = 14;
            row += 2;

            sheet.Range[$"A{row}"].Text = "Province";
            sheet.Range[$"B{row}"].Text = "Count";
            sheet.Range[$"A{row}:B{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}:B{row}"].CellStyle.Color = Color.LightGray;
            row++;

            foreach (var province in provinceDistribution)
            {
                sheet.Range[$"A{row}"].Text = province.Category;
                sheet.Range[$"B{row}"].Number = province.Value;
                row++;
            }

            row += 3;

            // Price level distribution
            sheet.Range[$"A{row}"].Text = "Price Level Distribution";
            sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}"].CellStyle.Font.Size = 14;
            row += 2;

            sheet.Range[$"A{row}"].Text = "Price Level";
            sheet.Range[$"B{row}"].Text = "Count";
            sheet.Range[$"A{row}:B{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}:B{row}"].CellStyle.Color = Color.LightGray;
            row++;

            foreach (var priceLevel in priceLevelDistribution)
            {
                sheet.Range[$"A{row}"].Text = priceLevel.Category;
                sheet.Range[$"B{row}"].Number = priceLevel.Value;
                row++;
            }

            sheet.UsedRange.AutofitColumns();
        }

        private void CreateActivitySheet(IWorksheet sheet, IEnumerable<Payment> recentPayments)
        {
            int row = 1;

            sheet.Range[$"A{row}"].Text = "Recent Payment Activity";
            sheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}"].CellStyle.Font.Size = 14;
            row += 2;

            sheet.Range[$"A{row}"].Text = "Date";
            sheet.Range[$"B{row}"].Text = "Grower ID";
            sheet.Range[$"C{row}"].Text = "Amount";
            sheet.Range[$"D{row}"].Text = "Type";
            sheet.Range[$"E{row}"].Text = "Status";
            sheet.Range[$"A{row}:E{row}"].CellStyle.Font.Bold = true;
            sheet.Range[$"A{row}:E{row}"].CellStyle.Color = Color.LightGray;
            row++;

            foreach (var payment in recentPayments)
            {
                sheet.Range[$"A{row}"].Text = payment.PaymentDate.ToString("MMM dd, yyyy");
                sheet.Range[$"B{row}"].Text = payment.GrowerId.ToString();
                sheet.Range[$"C{row}"].Number = (double)payment.Amount;
                sheet.Range[$"C{row}"].NumberFormat = "$#,##0.00";
                sheet.Range[$"D{row}"].Text = payment.PaymentType;
                sheet.Range[$"E{row}"].Text = payment.Status;
                row++;
            }

            sheet.UsedRange.AutofitColumns();
        }

        #endregion

        #region Word Helper Methods

        private void CreateWordSummarySection(IWSection section, DashboardSummary summary)
        {
            IWParagraph summaryHeader = section.AddParagraph();
            summaryHeader.AppendText("Executive Summary");
            summaryHeader.ApplyStyle(BuiltinStyle.Heading2);

            IWTable summaryTable = section.AddTable();
            summaryTable.ResetCells(10, 2);

            // Add summary data
            AddWordTableRow(summaryTable, 0, "Total Growers", summary.TotalGrowers.ToString());
            AddWordTableRow(summaryTable, 1, "Active Growers", summary.ActiveGrowers.ToString());
            AddWordTableRow(summaryTable, 2, "On Hold Growers", summary.OnHoldGrowers.ToString());
            AddWordTableRow(summaryTable, 3, "Total Payments", summary.TotalPayments.ToString());
            AddWordTableRow(summaryTable, 4, "Total Payment Amount", summary.TotalPaymentAmount.ToString("C"));
            AddWordTableRow(summaryTable, 5, "Average Payment Amount", summary.AveragePaymentAmount.ToString("C"));
            AddWordTableRow(summaryTable, 6, "Total Batches", summary.TotalBatches.ToString());
            AddWordTableRow(summaryTable, 7, "Completed Batches", summary.CompletedBatches.ToString());
            AddWordTableRow(summaryTable, 8, "Pending Batches", summary.PendingBatches.ToString());
            AddWordTableRow(summaryTable, 9, "Completion Rate", $"{summary.CompletionRate:F1}%");

            summaryTable.ApplyStyle(BuiltinTableStyle.TableGrid);
        }

        private void CreateWordChartsSection(IWSection section, IEnumerable<ChartDataPoint> monthlyTrend,
            IEnumerable<ChartDataPoint> growerPerformance, IEnumerable<ChartDataPoint> paymentMethodDistribution,
            IEnumerable<ChartDataPoint> provinceDistribution, IEnumerable<ChartDataPoint> priceLevelDistribution)
        {
            IWParagraph chartsHeader = section.AddParagraph();
            chartsHeader.AppendText("Analytics & Trends");
            chartsHeader.ApplyStyle(BuiltinStyle.Heading2);

            // Monthly trend
            IWParagraph trendHeader = section.AddParagraph();
            trendHeader.AppendText("Monthly Payment Trend");
            trendHeader.ApplyStyle(BuiltinStyle.Heading3);

            IWTable trendTable = section.AddTable();
            trendTable.ResetCells(monthlyTrend.Count() + 1, 2);

            // Add headers
            trendTable[0, 0].AddParagraph().AppendText("Month");
            trendTable[0, 1].AddParagraph().AppendText("Amount");

            // Add data
            int row = 1;
            foreach (var trend in monthlyTrend)
            {
                trendTable[row, 0].AddParagraph().AppendText(trend.Category);
                trendTable[row, 1].AddParagraph().AppendText(trend.Value.ToString("C"));
                row++;
            }

            trendTable.ApplyStyle(BuiltinTableStyle.TableGrid);

            section.AddParagraph(); // Empty line

            // Grower performance
            IWParagraph performanceHeader = section.AddParagraph();
            performanceHeader.AppendText("Top Grower Performance");
            performanceHeader.ApplyStyle(BuiltinStyle.Heading3);

            IWTable performanceTable = section.AddTable();
            performanceTable.ResetCells(growerPerformance.Count() + 1, 2);

            // Add headers
            performanceTable[0, 0].AddParagraph().AppendText("Grower");
            performanceTable[0, 1].AddParagraph().AppendText("Total Amount");

            // Add data
            row = 1;
            foreach (var performance in growerPerformance)
            {
                performanceTable[row, 0].AddParagraph().AppendText(performance.Category);
                performanceTable[row, 1].AddParagraph().AppendText(performance.Value.ToString("C"));
                row++;
            }

            performanceTable.ApplyStyle(BuiltinTableStyle.TableGrid);
        }

        private void CreateWordActivitySection(IWSection section, IEnumerable<Payment> recentPayments)
        {
            IWParagraph activityHeader = section.AddParagraph();
            activityHeader.AppendText("Recent Payment Activity");
            activityHeader.ApplyStyle(BuiltinStyle.Heading2);

            IWTable activityTable = section.AddTable();
            activityTable.ResetCells(recentPayments.Count() + 1, 5);

            // Add headers
            activityTable[0, 0].AddParagraph().AppendText("Date");
            activityTable[0, 1].AddParagraph().AppendText("Grower ID");
            activityTable[0, 2].AddParagraph().AppendText("Amount");
            activityTable[0, 3].AddParagraph().AppendText("Type");
            activityTable[0, 4].AddParagraph().AppendText("Status");

            // Add data
            int row = 1;
            foreach (var payment in recentPayments)
            {
                activityTable[row, 0].AddParagraph().AppendText(payment.PaymentDate.ToString("MMM dd, yyyy"));
                activityTable[row, 1].AddParagraph().AppendText(payment.GrowerId.ToString());
                activityTable[row, 2].AddParagraph().AppendText(payment.Amount.ToString("C"));
                activityTable[row, 3].AddParagraph().AppendText(payment.PaymentType);
                activityTable[row, 4].AddParagraph().AppendText(payment.Status);
                row++;
            }

            activityTable.ApplyStyle(BuiltinTableStyle.TableGrid);
        }

        private void AddWordTableRow(IWTable table, int rowIndex, string label, string value)
        {
            table[rowIndex, 0].AddParagraph().AppendText(label);
            table[rowIndex, 1].AddParagraph().AppendText(value);
        }

        #endregion
    }
}