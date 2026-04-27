using Dapper;
using SupermarketPOS.Data;
using SupermarketPOS.Models;

namespace SupermarketPOS.Repositories;

public interface IShiftRepository
{
    Task<Shift?> GetOpenShiftAsync();
    Task<Shift?> GetOpenShiftByCashierAsync(int cashierId);
    Task<int> CreateShiftAsync(Shift shift);
    Task UpdateShiftAsync(Shift shift);
}

public class ShiftRepository : IShiftRepository
{
    private readonly DatabaseContext _db;

    public ShiftRepository(DatabaseContext db) => _db = db;

    public async Task<Shift?> GetOpenShiftAsync()
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Shift>(
            "SELECT * FROM Shifts WHERE Status = 'Open'");
    }

    public async Task<Shift?> GetOpenShiftByCashierAsync(int cashierId)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Shift>(
            "SELECT * FROM Shifts WHERE Status = 'Open' AND CashierId = @CashierId",
            new { CashierId = cashierId });
    }

    public async Task<int> CreateShiftAsync(Shift shift)
    {
        using var conn = _db.GetConnection();
        var sql = @"
            INSERT INTO Shifts (CashierId, CashierName, OpenedAt, StartingBalance, ExpectedBalance, ActualBalance, Status)
            VALUES (@CashierId, @CashierName, @OpenedAt, @StartingBalance, @ExpectedBalance, @ActualBalance, @Status);
            SELECT last_insert_rowid();";
            
        return await conn.ExecuteScalarAsync<int>(sql, shift);
    }

    public async Task UpdateShiftAsync(Shift shift)
    {
        using var conn = _db.GetConnection();
        var sql = @"
            UPDATE Shifts SET
                ClosedAt = @ClosedAt,
                ExpectedBalance = @ExpectedBalance,
                ActualBalance = @ActualBalance,
                Status = @Status
            WHERE Id = @Id";
            
        await conn.ExecuteAsync(sql, shift);
    }
}
