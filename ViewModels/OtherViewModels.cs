using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using SupermarketPOS.Helpers;
using SupermarketPOS.Models;
using SupermarketPOS.Services;

namespace SupermarketPOS.ViewModels;

// ─── Reports ViewModel ────────────────────────────────────────────────────────

public class ReportsViewModel : BaseViewModel
{
    private readonly ISaleService _saleService;

    public ObservableCollection<TopProduct> TopProducts { get; } = new();
    public ObservableCollection<Sale>       SalesHistory { get; } = new();

    private DateTime _fromDate = DateTime.Today;
    public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }

    private DateTime _toDate = DateTime.Today;
    public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }

    private int _totalTransactions;
    public int TotalTransactions { get => _totalTransactions; set => SetProperty(ref _totalTransactions, value); }

    private decimal _totalRevenue;
    public decimal TotalRevenue { get => _totalRevenue; set => SetProperty(ref _totalRevenue, value); }

    private decimal _totalProfit;
    public decimal TotalProfit { get => _totalProfit; set => SetProperty(ref _totalProfit, value); }

    private decimal _totalDiscount;
    public decimal TotalDiscount { get => _totalDiscount; set => SetProperty(ref _totalDiscount, value); }

    public ICommand LoadReportCommand { get; }
    public ICommand TodayCommand     { get; }
    public ICommand ThisWeekCommand  { get; }
    public ICommand ThisMonthCommand { get; }

    public ReportsViewModel(ISaleService saleService)
    {
        _saleService = saleService;

        LoadReportCommand = new AsyncRelayCommand(LoadReportAsync);
        TodayCommand      = new AsyncRelayCommand(() => { FromDate = ToDate = DateTime.Today; return LoadReportAsync(); });
        ThisWeekCommand   = new AsyncRelayCommand(() => { FromDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek); ToDate = DateTime.Today; return LoadReportAsync(); });
        ThisMonthCommand  = new AsyncRelayCommand(() => { FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); ToDate = DateTime.Today; return LoadReportAsync(); });
    }

    public async Task LoadReportAsync()
    {
        await RunAsync(async () =>
        {
            var report = await _saleService.GetDailyReportAsync(FromDate);

            if (FromDate != ToDate)
            {
                // Multi-day: aggregate
                var sales = (await _saleService.GetSalesHistoryAsync(FromDate, ToDate)).ToList();
                TotalTransactions = sales.Count;
                TotalRevenue      = sales.Sum(s => s.Total);
                TotalDiscount     = sales.Sum(s => s.Discount);

                SalesHistory.Clear();
                foreach (var s in sales) SalesHistory.Add(s);

                var topProds = await _saleService.GetTopProductsAsync(FromDate, ToDate, 10);
                TopProducts.Clear();
                foreach (var p in topProds) TopProducts.Add(p);
                TotalProfit = TopProducts.Sum(p => p.Profit);
            }
            else
            {
                TotalTransactions = report.TotalTransactions;
                TotalRevenue      = report.TotalRevenue;
                TotalProfit       = report.TotalProfit;
                TotalDiscount     = report.TotalDiscount;

                var sales = await _saleService.GetSalesHistoryAsync(FromDate, ToDate);
                SalesHistory.Clear();
                foreach (var s in sales) SalesHistory.Add(s);

                TopProducts.Clear();
                foreach (var p in report.TopProducts) TopProducts.Add(p);
            }

            SetStatus($"{FromDate:dd/MM} - {ToDate:dd/MM} hisoboti yuklandi");
        });
    }
}

// ─── Login ViewModel ──────────────────────────────────────────────────────────

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    private string _username = string.Empty;
    public string Username { get => _username; set => SetProperty(ref _username, value); }

    private string _password = string.Empty;
    public string Password { get => _password; set => SetProperty(ref _password, value); }

    public bool LoginSuccess { get; private set; }

    public ICommand LoginCommand { get; }

    public event Action? OnLoginSuccess;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        LoginCommand = new AsyncRelayCommand(LoginAsync);
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            SetStatus("Foydalanuvchi nomi va parol kiritilishi shart!", true);
            return;
        }

        await RunAsync(async () =>
        {
            var user = await _authService.LoginAsync(Username, Password);
            if (user == null)
            {
                SetStatus("Noto'g'ri foydalanuvchi nomi yoki parol!", true);
                return;
            }

            LoginSuccess = true;
            SetStatus($"Xush kelibsiz, {user.FullName}!");
            OnLoginSuccess?.Invoke();
        });
    }
}

// ─── Main Shell ViewModel ─────────────────────────────────────────────────────

public class MainViewModel : BaseViewModel
{
    private readonly IProductService _productService;
    private readonly IAuthService    _authService;
    private readonly IShiftService   _shiftService;

    public SalesViewModel              SalesVM     { get; }
    public ProductManagementViewModel  ProductsVM  { get; }
    public ReportsViewModel            ReportsVM   { get; }
    public CategoryManagementViewModel CategoriesVM { get; }
    public ReturnsViewModel            ReturnsVM   { get; }
    public ExpensesViewModel           ExpensesVM  { get; }
    public UserManagementViewModel     UsersVM     { get; }

