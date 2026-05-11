namespace SupermarketPOS.Models;

public class Sale
{
    public int Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Change { get; set; }
    public string PaymentMethod { get; set; } = "Naqd";
    public string Status { get; set; } = "Completed";
    public bool IsSynced { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<SaleItem> Items { get; set; } = new();
}

public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public double Quantity { get; set; }
    public decimal Discount { get; set; }
    public decimal Total => UnitPrice * (decimal)Quantity - Discount;
    public decimal Profit => (UnitPrice - CostPrice) * (decimal)Quantity;
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Cashier"; // Admin, Cashier
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLogin { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#2196F3";
    public bool IsActive { get; set; } = true;
}

public class DailySalesReport
{
    public DateTime Date { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalDiscount { get; set; }
    public List<TopProduct> TopProducts { get; set; } = new();
}

public class TopProduct
{
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public double QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Profit { get; set; }
}
