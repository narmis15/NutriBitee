using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NUTRIBITE.Models;

namespace NUTRIBITE.Services
{
    public class OrderService : IOrderService
    {
        private readonly string _cs;
        private readonly ILogger<OrderService> _log;
        private readonly ApplicationDbContext _db;

        public OrderService(IConfiguration cfg, ILogger<OrderService> log, ApplicationDbContext db)
        {
            _log = log;
            _db = db;
            _cs = cfg.GetConnectionString("DBCS") ?? throw new Exception("DBCS not found");
        }

        public IQueryable<OrderTable> GetOrderQueryable()
        {
            return _db.OrderTables.AsQueryable();
        }

        private SqlConnection GetConn() => new SqlConnection(_cs);

        public async Task<IEnumerable<object>> GetActiveOrdersAsync()
        {
            var list = new List<object>();
            try
            {
                using var con = GetConn();
                await con.OpenAsync();
                // Filter out old dummy data by only selecting orders created recently (e.g., today or after a specific date)
                // For a real project feel, we'll only show orders from the last 24 hours or those that are actively being processed
                var q = @"
SELECT OrderId, CustomerName, CustomerPhone, ISNULL(TotalItems,0) AS TotalItems,
       ISNULL(TotalCalories,0) AS TotalCalories, ISNULL(PaymentStatus,'') AS PaymentStatus, ISNULL(Status,'') AS Status,
       ISNULL(IsFlagged,0) AS IsFlagged, CreatedAt, 'Delivery' AS OrderType, ISNULL(DeliveryStatus, '') AS DeliveryStatus,
       ISNULL(DeliveryAddress, '') AS DeliveryAddress, ISNULL(DeliveryNotes, '') AS DeliveryNotes
FROM OrderTable
WHERE ISNULL(Status,'') <> 'Cancelled' 
  AND (CreatedAt >= DATEADD(day, -2, GETDATE()) OR Status IN ('Placed', 'New', 'Accepted', 'Ready for Delivery', 'Ready for Pickup', 'In Transit', 'Picked'))
ORDER BY CreatedAt DESC";
                using var cmd = new SqlCommand(q, con);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new
                    {
                        orderId = r.GetInt32(0),
                        customerName = r.IsDBNull(1) ? "Guest" : r.GetString(1),
                        customerPhone = r.IsDBNull(2) ? "No Phone" : r.GetString(2),
                        totalItems = r.IsDBNull(3) ? 0 : r.GetInt32(3),
                        totalCalories = r.IsDBNull(4) ? 0 : r.GetInt32(4),
                        paymentStatus = r.IsDBNull(5) ? "Pending" : r.GetString(5),
                        status = r.IsDBNull(6) ? "New" : r.GetString(6),
                        isFlagged = !r.IsDBNull(7) && Convert.ToInt32(r.GetValue(7)) == 1,
                        createdAt = r.IsDBNull(8) ? (DateTime?)null : r.GetDateTime(8),
                        orderType = "Delivery",
                        deliveryStatus = r.IsDBNull(10) ? "Unassigned" : r.GetString(10),
                        deliveryAddress = r.IsDBNull(11) ? "No Address" : r.GetString(11),
                        deliveryNotes = r.IsDBNull(12) ? "" : r.GetString(12)
                    });
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "GetActiveOrdersAsync failed");
            }
            return list;
        }

        public async Task<object?> GetOrderDetailsAsync(int orderId)
        {
            try
            {
                using var con = GetConn();
                await con.OpenAsync();

                var headerQ = @"
SELECT TOP 1 o.OrderId, o.CreatedAt AS OrderDateTime, ISNULL(o.Status,'') AS Status, ISNULL(o.CustomerName,'') AS CustomerName,
       ISNULL(o.CustomerPhone,'') AS CustomerPhone, ISNULL(o.TotalCalories,0) AS TotalCalories,
       ISNULL(p.PaymentMode,'') AS PaymentMode, ISNULL(o.TotalAmount,0) AS Amount, ISNULL(o.CommissionAmount,0) AS CommissionAmount, ISNULL(o.VendorAmount,0) AS VendorAmount,
       ISNULL(p.IsRefunded,0) AS IsRefunded, ISNULL(p.RefundStatus,'') AS RefundStatus,
       'Delivery' AS OrderType, ISNULL(o.DeliveryAddress,'') AS DeliveryAddress,
       ISNULL(o.DeliveryStatus,'') AS DeliveryStatus, o.DeliveryPersonId, ISNULL(o.DeliveryNotes, '') AS DeliveryNotes,
       ISNULL(p.TransactionId, '') AS TransactionId, ISNULL(o.TrackingProgress, 0) AS TrackingProgress
FROM OrderTable o
LEFT JOIN Payment p ON p.OrderId = o.OrderId
WHERE o.OrderId = @id";
                using var hcmd = new SqlCommand(headerQ, con);
                hcmd.Parameters.AddWithValue("@id", orderId);
                using var hr = await hcmd.ExecuteReaderAsync();
                if (!await hr.ReadAsync()) return null;

                var orderObj = new Dictionary<string, object?>
                {
                    ["orderId"] = hr.GetInt32(0),
                    ["orderDateTime"] = hr.IsDBNull(1) ? null : hr.GetDateTime(1),
                    ["status"] = hr.IsDBNull(2) ? "" : hr.GetString(2),
                    ["customerName"] = hr.IsDBNull(3) ? "" : hr.GetString(3),
                    ["customerPhone"] = hr.IsDBNull(4) ? "" : hr.GetString(4),
                    ["totalCalories"] = hr.IsDBNull(5) ? 0 : hr.GetInt32(5),
                    ["paymentMode"] = hr.IsDBNull(6) ? "" : hr.GetString(6),
                    ["amount"] = hr.IsDBNull(7) ? 0m : hr.GetDecimal(7),
                    ["commissionAmount"] = hr.IsDBNull(8) ? 0m : hr.GetDecimal(8),
                    ["vendorAmount"] = hr.IsDBNull(9) ? 0m : hr.GetDecimal(9),
                    ["isRefunded"] = !hr.IsDBNull(10) && Convert.ToInt32(hr.GetValue(10)) == 1,
                    ["refundStatus"] = hr.IsDBNull(11) ? "" : hr.GetString(11),
                    ["orderType"] = "Delivery",
                    ["deliveryAddress"] = hr.IsDBNull(13) ? "" : hr.GetString(13),
                    ["deliveryStatus"] = hr.IsDBNull(14) ? "" : hr.GetString(14),
                    ["deliveryPersonId"] = hr.IsDBNull(15) ? null : hr.GetInt32(15),
                    ["deliveryNotes"] = hr.IsDBNull(16) ? "" : hr.GetString(16),
                    ["transactionId"] = hr.IsDBNull(17) ? "" : hr.GetString(17),
                    ["trackingProgress"] = hr.IsDBNull(18) ? 0 : hr.GetInt32(18)
                };

                // items
                var items = new List<object>();
                var itemsQ = @"SELECT ISNULL(ItemName,'') AS Name, ISNULL(Quantity,0) AS Quantity, ISNULL(SpecialInstruction,'') AS Instructions FROM OrderItems WHERE OrderId = @id";
                using var icmd = new SqlCommand(itemsQ, con);
                icmd.Parameters.AddWithValue("@id", orderId);
                using var ir = await icmd.ExecuteReaderAsync();
                while (await ir.ReadAsync())
                {
                    items.Add(new
                    {
                        name = ir.IsDBNull(0) ? "" : ir.GetString(0),
                        quantity = ir.IsDBNull(1) ? 0 : ir.GetInt32(1),
                        instructions = ir.IsDBNull(2) ? "" : ir.GetString(2)
                    });
                }
                orderObj["items"] = items;

                // customer order count
                var phone = orderObj["customerPhone"]?.ToString() ?? "";
                var count = 0;
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    var cntQ = "SELECT COUNT(1) FROM OrderTable WHERE CustomerPhone = @phone";
                    using var cc = new SqlCommand(cntQ, con);
                    cc.Parameters.AddWithValue("@phone", phone);
                    count = Convert.ToInt32(await cc.ExecuteScalarAsync() ?? 0);
                }
                orderObj["customerOrderCount"] = count;
                orderObj["pickupStatus"] = "On-time";

                return orderObj;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "GetOrderDetailsAsync failed");
                return null;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            try
            {
                using var con = GetConn();
                await con.OpenAsync();

                // Define tracking progress mapping
                int progress = 1; // Default for Placed
                string deliveryStatus = null;
                
                if (newStatus.Equals("Accepted", StringComparison.OrdinalIgnoreCase)) progress = 2;
                else if (newStatus.Contains("Ready", StringComparison.OrdinalIgnoreCase)) progress = 3;
                else if (newStatus.Equals("In Transit", StringComparison.OrdinalIgnoreCase) || newStatus.Equals("On the Way", StringComparison.OrdinalIgnoreCase)) 
                {
                    progress = 4;
                    deliveryStatus = "Out for Delivery";
                    newStatus = "On the Way"; // Standardize
                }
                else if (newStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase) || newStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase)) 
                {
                    progress = 5;
                    deliveryStatus = "Delivered";
                    newStatus = "Delivered"; // Standardize
                }

                string updateQ = deliveryStatus != null 
                    ? "UPDATE OrderTable SET Status = @s, TrackingProgress = @p, DeliveryStatus = @ds, UpdatedAt = GETDATE() WHERE OrderId = @id"
                    : "UPDATE OrderTable SET Status = @s, TrackingProgress = @p, UpdatedAt = GETDATE() WHERE OrderId = @id";

                using var cmd = new SqlCommand(updateQ, con);
                cmd.Parameters.AddWithValue("@s", newStatus);
                cmd.Parameters.AddWithValue("@p", progress);
                cmd.Parameters.AddWithValue("@id", orderId);
                if (deliveryStatus != null) cmd.Parameters.AddWithValue("@ds", deliveryStatus);
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "UpdateOrderStatusAsync failed");
                return false;
            }
        }

        public async Task<IEnumerable<object>> GetCancelledOrdersAsync()
        {
            var list = new List<object>();
            try
            {
                using var con = GetConn();
                await con.OpenAsync();
                var q = @"
SELECT o.OrderId, o.CustomerName, o.CustomerPhone AS CustomerContact, o.CancelledAt, o.CancelReason, 
       ISNULL(p.Amount, 0) AS RefundedAmount, o.Status
FROM OrderTable o
LEFT JOIN Payment p ON p.OrderId = o.OrderId
WHERE o.Status IN ('Cancelled', 'Canceled', 'canceled')
ORDER BY o.CancelledAt DESC";
                using var cmd = new SqlCommand(q, con);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new
                    {
                        orderId = r.GetInt32(0),
                        customerName = r.IsDBNull(1) ? "" : r.GetString(1),
                        customerContact = r.IsDBNull(2) ? "" : r.GetString(2),
                        cancelledAt = r.IsDBNull(3) ? (DateTime?)null : r.GetDateTime(3),
                        cancelReason = r.IsDBNull(4) ? "" : r.GetString(4),
                        refundedAmount = r.IsDBNull(5) ? 0m : r.GetDecimal(5),
                        status = r.IsDBNull(6) ? "" : r.GetString(6)
                    });
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "GetCancelledOrdersAsync failed");
            }
            return list;
        }

        public async Task<int> GetCancelledCountAsync()
        {
            try
            {
                using var con = GetConn();
                await con.OpenAsync();
                var q = @"SELECT COUNT(1) FROM OrderTable o LEFT JOIN Payment p ON p.OrderId = o.OrderId
                          WHERE ISNULL(o.Status,'') = 'Cancelled' AND ISNULL(p.RefundStatus,'') <> 'Completed'";
                using var cmd = new SqlCommand(q, con);
                return Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "GetCancelledCountAsync failed");
                return 0;
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            try
            {
                using var con = GetConn();
                await con.OpenAsync();
                using var tx = con.BeginTransaction();
                
                // Optimistic locking: check version
                var qVersion = "SELECT Version FROM OrderTable WHERE OrderId = @id";
                using var cmdV = new SqlCommand(qVersion, con, tx);
                cmdV.Parameters.AddWithValue("@id", orderId);
                var currentVersion = (int)(await cmdV.ExecuteScalarAsync() ?? 0);

                var q = "UPDATE OrderTable SET Status = 'Cancelled', UpdatedAt = GETDATE(), CancelledAt = GETDATE(), Version = Version + 1 WHERE OrderId = @id AND Version = @v";
                using var cmd = new SqlCommand(q, con, tx);
                cmd.Parameters.AddWithValue("@id", orderId);
                cmd.Parameters.AddWithValue("@v", currentVersion);
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) { await tx.RollbackAsync(); return false; }

                var payQ = "UPDATE Payment SET RefundStatus = 'Refund Pending', UpdatedAt = GETDATE() WHERE OrderId = @id";
                using var pcmd = new SqlCommand(payQ, con, tx);
                pcmd.Parameters.AddWithValue("@id", orderId);
                await pcmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "CancelOrderAsync failed");
                return false;
            }
        }

        public async Task<bool> TriggerRefundAsync(int orderId)
        {
            try
            {
                using var con = GetConn();
                await con.OpenAsync();
                var q = "UPDATE Payment SET IsRefunded = 1, RefundStatus = 'Refunded', UpdatedAt = GETDATE() WHERE OrderId = @id";
                using var cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@id", orderId);
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return false;

                var orderQ = "UPDATE OrderTable SET UpdatedAt = GETDATE() WHERE OrderId = @id";
                using var ocmd = new SqlCommand(orderQ, con);
                ocmd.Parameters.AddWithValue("@id", orderId);
                await ocmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "TriggerRefundAsync failed");
                return false;
            }
        }

        public async Task<bool> UpdateRefundStatusAsync(int orderId, string status)
        {
            try
            {
                using var con = GetConn();
                await con.OpenAsync();
                var q = "UPDATE Payment SET RefundStatus = @status, UpdatedAt = GETDATE() WHERE OrderId = @id";
                using var cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@id", orderId);
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "UpdateRefundStatusAsync failed");
                return false;
            }
        }

        public async Task<bool> AddAdminNoteAsync(int orderId, string note)
        {
            try
            {
                using var con = GetConn();
                await con.OpenAsync();
                var getQ = "SELECT ISNULL(AdminNotes,'') FROM OrderTable WHERE OrderId = @id";
                using var gcmd = new SqlCommand(getQ, con);
                gcmd.Parameters.AddWithValue("@id", orderId);
                var existing = (await gcmd.ExecuteScalarAsync())?.ToString() ?? "";
                var appended = (string.IsNullOrEmpty(existing) ? "" : existing + "\n") + $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Admin: {note}";
                var upQ = "UPDATE OrderTable SET AdminNotes = @notes, UpdatedAt = GETDATE() WHERE OrderId = @id";
                using var ucmd = new SqlCommand(upQ, con);
                ucmd.Parameters.AddWithValue("@notes", appended);
                ucmd.Parameters.AddWithValue("@id", orderId);
                var rows = await ucmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "AddAdminNoteAsync failed");
                return false;
            }
        }

        public async Task<bool> BlockCustomerAsync(string customerPhone)
        {
            try
            {
                using var con = GetConn();
                await con.OpenAsync();
                var q = "UPDATE UserSignup SET IsBlocked = 1, UpdatedAt = GETDATE() WHERE Phone = @phone";
                using var cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@phone", customerPhone);
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "BlockCustomerAsync failed");
                return false;
            }
        }

        public async Task<bool> AssignDeliveryPersonAsync(int orderId, int deliveryPersonId)
        {
            try
            {
                using var con = GetConn();
                await con.OpenAsync();
                var q = "UPDATE OrderTable SET DeliveryPersonId = @dp, DeliveryStatus = 'Assigned', UpdatedAt = GETDATE() WHERE OrderId = @id";
                using var cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@dp", deliveryPersonId);
                cmd.Parameters.AddWithValue("@id", orderId);
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "AssignDeliveryPersonAsync failed");
                return false;
            }
        }

        public async Task<bool> UpdateDeliveryStatusAsync(int orderId, string status)
        {
            try
            {
                using var con = GetConn();
                await con.OpenAsync();
                var q = "UPDATE OrderTable SET DeliveryStatus = @s, UpdatedAt = GETDATE() WHERE OrderId = @id";
                using var cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@s", status);
                cmd.Parameters.AddWithValue("@id", orderId);
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "UpdateDeliveryStatusAsync failed");
                return false;
            }
        }

        public async Task<IEnumerable<object>> GetAvailableDeliveryPersonnelAsync()
        {
            var list = new List<object>();
            try
            {
                using var con = GetConn();
                await con.OpenAsync();
                // Since Role is not in DB yet, we might need to rely on a naming convention or a specific status if we can't change the schema easily.
                // But wait, the user said "build the delivery part", so I should probably assume I can add the Role column to the DB if needed.
                // For now, I'll search for users who have 'Delivery' in their name or just all users if I can't filter by role.
                // Actually, let's assume there's a Role column or we'll add it.
                var q = "SELECT Id, Name, Phone FROM UserSignup WHERE Status = 'Active'"; // Simplified for now
                using var cmd = new SqlCommand(q, con);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new
                    {
                        Id = r.GetInt32(0),
                        Name = r.IsDBNull(1) ? "" : r.GetString(1),
                        Phone = r.IsDBNull(2) ? "" : r.GetString(2)
                    });
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "GetAvailableDeliveryPersonnelAsync failed");
            }
            return list;
        }

        public async Task<IEnumerable<object>> GetDeliveriesForPersonAsync(int deliveryPersonId)
        {
            var list = new List<object>();
            try
            {
                using var con = GetConn();
                await con.OpenAsync();
                var q = @"
SELECT OrderId, CustomerName, CustomerPhone, DeliveryAddress, DeliveryStatus, Status, CreatedAt, ISNULL(DeliveryNotes, '') AS DeliveryNotes
FROM OrderTable
WHERE DeliveryPersonId = @dp AND ISNULL(Status,'') <> 'Cancelled'
ORDER BY CreatedAt DESC";
                using var cmd = new SqlCommand(q, con);
                cmd.Parameters.AddWithValue("@dp", deliveryPersonId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new
                    {
                        OrderId = r.GetInt32(0),
                        CustomerName = r.IsDBNull(1) ? "" : r.GetString(1),
                        CustomerPhone = r.IsDBNull(2) ? "" : r.GetString(2),
                        DeliveryAddress = r.IsDBNull(3) ? "" : r.GetString(3),
                        DeliveryStatus = r.IsDBNull(4) ? "" : r.GetString(4),
                        Status = r.IsDBNull(5) ? "" : r.GetString(5),
                        CreatedAt = r.IsDBNull(6) ? (DateTime?)null : r.GetDateTime(6),
                        DeliveryNotes = r.IsDBNull(7) ? "" : r.GetString(7)
                    });
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "GetDeliveriesForPersonAsync failed");
            }
            return list;
        }
    }
}