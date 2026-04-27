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
}

public class SyncService : ISyncService
{
    private readonly ISaleRepository _saleRepo;
    private readonly HttpClient _httpClient;

    // TODO: Bularni kelajakda sozlamalardan (Settings) olinadigan qilish kerak.
    private const string BackendApiUrl = "http://localhost:5000/api/sales/sync";
    private const string ApiKey = "YOUR_TENANT_API_KEY";

    public SyncService(ISaleRepository saleRepo)
    {
        _saleRepo = saleRepo;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
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

            var response = await _httpClient.PostAsJsonAsync(BackendApiUrl, payload);

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
}
