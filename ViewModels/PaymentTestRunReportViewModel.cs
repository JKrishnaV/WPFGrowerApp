using System;
using System.Collections.Generic;
using System.Linq;
using WPFGrowerApp.Models; // For TestRunResult etc.
using WPFGrowerApp.Commands; // For RelayCommand (assuming it's here)
using System.Windows.Input;
using System.Collections.ObjectModel; // For ObservableCollection if needed for charts
using System.Threading.Tasks; // Added for Task
using Microsoft.Win32; // For SaveFileDialog
using System.Windows.Controls; // For PrintDialog
using System.Diagnostics; // For Debug.WriteLine
using Syncfusion.Pdf; // For PDF document
using Syncfusion.Pdf.Graphics; // For drawing text/shapes
using Syncfusion.Pdf.Grid; // For PDF grid
using System.Drawing; // For PointF, SizeF etc.
using Syncfusion.XlsIO; // For Excel export
using System.Data; // For DataTable
using System.IO; // For MemoryStream

// Using the correct namespace after the file move
namespace WPFGrowerApp.ViewModels 
{
    // Assuming ViewModelBase provides INotifyPropertyChanged and is in WPFGrowerApp.ViewModels
    public class PaymentTestRunReportViewModel : ViewModelBase 
    {
        private TestRunResult _testRunResult;
        private bool _isExportingOrPrinting; // Flag for export/print operations

        public TestRunResult TestRunData
        {
            get => _testRunResult;
            private set => SetProperty(ref _testRunResult, value);
        }

        // Expose specific parts of the result for easier binding
        public TestRunInputParameters InputParameters => TestRunData?.InputParameters;
        public List<TestRunGrowerPayment> GrowerPayments => TestRunData?.GrowerPayments ?? new List<TestRunGrowerPayment>();
        public List<string> GeneralErrors => TestRunData?.GeneralErrors ?? new List<string>();
        public bool HasAnyErrors => TestRunData?.HasAnyErrors ?? false;

        // Properties for potential chart data sources (example)
        public List<ChartDataPoint> TopGrowerPayments { get; private set; }
        public List<ChartDataPoint> PaymentsByProduct { get; private set; }


        public PaymentTestRunReportViewModel(TestRunResult testRunResult)
        {
            _testRunResult = testRunResult ?? throw new ArgumentNullException(nameof(testRunResult));
            PrepareChartData();
        }

        // --- Commands ---
        public ICommand ExportPdfCommand => new RelayCommand(async o => await ExportReportAsync("PDF"), o => !IsExportingOrPrinting);
        public ICommand ExportExcelCommand => new RelayCommand(async o => await ExportReportAsync("Excel"), o => !IsExportingOrPrinting);
        public ICommand PrintCommand => new RelayCommand(async o => await PrintReportAsync(), o => !IsExportingOrPrinting);


        public bool IsExportingOrPrinting
        {
            get => _isExportingOrPrinting;
            private set
            {
                 if(SetProperty(ref _isExportingOrPrinting, value))
                 {
                     // Update CanExecute for export/print commands
                     ((RelayCommand)ExportPdfCommand).RaiseCanExecuteChanged();
                     ((RelayCommand)ExportExcelCommand).RaiseCanExecuteChanged();
                     ((RelayCommand)PrintCommand).RaiseCanExecuteChanged();
                 }
            }
        }

        private void PrepareChartData()
        {
            // Example: Prepare data for Top 5 Grower Payments Pie Chart
            TopGrowerPayments = GrowerPayments
                .OrderByDescending(gp => gp.TotalCalculatedPayment)
                .Take(5)
                .Select(gp => new ChartDataPoint { Category = $"{gp.GrowerNumber} - {gp.GrowerName}", Value = (double)gp.TotalCalculatedPayment })
                .ToList();

             // Example: Prepare data for Payments by Product Bar Chart
             PaymentsByProduct = GrowerPayments
                .SelectMany(gp => gp.ReceiptDetails.Where(rd => string.IsNullOrEmpty(rd.ErrorMessage))) // Only include non-error receipts
                .GroupBy(rd => rd.Product)
                .Select(g => new ChartDataPoint { Category = g.Key ?? "Unknown", Value = (double)g.Sum(rd => rd.CalculatedTotalAmount) }) // Handle potential null key
                .OrderBy(dp => dp.Category)
                .ToList();

            // Notify that chart data properties have changed if necessary (depends on ViewModelBase implementation)
            OnPropertyChanged(nameof(TopGrowerPayments));
            OnPropertyChanged(nameof(PaymentsByProduct));
        }


