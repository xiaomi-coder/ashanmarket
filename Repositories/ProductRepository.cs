using Dapper;
using SupermarketPOS.Data;
using SupermarketPOS.Models;

namespace SupermarketPOS.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<IEnumerable<Product>> GetAllAsync(bool activeOnly = true);
    Task<IEnumerable<Product>> SearchAsync(string query);
    Task<IEnumerable<Product>> GetLowStockAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<int> AddAsync(Product product);
    Task<bool> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
    Task<bool> UpdateStockAsync(int productId, int quantityChange);
    Task<IEnumerable<Category>> GetCategoriesAsync();
    Task<int> AddCategoryAsync(Category category);
    Task<bool> UpdateCategoryAsync(Category category);
    Task<bool> DeleteCategoryAsync(int id);
}

public class ProductRepository : IProductRepository
{
    private readonly DatabaseContext _db;

    public ProductRepository(DatabaseContext db) => _db = db;

    public async Task<Product?> GetByBarcodeAsync(string barcode)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>(@"
            SELECT p.*, c.Name AS CategoryName 
            FROM Products p
            LEFT JOIN Categories c ON p.CategoryId = c.Id
            WHERE p.Barcode = @Barcode AND p.IsActive = 1",
            new { Barcode = barcode });
    }

    public async Task<IEnumerable<Product>> GetAllAsync(bool activeOnly = true)
    {
        using var conn = _db.GetConnection();
        var sql = activeOnly
            ? "SELECT p.*, c.Name AS CategoryName FROM Products p LEFT JOIN Categories c ON p.CategoryId = c.Id WHERE p.IsActive = 1 ORDER BY p.Name"
            : "SELECT p.*, c.Name AS CategoryName FROM Products p LEFT JOIN Categories c ON p.CategoryId = c.Id ORDER BY p.Name";
        return await conn.QueryAsync<Product>(sql);
    }

    public async Task<IEnumerable<Product>> SearchAsync(string query)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<Product>(@"
            SELECT p.*, c.Name AS CategoryName 
            FROM Products p
            LEFT JOIN Categories c ON p.CategoryId = c.Id
            WHERE p.IsActive = 1 
              AND (p.Name LIKE @Q OR p.Barcode LIKE @Q)
            ORDER BY p.Name
            LIMIT 50",
            new { Q = $"%{query}%" });
    }

    public async Task<IEnumerable<Product>> GetLowStockAsync()
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<Product>(@"
            SELECT p.*, c.Name AS CategoryName 
            FROM Products p
            LEFT JOIN Categories c ON p.CategoryId = c.Id
            WHERE p.IsActive = 1 AND p.Stock <= p.LowStockThreshold
            ORDER BY p.Stock ASC");
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>(@"
            SELECT p.*, c.Name AS CategoryName 
            FROM Products p
            LEFT JOIN Categories c ON p.CategoryId = c.Id
            WHERE p.Id = @Id", new { Id = id });
    }

    public async Task<int> AddAsync(Product product)
    {
        using var conn = _db.GetConnection();
        return await conn.QuerySingleAsync<int>(@"
            INSERT INTO Products (Barcode, Name, Price, CostPrice, Stock, LowStockThreshold, CategoryId, Unit, IsActive)
            VALUES (@Barcode, @Name, @Price, @CostPrice, @Stock, @LowStockThreshold, @CategoryId, @Unit, @IsActive);
            SELECT last_insert_rowid();", product);
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        using var conn = _db.GetConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Products 
            SET Barcode=@Barcode, Name=@Name, Price=@Price, CostPrice=@CostPrice,
                Stock=@Stock, LowStockThreshold=@LowStockThreshold, CategoryId=@CategoryId,
                Unit=@Unit, IsActive=@IsActive, UpdatedAt=datetime('now','localtime')
            WHERE Id=@Id", product);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.GetConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE Products SET IsActive=0 WHERE Id=@Id", new { Id = id });
        return rows > 0;
    }

    public async Task<bool> UpdateStockAsync(int productId, int quantityChange)
    {
        using var conn = _db.GetConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Products 
            SET Stock = Stock + @Change, UpdatedAt=datetime('now','localtime')
            WHERE Id=@Id",
            new { Change = quantityChange, Id = productId });
        return rows > 0;
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<Category>(
            "SELECT * FROM Categories WHERE IsActive=1 ORDER BY Name");
    }

    public async Task<int> AddCategoryAsync(Category category)
    {
        using var conn = _db.GetConnection();
        return await conn.QuerySingleAsync<int>(@"
            INSERT INTO Categories (Name, Description, Color) 
            VALUES (@Name, @Description, @Color);
            SELECT last_insert_rowid();", category);
    }

    public async Task<bool> UpdateCategoryAsync(Category category)
    {
        using var conn = _db.GetConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Categories 
            SET Name=@Name, Description=@Description, Color=@Color
            WHERE Id=@Id", category);
        return rows > 0;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        using var conn = _db.GetConnection();
        // Set IsActive to 0 instead of hard delete
        var rows = await conn.ExecuteAsync(
            "UPDATE Categories SET IsActive=0 WHERE Id=@Id", new { Id = id });
        return rows > 0;
    }
}
