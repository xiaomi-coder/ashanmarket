using System;

namespace SupermarketPOS.Models;

public class Customer
{
    public int Id { get; set; }
    public string Phone { get; set; } = string.Empty; // e.g. 998901234567
    public string Name { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; } // O'sha mijoz jami qancha savdo qilgan
    public decimal DebtBalance { get; set; } // Olinmagan qarzlar qoldig'i
    public decimal DiscountPercent { get; set; } = 0; // Mijozga berilgan doimiy chegirma foizi (0-100)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
