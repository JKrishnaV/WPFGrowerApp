using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IReceiptVoidService
    {
        Task<VoidReceiptResult> VoidReceiptWithCascadingAsync(string receiptNumber, string reason, string voidedBy);
        Task<VoidReceiptResult> AnalyzeReceiptVoidImpactAsync(string receiptNumber);
        Task<bool> CanVoidReceiptAsync(string receiptNumber);
    }
}
