using Dapper;
using SupermarketPOS.Data;
using SupermarketPOS.Models;

namespace SupermarketPOS.Repositories;

public interface IExpenseRepository
{
    Task<int> CreateExpenseAsync(Expense expense);
    Task<IEnumerable<Expense>> GetExpensesByDateAsync(DateTime date);
    Task<IEnumerable<Expense>> GetExpensesByShiftAsync(int shiftId);
    Task<decimal> GetTotalExpensesByDateAsync(DateTime date);
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
            INSERT INTO Expenses (Amount, Reason, UserId, CashierName, ShiftId)
            VALUES (@Amount, @Reason, @UserId, @CashierName, @ShiftId);
            SELECT last_insert_rowid();", expense);
    }

    public async Task<IEnumerable<Expense>> GetExpensesByDateAsync(DateTime date)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<Expense>(@"
            SELECT * FROM Expenses
            WHERE DATE(CreatedAt) = DATE(@Date)", 
            new { Date = date.ToString("yyyy-MM-dd") });
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
}
