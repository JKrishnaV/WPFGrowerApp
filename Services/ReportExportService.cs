using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.XlsIO;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using WPFGrowerApp.Models;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.DataAccess.Models;
using System.Drawing;
using Syncfusion.Pdf.Grid;

namespace WPFGrowerApp.Services
{
    public class ReportExportService
    {
        public void ExportToPdf(ObservableCollection<Grower> growers, string reportType, UIElement chartElement = null)
        {
            try
            {
                // Create a new PDF document
                using (PdfDocument document = new PdfDocument())
                {
                    // Add a page
                    PdfPage page = document.Pages.Add();
                    
                    // Create PDF graphics for the page
                    PdfGraphics graphics = page.Graphics;
                    
                    // Set the standard font
                    PdfFont headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
                    PdfFont subHeaderFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14);
                    PdfFont normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
                    
                    // Draw the header
                    graphics.DrawString("Berry Farm Management System", headerFont, PdfBrushes.DarkBlue, new PointF(0, 0));
                    graphics.DrawString(reportType, subHeaderFont, PdfBrushes.DarkBlue, new PointF(0, 30));
                    graphics.DrawString($"Generated on: {DateTime.Now.ToString("MMMM dd, yyyy")}", normalFont, PdfBrushes.Black, new PointF(0, 60));
                    
                    // If chart element is provided, capture it as an image and add to PDF
                    if (chartElement != null)
                    {
                        // In a real implementation, we would capture the chart as an image
                        // For this example, we'll just add a placeholder text
                        graphics.DrawString("[Chart visualization would be included here]", normalFont, PdfBrushes.Black, new PointF(0, 100));
                    }
                    else
                    {
                        // Create a table for grower data
                        PdfGrid grid = new PdfGrid();
                        grid.Style.Font = normalFont;
                        
                        // Add columns
                        grid.Columns.Add(7);
                        
                        // Add headers
                        PdfGridRow header = grid.Headers.Add(1)[0];
                        header.Cells[0].Value = "Grower #";
                        header.Cells[1].Value = "Grower Name";
                        header.Cells[2].Value = "City";
                        header.Cells[3].Value = "Province";
                        header.Cells[4].Value = "Phone";
                        header.Cells[5].Value = "Acres";
                        header.Cells[6].Value = "Pay Group";
                        
                        // Add rows
                        foreach (var grower in growers)
                        {
                            PdfGridRow row = grid.Rows.Add();
                            row.Cells[0].Value = grower.GrowerNumber.ToString();
                            row.Cells[1].Value = grower.GrowerName ?? string.Empty;
                            row.Cells[2].Value = grower.City ?? string.Empty;
                            row.Cells[3].Value = grower.Prov ?? string.Empty;
                            row.Cells[4].Value = grower.Phone ?? string.Empty;
                            row.Cells[5].Value = "0"; // Acres field not available in new model
                            row.Cells[6].Value = grower.PaymentGroupId.ToString();
                        }
                        
                        // Draw the grid
                        grid.Draw(page, new PointF(0, 100));
                    }
                    
                    // Show save dialog
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "PDF files (*.pdf)|*.pdf",
                        DefaultExt = "pdf",
                        FileName = $"BerryFarm_{reportType.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}"
                    };
                    
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        // Save the document
                        document.Save(saveFileDialog.FileName);
                        
                        // Open the document
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to PDF: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public void ExportToExcel(ObservableCollection<Grower> growers, string reportType)
        {
            try
            {
                // Create a new Excel engine
                using (ExcelEngine excelEngine = new ExcelEngine())
                {
                    // Create a new workbook
                    IApplication application = excelEngine.Excel;
                    application.DefaultVersion = ExcelVersion.Excel2016;
                    IWorkbook workbook = application.Workbooks.Create(1);
                    IWorksheet sheet = workbook.Worksheets[0];
                    
                    // Add header
                    sheet.Range["A1"].Text = "Berry Farm Management System";
                    sheet.Range["A2"].Text = reportType;
                    sheet.Range["A3"].Text = $"Generated on: {DateTime.Now.ToString("MMMM dd, yyyy")}";
                    
                    // Format header
                    sheet.Range["A1:A3"].CellStyle.Font.Bold = true;
                    sheet.Range["A1"].CellStyle.Font.Size = 16;
                    sheet.Range["A2"].CellStyle.Font.Size = 14;
                    
                    // Add column headers
                    sheet.Range["A5"].Text = "Grower #";
                    sheet.Range["B5"].Text = "Grower Name";
                    sheet.Range["C5"].Text = "Cheque Name";
                    sheet.Range["D5"].Text = "Address";
                    sheet.Range["E5"].Text = "City";
                    sheet.Range["F5"].Text = "Province";
                    sheet.Range["G5"].Text = "Postal Code";
                    sheet.Range["H5"].Text = "Phone";
                    sheet.Range["I5"].Text = "Acres";
                    sheet.Range["J5"].Text = "Pay Group";
                    sheet.Range["K5"].Text = "Price Level";
                    
                    // Format column headers
                    sheet.Range["A5:K5"].CellStyle.Font.Bold = true;
                    sheet.Range["A5:K5"].CellStyle.Color = System.Drawing.Color.LightGray;
                    
                    // Add data
                    int row = 6;
                    foreach (var grower in growers)
                    {
                        if (double.TryParse(grower.GrowerNumber, out double growerNum))
                            sheet.Range[$"A{row}"].Number = growerNum;
                        else
                            sheet.Range[$"A{row}"].Number = 0;
                        sheet.Range[$"B{row}"].Text = grower.GrowerName ?? string.Empty;
                        sheet.Range[$"C{row}"].Text = grower.ChequeName ?? string.Empty;
                        sheet.Range[$"D{row}"].Text = grower.Address ?? string.Empty;
                        sheet.Range[$"E{row}"].Text = grower.City ?? string.Empty;
                        sheet.Range[$"F{row}"].Text = grower.Prov ?? string.Empty;
                        sheet.Range[$"G{row}"].Text = grower.Postal ?? string.Empty;
                        sheet.Range[$"H{row}"].Text = grower.Phone ?? string.Empty;
                        sheet.Range[$"I{row}"].Number = 0; // Acres field not available in new model
                        sheet.Range[$"J{row}"].Text = grower.PaymentGroupId.ToString();
                        sheet.Range[$"K{row}"].Number = grower.PriceLevel;
                        row++;
                    }
                    
                    // Auto-fit columns
                    sheet.UsedRange.AutofitColumns();
                    
                    // Show save dialog
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Excel files (*.xlsx)|*.xlsx",
                        DefaultExt = "xlsx",
                        FileName = $"BerryFarm_{reportType.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}"
                    };
                    
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        // Save the workbook
                        workbook.SaveAs(saveFileDialog.FileName);
                        
                        // Open the document
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public void ExportToWord(ObservableCollection<Grower> growers, string reportType)
        {
            try
            {
                // Create a new Word document
                using (WordDocument document = new WordDocument())
                {
                    // Add a section
                    IWSection section = document.AddSection();
                    
                    // Add a paragraph for the header
                    IWParagraph headerPara = section.AddParagraph();
                    headerPara.AppendText("Berry Farm Management System");
                    headerPara.ApplyStyle(BuiltinStyle.Heading1);
                    
                    // Add a paragraph for the report type
                    IWParagraph reportTypePara = section.AddParagraph();
                    reportTypePara.AppendText(reportType);
                    reportTypePara.ApplyStyle(BuiltinStyle.Heading2);
                    
                    // Add a paragraph for the date
                    IWParagraph datePara = section.AddParagraph();
                    datePara.AppendText($"Generated on: {DateTime.Now.ToString("MMMM dd, yyyy")}");
                    
                    // Add a paragraph for spacing
                    section.AddParagraph();
                    
                    // Add a table
                    IWTable table = section.AddTable();
                    table.ResetCells(growers.Count + 1, 7); // +1 for header row
                    
                    // Add header row
                    table[0, 0].AddParagraph().AppendText("Grower #");
                    table[0, 1].AddParagraph().AppendText("Grower Name");
                    table[0, 2].AddParagraph().AppendText("City");
                    table[0, 3].AddParagraph().AppendText("Province");
                    table[0, 4].AddParagraph().AppendText("Phone");
                    table[0, 5].AddParagraph().AppendText("Acres");
                    table[0, 6].AddParagraph().AppendText("Pay Group");

                    // Format header row
                    foreach (WTableCell cell in table.Rows[0].Cells)
                    {
                        cell.CellFormat.VerticalAlignment = Syncfusion.DocIO.DLS.VerticalAlignment.Middle;
                    }
                    //table.Rows[0].Cells.VerticalAlignment = Syncfusion.DocIO.DLS.VerticalAlignment.Middle;
                    table.Rows[0].Height = 20;
                    table.Rows[0].IsHeader = true;
                    
                    // Add data rows
                    for (int i = 0; i < growers.Count; i++)
                    {
                        var grower = growers[i];
                        table[i + 1, 0].AddParagraph().AppendText(grower.GrowerNumber.ToString());
                        table[i + 1, 1].AddParagraph().AppendText(grower.GrowerName ?? string.Empty);
                        table[i + 1, 2].AddParagraph().AppendText(grower.City ?? string.Empty);
                        table[i + 1, 3].AddParagraph().AppendText(grower.Prov ?? string.Empty);
                        table[i + 1, 4].AddParagraph().AppendText(grower.Phone ?? string.Empty);
                        table[i + 1, 5].AddParagraph().AppendText("0"); // Acres field not available in new model
                        table[i + 1, 6].AddParagraph().AppendText(grower.PaymentGroupId.ToString());
                    }

                    // Apply table formatting
                    // Apply table formatting
                    table.ApplyStyle(BuiltinTableStyle.TableGrid);
                    

                    // Show save dialog
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Word files (*.docx)|*.docx",
                        DefaultExt = "docx",
                        FileName = $"BerryFarm_{reportType.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}"
                    };
                    
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        // Save the document
                        document.Save(saveFileDialog.FileName);
                        
                        // Open the document
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Word: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