    private BaseViewModel _currentView;
    public BaseViewModel CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    private int _lowStockCount;
    public int LowStockCount { get => _lowStockCount; set => SetProperty(ref _lowStockCount, value); }

    private string _currentUserName = string.Empty;
    public string CurrentUserName { get => _currentUserName; set => SetProperty(ref _currentUserName, value); }

    private bool _isAdmin;
    public bool IsAdmin { get => _isAdmin; set => SetProperty(ref _isAdmin, value); }

    private Shift? _currentShift;
    public Shift? CurrentShift { get => _currentShift; set => SetProperty(ref _currentShift, value); }

    public string ShiftStatusText => CurrentShift != null 
        ? $"🟢 Smena ochiq ({CurrentShift.OpenedAt:HH:mm})" 
        : "🔴 Smena yopiq";

    public ICommand NavigateSalesCommand    { get; }
    public ICommand NavigateProductsCommand { get; }
    public ICommand NavigateReportsCommand  { get; }
    public ICommand NavigateCategoriesCommand { get; }
    public ICommand NavigateReturnsCommand    { get; }
    public ICommand NavigateExpensesCommand   { get; }
    public ICommand NavigateUsersCommand      { get; }
    public ICommand BackupCommand           { get; }
    public ICommand ManageShiftCommand      { get; }
    public ICommand SyncCommand             { get; }

    public MainViewModel(
        SalesViewModel salesVM,
        ProductManagementViewModel productsVM,
        ReportsViewModel reportsVM,
        CategoryManagementViewModel categoriesVM,
        ReturnsViewModel returnsVM,
        ExpensesViewModel expensesVM,
        UserManagementViewModel usersVM,
        IProductService productService,
        IAuthService authService,
        IShiftService shiftService,
        ISyncService syncService)
    {
        SalesVM          = salesVM;
        ProductsVM       = productsVM;
        ReportsVM        = reportsVM;
        CategoriesVM     = categoriesVM;
        ReturnsVM        = returnsVM;
        ExpensesVM       = expensesVM;
        UsersVM          = usersVM;
        _productService  = productService;
        _authService     = authService;
        _shiftService    = shiftService;
        _currentView     = salesVM;

        NavigateSalesCommand    = new RelayCommand(() => CurrentView = SalesVM);
        NavigateProductsCommand = new RelayCommand(() => { CurrentView = ProductsVM; _ = ProductsVM.LoadProductsAsync(); });
        NavigateReportsCommand  = new RelayCommand(() => { CurrentView = ReportsVM; _ = ReportsVM.LoadReportAsync(); });
        NavigateCategoriesCommand = new RelayCommand(() => CurrentView = CategoriesVM);
        NavigateReturnsCommand  = new RelayCommand(() => CurrentView = ReturnsVM);
        NavigateExpensesCommand = new RelayCommand(() => { CurrentView = ExpensesVM; _ = ExpensesVM.LoadExpensesAsync(); });
        NavigateUsersCommand    = new RelayCommand(() => { CurrentView = UsersVM; _ = UsersVM.LoadUsersAsync(); });
        BackupCommand           = new AsyncRelayCommand(BackupDatabaseAsync);
        ManageShiftCommand      = new AsyncRelayCommand(ManageShiftAsync);
        SyncCommand             = new AsyncRelayCommand(async () => {
            var success = await syncService.SyncSalesAsync();
            if (success) 
                System.Windows.MessageBox.Show("Sinxronizatsiya muvaffaqiyatli yakunlandi!", "Sync", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        });
    }

    public async Task InitializeAsync()
    {
        CurrentUserName = _authService.CurrentUser?.FullName ?? "";
        IsAdmin = _authService.IsAdmin;

        var lowStock = await _productService.GetLowStockAsync();
        LowStockCount = lowStock.Count();

        await LoadShiftAsync();
        if (CurrentShift == null)
        {
            // Prompt to open shift
            await ManageShiftAsync();
        }
    }

    private async Task LoadShiftAsync()
    {
        CurrentShift = await _shiftService.GetOpenShiftAsync();
        OnPropertyChanged(nameof(ShiftStatusText));
    }

    private async Task ManageShiftAsync()
    {
        bool isOpening = CurrentShift == null;
        var vm = new ShiftViewModel(_shiftService, _authService, isOpening, CurrentShift);
        
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var win = new SupermarketPOS.Views.ShiftWindow { DataContext = vm };
            vm.CloseAction = (success) =>
            {
                win.DialogResult = success;
                win.Close();
            };
            win.ShowDialog();
        });

        await LoadShiftAsync();
    }

    private async Task BackupDatabaseAsync()
    {
        await RunAsync(async () =>
        {
            await Task.Run(() =>
            {
                var backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "POS_Backup");
                Directory.CreateDirectory(backupDir);

                var srcPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SupermarketPOS", "supermarket_pos.db");

                var dest = Path.Combine(backupDir, $"backup_{DateTime.Now:yyyyMMdd_HHmm}.db");
                File.Copy(srcPath, dest, true);
            });
            SetStatus("Zaxira nusxa muvaffaqiyatli saqlandi (Desktop/POS_Backup)");
        });
    }
}
