

using System.Threading.Tasks;

namespace TeleMedicineApp.Services
{
    public interface IPaymentService
    {
        Task<string> InitiatePaymentAsync(decimal amount, string orderId, string orderName, string customerName, string customerEmail, string customerPhone);
        //Task<string> InitiatePaymentAsync(decimal amount, int orderId, string orderName,string fullName);
        Task<bool> VerifyPaymentAsync(string pidx, decimal amount);
        //Task<bool> VerifyPaymentAsync(string paymentId, string transactionId, decimal amount); // Updated method signature
    }
}