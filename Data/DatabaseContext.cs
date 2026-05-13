using Microsoft.Data.Sqlite;
using System.IO;

namespace SupermarketPOS.Data;

public class DatabaseContext
{
    private readonly string _connectionString;

    public DatabaseContext(string dbPath = "sotuvpos_v2.db")
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, dbPath);

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
    Stock             REAL    NOT NULL DEFAULT 0,
    LowStockThreshold REAL    NOT NULL DEFAULT 10,
    CategoryId        INTEGER NOT NULL DEFAULT 1,
    Unit              TEXT    NOT NULL DEFAULT 'dona',
    IsActive          INTEGER NOT NULL DEFAULT 1,
    ParentProductId   INTEGER,
    Multiplier        REAL    NOT NULL DEFAULT 1,
    CreatedAt         TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
    UpdatedAt         TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
    FOREIGN KEY (ParentProductId) REFERENCES Products(Id)
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
    Quantity    REAL    NOT NULL DEFAULT 1,
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
    DebtBalance     REAL    NOT NULL DEFAULT 0,
    DiscountPercent REAL    NOT NULL DEFAULT 0,
    DebtTermDays    INTEGER NOT NULL DEFAULT 30,
    OldestDebtDate  TEXT,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS DebtTransactions (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId  INTEGER NOT NULL,
    Amount      REAL    NOT NULL,
    Type        TEXT    NOT NULL, -- 'Given', 'Paid'
    SaleId      INTEGER,
    Notes       TEXT    DEFAULT '',
    CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (SaleId) REFERENCES Sales(Id)
);

CREATE TABLE IF NOT EXISTS ExpenseCategories (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT    NOT NULL UNIQUE,
    IsActive    INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Expenses (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Amount      REAL    NOT NULL,
    CategoryId  INTEGER,
    Reason      TEXT    NOT NULL,
    CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
    UserId      INTEGER NOT NULL,
    CashierName TEXT    NOT NULL DEFAULT '',
    ShiftId     INTEGER,
    FOREIGN KEY (CategoryId) REFERENCES ExpenseCategories(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (ShiftId) REFERENCES Shifts(Id)
);

CREATE INDEX IF NOT EXISTS idx_products_barcode ON Products(Barcode);
CREATE INDEX IF NOT EXISTS idx_products_name    ON Products(Name);
CREATE INDEX IF NOT EXISTS idx_sales_date       ON Sales(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_saleitems_sale   ON SaleItems(SaleId);
CREATE INDEX IF NOT EXISTS idx_shifts_status    ON Shifts(Status);
CREATE INDEX IF NOT EXISTS idx_customers_phone  ON Customers(Phone);
CREATE INDEX IF NOT EXISTS idx_debt_customer    ON DebtTransactions(CustomerId);
";
        using var cmd = conn.CreateCommand();
        cmd.CommandText = schema;
        cmd.ExecuteNonQuery();

        // Migrations
        try { using var u1 = conn.CreateCommand(); u1.CommandText = "ALTER TABLE Sales ADD COLUMN IsSynced INTEGER NOT NULL DEFAULT 0;"; u1.ExecuteNonQuery(); } catch { }
        try { using var u2 = conn.CreateCommand(); u2.CommandText = "ALTER TABLE Customers ADD COLUMN DebtBalance REAL NOT NULL DEFAULT 0;"; u2.ExecuteNonQuery(); } catch { }
        try { using var u3 = conn.CreateCommand(); u3.CommandText = "ALTER TABLE Products ADD COLUMN ParentProductId INTEGER;"; u3.ExecuteNonQuery(); } catch { }
        try { using var u4 = conn.CreateCommand(); u4.CommandText = "ALTER TABLE Products ADD COLUMN Multiplier REAL NOT NULL DEFAULT 1;"; u4.ExecuteNonQuery(); } catch { }
        try { using var u5 = conn.CreateCommand(); u5.CommandText = "ALTER TABLE Customers ADD COLUMN DebtTermDays INTEGER NOT NULL DEFAULT 30;"; u5.ExecuteNonQuery(); } catch { }
        try { using var u6 = conn.CreateCommand(); u6.CommandText = "ALTER TABLE Customers ADD COLUMN OldestDebtDate TEXT;"; u6.ExecuteNonQuery(); } catch { }
        try { using var u7 = conn.CreateCommand(); u7.CommandText = "ALTER TABLE Expenses ADD COLUMN CategoryId INTEGER;"; u7.ExecuteNonQuery(); } catch { }

        // Duplikat xarajatlarni tozalash (Pull Sync xatosini tuzatish)
        try { 
            using var dup = conn.CreateCommand(); 
            dup.CommandText = "DELETE FROM Expenses WHERE Id NOT IN (SELECT MIN(Id) FROM Expenses GROUP BY Amount, Reason, SUBSTR(CreatedAt,1,16));"; 
            var deleted = dup.ExecuteNonQuery();
            if (deleted > 0) System.Diagnostics.Debug.WriteLine($"🧹 {deleted} ta duplikat xarajat tozalandi!");
        } catch { }
    }

    public string GetDatabasePath()
    {
        return _connectionString.Replace("Data Source=", "").Split(';')[0];
    }
}
