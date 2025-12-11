using StoreApp.Data;
using StoreApp.Models;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Repository
{
    public class PaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        // Các phương thức liên quan sẽ được triển khai ở đây.
        // 1. Thêm bản ghi payment mới vào cơ sở dữ liệu
        public async Task<Payment> AddPaymentAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        // 2. Lấy payment theo Id đơn hàng (OrderId)
        public async Task<Payment?> GetByOrderIdAsync(int orderId)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId);
        }

        // 3. Cập nhật trạng thái thanh toán
        public async Task UpdatePaymentStatusAsync(int paymentId, string status)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment != null)
            {
                payment.Status = status;
                await _context.SaveChangesAsync();
            }
        }

        //Lấy max Id
        public int GetMaxId()
        {
            return _context.Payments.Any() 
                ? _context.Payments.Max(p => p.Id) 
                : 0;
        }
        
        public async Task<Payment?> UpdatePaymentAsync(Payment payment)
        {
            var existingPayment = await _context.Payments.FindAsync(payment.Id);
            if (existingPayment == null)
                return null;

            // Cập nhật các trường cần thiết
            existingPayment.OrderId = payment.OrderId;
            existingPayment.Amount = payment.Amount;
            existingPayment.Method = payment.Method;
            existingPayment.TransactionRef = payment.TransactionRef;
            existingPayment.Status = payment.Status;


            await _context.SaveChangesAsync();
            return existingPayment;
        }

        public async Task<Payment?> GetByOrderIdAsyncVer2(int orderId)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId);
        }

        /// <summary>
        /// Cập nhật trạng thái payment theo OrderId (mỗi order chỉ có 1 payment)
        /// </summary>
        public async Task<bool> UpdatePaymentStatusByOrderIdAsync(int orderId, string newStatus)
        {
            var payment = await _context.Payments
                                        .FirstOrDefaultAsync(p => p.OrderId == orderId);

            if (payment == null)
                return false; // Không tìm thấy payment

            payment.Status = newStatus;
            await _context.SaveChangesAsync();

            return true;
        }



    }
}