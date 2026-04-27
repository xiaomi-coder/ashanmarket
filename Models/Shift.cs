using System;

namespace SupermarketPOS.Models;

public class Shift
{
    public int Id { get; set; }
    public int CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; } = DateTime.Now;
    public DateTime? ClosedAt { get; set; }
    public decimal StartingBalance { get; set; } // Smena ochilganda kassadagi pul
    public decimal ExpectedBalance { get; set; } // Dastur bo'yicha kutilayotgan pul (Start + savdolar)
    public decimal ActualBalance { get; set; }   // Kassir smenani yopganda kiritgan haqiqiy pul
    public decimal Difference => ActualBalance - ExpectedBalance; // Kamomad yoki ortiqcha
    public string Status { get; set; } = "Open"; // Open, Closed
}
