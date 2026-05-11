using Dapper;
using SupermarketPOS.Data;
using SupermarketPOS.Models;

namespace SupermarketPOS.Repositories;

public interface IExpenseRepository
{
    Task<int> CreateExpenseAsync(Expense expense);
    Task<IEnumerable<Expense>> GetExpensesByDateAsync(DateTime date);
    Task<IEnumerable<Expense>> GetExpensesByDateRangeAsync(DateTime from, DateTime to, int? categoryId = null);
    Task<IEnumerable<Expense>> GetExpensesByShiftAsync(int shiftId);
    Task<decimal> GetTotalExpensesByDateAsync(DateTime date);
    
    Task<IEnumerable<ExpenseCategory>> GetCategoriesAsync();
    Task<int> AddCategoryAsync(ExpenseCategory category);
}

public class ExpenseRepository : IExpenseRepository
{
    private readonly DatabaseContext _db;

    public ExpenseRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<int> CreateExpenseAsync(Expense expense)
    {
        using var conn = _db.GetConnection();
        return await conn.QuerySingleAsync<int>(@"
            INSERT INTO Expenses (Amount, CategoryId, Reason, CreatedAt, UserId, CashierName, ShiftId)
            VALUES (@Amount, @CategoryId, @Reason, @CreatedAt, @UserId, @CashierName, @ShiftId);
            SELECT last_insert_rowid();", expense);
    }

    public async Task<IEnumerable<Expense>> GetExpensesByDateAsync(DateTime date)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<Expense>(@"
            SELECT e.*, c.Name as CategoryName 
            FROM Expenses e
            LEFT JOIN ExpenseCategories c ON e.CategoryId = c.Id
            WHERE DATE(e.CreatedAt) = DATE(@Date)
            ORDER BY e.CreatedAt DESC", 
            new { Date = date.ToString("yyyy-MM-dd") });
    }

    public async Task<IEnumerable<Expense>> GetExpensesByDateRangeAsync(DateTime from, DateTime to, int? categoryId = null)
    {
        using var conn = _db.GetConnection();
        var sql = @"
            SELECT e.*, c.Name as CategoryName 
            FROM Expenses e
            LEFT JOIN ExpenseCategories c ON e.CategoryId = c.Id
            WHERE DATE(e.CreatedAt) >= DATE(@FromDate) AND DATE(e.CreatedAt) <= DATE(@ToDate)";
            
        if (categoryId.HasValue && categoryId.Value > 0)
            sql += " AND e.CategoryId = @CategoryId";
            
        sql += " ORDER BY e.CreatedAt DESC";

        return await conn.QueryAsync<Expense>(sql, 
            new { 
                FromDate = from.ToString("yyyy-MM-dd"), 
                ToDate = to.ToString("yyyy-MM-dd"),
                CategoryId = categoryId
            });
    }

    public async Task<IEnumerable<Expense>> GetExpensesByShiftAsync(int shiftId)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<Expense>(
            "SELECT * FROM Expenses WHERE ShiftId = @ShiftId ORDER BY CreatedAt DESC", 
            new { ShiftId = shiftId });
    }

    public async Task<decimal> GetTotalExpensesByDateAsync(DateTime date)
    {
        using var conn = _db.GetConnection();
        return await conn.QuerySingleOrDefaultAsync<decimal>(@"
            SELECT COALESCE(SUM(Amount), 0) FROM Expenses
            WHERE DATE(CreatedAt) = DATE(@Date)", 
            new { Date = date.ToString("yyyy-MM-dd") });
    }

    public async Task<IEnumerable<ExpenseCategory>> GetCategoriesAsync()
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<ExpenseCategory>("SELECT * FROM ExpenseCategories WHERE IsActive = 1 ORDER BY Name");
    }

    public async Task<int> AddCategoryAsync(ExpenseCategory category)
    {
        using var conn = _db.GetConnection();
        return await conn.QuerySingleAsync<int>(@"
            INSERT INTO ExpenseCategories (Name, IsActive)
            VALUES (@Name, @IsActive);
            SELECT last_insert_rowid();", category);
    }
}
