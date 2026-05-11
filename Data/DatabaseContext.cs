using Microsoft.Data.Sqlite;
using System.IO;

namespace SupermarketPOS.Data;

public class DatabaseContext
{
    private readonly string _connectionString;

    public DatabaseContext(string dbPath = "supermarket_pos.db")
    {
        var fullPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SupermarketPOS",
            dbPath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _connectionString = $"Data Source={fullPath};Cache=Shared;";
        InitializeDatabase();
    }

    public SqliteConnection GetConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        // Enable WAL mode for performance
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA cache_size=10000; PRAGMA foreign_keys=ON;";
        cmd.ExecuteNonQuery();
        return conn;
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var schema = @"
PRAGMA journal_mode=WAL;
PRAGMA foreign_keys=ON;

CREATE TABLE IF NOT EXISTS Categories (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT    NOT NULL UNIQUE,
    Description TEXT    DEFAULT '',
    Color       TEXT    DEFAULT '#2196F3',
    IsActive    INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Products (
    Id                INTEGER PRIMARY KEY AUTOINCREMENT,
    Barcode           TEXT    NOT NULL UNIQUE,
    Name              TEXT    NOT NULL,
    Price             REAL    NOT NULL DEFAULT 0,
    CostPrice         REAL    NOT NULL DEFAULT 0,
    Stock             INTEGER NOT NULL DEFAULT 0,
    LowStockThreshold INTEGER NOT NULL DEFAULT 10,
    CategoryId        INTEGER NOT NULL DEFAULT 1,
    Unit              TEXT    NOT NULL DEFAULT 'dona',
    IsActive          INTEGER NOT NULL DEFAULT 1,
    CreatedAt         TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
    UpdatedAt         TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);

CREATE TABLE IF NOT EXISTS Users (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Username     TEXT    NOT NULL UNIQUE,
    PasswordHash TEXT    NOT NULL,
    FullName     TEXT    NOT NULL,
    Role         TEXT    NOT NULL DEFAULT 'Cashier',
    IsActive     INTEGER NOT NULL DEFAULT 1,
    CreatedAt    TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
    LastLogin    TEXT
);

CREATE TABLE IF NOT EXISTS Sales (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleNumber    TEXT    NOT NULL UNIQUE,
    UserId        INTEGER NOT NULL,
    CashierName   TEXT    NOT NULL DEFAULT '',
    SubTotal      REAL    NOT NULL DEFAULT 0,
    Discount      REAL    NOT NULL DEFAULT 0,
    Tax           REAL    NOT NULL DEFAULT 0,
    Total         REAL    NOT NULL DEFAULT 0,
    AmountPaid    REAL    NOT NULL DEFAULT 0,
    Change        REAL    NOT NULL DEFAULT 0,
    PaymentMethod TEXT    NOT NULL DEFAULT 'Naqd',
    Status        TEXT    NOT NULL DEFAULT 'Completed',
    IsSynced      INTEGER NOT NULL DEFAULT 0,
    CreatedAt     TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS SaleItems (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleId      INTEGER NOT NULL,
    ProductId   INTEGER NOT NULL,
    ProductName TEXT    NOT NULL,
    Barcode     TEXT    NOT NULL DEFAULT '',
    UnitPrice   REAL    NOT NULL DEFAULT 0,
    CostPrice   REAL    NOT NULL DEFAULT 0,
    Quantity    INTEGER NOT NULL DEFAULT 1,
    Discount    REAL    NOT NULL DEFAULT 0,
    FOREIGN KEY (SaleId)    REFERENCES Sales(Id)    ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE IF NOT EXISTS Shifts (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    CashierId       INTEGER NOT NULL,
    CashierName     TEXT    NOT NULL,
    OpenedAt        TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
    ClosedAt        TEXT,
    StartingBalance REAL    NOT NULL DEFAULT 0,
    ExpectedBalance REAL    NOT NULL DEFAULT 0,
    ActualBalance   REAL    NOT NULL DEFAULT 0,
    Status          TEXT    NOT NULL DEFAULT 'Open',
    FOREIGN KEY (CashierId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS Customers (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Phone           TEXT    NOT NULL UNIQUE,
    Name            TEXT    NOT NULL,
    TotalSpent      REAL    NOT NULL DEFAULT 0,
    DiscountPercent REAL    NOT NULL DEFAULT 0,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS Expenses (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Amount      REAL    NOT NULL,
    Reason      TEXT    NOT NULL,
    CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
    UserId      INTEGER NOT NULL,
    CashierName TEXT    NOT NULL DEFAULT '',
    ShiftId     INTEGER,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (ShiftId) REFERENCES Shifts(Id)
);

CREATE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);
CREATE INDEX IF NOT EXISTS idx_products_name    ON Products(Name);
CREATE INDEX IF NOT EXISTS idx_sales_date       ON Sales(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_saleitems_sale   ON SaleItems(SaleId);
CREATE INDEX IF NOT EXISTS idx_shifts_status    ON Shifts(Status);
CREATE INDEX IF NOT EXISTS idx_customers_phone  ON Customers(Phone);
";
        using var cmd = conn.CreateCommand();
        cmd.CommandText = schema;
        cmd.ExecuteNonQuery();

        try
        {
            using var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = "ALTER TABLE Sales ADD COLUMN IsSynced INTEGER NOT NULL DEFAULT 0;";
            updateCmd.ExecuteNonQuery();
        }
        catch
        {
            // Column already exists
        }
    }

    public string GetDatabasePath()
    {
        return _connectionString.Replace("Data Source=", "").Split(';')[0];
    }
}
