using System;

namespace SupermarketPOS.Models;

public class DebtTransaction
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = "Given"; // "Given" (Qarzga mol oldi) or "Paid" (Pul olib kelib to'ladi)
    public int? SaleId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
