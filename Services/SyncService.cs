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
}
