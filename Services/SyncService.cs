using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IO;
using System.Diagnostics;
using SupermarketPOS.Repositories;
using SupermarketPOS.Models;

namespace SupermarketPOS.Services;

public interface ISyncService
{
    Task<bool> SyncSalesAsync();
    Task<bool> AuthenticateCloudAsync(string password);
    Task<bool> PushProductsAsync();
    Task<bool> PushDebtsAsync();
    Task<bool> PushExpensesAsync();
    Task<bool> PullAllFromCloudAsync();
}

public class SyncService : ISyncService
{
    private readonly ISaleRepository _saleRepo;
    private readonly HttpClient _httpClient;
    private readonly IUserRepository _userRepo;
    private readonly object _syncLock = new();
    private bool _isSyncing = false;
    private static readonly string _logFile = Path.Combine(AppContext.BaseDirectory, "sync_log.txt");

    private static void Log(string msg)
    {
        try { File.AppendAllText(_logFile, $"[{DateTime.Now:HH:mm:ss}] {msg}\n"); } catch { }
        Debug.WriteLine($"[SYNC] {msg}");
    }

    public SyncService(ISaleRepository saleRepo, IUserRepository userRepo)
    {
        _saleRepo = saleRepo;
        _userRepo = userRepo;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        Log("SyncService yaratildi.");
    }

