using SupermarketPOS.Models;
using SupermarketPOS.Repositories;
using System.IO;
using CsvHelper;
using System.Globalization;

namespace SupermarketPOS.Services;

// ─── Auth Service ────────────────────────────────────────────────────────────

public interface IAuthService
{
    Task<User?> LoginAsync(string username, string password);
    void Logout();
    User? CurrentUser { get; }
    bool IsAdmin { get; }
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    public User? CurrentUser { get; private set; }
    public bool IsAdmin => CurrentUser?.Role == "Admin";

    public AuthService(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        CurrentUser = user;
        await _userRepo.UpdateLastLoginAsync(user.Id);
        return user;
    }

    public void Logout() => CurrentUser = null;
}

// ─── Product Service ─────────────────────────────────────────────────────────

public interface IProductService
{
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<IEnumerable<Product>> SearchAsync(string query);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<IEnumerable<Product>> GetLowStockAsync();
    Task<int> AddProductAsync(Product product);
    Task<bool> UpdateProductAsync(Product product);
    Task<bool> DeleteProductAsync(int id);
    Task<bool> ImportFromCsvAsync(string filePath);
    Task<IEnumerable<Category>> GetCategoriesAsync();
    Task<int> AddCategoryAsync(Category category);
    Task<bool> UpdateCategoryAsync(Category category);
    Task<bool> DeleteCategoryAsync(int id);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _repo;

    public ProductService(IProductRepository repo) => _repo = repo;

    public Task<Product?> GetByBarcodeAsync(string barcode) => _repo.GetByBarcodeAsync(barcode);
    public Task<IEnumerable<Product>> SearchAsync(string query) => _repo.SearchAsync(query);
    public Task<IEnumerable<Product>> GetAllAsync() => _repo.GetAllAsync();
    public Task<IEnumerable<Product>> GetLowStockAsync() => _repo.GetLowStockAsync();
    public Task<IEnumerable<Category>> GetCategoriesAsync() => _repo.GetCategoriesAsync();
    public Task<int> AddCategoryAsync(Category category) => _repo.AddCategoryAsync(category);
    public Task<bool> UpdateCategoryAsync(Category category) => _repo.UpdateCategoryAsync(category);
    public Task<bool> DeleteCategoryAsync(int id) => _repo.DeleteCategoryAsync(id);

    public async Task<int> AddProductAsync(Product product)
    {
        ValidateProduct(product);
        return await _repo.AddAsync(product);
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        ValidateProduct(product);
        return await _repo.UpdateAsync(product);
    }

    public Task<bool> DeleteProductAsync(int id) => _repo.DeleteAsync(id);

    public async Task<bool> ImportFromCsvAsync(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);

        var records = csv.GetRecords<dynamic>().ToList();
        foreach (var r in records)
        {
            var dict = (IDictionary<string, object>)r;
            var product = new Product
            {
                Barcode = dict.ContainsKey("Barcode") ? dict["Barcode"]?.ToString() ?? "" : "",
                Name = dict.ContainsKey("Name") ? dict["Name"]?.ToString() ?? "" : "",
                Price = dict.ContainsKey("Price") ? decimal.Parse(dict["Price"]?.ToString() ?? "0") : 0,
                CostPrice = dict.ContainsKey("CostPrice") ? decimal.Parse(dict["CostPrice"]?.ToString() ?? "0") : 0,
                Stock = dict.ContainsKey("Stock") ? int.Parse(dict["Stock"]?.ToString() ?? "0") : 0,
                CategoryId = dict.ContainsKey("CategoryId") ? int.Parse(dict["CategoryId"]?.ToString() ?? "1") : 1,
                Unit = dict.ContainsKey("Unit") ? dict["Unit"]?.ToString() ?? "dona" : "dona",
            };
            if (!string.IsNullOrWhiteSpace(product.Barcode) && !string.IsNullOrWhiteSpace(product.Name))
                await _repo.AddAsync(product);
        }
        return true;
    }

    private static void ValidateProduct(Product p)
    {
        if (string.IsNullOrWhiteSpace(p.Barcode)) throw new Exception("Barcode kiritilishi shart!");
        if (string.IsNullOrWhiteSpace(p.Name))    throw new Exception("Mahsulot nomi kiritilishi shart!");
        if (p.Price < 0)                           throw new Exception("Narx manfiy bo'lishi mumkin emas!");
        if (p.Stock < 0)                           throw new Exception("Qoldiq manfiy bo'lishi mumkin emas!");
    }
}

// ─── Shift Service ───────────────────────────────────────────────────────────

public interface IShiftService
{
    Task<Shift?> GetOpenShiftAsync();
    Task<Shift?> GetOpenShiftByCashierAsync(int cashierId);
    Task<Shift> OpenShiftAsync(int cashierId, string cashierName, decimal startingBalance);
    Task CloseShiftAsync(int shiftId, decimal actualBalance);
}

public class ShiftService : IShiftService
{
    private readonly IShiftRepository _shiftRepo;
    private readonly ISaleRepository _saleRepo;
    private readonly IExpenseRepository _expenseRepo;

