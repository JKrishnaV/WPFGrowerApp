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
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Models;
using System.Drawing;

namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Service for exporting payment distribution data to various formats
    /// </summary>
    public class PaymentDistributionExportService
    {
        /// <summary>
        /// Export payment distribution data to the specified format
        /// </summary>
        public async Task<bool> ExportPaymentDistributionAsync(
            List<GrowerPaymentSelection> growerSelections, 
            List<PaymentBatch> selectedBatches,
            string exportFormat,
            string reportTitle = "Enhanced Payment Distribution")
        {
            try
            {
                Logger.Info($"Starting {exportFormat} export of payment distribution data");

                if (!growerSelections.Any())
                {
                    MessageBox.Show("No payment data available to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                string defaultFileName = $"PaymentDistribution_{DateTime.Now:yyyyMMdd_HHmmss}";
                string filter;
                string fileExtension;

                // Set up file dialog based on format
                switch (exportFormat.ToUpper())
                {
                    case "EXCEL":
                        filter = "Excel files (*.xlsx)|*.xlsx";
                        fileExtension = "xlsx";
                        break;
                    case "PDF":
                        filter = "PDF files (*.pdf)|*.pdf";
                        fileExtension = "pdf";
                        break;
                    case "CSV":
                        filter = "CSV files (*.csv)|*.csv";
                        fileExtension = "csv";
                        break;
                    case "WORD":
                        filter = "Word files (*.docx)|*.docx";
                        fileExtension = "docx";
                        break;
                    default:
                        throw new ArgumentException($"Unsupported export format: {exportFormat}");
                }

                // Show save dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = filter,
                    DefaultExt = fileExtension,
                    FileName = $"{defaultFileName}.{fileExtension}",
                    Title = $"Save Payment Distribution as {exportFormat.ToUpper()}"
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    return false; // User cancelled
                }

                string filePath = saveFileDialog.FileName;

                // Export based on format
                bool success = exportFormat.ToUpper() switch
                {
                    "EXCEL" => await ExportToExcelAsync(growerSelections, selectedBatches, filePath, reportTitle),
                    "PDF" => await ExportToPdfAsync(growerSelections, selectedBatches, filePath, reportTitle),
                    "CSV" => await ExportToCsvAsync(growerSelections, selectedBatches, filePath, reportTitle),
                    "WORD" => await ExportToWordAsync(growerSelections, selectedBatches, filePath, reportTitle),
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

                    MessageBox.Show($"Payment distribution successfully exported to {Path.GetFileName(filePath)}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting payment distribution: {ex.Message}", ex);
                MessageBox.Show($"Error exporting data: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Export to Excel format
        /// </summary>
        private async Task<bool> ExportToExcelAsync(List<GrowerPaymentSelection> growerSelections, List<PaymentBatch> selectedBatches, string filePath, string reportTitle)
        {
            try
            {
                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Excel2016;
                    IWorkbook workbook = application.Workbooks.Create(1);
                    IWorksheet sheet = workbook.Worksheets[0];
                    sheet.Name = "Payment Distribution";

                    // Add header information
                    sheet.Range["A1"].Text = "Berry Farm Management System";
                    sheet.Range["A2"].Text = reportTitle;
                    sheet.Range["A3"].Text = $"Generated on: {DateTime.Now:MMMM dd, yyyy HH:mm}";
                    sheet.Range["A4"].Text = $"Selected Batches: {string.Join(", ", selectedBatches.Select(b => b.BatchNumber))}";
                    sheet.Range["A5"].Text = $"Total Growers: {growerSelections.Count}";
                    sheet.Range["A6"].Text = $"Total Amount: {growerSelections.Sum(g => g.RegularAmount):C}";

                    // Format header
                    sheet.Range["A1:A6"].CellStyle.Font.Bold = true;
                    sheet.Range["A1"].CellStyle.Font.Size = 16;
                    sheet.Range["A2"].CellStyle.Font.Size = 14;

                    // Add column headers
                    int startRow = 8;
                    sheet.Range[$"A{startRow}"].Text = "Grower #";
                    sheet.Range[$"B{startRow}"].Text = "Grower Name";
                    sheet.Range[$"C{startRow}"].Text = "Payment Type";
                    sheet.Range[$"D{startRow}"].Text = "Regular Amount";
                    sheet.Range[$"E{startRow}"].Text = "Consolidated Amount";
                    sheet.Range[$"F{startRow}"].Text = "Outstanding Advances";
                    sheet.Range[$"G{startRow}"].Text = "Net Amount";
                    sheet.Range[$"H{startRow}"].Text = "Status";
                    sheet.Range[$"I{startRow}"].Text = "Can Consolidate";

                    // Format column headers
                    sheet.Range[$"A{startRow}:I{startRow}"].CellStyle.Font.Bold = true;
                    sheet.Range[$"A{startRow}:I{startRow}"].CellStyle.Color = Color.LightGray;

                    // Add data rows
                    int row = startRow + 1;
                    foreach (var grower in growerSelections)
                    {
                        sheet.Range[$"A{row}"].Text = grower.GrowerNumber;
                        sheet.Range[$"B{row}"].Text = grower.GrowerName;
                        sheet.Range[$"C{row}"].Text = grower.SelectedPaymentType.ToString();
                        sheet.Range[$"D{row}"].Number = (double)grower.RegularAmount;
                        sheet.Range[$"E{row}"].Number = (double)grower.ConsolidatedAmount;
                        sheet.Range[$"F{row}"].Number = (double)grower.OutstandingAdvances;
                        sheet.Range[$"G{row}"].Number = (double)grower.NetRegularAmount;
                        sheet.Range[$"H{row}"].Text = grower.StatusDisplay;
                        sheet.Range[$"I{row}"].Text = grower.CanBeConsolidated ? "Yes" : "No";
                        row++;
                    }

                    // Format currency columns
                    sheet.Range[$"D{startRow + 1}:G{row - 1}"].NumberFormat = "$#,##0.00";

                    // Auto-fit columns
                    sheet.UsedRange.AutofitColumns();

                    // Save the workbook
                    workbook.SaveAs(filePath);
                }

                Logger.Info($"Excel export completed successfully: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting to Excel: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Export to PDF format
        /// </summary>
        private async Task<bool> ExportToPdfAsync(List<GrowerPaymentSelection> growerSelections, List<PaymentBatch> selectedBatches, string filePath, string reportTitle)
        {
            try
            {
                using (PdfDocument document = new PdfDocument())
                {
                    PdfPage page = document.Pages.Add();
                    PdfGraphics graphics = page.Graphics;

                    // Set fonts
                    PdfFont headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
                    PdfFont subHeaderFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
                    PdfFont normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
                    PdfFont smallFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8);

                    float yPosition = 0;

                    // Draw header
                    graphics.DrawString("Berry Farm Management System", headerFont, PdfBrushes.DarkBlue, new PointF(0, yPosition));
                    yPosition += 25;

                    graphics.DrawString(reportTitle, subHeaderFont, PdfBrushes.DarkBlue, new PointF(0, yPosition));
                    yPosition += 20;

                    graphics.DrawString($"Generated on: {DateTime.Now:MMMM dd, yyyy HH:mm}", normalFont, PdfBrushes.Black, new PointF(0, yPosition));
                    yPosition += 15;

                    graphics.DrawString($"Selected Batches: {string.Join(", ", selectedBatches.Select(b => b.BatchNumber))}", normalFont, PdfBrushes.Black, new PointF(0, yPosition));
                    yPosition += 15;

                    graphics.DrawString($"Total Growers: {growerSelections.Count} | Total Amount: {growerSelections.Sum(g => g.RegularAmount):C}", normalFont, PdfBrushes.Black, new PointF(0, yPosition));
                    yPosition += 30;

                    // Create table
                    PdfGrid grid = new PdfGrid();
                    grid.Style.Font = smallFont;
                    grid.Columns.Add(9);

                    // Add headers
                    PdfGridRow headerRow = grid.Headers.Add(1)[0];
                    headerRow.Cells[0].Value = "Grower #";
                    headerRow.Cells[1].Value = "Grower Name";
                    headerRow.Cells[2].Value = "Type";
                    headerRow.Cells[3].Value = "Regular";
                    headerRow.Cells[4].Value = "Consolidated";
                    headerRow.Cells[5].Value = "Advances";
                    headerRow.Cells[6].Value = "Net Amount";
                    headerRow.Cells[7].Value = "Status";
                    headerRow.Cells[8].Value = "Consolidate";

                    // Add data rows
                    foreach (var grower in growerSelections)
                    {
                        PdfGridRow row = grid.Rows.Add();
                        row.Cells[0].Value = grower.GrowerNumber;
                        row.Cells[1].Value = grower.GrowerName;
                        row.Cells[2].Value = grower.SelectedPaymentType.ToString();
                        row.Cells[3].Value = grower.RegularAmount.ToString("C");
                        row.Cells[4].Value = grower.ConsolidatedAmount.ToString("C");
                        row.Cells[5].Value = grower.OutstandingAdvances.ToString("C");
                        row.Cells[6].Value = grower.NetRegularAmount.ToString("C");
                        row.Cells[7].Value = grower.StatusDisplay;
                        row.Cells[8].Value = grower.CanBeConsolidated ? "Yes" : "No";
                    }

                    // Draw the grid
                    grid.Draw(page, new PointF(0, yPosition));

                    // Save the document
                    document.Save(filePath);
                }

                Logger.Info($"PDF export completed successfully: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting to PDF: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Export to CSV format
        /// </summary>
        private async Task<bool> ExportToCsvAsync(List<GrowerPaymentSelection> growerSelections, List<PaymentBatch> selectedBatches, string filePath, string reportTitle)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Write header information
                    writer.WriteLine($"Berry Farm Management System - {reportTitle}");
                    writer.WriteLine($"Generated on: {DateTime.Now:MMMM dd, yyyy HH:mm}");
                    writer.WriteLine($"Selected Batches: {string.Join(", ", selectedBatches.Select(b => b.BatchNumber))}");
                    writer.WriteLine($"Total Growers: {growerSelections.Count}");
                    writer.WriteLine($"Total Amount: {growerSelections.Sum(g => g.RegularAmount):C}");
                    writer.WriteLine(); // Empty line

                    // Write CSV headers
                    writer.WriteLine("Grower #,Grower Name,Payment Type,Regular Amount,Consolidated Amount,Outstanding Advances,Net Amount,Status,Can Consolidate");

                    // Write data rows
                    foreach (var grower in growerSelections)
                    {
                        writer.WriteLine($"\"{grower.GrowerNumber}\",\"{grower.GrowerName}\",\"{grower.SelectedPaymentType}\",{grower.RegularAmount},{grower.ConsolidatedAmount},{grower.OutstandingAdvances},{grower.NetRegularAmount},\"{grower.StatusDisplay}\",{(grower.CanBeConsolidated ? "Yes" : "No")}");
                    }
                }

                Logger.Info($"CSV export completed successfully: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting to CSV: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Export to Word format
        /// </summary>
        private async Task<bool> ExportToWordAsync(List<GrowerPaymentSelection> growerSelections, List<PaymentBatch> selectedBatches, string filePath, string reportTitle)
        {
            try
            {
                using (WordDocument document = new WordDocument())
                {
                    IWSection section = document.AddSection();

                    // Add header
                    IWParagraph headerPara = section.AddParagraph();
                    headerPara.AppendText("Berry Farm Management System");
                    headerPara.ApplyStyle(BuiltinStyle.Heading1);

                    IWParagraph titlePara = section.AddParagraph();
                    titlePara.AppendText(reportTitle);
                    titlePara.ApplyStyle(BuiltinStyle.Heading2);

                    IWParagraph datePara = section.AddParagraph();
                    datePara.AppendText($"Generated on: {DateTime.Now:MMMM dd, yyyy HH:mm}");

                    IWParagraph batchPara = section.AddParagraph();
                    batchPara.AppendText($"Selected Batches: {string.Join(", ", selectedBatches.Select(b => b.BatchNumber))}");

                    IWParagraph summaryPara = section.AddParagraph();
                    summaryPara.AppendText($"Total Growers: {growerSelections.Count} | Total Amount: {growerSelections.Sum(g => g.RegularAmount):C}");

                    section.AddParagraph(); // Empty line

                    // Add table
                    IWTable table = section.AddTable();
                    table.ResetCells(growerSelections.Count + 1, 9); // +1 for header row

                    // Add header row
                    string[] headers = { "Grower #", "Grower Name", "Type", "Regular", "Consolidated", "Advances", "Net Amount", "Status", "Consolidate" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        table[0, i].AddParagraph().AppendText(headers[i]);
                    }

                    // Format header row
                    foreach (WTableCell cell in table.Rows[0].Cells)
                    {
                        cell.CellFormat.VerticalAlignment = Syncfusion.DocIO.DLS.VerticalAlignment.Middle;
                    }
                    table.Rows[0].Height = 20;
                    table.Rows[0].IsHeader = true;

                    // Add data rows
                    for (int i = 0; i < growerSelections.Count; i++)
                    {
                        var grower = growerSelections[i];
                        table[i + 1, 0].AddParagraph().AppendText(grower.GrowerNumber);
                        table[i + 1, 1].AddParagraph().AppendText(grower.GrowerName);
                        table[i + 1, 2].AddParagraph().AppendText(grower.SelectedPaymentType.ToString());
                        table[i + 1, 3].AddParagraph().AppendText(grower.RegularAmount.ToString("C"));
                        table[i + 1, 4].AddParagraph().AppendText(grower.ConsolidatedAmount.ToString("C"));
                        table[i + 1, 5].AddParagraph().AppendText(grower.OutstandingAdvances.ToString("C"));
                        table[i + 1, 6].AddParagraph().AppendText(grower.NetRegularAmount.ToString("C"));
                        table[i + 1, 7].AddParagraph().AppendText(grower.StatusDisplay);
                        table[i + 1, 8].AddParagraph().AppendText(grower.CanBeConsolidated ? "Yes" : "No");
                    }

                    // Apply table formatting
                    table.ApplyStyle(BuiltinTableStyle.TableGrid);

                    // Save the document
                    document.Save(filePath);
                }

                Logger.Info($"Word export completed successfully: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting to Word: {ex.Message}", ex);
                return false;
            }
        }
    }
}
