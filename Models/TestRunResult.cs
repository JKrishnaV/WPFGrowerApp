using System.Collections.Generic;
using System.Linq; // Added for .Any()

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Encapsulates the results of an advance payment test run,
    /// including the input parameters used and the calculated payment details.
    /// </summary>
    public class TestRunResult
    {
        /// <summary>
        /// The input parameters used to generate this test run result.
        /// </summary>
        public TestRunInputParameters InputParameters { get; set; } = new TestRunInputParameters();

        /// <summary>
        /// The calculated payment details for each grower included in the test run.
        /// </summary>
        public List<TestRunGrowerPayment> GrowerPayments { get; set; } = new List<TestRunGrowerPayment>();

        /// <summary>
        /// Any general errors that occurred during the test run calculation process
        /// (not specific to a single receipt or grower).
        /// </summary>
        public List<string> GeneralErrors { get; set; } = new List<string>();

        /// <summary>
        /// Indicates if any errors occurred during the test run (either general or specific to growers/receipts).
        /// </summary>
        public bool HasAnyErrors => GeneralErrors.Count > 0 || GrowerPayments.Any(gp => gp.HasErrors);
    }
}