    public ShiftService(IShiftRepository shiftRepo, ISaleRepository saleRepo, IExpenseRepository expenseRepo)
    {
        _shiftRepo = shiftRepo;
        _saleRepo = saleRepo;
        _expenseRepo = expenseRepo;
    }

    public Task<Shift?> GetOpenShiftAsync() => _shiftRepo.GetOpenShiftAsync();

    public Task<Shift?> GetOpenShiftByCashierAsync(int cashierId) => _shiftRepo.GetOpenShiftByCashierAsync(cashierId);

    public async Task<Shift> OpenShiftAsync(int cashierId, string cashierName, decimal startingBalance)
    {
        var active = await _shiftRepo.GetOpenShiftAsync();
        if (active != null)
            throw new Exception("Boshqa smena hali yopilmagan!");

        var shift = new Shift
        {
            CashierId = cashierId,
            CashierName = cashierName,
            StartingBalance = startingBalance,
            ExpectedBalance = startingBalance,
            Status = "Open"
        };
        shift.Id = await _shiftRepo.CreateShiftAsync(shift);
        return shift;
    }

    public async Task CloseShiftAsync(int shiftId, decimal actualBalance)
    {
        var shift = await _shiftRepo.GetOpenShiftAsync();
        if (shift == null || shift.Id != shiftId)
            throw new Exception("Faol smena topilmadi!");

        // Hisob-kitob qilish: Smena davridagi savdolar summasini qidirish
        // Oson yo'li: SaleRepo orqali CashierId va CreatedAt > OpenedAt bilan savdolarni olish
        var sales = await _saleRepo.GetSalesByCashierAndDateAsync(shift.CashierId, shift.OpenedAt, DateTime.Now);
        
        // Jami naqd pul tushumi
        var cashSales = sales.Where(s => s.PaymentMethod == "Naqd").Sum(s => s.AmountPaid - s.Change);
        
        // Smena davridagi xarajatlar
        var expenses = await _expenseRepo.GetExpensesByShiftAsync(shift.Id);
        var totalExpenses = expenses.Sum(e => e.Amount);

        shift.ExpectedBalance = shift.StartingBalance + cashSales - totalExpenses;
        shift.ActualBalance = actualBalance;
        shift.ClosedAt = DateTime.Now;
        shift.Status = "Closed";

        await _shiftRepo.UpdateShiftAsync(shift);
    }
}

// ─── Customer Service ────────────────────────────────────────────────────────

public interface ICustomerService
{
    Task<Customer?> GetByPhoneAsync(string phone);
    Task<Customer> GetOrCreateAsync(string phone, string name, decimal defaultDiscount = 0);
    Task UpdateTotalSpentAsync(int customerId, decimal amount);
    Task<IEnumerable<Customer>> GetAllWithDebtAsync();
    Task<IEnumerable<DebtTransaction>> GetDebtTransactionsAsync(int customerId);
    Task<int> PayDebtAsync(int customerId, decimal amount, string notes = "Qarz to'landi");
    Task<int> AddDebtAsync(int customerId, decimal amount, int? saleId = null, string notes = "Sotuv orqali qarz");
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepo;

    public CustomerService(ICustomerRepository customerRepo)
    {
        _customerRepo = customerRepo;
    }

    public Task<Customer?> GetByPhoneAsync(string phone) => _customerRepo.GetByPhoneAsync(phone);

    public async Task<Customer> GetOrCreateAsync(string phone, string name, decimal defaultDiscount = 0)
    {
        var existing = await _customerRepo.GetByPhoneAsync(phone);
        if (existing != null) return existing;

        var newCustomer = new Customer
        {
            Phone = phone,
            Name = name,
            DiscountPercent = defaultDiscount
        };
        newCustomer.Id = await _customerRepo.CreateAsync(newCustomer);
        return newCustomer;
    }

    public Task UpdateTotalSpentAsync(int customerId, decimal amount)
    {
        return _customerRepo.UpdateTotalSpentAsync(customerId, amount);
    }

    public Task<IEnumerable<Customer>> GetAllWithDebtAsync()
    {
        return _customerRepo.GetAllWithDebtAsync();
    }

    public Task<IEnumerable<DebtTransaction>> GetDebtTransactionsAsync(int customerId)
    {
        return _customerRepo.GetDebtTransactionsAsync(customerId);
    }

    public async Task<int> PayDebtAsync(int customerId, decimal amount, string notes = "Qarz to'landi")
    {
        var transaction = new DebtTransaction
        {
            CustomerId = customerId,
            Amount = amount,
            Type = "Paid",
            Notes = notes
        };
        return await _customerRepo.AddDebtTransactionAsync(transaction);
    }

    public async Task<int> AddDebtAsync(int customerId, decimal amount, int? saleId = null, string notes = "Sotuv orqali qarz")
    {
        var transaction = new DebtTransaction
        {
            CustomerId = customerId,
            Amount = amount,
            Type = "Given",
            SaleId = saleId,
            Notes = notes
        };
        return await _customerRepo.AddDebtTransactionAsync(transaction);
    }
}

