using Dapper;
using SupermarketPOS.Data;
using SupermarketPOS.Models;

namespace SupermarketPOS.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<IEnumerable<User>> GetAllAsync();
    Task<int> AddAsync(User user);
    Task<bool> UpdateAsync(User user);
    Task<bool> UpdateLastLoginAsync(int userId);
    Task<bool> DeleteAsync(int id);
}

public class UserRepository : IUserRepository
{
    private readonly DatabaseContext _db;

    public UserRepository(DatabaseContext db) => _db = db;

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Username=@Username AND IsActive=1",
            new { Username = username });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<User>(
            "SELECT Id, Username, FullName, Role, IsActive, CreatedAt, LastLogin FROM Users ORDER BY FullName");
    }

    public async Task<int> AddAsync(User user)
    {
        using var conn = _db.GetConnection();
        return await conn.QuerySingleAsync<int>(@"
            INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive)
            VALUES (@Username, @PasswordHash, @FullName, @Role, @IsActive);
            SELECT last_insert_rowid();", user);
    }

    public async Task<bool> UpdateAsync(User user)
    {
        using var conn = _db.GetConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Users SET FullName=@FullName, Role=@Role, IsActive=@IsActive, PasswordHash=@PasswordHash
            WHERE Id=@Id", user);
        return rows > 0;
    }

    public async Task<bool> UpdateLastLoginAsync(int userId)
    {
        using var conn = _db.GetConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Users SET LastLogin=datetime('now','localtime') WHERE Id=@Id",
            new { Id = userId });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.GetConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE Users SET IsActive=0 WHERE Id=@Id", new { Id = id });
        return rows > 0;
    }
}
