using Dapper;
using SupermarketPOS.Data;
using SupermarketPOS.Models;

namespace SupermarketPOS.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByPhoneAsync(string phone);
    Task<int> CreateAsync(Customer customer);
    Task UpdateTotalSpentAsync(int customerId, decimal amount);
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
            INSERT INTO Customers (Phone, Name, DiscountPercent)
            VALUES (@Phone, @Name, @DiscountPercent);
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
}
