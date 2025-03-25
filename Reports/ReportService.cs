using System;
using System.Collections.Generic;
using AspNetCore.Reporting;
using System.IO;

namespace WPFGrowerApp.Reports
{
    public class ReportService
    {
        public LocalReport LoadReport(string reportPath)
        {
            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException($"Report file not found: {reportPath}");
            }

            var report = new LocalReport(reportPath);
            return report;
        }
    }

    public class ReportDataSource
    {
        public string Name { get; }
        public object Value { get; }

        public ReportDataSource(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }

    public class LocalReport : IDisposable
    {
        private readonly string _reportPath;
        private readonly List<ReportDataSource> _dataSources;
        private bool _disposed;

        public LocalReport(string reportPath)
        {
            _reportPath = reportPath;
            _dataSources = new List<ReportDataSource>();
        }

        public List<ReportDataSource> DataSources => _dataSources;

        public void AddDataSource(ReportDataSource dataSource)
        {
            _dataSources.Add(dataSource);
        }

        public ReportResult Execute(RenderType renderType)
        {
            var report = new AspNetCore.Reporting.LocalReport(_reportPath);
            foreach (var dataSource in _dataSources)
            {
                report.AddDataSource(dataSource.Name, dataSource.Value);
            }

            return report.Execute(renderType);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
} 