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

        // Users and Products are now fetched from the cloud during First-Run Sync.
        // No dummy data is seeded locally to prevent polluting the tenant's database.
    }
}
