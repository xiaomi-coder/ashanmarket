namespace SupermarketPOS.Models;

public class Product
{
    public int Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public double Stock { get; set; }
    public double LowStockThreshold { get; set; } = 10;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = "dona";
    public int? ParentProductId { get; set; } // Asosiy mahsulot (Fleyka uchun dona tuxum ID si)
    public double Multiplier { get; set; } = 1; // 1 fleyka = 30 dona
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsLowStock => Stock <= LowStockThreshold;
}
