using System;

namespace SupermarketPOS.Models;

public class Expense
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int UserId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public int? ShiftId { get; set; }
}
