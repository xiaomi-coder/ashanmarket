using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
}

public class SyncService : ISyncService
{
    private readonly ISaleRepository _saleRepo;
    private readonly HttpClient _httpClient;
    private readonly IUserRepository _userRepo;
    public SyncService(ISaleRepository saleRepo, IUserRepository userRepo)
    {
        _saleRepo = saleRepo;
        _userRepo = userRepo;
        _httpClient = new HttpClient();
    }

    public async Task<bool> SyncSalesAsync()
    {
        try
        {
            var unsyncedSales = await _saleRepo.GetUnsyncedSalesAsync();
            var saleList = unsyncedSales.ToList();

            if (!saleList.Any())
            {
                Debug.WriteLine("Sinxronizatsiya uchun yangi sotuvlar yo'q.");
                return true;
            }

            var settings = SettingsManager.Load();
            if (string.IsNullOrWhiteSpace(settings.BackendApiUrl) || string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                Debug.WriteLine("Cloud Settings (API URL yoki API Key) kiritilmagan. Sinxronizatsiya bekor qilindi.");
                return false;
            }

            string syncUrl = $"{settings.BackendApiUrl.TrimEnd('/')}/sales/sync";
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", settings.ApiKey);

            // Backend formatiga moslash
            var payload = new
            {
                sales = saleList.Select(s => new
                {
                    s.SaleNumber,
                    s.CashierName,
                    s.SubTotal,
                    s.Discount,
                    s.Total,
                    s.AmountPaid,
                    s.Change,
                    s.PaymentMethod,
                    s.CreatedAt,
                    items = s.Items.Select(i => new
                    {
                        i.ProductId,
                        i.ProductName,
                        i.Barcode,
                        i.UnitPrice,
                        i.CostPrice,
                        i.Quantity,
                        i.Discount
                    }).ToList()
                }).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync(syncUrl, payload);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<JsonElement>();
                int syncedCount = content.GetProperty("synced").GetInt32();

                Debug.WriteLine($"{syncedCount} ta sotuv muvaffaqiyatli sinxronizatsiya qilindi.");

                // Jo'natilganlarni bazada belgilash
                await _saleRepo.MarkSalesAsSyncedAsync(saleList.Select(x => x.Id));
                return true;
            }
            else
            {
                Debug.WriteLine($"Sync xatosi: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Sinxronizatsiya jarayonida xatolik yuz berdi: {ex}");
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
    public async Task<bool> PushProductsAsync()
    {
        try
        {
            var settings = SettingsManager.Load();
            if (string.IsNullOrWhiteSpace(settings.BackendApiUrl) || string.IsNullOrWhiteSpace(settings.ApiKey))
                return false;

            // Using direct SQL to fetch all products to push
            var query = "SELECT Barcode, Name as name, Price as price, CostPrice as costPrice, Stock as stock, LowStockThreshold as lowStockThreshold, Unit as unit FROM Products WHERE IsActive=1";
            var _db = new SupermarketPOS.Data.DatabaseContext();
            using var conn = _db.GetConnection();
            var products = await Dapper.SqlMapper.QueryAsync(conn, query);

            var payload = new { products };
            var url = $"{settings.BackendApiUrl.TrimEnd('/')}/products/sync/upload";
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", settings.ApiKey);

            var response = await _httpClient.PostAsJsonAsync(url, payload);
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
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", settings.ApiKey);

            var response = await _httpClient.PostAsJsonAsync(url, payload);
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
            var expenses = await Dapper.SqlMapper.QueryAsync(conn, "SELECT Amount as amount, Reason as reason, CreatedAt as createdAt FROM Expenses");

            var payload = new { expenses };
            var url = $"{settings.BackendApiUrl.TrimEnd('/')}/expenses/sync/upload";
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", settings.ApiKey);

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
