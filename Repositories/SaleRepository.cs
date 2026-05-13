using Dapper;
using SupermarketPOS.Data;
using SupermarketPOS.Models;

namespace SupermarketPOS.Repositories;

public interface ISaleRepository
{
    Task<int> CreateSaleAsync(Sale sale);
    Task<Sale?> GetByIdAsync(int id);
    Task<Sale?> GetBySaleNumberAsync(string saleNumber);
    Task<IEnumerable<Sale>> GetSalesAsync(DateTime from, DateTime to);
    Task<IEnumerable<Sale>> GetSalesByCashierAndDateAsync(int cashierId, DateTime from, DateTime to);
    Task<DailySalesReport> GetDailyReportAsync(DateTime date);
    Task<IEnumerable<TopProduct>> GetTopProductsAsync(DateTime from, DateTime to, int limit = 10);
    Task<decimal> GetRevenueSummaryAsync(DateTime from, DateTime to);
    Task<string> GenerateSaleNumberAsync();
    
    // Sync
    Task<IEnumerable<Sale>> GetUnsyncedSalesAsync();
    Task MarkSalesAsSyncedAsync(IEnumerable<int> saleIds);
}

public class SaleRepository : ISaleRepository
{
    private readonly DatabaseContext _db;

    public SaleRepository(DatabaseContext db) => _db = db;

    public async Task<int> CreateSaleAsync(Sale sale)
    {
        using var conn = _db.GetConnection();
        using var transaction = conn.BeginTransaction();
        try
        {
            var saleId = await conn.QuerySingleAsync<int>(@"
                INSERT INTO Sales 
                    (SaleNumber, UserId, CashierName, SubTotal, Discount, Tax, Total, AmountPaid, Change, PaymentMethod, Status)
                VALUES 
                    (@SaleNumber, @UserId, @CashierName, @SubTotal, @Discount, @Tax, @Total, @AmountPaid, @Change, @PaymentMethod, @Status);
                SELECT last_insert_rowid();", sale, transaction);

            foreach (var item in sale.Items)
            {
                item.SaleId = saleId;
                await conn.ExecuteAsync(@"
                    INSERT INTO SaleItems (SaleId, ProductId, ProductName, Barcode, UnitPrice, CostPrice, Quantity, Discount)
                    VALUES (@SaleId, @ProductId, @ProductName, @Barcode, @UnitPrice, @CostPrice, @Quantity, @Discount)",
                    item, transaction);

                // Get product to check if it has a Parent
                var prod = await conn.QuerySingleOrDefaultAsync<Product>(
                    "SELECT ParentProductId, Multiplier FROM Products WHERE Id = @Id", 
                    new { Id = item.ProductId }, transaction);

                if (prod != null && prod.ParentProductId.HasValue && prod.ParentProductId.Value > 0)
                {
                    // It's a compound product (e.g. Fleyka). Decrement Parent stock.
                    await conn.ExecuteAsync(@"
                        UPDATE Products SET Stock = Stock - @TotalQty 
                        WHERE Id = @ParentId",
                        new { TotalQty = item.Quantity * prod.Multiplier, ParentId = prod.ParentProductId.Value }, transaction);
                }
                else
                {
                    // Normal product
                    await conn.ExecuteAsync(@"
                        UPDATE Products SET Stock = Stock - @Qty 
                        WHERE Id = @ProductId",
                        new { Qty = item.Quantity, item.ProductId }, transaction);
                }
            }

            transaction.Commit();
            return saleId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<Sale?> GetByIdAsync(int id)
    {
        using var conn = _db.GetConnection();
        var sale = await conn.QueryFirstOrDefaultAsync<Sale>(
            "SELECT * FROM Sales WHERE Id=@Id", new { Id = id });
        if (sale == null) return null;

        var items = await conn.QueryAsync<SaleItem>(
            "SELECT * FROM SaleItems WHERE SaleId=@SaleId", new { SaleId = id });
        sale.Items = items.ToList();
        return sale;
    }

    public async Task<Sale?> GetBySaleNumberAsync(string saleNumber)
    {
        using var conn = _db.GetConnection();
        var sale = await conn.QueryFirstOrDefaultAsync<Sale>(
            "SELECT * FROM Sales WHERE SaleNumber=@SaleNumber", new { SaleNumber = saleNumber });
        if (sale == null) return null;

        var items = await conn.QueryAsync<SaleItem>(
            "SELECT * FROM SaleItems WHERE SaleId=@SaleId", new { SaleId = sale.Id });
        sale.Items = items.ToList();
        return sale;
    }

    public async Task<IEnumerable<Sale>> GetSalesAsync(DateTime from, DateTime to)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<Sale>(@"
            SELECT * FROM Sales 
            WHERE CreatedAt >= @From AND CreatedAt < @To
            ORDER BY CreatedAt DESC",
            new { From = from.ToString("yyyy-MM-dd"), To = to.AddDays(1).ToString("yyyy-MM-dd") });
    }

    public async Task<IEnumerable<Sale>> GetSalesByCashierAndDateAsync(int cashierId, DateTime from, DateTime to)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<Sale>(@"
            SELECT * FROM Sales 
            WHERE UserId = @CashierId AND CreatedAt >= @From AND CreatedAt <= @To AND Status = 'Completed'",
            new { CashierId = cashierId, From = from.ToString("yyyy-MM-dd HH:mm:ss"), To = to.ToString("yyyy-MM-dd HH:mm:ss") });
    }

    public async Task<DailySalesReport> GetDailyReportAsync(DateTime date)
    {
        using var conn = _db.GetConnection();
        var dateStr = date.ToString("yyyy-MM-dd");

        var summary = await conn.QueryFirstOrDefaultAsync(@"
            SELECT 
                COUNT(*) AS TotalTransactions,
                COALESCE(SUM(Total),0) AS TotalRevenue,
                COALESCE(SUM(Discount),0) AS TotalDiscount
            FROM Sales
            WHERE DATE(CreatedAt) = @Date AND Status = 'Completed'",
            new { Date = dateStr });

        var costData = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT COALESCE(SUM(si.CostPrice * si.Quantity), 0) AS TotalCost
            FROM SaleItems si
            INNER JOIN Sales s ON si.SaleId = s.Id
            WHERE DATE(s.CreatedAt) = @Date AND s.Status = 'Completed'",
            new { Date = dateStr });

        decimal totalRevenue = Convert.ToDecimal(summary?.TotalRevenue ?? 0);
        decimal totalCost = Convert.ToDecimal(costData?.TotalCost ?? 0);

        var topProducts = await GetTopProductsAsync(date, date, 5);

        return new DailySalesReport
        {
            Date = date,
            TotalTransactions = (int)(summary?.TotalTransactions ?? 0),
            TotalRevenue = totalRevenue,
            TotalCost = totalCost,
            TotalProfit = totalRevenue - totalCost,
            TotalDiscount = (decimal)(summary?.TotalDiscount ?? 0),
            TopProducts = topProducts.ToList()
        };
    }

