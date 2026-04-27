using Dapper;
using SupermarketPOS.Models;

namespace SupermarketPOS.Data;

public class DatabaseSeeder
{
    private readonly DatabaseContext _db;

    public DatabaseSeeder(DatabaseContext db)
    {
        _db = db;
    }

    public void Seed()
    {
        using var conn = _db.GetConnection();

        // Check if already seeded
        var userCount = conn.QuerySingle<int>("SELECT COUNT(*) FROM Users");
        if (userCount > 0) return;

        // Seed Categories
        var categories = new[]
        {
            new { Name = "Oziq-ovqat",    Description = "Oziq-ovqat mahsulotlari", Color = "#4CAF50" },
            new { Name = "Ichimliklar",   Description = "Suv, sharbat, gazli ichimliklar", Color = "#2196F3" },
            new { Name = "Sut mahsulotlari", Description = "Sut, qatiq, pishloq", Color = "#FFF9C4" },
            new { Name = "Non va shirinlik", Description = "Non, toʻliq donli mahsulotlar", Color = "#FF9800" },
            new { Name = "Go'sht",        Description = "Mol, qo'y, tovuq go'shti", Color = "#F44336" },
            new { Name = "Sabzavotlar",   Description = "Yangi sabzavotlar", Color = "#8BC34A" },
            new { Name = "Mevalar",       Description = "Yangi mevalar", Color = "#E91E63" },
            new { Name = "Uy kimyosi",    Description = "Tozalovchi vositalar", Color = "#9C27B0" },
            new { Name = "Gigiyena",      Description = "Shaxsiy gigiyena mahsulotlari", Color = "#00BCD4" },
            new { Name = "Boshqalar",     Description = "Boshqa mahsulotlar", Color = "#607D8B" },
        };

        foreach (var cat in categories)
        {
            conn.Execute(
                "INSERT OR IGNORE INTO Categories (Name, Description, Color) VALUES (@Name, @Description, @Color)",
                cat);
        }

        // Seed Admin User
        conn.Execute(@"
            INSERT OR IGNORE INTO Users (Username, PasswordHash, FullName, Role)
            VALUES ('admin', @Hash, 'Administrator', 'Admin')",
            new { Hash = BCrypt.Net.BCrypt.HashPassword("admin123") });

        conn.Execute(@"
            INSERT OR IGNORE INTO Users (Username, PasswordHash, FullName, Role)
            VALUES ('kassir', @Hash, 'Asosiy Kassir', 'Cashier')",
            new { Hash = BCrypt.Net.BCrypt.HashPassword("kassir123") });

        // Seed Sample Products
        var products = new List<object>
        {
            new { Barcode="8690000001001", Name="Coca-Cola 0.5L",          Price=8000m,  CostPrice=6000m,  Stock=150, LowStockThreshold=20, CategoryId=2, Unit="dona" },
            new { Barcode="8690000001002", Name="Pepsi 0.5L",              Price=7500m,  CostPrice=5500m,  Stock=120, LowStockThreshold=20, CategoryId=2, Unit="dona" },
            new { Barcode="8690000001003", Name="Sprite 0.5L",             Price=7500m,  CostPrice=5500m,  Stock=100, LowStockThreshold=20, CategoryId=2, Unit="dona" },
            new { Barcode="8690000002001", Name="Arzon non",               Price=3500m,  CostPrice=2500m,  Stock=80,  LowStockThreshold=15, CategoryId=4, Unit="dona" },
            new { Barcode="8690000002002", Name="Lipton choy 100g",        Price=25000m, CostPrice=18000m, Stock=60,  LowStockThreshold=10, CategoryId=1, Unit="quti" },
            new { Barcode="8690000002003", Name="Nescafe Classic 200g",    Price=55000m, CostPrice=42000m, Stock=40,  LowStockThreshold=8,  CategoryId=1, Unit="quti" },
            new { Barcode="8690000003001", Name="Sut 1L",                  Price=12000m, CostPrice=9000m,  Stock=90,  LowStockThreshold=20, CategoryId=3, Unit="litr" },
            new { Barcode="8690000003002", Name="Qatiq 500g",              Price=9000m,  CostPrice=6500m,  Stock=70,  LowStockThreshold=15, CategoryId=3, Unit="dona" },
            new { Barcode="8690000003003", Name="Pishloq 1kg",             Price=95000m, CostPrice=75000m, Stock=25,  LowStockThreshold=5,  CategoryId=3, Unit="kg"   },
            new { Barcode="8690000004001", Name="Tovuq go'shti 1kg",       Price=45000m, CostPrice=35000m, Stock=50,  LowStockThreshold=10, CategoryId=5, Unit="kg"   },
            new { Barcode="8690000004002", Name="Mol go'shti 1kg",         Price=85000m, CostPrice=68000m, Stock=30,  LowStockThreshold=5,  CategoryId=5, Unit="kg"   },
            new { Barcode="8690000005001", Name="Pomidor 1kg",             Price=8000m,  CostPrice=5000m,  Stock=200, LowStockThreshold=30, CategoryId=6, Unit="kg"   },
            new { Barcode="8690000005002", Name="Kartoshka 1kg",           Price=5000m,  CostPrice=3000m,  Stock=300, LowStockThreshold=50, CategoryId=6, Unit="kg"   },
            new { Barcode="8690000005003", Name="Piyoz 1kg",               Price=4000m,  CostPrice=2500m,  Stock=250, LowStockThreshold=40, CategoryId=6, Unit="kg"   },
            new { Barcode="8690000006001", Name="Olma 1kg",                Price=12000m, CostPrice=8000m,  Stock=100, LowStockThreshold=20, CategoryId=7, Unit="kg"   },
            new { Barcode="8690000006002", Name="Banan 1kg",               Price=18000m, CostPrice=13000m, Stock=80,  LowStockThreshold=15, CategoryId=7, Unit="kg"   },
            new { Barcode="8690000007001", Name="Ariel kir yuvish 3kg",    Price=85000m, CostPrice=65000m, Stock=35,  LowStockThreshold=8,  CategoryId=8, Unit="quti" },
            new { Barcode="8690000007002", Name="Fairy idish yuvish 500ml",Price=22000m, CostPrice=16000m, Stock=55,  LowStockThreshold=10, CategoryId=8, Unit="shisha"},
            new { Barcode="8690000008001", Name="Colgate tish pastasi 150g",Price=18000m,CostPrice=13000m, Stock=65,  LowStockThreshold=12, CategoryId=9, Unit="tuba" },
            new { Barcode="8690000008002", Name="Shampun Head&Shoulders 400ml",Price=45000m,CostPrice=35000m,Stock=40,LowStockThreshold=8, CategoryId=9, Unit="shisha"},
            new { Barcode="8690000001010", Name="Mineral suv 1.5L",        Price=5000m,  CostPrice=3000m,  Stock=200, LowStockThreshold=30, CategoryId=2, Unit="shisha"},
            new { Barcode="8690000001011", Name="Fruktoviy sharbat 1L",    Price=15000m, CostPrice=11000m, Stock=80,  LowStockThreshold=15, CategoryId=2, Unit="shisha"},
            new { Barcode="8690000002010", Name="Makaron 500g",            Price=12000m, CostPrice=8500m,  Stock=120, LowStockThreshold=20, CategoryId=1, Unit="paket" },
            new { Barcode="8690000002011", Name="Guruch 1kg",              Price=18000m, CostPrice=13000m, Stock=150, LowStockThreshold=25, CategoryId=1, Unit="kg"   },
            new { Barcode="8690000002012", Name="Un 2kg",                  Price=22000m, CostPrice=16000m, Stock=100, LowStockThreshold=20, CategoryId=1, Unit="paket" },
            new { Barcode="8690000002013", Name="Shakar 1kg",              Price=14000m, CostPrice=10000m, Stock=130, LowStockThreshold=25, CategoryId=1, Unit="kg"   },
            new { Barcode="8690000002014", Name="Tuz 1kg",                 Price=4000m,  CostPrice=2500m,  Stock=90,  LowStockThreshold=15, CategoryId=1, Unit="paket" },
            new { Barcode="8690000002015", Name="O'simlik yog'i 1L",       Price=28000m, CostPrice=22000m, Stock=75,  LowStockThreshold=15, CategoryId=1, Unit="shisha"},
            new { Barcode="8690000009001", Name="Batareyka AA 4ta",        Price=20000m, CostPrice=14000m, Stock=45,  LowStockThreshold=10, CategoryId=10,Unit="quti" },
            new { Barcode="8690000009002", Name="Polietilen paket 100ta",  Price=8000m,  CostPrice=5000m,  Stock=60,  LowStockThreshold=10, CategoryId=10,Unit="bog'" },
        };

        foreach (var p in products)
        {
            conn.Execute(@"
                INSERT OR IGNORE INTO Products 
                    (Barcode, Name, Price, CostPrice, Stock, LowStockThreshold, CategoryId, Unit)
                VALUES 
                    (@Barcode, @Name, @Price, @CostPrice, @Stock, @LowStockThreshold, @CategoryId, @Unit)",
                p);
        }
    }
}
