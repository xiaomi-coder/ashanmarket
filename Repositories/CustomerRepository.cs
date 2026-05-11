using Dapper;
using SupermarketPOS.Data;
using SupermarketPOS.Models;

namespace SupermarketPOS.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByPhoneAsync(string phone);
    Task<int> CreateAsync(Customer customer);
    Task UpdateTotalSpentAsync(int customerId, decimal amount);
    Task<IEnumerable<Customer>> GetAllWithDebtAsync();
    Task<IEnumerable<DebtTransaction>> GetDebtTransactionsAsync(int customerId);
    Task<int> AddDebtTransactionAsync(DebtTransaction transaction);
    Task UpdateDebtTermAsync(int customerId, int termDays);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly DatabaseContext _db;

    public CustomerRepository(DatabaseContext db) => _db = db;

    public async Task<Customer?> GetByPhoneAsync(string phone)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Customer>(
            "SELECT * FROM Customers WHERE Phone = @Phone", new { Phone = phone });
    }

    public async Task<int> CreateAsync(Customer customer)
    {
        using var conn = _db.GetConnection();
        var sql = @"
            INSERT INTO Customers (Phone, Name, DiscountPercent, DebtTermDays)
            VALUES (@Phone, @Name, @DiscountPercent, @DebtTermDays);
            SELECT last_insert_rowid();";
        return await conn.ExecuteScalarAsync<int>(sql, customer);
    }

    public async Task UpdateTotalSpentAsync(int customerId, decimal amount)
    {
        using var conn = _db.GetConnection();
        await conn.ExecuteAsync(
            "UPDATE Customers SET TotalSpent = TotalSpent + @Amount WHERE Id = @Id",
            new { Amount = amount, Id = customerId });
    }

    public async Task<IEnumerable<Customer>> GetAllWithDebtAsync()
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<Customer>(
            "SELECT * FROM Customers WHERE DebtBalance > 0 ORDER BY Name ASC");
    }

    public async Task<IEnumerable<DebtTransaction>> GetDebtTransactionsAsync(int customerId)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<DebtTransaction>(
            "SELECT * FROM DebtTransactions WHERE CustomerId = @CustomerId ORDER BY CreatedAt DESC", 
            new { CustomerId = customerId });
    }

    public async Task<int> AddDebtTransactionAsync(DebtTransaction transaction)
    {
        using var conn = _db.GetConnection();
        using var dbTransaction = conn.BeginTransaction();
        try
        {
            var sql = @"
                INSERT INTO DebtTransactions (CustomerId, Amount, Type, SaleId, Notes)
                VALUES (@CustomerId, @Amount, @Type, @SaleId, @Notes);
                SELECT last_insert_rowid();";
            
            var id = await conn.ExecuteScalarAsync<int>(sql, transaction, dbTransaction);

            var balanceChange = transaction.Type == "Given" ? transaction.Amount : -transaction.Amount;
            
            var updateSql = @"
                UPDATE Customers 
                SET DebtBalance = DebtBalance + @Change,
                    OldestDebtDate = CASE 
                        WHEN OldestDebtDate IS NULL AND (DebtBalance + @Change) > 0 THEN datetime('now','localtime')
                        WHEN (DebtBalance + @Change) <= 0 THEN NULL
                        ELSE OldestDebtDate
                    END
                WHERE Id = @Id";
                
            await conn.ExecuteAsync(updateSql, new { Change = balanceChange, Id = transaction.CustomerId }, dbTransaction);

            dbTransaction.Commit();
            return id;
        }
        catch
        {
            dbTransaction.Rollback();
            throw;
        }
    }

    public async Task UpdateDebtTermAsync(int customerId, int termDays)
    {
        using var conn = _db.GetConnection();
        await conn.ExecuteAsync("UPDATE Customers SET DebtTermDays = @Days WHERE Id = @Id", new { Days = termDays, Id = customerId });
    }
}
