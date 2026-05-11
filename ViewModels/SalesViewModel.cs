using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SupermarketPOS.Helpers;
using SupermarketPOS.Models;
using SupermarketPOS.Services;

namespace SupermarketPOS.ViewModels;

public class CartItem : BaseViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }

    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set { SetProperty(ref _quantity, value < 1 ? 1 : value); OnPropertyChanged(nameof(Total)); }
    }

    private decimal _discount;
    public decimal Discount
    {
        get => _discount;
        set { SetProperty(ref _discount, value); OnPropertyChanged(nameof(Total)); }
    }

    public decimal Total => UnitPrice * Quantity - Discount;
}

public class SalesViewModel : BaseViewModel
{
    private readonly IProductService _productService;
    private readonly ISaleService _saleService;
    private readonly IAuthService _authService;
    private readonly IReceiptPrinterService _printerService;
    private readonly ICustomerService _customerService;

    public ObservableCollection<CartItem> CartItems { get; } = new();
    public ObservableCollection<Product> SearchResults { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Product> CategoryProducts { get; } = new();
    public ObservableCollection<HeldCart> HeldCarts { get; } = new();

    private Category? _selectedCategory;
    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                _ = LoadCategoryProductsAsync();
            }
        }
    }

    private string _barcodeInput = string.Empty;
    public string BarcodeInput
    {
        get => _barcodeInput;
        set => SetProperty(ref _barcodeInput, value);
    }

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            SetProperty(ref _searchQuery, value);
            _ = SearchProductsAsync();
        }
    }

    private decimal _subtotal;
    public decimal SubTotal
    {
        get => _subtotal;
        private set => SetProperty(ref _subtotal, value);
    }

    private decimal _discount;
    public decimal Discount
    {
        get => _discount;
        set { SetProperty(ref _discount, value); RecalculateTotals(); }
    }

    private decimal _total;
    public decimal Total
    {
        get => _total;
        private set => SetProperty(ref _total, value);
    }

    private decimal _amountPaid;
    public decimal AmountPaid
    {
        get => _amountPaid;
        set { SetProperty(ref _amountPaid, value); OnPropertyChanged(nameof(Change)); }
    }

    public decimal Change => AmountPaid - Total;

    private string _paymentMethod = "Naqd";
    public string PaymentMethod
    {
        get => _paymentMethod;
        set => SetProperty(ref _paymentMethod, value);
    }

    private CartItem? _selectedItem;
    public CartItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    private string _lastReceiptText = string.Empty;
    public string LastReceiptText
    {
        get => _lastReceiptText;
        set => SetProperty(ref _lastReceiptText, value);
    }

    private bool _showReceipt;
    public bool ShowReceipt
    {
        get => _showReceipt;
        set => SetProperty(ref _showReceipt, value);
    }

    private string _customerPhone = string.Empty;
    public string CustomerPhone
    {
        get => _customerPhone;
        set => SetProperty(ref _customerPhone, value);
    }

    private Customer? _currentCustomer;
    public Customer? CurrentCustomer
    {
        get => _currentCustomer;
        set { SetProperty(ref _currentCustomer, value); RecalculateTotals(); }
    }

    // Commands
    public ICommand FindCustomerCommand { get; }
    public ICommand ClearCustomerCommand { get; }
    public ICommand ScanBarcodeCommand { get; }
    public ICommand AddToCartCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand IncreaseQtyCommand { get; }
    public ICommand DecreaseQtyCommand { get; }
    public ICommand CompleteSaleCommand { get; }
    public ICommand ClearCartCommand { get; }
    public ICommand SetAmountExactCommand { get; }
    public ICommand CloseReceiptCommand { get; }
    public ICommand SelectCategoryCommand { get; }
    public ICommand HoldCartCommand { get; }
    public ICommand ResumeCartCommand { get; }

    public SalesViewModel(
        IProductService productService,
        ISaleService saleService,
        IAuthService authService,
        IReceiptPrinterService printerService,
        ICustomerService customerService)
    {
        _productService = productService;
        _saleService = saleService;
        _authService = authService;
        _printerService = printerService;
        _customerService = customerService;

        FindCustomerCommand  = new AsyncRelayCommand(FindCustomerAsync);
        ClearCustomerCommand = new RelayCommand(() => { CurrentCustomer = null; CustomerPhone = string.Empty; RecalculateTotals(); });
        ScanBarcodeCommand   = new AsyncRelayCommand(ScanBarcodeAsync);
        AddToCartCommand     = new RelayCommand<Product>(AddToCart);
        RemoveItemCommand    = new RelayCommand<CartItem>(RemoveFromCart);
        IncreaseQtyCommand   = new RelayCommand<CartItem>(item => { if (item != null) { item.Quantity++; RecalculateTotals(); } });
        DecreaseQtyCommand   = new RelayCommand<CartItem>(item => { if (item != null && item.Quantity > 1) { item.Quantity--; RecalculateTotals(); } });
        CompleteSaleCommand  = new AsyncRelayCommand(CompleteSaleAsync, () => CartItems.Any() && Total > 0);
        ClearCartCommand     = new RelayCommand(ClearCart);
        SetAmountExactCommand = new RelayCommand(() => { AmountPaid = Total; });
        CloseReceiptCommand  = new RelayCommand(() => ShowReceipt = false);
        SelectCategoryCommand = new RelayCommand<Category>(c => SelectedCategory = c);
        HoldCartCommand      = new RelayCommand(HoldCart);
        ResumeCartCommand    = new RelayCommand<HeldCart>(ResumeCart);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await RunAsync(async () =>
        {
            var cats = await _productService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in cats) Categories.Add(c);
            if (Categories.Any()) SelectedCategory = Categories.First();
        });
    }

    private async Task LoadCategoryProductsAsync()
    {
        if (SelectedCategory == null) return;
        await RunAsync(async () =>
        {
            var allProducts = await _productService.GetAllAsync();
            var catProds = allProducts.Where(p => p.CategoryId == SelectedCategory.Id);
            CategoryProducts.Clear();
            foreach (var p in catProds) CategoryProducts.Add(p);
        });
    }

    public async Task ScanBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

        var barcode = BarcodeInput.Trim();
        BarcodeInput = string.Empty;

        await RunAsync(async () =>
        {
            var product = await _productService.GetByBarcodeAsync(barcode);
            if (product == null)
            {
                SetStatus($"'{barcode}' uchun mahsulot topilmadi!", true);
                return;
            }
            if (product.Stock <= 0)
            {
                SetStatus($"{product.Name} - stokda yo'q!", true);
                return;
            }
            AddToCart(product);
            SetStatus($"{product.Name} savatchaga qo'shildi");
        });
    }

    public void AddToCart(Product? product)
    {
        if (product == null) return;

        var existing = CartItems.FirstOrDefault(x => x.ProductId == product.Id);
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            CartItems.Add(new CartItem
            {
                ProductId   = product.Id,
                ProductName = product.Name,
                Barcode     = product.Barcode,
                UnitPrice   = product.Price,
                CostPrice   = product.CostPrice,
                Quantity    = 1,
            });
        }
        RecalculateTotals();
    }

    private void RemoveFromCart(CartItem? item)
    {
        if (item != null)
        {
            CartItems.Remove(item);
            RecalculateTotals();
        }
    }

    private void ClearCart()
    {
        CartItems.Clear();
        Discount = 0;
        AmountPaid = 0;
        SearchQuery = string.Empty;
        SearchResults.Clear();
        CurrentCustomer = null;
        CustomerPhone = string.Empty;
        RecalculateTotals();
        SetStatus("Savat tozalandi");
    }

    private void HoldCart()
    {
        if (!CartItems.Any()) return;
        
        var newCart = new HeldCart();
        foreach(var item in CartItems) newCart.Items.Add(item);
        
        HeldCarts.Add(newCart);
        ClearCart();
        SetStatus("Savat kutishga olindi");
    }

    private void ResumeCart(HeldCart? cart)
    {
        if (cart == null) return;
        
        // Agar joriy savatda mahsulot bo'lsa, avval uni kutishga olamiz (yoki shunchaki tozalaymiz)
        if (CartItems.Any())
        {
            var currentCart = new HeldCart();
            foreach(var item in CartItems) currentCart.Items.Add(item);
            HeldCarts.Add(currentCart);
        }
        
        ClearCart();
        foreach(var item in cart.Items) CartItems.Add(item);
        
        HeldCarts.Remove(cart);
        RecalculateTotals();
        SetStatus("Savat davom ettirilmoqda");
    }

    public void RecalculateTotals()
    {
        SubTotal = CartItems.Sum(x => x.Total);
        
        // Agar mijoz tanlangan bo'lsa, uning chegirma foizi asosida jami summadan chegirma hisoblanadi
        if (CurrentCustomer != null && CurrentCustomer.DiscountPercent > 0)
        {
            Discount = SubTotal * (CurrentCustomer.DiscountPercent / 100);
        }
        else
        {
            // Agar qo'lda kiritilgan chegirma bo'lsa, o'shani qoldiramiz. (Mijoz bo'lmasa Discount=0 qilmaymiz,
            // chunki kassir o'zi chegirma yozgan bo'lishi mumkin)
        }

        Total = SubTotal - Discount;
        if (Total < 0) Total = 0;
        OnPropertyChanged(nameof(Change));
    }

    private async Task FindCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerPhone)) return;
        
        await RunAsync(async () =>
        {
            // Oson bo'lishi uchun, agar mijoz topilmasa darhol yaratamiz (0% chegirma bilan)
            var customer = await _customerService.GetOrCreateAsync(CustomerPhone.Trim(), "Mijoz " + CustomerPhone.Trim(), 0);
            CurrentCustomer = customer;
            SetStatus($"Mijoz biriktirildi. (Chegirma: {customer.DiscountPercent}%)");
        });
    }

    private async Task SearchProductsAsync()
    {
        if (SearchQuery.Length < 2)
        {
            SearchResults.Clear();
            return;
        }

        var results = await _productService.SearchAsync(SearchQuery);
        SearchResults.Clear();
        foreach (var p in results)
            SearchResults.Add(p);
    }

    private async Task CompleteSaleAsync()
    {
        if (!CartItems.Any()) return;
        if (AmountPaid < Total)
        {
            SetStatus("To'lov summasi yetarli emas!", true);
            return;
        }

        await RunAsync(async () =>
        {
            var sale = new Sale
            {
                UserId      = _authService.CurrentUser?.Id ?? 1,
                CashierName = _authService.CurrentUser?.FullName ?? "Kassir",
                SubTotal    = SubTotal,
                Discount    = Discount,
                Total       = Total,
                AmountPaid  = AmountPaid,
                Change      = Change,
                PaymentMethod = PaymentMethod,
                Items = CartItems.Select(c => new SaleItem
                {
                    ProductId   = c.ProductId,
                    ProductName = c.ProductName,
                    Barcode     = c.Barcode,
                    UnitPrice   = c.UnitPrice,
                    CostPrice   = c.CostPrice,
                    Quantity    = c.Quantity,
                    Discount    = c.Discount,
                }).ToList()
            };

            var saleId = await _saleService.CompleteSaleAsync(sale);
            sale.Id = saleId;

            if (CurrentCustomer != null)
            {
                await _customerService.GetOrCreateAsync(CurrentCustomer.Phone, CurrentCustomer.Name); // Just to ensure it exists
                await _customerService.UpdateTotalSpentAsync(CurrentCustomer.Id, sale.Total);
            }

            // Chekni printerga chiqarish
            try
            {
                Application.Current.Dispatcher.Invoke(() => 
                {
                    _printerService.PrintReceipt(sale);
                });
            }
            catch (Exception ex)
            {
                SetStatus($"Chek printerda xatolik: {ex.Message}", true);
            }

            LastReceiptText = ReceiptGenerator.GenerateText(sale);
            ShowReceipt = true;

            ClearCart();
            SetStatus($"Sotuv #{sale.SaleNumber} muvaffaqiyatli yakunlandi!");
        });
    }
}

// Generic RelayCommand<T>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) =>
        _canExecute?.Invoke(parameter is T t ? t : default) ?? true;

    public void Execute(object? parameter) =>
        _execute(parameter is T t ? t : default);
}
