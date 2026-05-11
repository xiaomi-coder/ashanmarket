using System.Collections.ObjectModel;
using System.Windows.Input;
using SupermarketPOS.Models;
using SupermarketPOS.Repositories;
using SupermarketPOS.Services;
using SupermarketPOS.Helpers;
using System.Windows;

namespace SupermarketPOS.ViewModels;

public class ExpensesViewModel : BaseViewModel
{
    private readonly IExpenseRepository _expenseRepo;
    private readonly IAuthService _authService;
    private readonly IShiftService _shiftService;

    public ObservableCollection<Expense> Expenses { get; } = new();
    public ObservableCollection<ExpenseCategory> Categories { get; } = new();

    // New Expense Form
    private string _newAmountText = string.Empty;
    public string NewAmountText
    {
        get => _newAmountText;
        set
        {
            if (SetProperty(ref _newAmountText, value))
            {
                var clean = new string(value.Where(char.IsDigit).ToArray());
                NewAmount = decimal.TryParse(clean, out var parsed) ? parsed : 0;
            }
        }
    }
    public decimal NewAmount { get; private set; }

    private ExpenseCategory? _selectedCategory;
    public ExpenseCategory? SelectedCategory { get => _selectedCategory; set => SetProperty(ref _selectedCategory, value); }

    private string _newReason = string.Empty;
    public string NewReason { get => _newReason; set => SetProperty(ref _newReason, value); }

    private DateTime _newDate = DateTime.Now;
    public DateTime NewDate { get => _newDate; set => SetProperty(ref _newDate, value); }

    // Dashboard Stats
    private decimal _totalExpensesToday;
    public decimal TotalExpensesToday { get => _totalExpensesToday; set => SetProperty(ref _totalExpensesToday, value); }

    private decimal _monthExpenses;
    public decimal MonthExpenses { get => _monthExpenses; set => SetProperty(ref _monthExpenses, value); }

    private decimal _filteredExpensesTotal;
    public decimal FilteredExpensesTotal { get => _filteredExpensesTotal; set => SetProperty(ref _filteredExpensesTotal, value); }

    // Filters
    private DateTime _filterFromDate = DateTime.Today;
    public DateTime FilterFromDate { get => _filterFromDate; set => SetProperty(ref _filterFromDate, value); }

    private DateTime _filterToDate = DateTime.Today;
    public DateTime FilterToDate { get => _filterToDate; set => SetProperty(ref _filterToDate, value); }

    private ExpenseCategory? _filterCategory;
    public ExpenseCategory? FilterCategory { get => _filterCategory; set => SetProperty(ref _filterCategory, value); }

    // Category Popup
    private bool _isAddCategoryOpen;
    public bool IsAddCategoryOpen { get => _isAddCategoryOpen; set => SetProperty(ref _isAddCategoryOpen, value); }

    private string _newCategoryName = string.Empty;
    public string NewCategoryName { get => _newCategoryName; set => SetProperty(ref _newCategoryName, value); }

    public ICommand AddExpenseCommand { get; }
    public ICommand LoadExpensesCommand { get; }
    public ICommand FilterCommand { get; }
    public ICommand OpenAddCategoryCommand { get; }
    public ICommand CloseAddCategoryCommand { get; }
    public ICommand SaveCategoryCommand { get; }

    public ExpensesViewModel(IExpenseRepository expenseRepo, IAuthService authService, IShiftService shiftService)
    {
        _expenseRepo = expenseRepo;
        _authService = authService;
        _shiftService = shiftService;

        AddExpenseCommand = new AsyncRelayCommand(AddExpenseAsync, () => NewAmount > 0 && SelectedCategory != null);
        LoadExpensesCommand = new AsyncRelayCommand(LoadExpensesAsync);
        FilterCommand = new AsyncRelayCommand(FilterAsync);
        
        OpenAddCategoryCommand = new RelayCommand(() => { NewCategoryName = ""; IsAddCategoryOpen = true; });
        CloseAddCategoryCommand = new RelayCommand(() => IsAddCategoryOpen = false);
        SaveCategoryCommand = new AsyncRelayCommand(SaveCategoryAsync, () => !string.IsNullOrWhiteSpace(NewCategoryName));

        _ = LoadInitialDataAsync();
    }

    private async Task LoadInitialDataAsync()
    {
        try
        {
            await LoadCategoriesAsync();
            await LoadExpensesAsync();
            await FilterAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Xarajatlarni yuklashda xatolik: {ex.Message}", "Xato", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        await RunAsync(async () =>
        {
            var cats = await _expenseRepo.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in cats) Categories.Add(c);
        });
    }

    private async Task SaveCategoryAsync()
    {
        await RunAsync(async () =>
        {
            var cat = new ExpenseCategory { Name = NewCategoryName.Trim(), IsActive = true };
            await _expenseRepo.AddCategoryAsync(cat);
            IsAddCategoryOpen = false;
            NewCategoryName = string.Empty;
            await LoadCategoriesAsync();
            SetStatus("Kategoriya saqlandi");
        });
    }

    public async Task LoadExpensesAsync()
    {
        await RunAsync(async () =>
        {
            // Dashboard Stats
            var today = DateTime.Today;
            TotalExpensesToday = await _expenseRepo.GetTotalExpensesByDateAsync(today);
            
            var monthFrom = new DateTime(today.Year, today.Month, 1);
            var monthData = await _expenseRepo.GetExpensesByDateRangeAsync(monthFrom, today);
            MonthExpenses = monthData.Sum(e => e.Amount);
        });
    }

    private async Task FilterAsync()
    {
        await RunAsync(async () =>
        {
            var data = await _expenseRepo.GetExpensesByDateRangeAsync(FilterFromDate, FilterToDate, FilterCategory?.Id);
            Expenses.Clear();
            foreach (var e in data) Expenses.Add(e);
            
            FilteredExpensesTotal = Expenses.Sum(e => e.Amount);
        });
    }

    private async Task AddExpenseAsync()
    {
        await RunAsync(async () =>
        {
            var activeShift = await _shiftService.GetOpenShiftAsync();
            if (activeShift == null)
            {
                MessageBox.Show("Faol smena topilmadi! Avval smenani oching.", "Xato", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var expense = new Expense
            {
                Amount = NewAmount,
                CategoryId = SelectedCategory?.Id,
                Reason = NewReason.Trim(),
                CreatedAt = NewDate, // custom date support
                UserId = _authService.CurrentUser?.Id ?? 1,
                CashierName = _authService.CurrentUser?.FullName ?? "Admin",
                ShiftId = activeShift.Id
            };

            await _expenseRepo.CreateExpenseAsync(expense);

            NewAmountText = string.Empty;
            NewReason = string.Empty;
            NewDate = DateTime.Now;
            SelectedCategory = null;
            
            await LoadInitialDataAsync();
            SetStatus("Xarajat qo'shildi");
        });
    }
}
