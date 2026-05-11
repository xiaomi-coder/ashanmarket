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
    public int DebtTermDays { get; set; } = 30; // Qarzni qaytarish muddati (kunlarda)
    public DateTime? OldestDebtDate { get; set; } // Yopilmagan eng eski qarz sanasi
    
    public int OverdueDays => OldestDebtDate.HasValue ? (int)(DateTime.Now - OldestDebtDate.Value).TotalDays : 0;
    public bool IsOverdue => OldestDebtDate.HasValue && OverdueDays > DebtTermDays;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