    // Thread-safe helper: x-api-key headeri bilan GET/POST qilish
    private HttpRequestMessage CreateRequest(HttpMethod method, string url, string apiKey)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("x-api-key", apiKey);
        return req;
    }

    public async Task<bool> SyncSalesAsync()
    {
        try
        {
            Log("SyncSalesAsync boshlandi...");
            var unsyncedSales = await _saleRepo.GetUnsyncedSalesAsync();
            var saleList = unsyncedSales.ToList();
            Log($"Sinxronizatsiya kutayotgan sotuvlar soni: {saleList.Count}");

            if (!saleList.Any())
            {
                Log("Yangi sotuvlar yo'q — skip.");
                return true;
            }

            var settings = SettingsManager.Load();
            Log($"Settings: URL={settings.BackendApiUrl}, ApiKey={(string.IsNullOrWhiteSpace(settings.ApiKey) ? "BO'SH" : "BOR")}");
            
            if (string.IsNullOrWhiteSpace(settings.BackendApiUrl) || string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                Log("API URL yoki API Key bo'sh — bekor qilindi.");
                return false;
            }

            string syncUrl = $"{settings.BackendApiUrl.TrimEnd('/')}/sales/sync";
            Log($"Sync URL: {syncUrl}");

            // Backend formatiga moslash (camelCase!)
            var payload = new
            {
                sales = saleList.Select(s => new
                {
                    saleNumber = s.SaleNumber,
                    cashierName = s.CashierName,
                    subTotal = s.SubTotal,
                    discount = s.Discount,
                    total = s.Total,
                    amountPaid = s.AmountPaid,
                    change = s.Change,
                    paymentMethod = s.PaymentMethod,
                    createdAt = s.CreatedAt,
                    items = s.Items.Select(i => new
                    {
                        productId = i.ProductId,
                        productName = i.ProductName,
                        barcode = i.Barcode,
                        unitPrice = i.UnitPrice,
                        costPrice = i.CostPrice,
                        quantity = i.Quantity,
                        discount = i.Discount
                    }).ToList()
                }).ToList()
            };

            Log($"Payload tayyorlandi. Sotuvlar: {payload.sales.Count}");
            var request = CreateRequest(HttpMethod.Post, syncUrl, settings.ApiKey!);
            request.Content = JsonContent.Create(payload);
            
            Log("HTTP so'rov yuborilmoqda...");
            var response = await _httpClient.SendAsync(request);
            Log($"HTTP javob: {(int)response.StatusCode} {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var rawBody = await response.Content.ReadAsStringAsync();
                Log($"Server javobi: {rawBody}");
                var content = JsonSerializer.Deserialize<JsonElement>(rawBody);
                int syncedCount = content.GetProperty("synced").GetInt32();
                Log($"✅ {syncedCount} ta sotuv sinxronizatsiya qilindi!");
                await _saleRepo.MarkSalesAsSyncedAsync(saleList.Select(x => x.Id));
                return true;
            }
            else
            {
                var errBody = await response.Content.ReadAsStringAsync();
                Log($"❌ Sync xatosi: {response.StatusCode} - {errBody}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Log($"❌ EXCEPTION: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> AuthenticateCloudAsync(string password)
    {
        try
        {
            var settings = SettingsManager.Load();
            if (string.IsNullOrWhiteSpace(settings.BackendApiUrl) || string.IsNullOrWhiteSpace(settings.CloudSlug))
                return false;

            var loginUrl = $"{settings.BackendApiUrl.TrimEnd('/')}/auth/login";
            var payload = new
            {
                slug = settings.CloudSlug,
                username = settings.CloudUsername,
                password = password
            };

            var response = await _httpClient.PostAsJsonAsync(loginUrl, payload);
            if (!response.IsSuccessStatusCode) return false;

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var tenant = content.GetProperty("tenant");
            
            if (tenant.TryGetProperty("apiKey", out var apiKeyElement) && apiKeyElement.ValueKind == JsonValueKind.String)
            {
                settings.ApiKey = apiKeyElement.GetString();
                settings.StoreName = tenant.GetProperty("name").GetString() ?? "Do'kon";
                SettingsManager.Save(settings);

                // Create the user locally so they can log in
                var cloudUser = content.GetProperty("user");
                var uName = cloudUser.GetProperty("username").GetString();
                var fName = cloudUser.GetProperty("fullName").GetString();
                var role = cloudUser.GetProperty("role").GetString() == "admin" ? "Admin" : "Cashier";

                // Save to local SQLite
                await _userRepo.AddAsync(new User
                {
                    Username = uName ?? settings.CloudUsername,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    FullName = fName ?? "Administrator",
                    Role = role,
                    IsActive = true
                });

                // ✅ Login bo'lgandan keyin serverdan barcha ma'lumotlarni tortib olish
                _ = Task.Run(async () =>
                {
                    Debug.WriteLine("☁️ Serverdan ma'lumotlarni tortib olish boshlandi...");
                    await PullAllFromCloudAsync();
                    Debug.WriteLine("☁️ Serverdan tortib olish tugadi!");
                });

                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Cloud auth failed: {ex}");
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PULL SYNC — Serverdan ma'lumotlarni tortib olish (yangi kompyuter uchun)
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<bool> PullAllFromCloudAsync()
    {
        // Bir vaqtda 2 ta pull sync ishlashining oldini olish
        lock (_syncLock)
        {
            if (_isSyncing) return Task.FromResult(false).Result;
            _isSyncing = true;
        }

        try
        {
            var settings = SettingsManager.Load();
            if (string.IsNullOrWhiteSpace(settings.BackendApiUrl) || string.IsNullOrWhiteSpace(settings.ApiKey))
                return false;

            var baseUrl = settings.BackendApiUrl.TrimEnd('/');
            var apiKey = settings.ApiKey!;

            // 1. Mahsulotlarni tortib olish
            await PullProductsFromCloudAsync(baseUrl, apiKey);

            // 2. Qarzlarni tortib olish
            await PullDebtsFromCloudAsync(baseUrl, apiKey);

            // 3. Xarajatlarni tortib olish (duplikatsiz)
            await PullExpensesFromCloudAsync(baseUrl, apiKey);

            // 4. Sotuvlar tarixini tortib olish
            await PullSalesFromCloudAsync(baseUrl, apiKey);

            Debug.WriteLine("✅ Serverdan barcha ma'lumotlar muvaffaqiyatli tortib olindi!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Pull sync xatosi: {ex}");
            return false;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private async Task PullProductsFromCloudAsync(string baseUrl, string apiKey)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, $"{baseUrl}/products/sync", apiKey);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (!json.TryGetProperty("products", out var products)) return;

            var _db = new SupermarketPOS.Data.DatabaseContext();
            using var conn = _db.GetConnection();

            int count = 0;
            foreach (var p in products.EnumerateArray())
            {
                var barcode = p.GetProperty("barcode").GetString() ?? "";
                var name = p.GetProperty("name").GetString() ?? "";
                var price = p.TryGetProperty("price", out var pr) ? pr.GetDecimal() : 0;
                var costPrice = p.TryGetProperty("costPrice", out var cp) ? cp.GetDecimal() : 0;
                var stock = p.TryGetProperty("stock", out var st) ? st.GetInt32() : 0;
                var unit = p.TryGetProperty("unit", out var un) ? un.GetString() ?? "dona" : "dona";

                // Agar barcode bilan tovar mavjud bo'lsa — yangilaymiz, aks holda qo'shamiz
                var exists = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<int?>(conn,
                    "SELECT Id FROM Products WHERE Barcode = @Barcode", new { Barcode = barcode });

                if (exists != null)
                {
                    await Dapper.SqlMapper.ExecuteAsync(conn,
                        "UPDATE Products SET Name=@Name, Price=@Price, CostPrice=@CostPrice, Stock=@Stock, Unit=@Unit WHERE Barcode=@Barcode",
                        new { Name = name, Price = price, CostPrice = costPrice, Stock = stock, Unit = unit, Barcode = barcode });
                }
                else
                {
                    await Dapper.SqlMapper.ExecuteAsync(conn,
                        @"INSERT INTO Products (Barcode, Name, Price, CostPrice, Stock, Unit, CategoryId, IsActive) 
                          VALUES (@Barcode, @Name, @Price, @CostPrice, @Stock, @Unit, 1, 1)",
                        new { Barcode = barcode, Name = name, Price = price, CostPrice = costPrice, Stock = stock, Unit = unit });
                }
                count++;
            }
            Debug.WriteLine($"📦 {count} ta mahsulot serverdan tortib olindi.");
        }
        catch (Exception ex) { Debug.WriteLine($"Pull products xatosi: {ex}"); }
    }

    private async Task PullDebtsFromCloudAsync(string baseUrl, string apiKey)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, $"{baseUrl}/debts/sync/download", apiKey);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (!json.TryGetProperty("customers", out var customers)) return;

            var _db = new SupermarketPOS.Data.DatabaseContext();
            using var conn = _db.GetConnection();

            int count = 0;
            foreach (var c in customers.EnumerateArray())
            {
                var phone = c.TryGetProperty("phone", out var ph) ? ph.GetString() ?? "N/A" : "N/A";
                var name = c.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "";
                var totalDebt = c.TryGetProperty("totalDebt", out var td) ? td.GetDecimal() : 0;

                var exists = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<int?>(conn,
                    "SELECT Id FROM Customers WHERE Phone = @Phone", new { Phone = phone });

                if (exists != null)
                {
                    await Dapper.SqlMapper.ExecuteAsync(conn,
                        "UPDATE Customers SET Name=@Name, DebtBalance=@DebtBalance WHERE Phone=@Phone",
                        new { Name = name, DebtBalance = totalDebt, Phone = phone });
                }
                else
                {
                    await Dapper.SqlMapper.ExecuteAsync(conn,
                        "INSERT INTO Customers (Phone, Name, DebtBalance) VALUES (@Phone, @Name, @DebtBalance)",
                        new { Phone = phone, Name = name, DebtBalance = totalDebt });
                }
                count++;
            }
            Debug.WriteLine($"💰 {count} ta qarz serverdan tortib olindi.");
        }
        catch (Exception ex) { Debug.WriteLine($"Pull debts xatosi: {ex}"); }
    }

    private async Task PullExpensesFromCloudAsync(string baseUrl, string apiKey)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, $"{baseUrl}/expenses/sync/download", apiKey);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (!json.TryGetProperty("expenses", out var expenses)) return;

            var _db = new SupermarketPOS.Data.DatabaseContext();
            using var conn = _db.GetConnection();

            int count = 0;
            foreach (var e in expenses.EnumerateArray())
            {
                var amount = e.TryGetProperty("amount", out var am) ? am.GetDecimal() : 0;
                var reason = e.TryGetProperty("reason", out var rs) ? rs.GetString() ?? "" : "";
                var categoryName = e.TryGetProperty("categoryName", out var cname) ? cname.GetString() ?? "" : "";
                var createdAt = e.TryGetProperty("date", out var d) ? d.GetString() ?? "" : "";
                var cashierName = e.TryGetProperty("cashierName", out var csh) ? csh.GetString() ?? "Sync" : "Sync";

                if (string.IsNullOrEmpty(createdAt)) 
                    createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Duplikat tekshirish
                var exists = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<int>(conn,
                    "SELECT COUNT(*) FROM Expenses WHERE Amount=@Amount AND Reason=@Reason AND SUBSTR(CreatedAt,1,10)=SUBSTR(@CreatedAt,1,10)",
                    new { Amount = amount, Reason = reason, CreatedAt = createdAt });
                if (exists > 0) continue;

                // Kategoriya tekshirish va yaratish
                if (string.IsNullOrWhiteSpace(categoryName)) categoryName = "Umumiy";
                var categoryId = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<int>(conn,
                    "SELECT Id FROM ExpenseCategories WHERE Name=@Name", new { Name = categoryName });
                if (categoryId == 0)
                {
                    categoryId = await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn,
                        "INSERT INTO ExpenseCategories (Name, IsActive) VALUES (@Name, 1); SELECT last_insert_rowid();",
                        new { Name = categoryName });
                }

                await Dapper.SqlMapper.ExecuteAsync(conn,
                    @"INSERT INTO Expenses (Amount, Reason, CategoryId, UserId, CashierName, CreatedAt) 
                      VALUES (@Amount, @Reason, @CategoryId, 1, @CashierName, @CreatedAt)",
                    new { Amount = amount, Reason = reason, CategoryId = categoryId, CashierName = cashierName, CreatedAt = createdAt });
                count++;
            }
            Debug.WriteLine($"📝 {count} ta xarajat serverdan tortib olindi.");
        }
        catch (Exception ex) 
        { 
            Debug.WriteLine($"Pull expenses xatosi: {ex}"); 
            System.IO.File.WriteAllText("sync_error.txt", ex.ToString());
        }
    }

    private async Task PullSalesFromCloudAsync(string baseUrl, string apiKey)
    {
        try
        {
            var url = $"{baseUrl}/sales/sync/download";
            var request = CreateRequest(HttpMethod.Get, url, apiKey);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (!json.TryGetProperty("sales", out var sales)) return;

            var _db = new SupermarketPOS.Data.DatabaseContext();
            using var conn = _db.GetConnection();

            int count = 0;
            foreach (var s in sales.EnumerateArray())
            {
                var saleNumber = s.TryGetProperty("saleNumber", out var sn) ? sn.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(saleNumber)) continue;

                // Duplikat tekshirish
                var exists = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<int>(conn,
                    "SELECT COUNT(*) FROM Sales WHERE SaleNumber=@SaleNumber",
                    new { SaleNumber = saleNumber });
                if (exists > 0) continue;

                var cashierName = s.TryGetProperty("cashierName", out var cn) ? cn.GetString() ?? "Kassir" : "Kassir";
                var total = s.TryGetProperty("total", out var t) ? t.GetDecimal() : 0;
                var subTotal = s.TryGetProperty("subTotal", out var st) ? st.GetDecimal() : total;
                var discount = s.TryGetProperty("discount", out var d) ? d.GetDecimal() : 0;
                var amountPaid = s.TryGetProperty("amountPaid", out var ap) ? ap.GetDecimal() : total;
                var change = s.TryGetProperty("change", out var ch) ? ch.GetDecimal() : 0;
                var paymentMethod = s.TryGetProperty("paymentMethod", out var pm) ? pm.GetString() ?? "Naqd" : "Naqd";
                var createdAt = s.TryGetProperty("createdAt", out var ca) ? ca.GetString() ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Sotuvni qo'shish
                var saleId = await Dapper.SqlMapper.QuerySingleAsync<long>(conn,
                    @"INSERT INTO Sales (SaleNumber, CashierName, SubTotal, Discount, Total, AmountPaid, Change, PaymentMethod, CreatedAt, UserId, IsSynced)
                      VALUES (@SaleNumber, @CashierName, @SubTotal, @Discount, @Total, @AmountPaid, @Change, @PaymentMethod, @CreatedAt, 1, 1);
                      SELECT last_insert_rowid();",
                    new { SaleNumber = saleNumber, CashierName = cashierName, SubTotal = subTotal, Discount = discount, Total = total, AmountPaid = amountPaid, Change = change, PaymentMethod = paymentMethod, CreatedAt = createdAt });

                // Sale items qo'shish
                if (s.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var productName = item.TryGetProperty("productName", out var pn) ? pn.GetString() ?? "" : "";
                        var barcode = item.TryGetProperty("barcode", out var bc) ? bc.GetString() ?? "" : "";
                        var unitPrice = item.TryGetProperty("unitPrice", out var up) ? up.GetDecimal() : 0;
                        var costPrice = item.TryGetProperty("costPrice", out var cp) ? cp.GetDecimal() : 0;
                        var quantity = item.TryGetProperty("quantity", out var q) ? q.GetDecimal() : 1;

                        await Dapper.SqlMapper.ExecuteAsync(conn,
                            @"INSERT INTO SaleItems (SaleId, ProductId, ProductName, Barcode, UnitPrice, CostPrice, Quantity, Discount)
                              VALUES (@SaleId, 0, @ProductName, @Barcode, @UnitPrice, @CostPrice, @Quantity, 0)",
                            new { SaleId = saleId, ProductName = productName, Barcode = barcode, UnitPrice = unitPrice, CostPrice = costPrice, Quantity = quantity });
                    }
                }
                count++;
            }
            Debug.WriteLine($"📊 {count} ta sotuv serverdan tortib olindi.");
        }
        catch (Exception ex) { Debug.WriteLine($"Pull sales xatosi: {ex}"); }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PUSH SYNC — Ma'lumotlarni serverga jo'natish
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<bool> PushProductsAsync()
    {
        try
        {
            var settings = SettingsManager.Load();
            if (string.IsNullOrWhiteSpace(settings.BackendApiUrl) || string.IsNullOrWhiteSpace(settings.ApiKey))
                return false;

            var query = "SELECT Barcode, Name as name, Price as price, CostPrice as costPrice, Stock as stock, LowStockThreshold as lowStockThreshold, Unit as unit FROM Products WHERE IsActive=1";
            var _db = new SupermarketPOS.Data.DatabaseContext();
            using var conn = _db.GetConnection();
            var products = await Dapper.SqlMapper.QueryAsync(conn, query);

            var payload = new { products };
            var url = $"{settings.BackendApiUrl.TrimEnd('/')}/products/sync/upload";
            
            var request = CreateRequest(HttpMethod.Post, url, settings.ApiKey!);
            request.Content = JsonContent.Create(payload);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Sinxronizatsiya (Tovarlar) xatosi: {ex}");
            return false;
        }
    }

    public async Task<bool> PushDebtsAsync()
    {
        try
        {
            var settings = SettingsManager.Load();
            if (string.IsNullOrWhiteSpace(settings.BackendApiUrl) || string.IsNullOrWhiteSpace(settings.ApiKey))
                return false;

            var _db = new SupermarketPOS.Data.DatabaseContext();
            using var conn = _db.GetConnection();
            var debts = await Dapper.SqlMapper.QueryAsync(conn, "SELECT Phone as phone, Name as name, DebtBalance as debtBalance FROM Customers WHERE DebtBalance > 0");

            var payload = new { debts };
            var url = $"{settings.BackendApiUrl.TrimEnd('/')}/debts/sync/upload";
            
            var request = CreateRequest(HttpMethod.Post, url, settings.ApiKey!);
            request.Content = JsonContent.Create(payload);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> PushExpensesAsync()
    {
        try
        {
            var settings = SettingsManager.Load();
            if (string.IsNullOrWhiteSpace(settings.BackendApiUrl) || string.IsNullOrWhiteSpace(settings.ApiKey))
                return false;

            var _db = new SupermarketPOS.Data.DatabaseContext();
            using var conn = _db.GetConnection();
            var expenses = await Dapper.SqlMapper.QueryAsync(conn, @"
                SELECT e.Amount as amount, e.Reason as reason, e.CreatedAt as createdAt, COALESCE(c.Name, 'Umumiy') as categoryName 
                FROM Expenses e
                LEFT JOIN ExpenseCategories c ON e.CategoryId = c.Id");

            var payload = new { expenses };
            var url = $"{settings.BackendApiUrl.TrimEnd('/')}/expenses/sync/upload";
            
            var request = CreateRequest(HttpMethod.Post, url, settings.ApiKey!);
            request.Content = JsonContent.Create(payload);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