    public async Task<IEnumerable<TopProduct>> GetTopProductsAsync(DateTime from, DateTime to, int limit = 10)
    {
        using var conn = _db.GetConnection();
        return await conn.QueryAsync<TopProduct>(@"
            SELECT 
                si.ProductName,
                si.Barcode,
                SUM(si.Quantity) AS QuantitySold,
                SUM(si.UnitPrice * si.Quantity) AS Revenue,
                SUM((si.UnitPrice - si.CostPrice) * si.Quantity) AS Profit
            FROM SaleItems si
            INNER JOIN Sales s ON si.SaleId = s.Id
            WHERE DATE(s.CreatedAt) >= @From AND DATE(s.CreatedAt) <= @To
              AND s.Status = 'Completed'
            GROUP BY si.ProductId, si.ProductName, si.Barcode
            ORDER BY QuantitySold DESC
            LIMIT @Limit",
            new
            {
                From = from.ToString("yyyy-MM-dd"),
                To = to.ToString("yyyy-MM-dd"),
                Limit = limit
            });
    }

    public async Task<decimal> GetRevenueSummaryAsync(DateTime from, DateTime to)
    {
        using var conn = _db.GetConnection();
        return await conn.QuerySingleAsync<decimal>(@"
            SELECT COALESCE(SUM(Total),0) FROM Sales
            WHERE DATE(CreatedAt) >= @From AND DATE(CreatedAt) <= @To AND Status='Completed'",
            new { From = from.ToString("yyyy-MM-dd"), To = to.ToString("yyyy-MM-dd") });
    }

    public async Task<string> GenerateSaleNumberAsync()
    {
        using var conn = _db.GetConnection();
        var count = await conn.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM Sales WHERE DATE(CreatedAt) = DATE('now','localtime')");
        var rnd = Guid.NewGuid().ToString("N")[..4].ToUpper(); // 4 xonali noyob kod
        return $"{DateTime.Now:yyyyMMdd}-{(count + 1):D4}-{rnd}";
    }

    public async Task<IEnumerable<Sale>> GetUnsyncedSalesAsync()
    {
        using var conn = _db.GetConnection();
        var sales = await conn.QueryAsync<Sale>(@"
            SELECT * FROM Sales 
            WHERE IsSynced = 0 AND Status = 'Completed'
            ORDER BY CreatedAt ASC");

        var saleList = sales.ToList();
        if (!saleList.Any()) return saleList;

        var saleIds = saleList.Select(s => s.Id).ToArray();
        var items = await conn.QueryAsync<SaleItem>(@"
            SELECT * FROM SaleItems WHERE SaleId IN @SaleIds",
            new { SaleIds = saleIds });

        var itemsBySale = items.GroupBy(i => i.SaleId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var sale in saleList)
        {
            if (itemsBySale.TryGetValue(sale.Id, out var saleItems))
            {
                sale.Items = saleItems;
            }
        }
        return saleList;
    }

    public async Task MarkSalesAsSyncedAsync(IEnumerable<int> saleIds)
    {
        if (saleIds == null || !saleIds.Any()) return;
        
        using var conn = _db.GetConnection();
        await conn.ExecuteAsync(@"
            UPDATE Sales 
            SET IsSynced = 1 
            WHERE Id IN @SaleIds", 
            new { SaleIds = saleIds });
    }
}
