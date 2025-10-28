using System;
using System.Collections.Generic;
using System.Linq;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.Services
{
    public class DashboardSampleDataService
    {
        public static List<Grower> GenerateSampleGrowers(int count = 100)
        {
            var random = new Random();
            var provinces = new[] { "BC", "AB", "SK", "MB", "ON", "QC", "NS", "NB", "NL", "PE" };
            var cities = new[] { "Vancouver", "Calgary", "Saskatoon", "Winnipeg", "Toronto", "Montreal", "Halifax", "Fredericton", "St. John's", "Charlottetown" };
            var growerNames = new[] { "Smith Farms", "Johnson Agriculture", "Williams Berry Co", "Brown Harvest", "Jones Produce", "Garcia Farms", "Miller Orchards", "Davis Crops", "Rodriguez Fields", "Martinez Gardens" };

            return Enumerable.Range(1, count).Select(i => new Grower
            {
                GrowerId = i,
                GrowerNumber = i.ToString(),
                FullName = $"{growerNames[random.Next(growerNames.Length)]} #{i:D3}",
                CheckPayeeName = $"{growerNames[random.Next(growerNames.Length)]} #{i:D3}",
                Address = $"{random.Next(100, 9999)} Main Street",
                City = cities[random.Next(cities.Length)],
                Province = provinces[random.Next(provinces.Length)],
                Postal = $"{random.Next(10000, 99999)}",
                PhoneNumber = $"{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}",
                MobileNumber = $"{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}",
                Email = $"grower{i}@example.com",
                PriceLevel = random.Next(1, 6),
                PaymentGroupId = random.Next(1, 4),
                IsActive = random.NextDouble() > 0.1, // 90% active
                IsOnHold = random.NextDouble() < 0.05, // 5% on hold
                Notes = random.NextDouble() > 0.7 ? $"Sample notes for grower {i}" : null
            }).ToList();
        }

        public static List<Payment> GenerateSamplePayments(List<Grower> growers, int count = 500)
        {
            var random = new Random();
            var paymentTypes = new[] { "Cheque", "Electronic Transfer", "Direct Deposit", "Wire Transfer" };
            var statuses = new[] { "Completed", "Pending", "Processed", "Failed" };

            return Enumerable.Range(1, count).Select(i => new Payment
            {
                PaymentId = i,
                GrowerId = growers[random.Next(growers.Count)].GrowerId,
                Amount = (decimal)(random.NextDouble() * 50000 + 1000), // $1,000 to $51,000
                PaymentDate = DateTime.Now.AddDays(-random.Next(365)), // Last year
                PaymentTypeId = random.Next(1, 5), // Payment type IDs 1-4
                Status = statuses[random.Next(statuses.Length)],
                PaymentBatchId = random.Next(1, 21) // 20 batches
            }).ToList();
        }

        public static List<PaymentBatch> GenerateSamplePaymentBatches(int count = 20)
        {
            var random = new Random();
            var statuses = new[] { "Completed", "Pending", "Processing", "Failed" };

            return Enumerable.Range(1, count).Select(i => new PaymentBatch
            {
                PaymentBatchId = i,
                BatchNumber = $"BATCH-{i:D4}",
                Status = statuses[random.Next(statuses.Length)],
                CreatedAt = DateTime.Now.AddDays(-random.Next(30)),
                ProcessedAt = random.NextDouble() > 0.2 ? DateTime.Now.AddDays(-random.Next(30)) : (DateTime?)null,
                TotalAmount = (decimal)(random.NextDouble() * 100000 + 10000), // $10,000 to $110,000
                TotalGrowers = random.Next(10, 100),
                Notes = $"Payment batch {i}"
            }).ToList();
        }
    }
}