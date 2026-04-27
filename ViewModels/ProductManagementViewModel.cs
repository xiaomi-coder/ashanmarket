using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using SupermarketPOS.Helpers;
using SupermarketPOS.Models;
using SupermarketPOS.Services;

namespace SupermarketPOS.ViewModels;

public class ProductManagementViewModel : BaseViewModel
{
    private readonly IProductService _productService;
    private readonly IBarcodePrinterService _barcodePrinterService;

    public ObservableCollection<Product>  Products   { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();

    private Product? _selectedProduct;
    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            SetProperty(ref _selectedProduct, value);
            if (value != null) LoadProductToForm(value);
        }
    }

    // Form fields
    private string _formBarcode = string.Empty;
    public string FormBarcode { get => _formBarcode; set => SetProperty(ref _formBarcode, value); }

    private string _formName = string.Empty;
    public string FormName { get => _formName; set => SetProperty(ref _formName, value); }

    private string _formPrice = string.Empty;
    public string FormPrice { get => _formPrice; set => SetProperty(ref _formPrice, value); }

    private string _formCostPrice = string.Empty;
    public string FormCostPrice { get => _formCostPrice; set => SetProperty(ref _formCostPrice, value); }

    private string _formStock = string.Empty;
    public string FormStock { get => _formStock; set => SetProperty(ref _formStock, value); }

    private string _formAddStock = string.Empty;
    public string FormAddStock { get => _formAddStock; set => SetProperty(ref _formAddStock, value); }

    private string _formUnit = "dona";
    public string FormUnit { get => _formUnit; set => SetProperty(ref _formUnit, value); }

    private string _formLowStock = "10";
    public string FormLowStock { get => _formLowStock; set => SetProperty(ref _formLowStock, value); }

    private Category? _formCategory;
    public Category? FormCategory { get => _formCategory; set => SetProperty(ref _formCategory, value); }

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set { SetProperty(ref _searchQuery, value); _ = FilterProductsAsync(); }
    }

    private bool _isEditMode;
    public bool IsEditMode { get => _isEditMode; set => SetProperty(ref _isEditMode, value); }

    public string[] Units { get; } = ["dona", "kg", "litr", "paket", "quti", "shisha", "tuba", "bog'", "metr"];

    public ICommand SaveProductCommand   { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand NewProductCommand    { get; }
    public ICommand RefreshCommand       { get; }
    public ICommand ImportCsvCommand     { get; }
    public ICommand PrintBarcodeCommand  { get; }
    public ICommand GenerateBarcodeCommand { get; }
    public ICommand AddStockCommand      { get; }

    public ProductManagementViewModel(IProductService productService, IBarcodePrinterService barcodePrinterService)
    {
        _productService = productService;
        _barcodePrinterService = barcodePrinterService;

        SaveProductCommand   = new AsyncRelayCommand(SaveProductAsync);
        DeleteProductCommand = new AsyncRelayCommand(DeleteProductAsync, () => SelectedProduct != null);
        NewProductCommand    = new RelayCommand(ClearForm);
        RefreshCommand       = new AsyncRelayCommand(LoadProductsAsync);
        ImportCsvCommand     = new AsyncRelayCommand(ImportCsvAsync);
        PrintBarcodeCommand  = new RelayCommand(PrintBarcode, () => SelectedProduct != null);
        GenerateBarcodeCommand = new RelayCommand(GenerateRandomBarcode);
        AddStockCommand      = new RelayCommand(AddStock, () => IsEditMode);
    }

    public async Task LoadProductsAsync()
    {
        await RunAsync(async () =>
        {
            var products = await _productService.GetAllAsync();
            Products.Clear();
            foreach (var p in products) Products.Add(p);

            var cats = await _productService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in cats) Categories.Add(c);

            SetStatus($"{Products.Count} ta mahsulot yuklandi");
        });
    }

    private async Task FilterProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadProductsAsync();
            return;
        }
        var results = await _productService.SearchAsync(SearchQuery);
        Products.Clear();
        foreach (var p in results) Products.Add(p);
    }

    private async Task SaveProductAsync()
    {
        if (string.IsNullOrWhiteSpace(FormBarcode) || string.IsNullOrWhiteSpace(FormName))
        {
            SetStatus("Xato: Shtrix-kod va Mahsulot nomi bo'sh bo'lishi mumkin emas.");
            return;
        }
        
        if (!decimal.TryParse(FormPrice, out var price) || price < 0)
        {
            SetStatus("Xato: Sotuv narxi noto'g'ri kiritildi.");
            return;
        }

        await RunAsync(async () =>
        {
            var product = new Product
            {
                Id                = SelectedProduct?.Id ?? 0,
                Barcode           = FormBarcode.Trim(),
                Name              = FormName.Trim(),
                Price             = price,
                CostPrice         = decimal.TryParse(FormCostPrice, out var cost) ? cost : 0,
                Stock             = int.TryParse(FormStock, out var stock) ? stock : 0,
                LowStockThreshold = int.TryParse(FormLowStock, out var low) ? low : 10,
                CategoryId        = FormCategory?.Id ?? 1,
                Unit              = FormUnit,
                IsActive          = true
            };

            if (IsEditMode && product.Id > 0)
            {
                await _productService.UpdateProductAsync(product);
                SetStatus($"'{product.Name}' yangilandi");
            }
            else
            {
                await _productService.AddProductAsync(product);
                SetStatus($"'{product.Name}' qo'shildi");
            }

            ClearForm();
            await LoadProductsAsync();
        });
    }

    private async Task DeleteProductAsync()
    {
        if (SelectedProduct == null) return;

        await RunAsync(async () =>
        {
            await _productService.DeleteProductAsync(SelectedProduct.Id);
            SetStatus($"'{SelectedProduct.Name}' o'chirildi");
            ClearForm();
            await LoadProductsAsync();
        });
    }

    private void LoadProductToForm(Product p)
    {
        FormBarcode   = p.Barcode;
        FormName      = p.Name;
        FormPrice     = p.Price.ToString();
        FormCostPrice = p.CostPrice.ToString();
        FormStock     = p.Stock.ToString();
        FormUnit      = p.Unit;
        FormLowStock  = p.LowStockThreshold.ToString();
        FormCategory  = Categories.FirstOrDefault(c => c.Id == p.CategoryId);
        IsEditMode    = true;
    }

    private void ClearForm()
    {
        FormBarcode   = string.Empty;
        FormName      = string.Empty;
        FormPrice     = string.Empty;
        FormCostPrice = string.Empty;
        FormStock     = string.Empty;
        FormUnit      = "dona";
        FormLowStock  = "10";
        FormCategory  = Categories.FirstOrDefault();
        SelectedProduct = null;
        IsEditMode    = false;
    }

    private async Task ImportCsvAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV Fayllar (*.csv)|*.csv",
            Title  = "CSV faylni tanlang"
        };

        if (dialog.ShowDialog() == true)
        {
            await RunAsync(async () =>
            {
                await _productService.ImportFromCsvAsync(dialog.FileName);
                await LoadProductsAsync();
                SetStatus("CSV import muvaffaqiyatli yakunlandi!");
            });
        }
    }

    private void PrintBarcode()
    {
        if (SelectedProduct != null)
        {
            try
            {
                _barcodePrinterService.PrintBarcode(SelectedProduct.Barcode, SelectedProduct.Name, SelectedProduct.Price);
                SetStatus("Shtrix-kod chop etishga yuborildi");
            }
            catch (Exception ex)
            {
                SetStatus($"Shtrix-kod xatosi: {ex.Message}", true);
            }
        }
    }

    private void GenerateRandomBarcode()
    {
        // 13 xonali random EAN-13 shtrix-kodi yasaymiz
        var random = new Random();
        string barcode = "";
        for (int i = 0; i < 12; i++)
            barcode += random.Next(0, 10).ToString();
            
        // EAN-13 oxirgi raqami (Check digit) hisoblash
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int num = int.Parse(barcode[i].ToString());
            sum += num * (i % 2 == 0 ? 1 : 3);
        }
        int checkDigit = (10 - (sum % 10)) % 10;
        barcode += checkDigit.ToString();

        FormBarcode = barcode;
        SetStatus("Shtrix-kod generatsiya qilindi");
    }

    private void AddStock()
    {
        if (double.TryParse(FormAddStock, out double added) && added > 0)
        {
            double current = 0;
            double.TryParse(FormStock, out current);
            FormStock = (current + added).ToString();
            FormAddStock = string.Empty;
            SetStatus($"{added} ta kirim qilindi. Saqlash tugmasini bosing.");
        }
        else
        {
            SetStatus("Kirim qilish uchun to'g'ri son kiriting!", true);
        }
    }
}
