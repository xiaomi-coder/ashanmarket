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

    private decimal _newAmount;
    public decimal NewAmount
    {
        get => _newAmount;
        set => SetProperty(ref _newAmount, value);
    }

    private string _newReason = string.Empty;
    public string NewReason
    {
        get => _newReason;
        set => SetProperty(ref _newReason, value);
    }

    private decimal _totalExpensesToday;
    public decimal TotalExpensesToday
    {
        get => _totalExpensesToday;
        set => SetProperty(ref _totalExpensesToday, value);
    }

    public ICommand AddExpenseCommand { get; }
    public ICommand LoadExpensesCommand { get; }

    public ExpensesViewModel(IExpenseRepository expenseRepo, IAuthService authService, IShiftService shiftService)
    {
        _expenseRepo = expenseRepo;
        _authService = authService;
        _shiftService = shiftService;

        AddExpenseCommand = new AsyncRelayCommand(AddExpenseAsync, () => NewAmount > 0 && !string.IsNullOrWhiteSpace(NewReason));
        LoadExpensesCommand = new AsyncRelayCommand(LoadExpensesAsync);

        _ = LoadExpensesAsync();
    }

    public async Task LoadExpensesAsync()
    {
        await RunAsync(async () =>
        {
            var activeShift = await _shiftService.GetOpenShiftAsync();
            IEnumerable<Expense> data;
            
            if (activeShift != null)
            {
                data = await _expenseRepo.GetExpensesByShiftAsync(activeShift.Id);
            }
            else
            {
                data = await _expenseRepo.GetExpensesByDateAsync(DateTime.Today);
            }

            Expenses.Clear();
            foreach (var e in data) Expenses.Add(e);

            TotalExpensesToday = Expenses.Sum(e => e.Amount);
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
                Reason = NewReason.Trim(),
                UserId = _authService.CurrentUser?.Id ?? 1,
                CashierName = _authService.CurrentUser?.FullName ?? "Admin",
                ShiftId = activeShift.Id
            };

            await _expenseRepo.CreateExpenseAsync(expense);

            NewAmount = 0;
            NewReason = string.Empty;
            
            await LoadExpensesAsync();
        });
    }
}