// ─── Barcode Printer Service ──────────────────────────────────────────────────

public interface IBarcodePrinterService
{
    void PrintBarcode(string barcode, string productName, decimal price);
}

public class BarcodePrinterService : IBarcodePrinterService
{
    public void PrintBarcode(string barcode, string productName, decimal price)
    {
        // NetBarcode orqali rasm yaratamiz
        var barcodeGenerator = new NetBarcode.Barcode(barcode, NetBarcode.Type.Code128, true);
        var base64Image = barcodeGenerator.GetBase64Image();
        var bytes = Convert.FromBase64String(base64Image);

        // WPF orqali chop etish
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var pd = new System.Windows.Controls.PrintDialog();
            if (pd.ShowDialog() == true)
            {
                var doc = new System.Windows.Documents.FlowDocument();
                doc.PageWidth = 200; // stiker kengligi
                doc.PagePadding = new System.Windows.Thickness(10);
                doc.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");

                // Nomi
                var nameText = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(productName))
                {
                    FontSize = 12,
                    FontWeight = System.Windows.FontWeights.Bold,
                    TextAlignment = System.Windows.TextAlignment.Center,
                    Margin = new System.Windows.Thickness(0, 0, 0, 5)
                };
                doc.Blocks.Add(nameText);

                // Rasm
                using var ms = new System.IO.MemoryStream(bytes);
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();

                var image = new System.Windows.Controls.Image
                {
                    Source = bitmap,
                    Width = 150,
                    Height = 60,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };

                var figure = new System.Windows.Documents.Figure();
                figure.Blocks.Add(new System.Windows.Documents.BlockUIContainer(image));
                doc.Blocks.Add(new System.Windows.Documents.Paragraph(figure) { TextAlignment = System.Windows.TextAlignment.Center, Margin = new System.Windows.Thickness(0) });

                // Narxi
                var priceText = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"{price:N0} so'm"))
                {
                    FontSize = 14,
                    FontWeight = System.Windows.FontWeights.Bold,
                    TextAlignment = System.Windows.TextAlignment.Center,
                    Margin = new System.Windows.Thickness(0, 5, 0, 0)
                };
                doc.Blocks.Add(priceText);

                var idp = ((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator;
                pd.PrintDocument(idp, "Shtrix-kod chop etish");
            }
        });
    }
}

// ─── Sale Service ─────────────────────────────────────────────────────────────

public interface ISaleService
{
    Task<int> CompleteSaleAsync(Sale sale);
    Task<DailySalesReport> GetDailyReportAsync(DateTime date);
    Task<IEnumerable<TopProduct>> GetTopProductsAsync(DateTime from, DateTime to, int limit = 10);
    Task<IEnumerable<Sale>> GetSalesHistoryAsync(DateTime from, DateTime to);
    Task<decimal> GetTotalRevenueAsync(DateTime from, DateTime to);
    Task<string> GenerateSaleNumberAsync();
}

public class SaleService : ISaleService
{
    private readonly ISaleRepository _repo;
    private readonly ICustomerRepository _customerRepo;

    public SaleService(ISaleRepository repo, ICustomerRepository customerRepo)
    {
        _repo = repo;
        _customerRepo = customerRepo;
    }

    public async Task<int> CompleteSaleAsync(Sale sale)
    {
        if (!sale.Items.Any()) throw new Exception("Sotuv bo'sh bo'lishi mumkin emas!");
        
        // Qarz bo'lmagan to'lovlar uchun summani tekshirish
        if (sale.PaymentMethod != "Qarz" && sale.AmountPaid < sale.Total) 
            throw new Exception("To'lov summasi yetarli emas!");

        sale.SaleNumber = await _repo.GenerateSaleNumberAsync();
        sale.Change = sale.AmountPaid > sale.Total ? sale.AmountPaid - sale.Total : 0;
        
        var saleId = await _repo.CreateSaleAsync(sale);

        // Agar "Qarz" bo'lsa, uni CustomerRepo orqali yozib qo'yamiz
        if (sale.PaymentMethod == "Qarz")
        {
            // Bizda Sale ob'ektida CustomerId yo'q, lekin bu mantiqni ViewModeL da yoki shuyerda hal qilishimiz kerak.
            // Yaxshisi buni ViewModeldan turib qilamiz, chunki Sale jadvalida CustomerId yo'q.
        }

        return saleId;
    }

    public Task<DailySalesReport> GetDailyReportAsync(DateTime date) => _repo.GetDailyReportAsync(date);
    public Task<IEnumerable<TopProduct>> GetTopProductsAsync(DateTime from, DateTime to, int limit = 10)
        => _repo.GetTopProductsAsync(from, to, limit);
    public Task<IEnumerable<Sale>> GetSalesHistoryAsync(DateTime from, DateTime to)
        => _repo.GetSalesAsync(from, to);
    public Task<decimal> GetTotalRevenueAsync(DateTime from, DateTime to)
        => _repo.GetRevenueSummaryAsync(from, to);
    public Task<string> GenerateSaleNumberAsync() => _repo.GenerateSaleNumberAsync();
}