        private async Task ExportReportAsync(string format)
        {
            IsExportingOrPrinting = true;
            // Inject or resolve IDialogService if needed for showing messages
            // IDialogService dialogService = ...; 
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                string defaultFileName = $"PaymentTestRun_{DateTime.Now:yyyyMMddHHmmss}";
                string filter;

                if (format.Equals("PDF", StringComparison.OrdinalIgnoreCase))
                {
                    filter = "PDF Document (*.pdf)|*.pdf";
                    saveFileDialog.DefaultExt = ".pdf";
                    defaultFileName += ".pdf";
                }
                else if (format.Equals("Excel", StringComparison.OrdinalIgnoreCase))
                {
                    filter = "Excel Workbook (*.xlsx)|*.xlsx";
                    saveFileDialog.DefaultExt = ".xlsx";
                    defaultFileName += ".xlsx";
                }
                else 
                {
                     filter = "All files (*.*)|*.*"; // Fallback
                }
                
                saveFileDialog.Filter = filter;
                saveFileDialog.FileName = defaultFileName;

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    Debug.WriteLine($"Exporting report to {format} at: {filePath}");

                    if (format.Equals("PDF", StringComparison.OrdinalIgnoreCase))
                    {
                        // Generate PDF into a stream
                        using (MemoryStream pdfStream = await GeneratePdfReport()) 
                        {
                            // Save the stream to the chosen file path
                            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                            {
                                pdfStream.CopyTo(fileStream);
                            }
                        }
                    }
                    else if (format.Equals("Excel", StringComparison.OrdinalIgnoreCase))
                    {
                        await GenerateExcelReport(filePath);
                    }
                    
                    Debug.WriteLine($"Export to {format} complete.");
                    // Consider showing success message via dialogService
                    // await dialogService.ShowMessageBoxAsync($"Report successfully exported to {filePath}", "Export Complete");
                }
            }
            catch (Exception ex)
            {
                 Debug.WriteLine($"Error during {format} export: {ex.Message}");
                 // Consider showing error message via dialogService
                 // await dialogService.ShowMessageBoxAsync($"An error occurred during export: {ex.Message}", "Export Error");
            }
            finally
            {
                IsExportingOrPrinting = false;
             }
         }

        // Modified to return MemoryStream instead of saving to file path
        // Made public to be callable from PaymentRunViewModel
        public Task<MemoryStream> GeneratePdfReport() 
        {
            return Task.Run(() => // Run PDF generation on a background thread
            {
                MemoryStream pdfStream = new MemoryStream();
                try
                {
                    using (PdfDocument document = new PdfDocument())
                    {
                        //Add a page to the document
                        PdfPage page = document.Pages.Add();
                        //Get graphics object
                        PdfGraphics graphics = page.Graphics;
                        //Set standard font
                        PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
                        PdfFont titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
                        PdfFont headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
                        float yPos = 0;
                        float xMargin = 10;
                        float pageMaxWidth = page.GetClientSize().Width - (2 * xMargin);

                        // Draw Title
                        graphics.DrawString("Advance Payment Test Run Report", titleFont, PdfBrushes.Black, new PointF(xMargin, yPos));
                        yPos += titleFont.Height + 15;

                        // Draw Summary
                        graphics.DrawString("Report Generated Based On:", headerFont, PdfBrushes.Black, new PointF(xMargin, yPos));
                        yPos += headerFont.Height + 5;
                        string summaryLine1 = $"Advance #: {InputParameters.AdvanceNumber}   Payment Date: {InputParameters.PaymentDate:d}   Cutoff Date: {InputParameters.CutoffDate:d}   Crop Year: {InputParameters.CropYear}";
                        graphics.DrawString(summaryLine1, font, PdfBrushes.Black, new PointF(xMargin, yPos));
                        yPos += font.Height + 5;
                        // Simplified filter display for PDF
                        string filters = $"Filters: Products: { (InputParameters.ProductDescriptions.Any() ? string.Join(",", InputParameters.ProductDescriptions) : "All")}; " +
                                         $"Processes: { (InputParameters.ProcessDescriptions.Any() ? string.Join(",", InputParameters.ProcessDescriptions) : "All")}; " +
                                         $"Excluded Growers: { (InputParameters.ExcludedGrowerDescriptions.Any() ? string.Join(",", InputParameters.ExcludedGrowerDescriptions) : "None")}; " +
                                         $"Excluded Pay Groups: { (InputParameters.ExcludedPayGroupDescriptions.Any() ? string.Join(",", InputParameters.ExcludedPayGroupDescriptions) : "None")}";
                        // Draw wrapped text for filters
                        PdfTextElement filterElement = new PdfTextElement(filters, font);
                        PdfLayoutResult filterDrawResult = filterElement.Draw(page, new RectangleF(xMargin, yPos, pageMaxWidth, page.GetClientSize().Height));
                        // Update yPos based on the actual height of the drawn text element
                        if (filterDrawResult != null) 
                        {
                            yPos += filterDrawResult.Bounds.Height + 10; 
                        }
                        else 
                        {
                            // Fallback if drawing failed or returned null unexpectedly
                            yPos += font.Height * 2 + 10; // Estimate based on font size
                        }

                        // Draw Grid
                        PdfGrid pdfGrid = new PdfGrid();
                        // Create DataTable from GrowerPayments
                        DataTable dataTable = CreateDataTableFromGrowerPayments();
                        pdfGrid.DataSource = dataTable;

                        // Apply Built-in Style
                        pdfGrid.ApplyBuiltinStyle(PdfGridBuiltinStyle.GridTable4Accent1);

                        // Auto-size columns based on content
                        foreach (PdfGridColumn column in pdfGrid.Columns)
                        {
                             column.Format = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);
                        }
                        pdfGrid.Headers[0].Style.Font = headerFont;

                        // Draw the grid
                        PdfGridLayoutFormat layoutFormat = new PdfGridLayoutFormat() { Layout = PdfLayoutType.Paginate };
                        pdfGrid.Draw(page, new PointF(xMargin, yPos), layoutFormat);

                        //Save the document to the stream
                        document.Save(pdfStream);
                        // Ensure the stream position is at the beginning for reading
                        pdfStream.Position = 0; 
                    }
                    return pdfStream;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error generating PDF report: {ex.Message}");
                    pdfStream.Dispose(); // Dispose stream on error
                    // Rethrow or handle appropriately (e.g., show message via dispatcher)
                    throw; 
                }
                // Note: The caller is responsible for disposing the returned MemoryStream
            });
        }

        private Task GenerateExcelReport(string filePath)
        {
             return Task.Run(() => // Run Excel generation on a background thread
             {
                 try
                 {
                     using (ExcelEngine excelEngine = new ExcelEngine())
                     {
                         IApplication application = excelEngine.Excel;
                         application.DefaultVersion = ExcelVersion.Xlsx;
                         IWorkbook workbook = application.Workbooks.Create(1);
                         IWorksheet worksheet = workbook.Worksheets[0];
                         worksheet.Name = "Payment Test Run";

                         int row = 1;

                         // Title
                         worksheet.Range[$"A{row}"].Text = "Advance Payment Test Run Report";
                         worksheet.Range[$"A{row}"].CellStyle.Font.Bold = true;
                         worksheet.Range[$"A{row}"].CellStyle.Font.Size = 14;
                         row += 2;

                         // Summary
                         worksheet.Range[$"A{row}"].Text = "Report Generated Based On:";
                         worksheet.Range[$"A{row}"].CellStyle.Font.Bold = true; row++;
                         worksheet.Range[$"A{row}"].Text = $"Advance #: {InputParameters.AdvanceNumber}"; row++;
                         worksheet.Range[$"A{row}"].Text = $"Payment Date: {InputParameters.PaymentDate:d}"; row++;
                         worksheet.Range[$"A{row}"].Text = $"Cutoff Date: {InputParameters.CutoffDate:d}"; row++;
                         worksheet.Range[$"A{row}"].Text = $"Crop Year: {InputParameters.CropYear}"; row++;
                         worksheet.Range[$"A{row}"].Text = $"Filters:"; row++;
                         worksheet.Range[$"B{row}"].Text = $"Products: {(InputParameters.ProductDescriptions.Any() ? string.Join(", ", InputParameters.ProductDescriptions) : "All")}"; row++;
                         worksheet.Range[$"B{row}"].Text = $"Processes: {(InputParameters.ProcessDescriptions.Any() ? string.Join(", ", InputParameters.ProcessDescriptions) : "All")}"; row++;
                         worksheet.Range[$"B{row}"].Text = $"Excluded Growers: {(InputParameters.ExcludedGrowerDescriptions.Any() ? string.Join(", ", InputParameters.ExcludedGrowerDescriptions) : "None")}"; row++;
                         worksheet.Range[$"B{row}"].Text = $"Excluded Pay Groups: {(InputParameters.ExcludedPayGroupDescriptions.Any() ? string.Join(", ", InputParameters.ExcludedPayGroupDescriptions) : "None")}"; row++;
                         row++; // Add blank row

                         // Data - Create DataTable first
                         DataTable dataTable = CreateDataTableFromGrowerPayments();
                         // Import data
                         worksheet.ImportDataTable(dataTable, true, row, 1);
                         row += dataTable.Rows.Count + 1; // Move past imported data

                         // Apply formatting
                         IStyle headerStyle = workbook.Styles.Add("HeaderStyle");
                         headerStyle.Font.Bold = true;
                         headerStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                         worksheet.Range[row - dataTable.Rows.Count -1, 1, row - dataTable.Rows.Count -1, dataTable.Columns.Count].CellStyle = headerStyle;

                         // Number formats
                         worksheet.Range[row - dataTable.Rows.Count, 1, row - 1, 1].NumberFormat = "0"; // Grower #
                         worksheet.Range[row - dataTable.Rows.Count, 4, row - 1, 4].NumberFormat = "0"; // Receipts
                         worksheet.Range[row - dataTable.Rows.Count, 5, row - 1, 5].NumberFormat = "0.00"; // Weight
                         worksheet.Range[row - dataTable.Rows.Count, 6, row - 1, 9].NumberFormat = "$#,##0.00"; // Currency columns

                         worksheet.UsedRange.AutofitColumns();

                         //Save the workbook
                         workbook.SaveAs(filePath);
                     }
                 }
                 catch (Exception ex)
                 {
                     Debug.WriteLine($"Error generating Excel report: {ex.Message}");
                     // Rethrow or handle appropriately
                     throw;
                 }
             });
        }

        // Made internal to be accessible by PaymentRunViewModel (or use public)
        // TODO: Refactor this into a shared helper/service later
        internal DataTable CreateDataTableFromGrowerPayments()
        {
             DataTable dataTable = new DataTable();
             // Define columns matching the RDLC DataSet Fields EXACTLY (Names and Types)
             dataTable.Columns.Add("Grower", typeof(float)); // Match RDLC TypeName="System.Single"
             dataTable.Columns.Add("Name", typeof(string)); // Matches
             dataTable.Columns.Add("OnHold", typeof(bool)); // Matches
             dataTable.Columns.Add("Receipts", typeof(int)); // Matches
             dataTable.Columns.Add("TotalWeight", typeof(float)); // Match RDLC TypeName="System.Single"
             dataTable.Columns.Add("TotalAdvance", typeof(float)); // Match RDLC TypeName="System.Single"
             dataTable.Columns.Add("TotalPremium", typeof(float)); // Match RDLC TypeName="System.Single"
             dataTable.Columns.Add("TotalDeduction", typeof(float)); // Match RDLC TypeName="System.Single"
             dataTable.Columns.Add("TotalPayment", typeof(float)); // Match RDLC TypeName="System.Single"
             dataTable.Columns.Add("HasErrors", typeof(bool)); // Matches

             // Populate rows
             foreach (var growerPayment in GrowerPayments)
             {
                 // Add data, casting decimals to float where necessary
                 dataTable.Rows.Add(
                     (float)growerPayment.GrowerNumber, // Cast int to float
                     growerPayment.GrowerName,
                     growerPayment.IsOnHold,
                     growerPayment.ReceiptCount, // int matches
                     (float)growerPayment.TotalNetWeight, // Cast decimal to float
                     (float)growerPayment.TotalCalculatedAdvanceAmount, // Cast decimal to float
                     (float)growerPayment.TotalCalculatedPremiumAmount, // Cast decimal to float
                     (float)growerPayment.TotalCalculatedDeductionAmount, // Cast decimal to float
                     (float)growerPayment.TotalCalculatedPayment, // Cast decimal to float
                     growerPayment.HasErrors // bool matches
                 );
             }
             return dataTable;
        }


         private async Task PrintReportAsync()
         {
              IsExportingOrPrinting = true; 
              // Inject or resolve IDialogService if needed for showing messages
             // IDialogService dialogService = ...; 
             try
             {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    Debug.WriteLine($"Placeholder: Would print report...");
                    
                    // TODO: Implement actual print logic
                    // This might involve:
                    // 1. Creating a FlowDocument or FixedDocument visual representation of the report data.
                    // 2. Possibly generating a PDF first using Syncfusion.Pdf.Wpf and then printing the PDF.
                    // 3. Or directly printing a visual element (like the SfDataGrid, though this is harder from VM).
                    // 4. Calling printDialog.PrintDocument(...) or printDialog.PrintVisual(...).
                    
                    await Task.Delay(500); // Simulate print work
                    
                    Debug.WriteLine($"Placeholder: Printing complete.");
                     // Consider showing success message via dialogService
                    // await dialogService.ShowMessageBoxAsync("Report sent to printer.", "Print Complete");
                }
             }
             catch (Exception ex)
             {
                 Debug.WriteLine($"Error during printing: {ex.Message}");
                 // Consider showing error message via dialogService
                 // await dialogService.ShowMessageBoxAsync($"An error occurred during printing: {ex.Message}", "Print Error");
             }
             finally
             {
                IsExportingOrPrinting = false;
             }
        }

        // Helper class for chart data
        public class ChartDataPoint
        {
            public string Category { get; set; }
            public double Value { get; set; }
        }
    }
}
