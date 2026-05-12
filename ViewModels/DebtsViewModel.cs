using System.Collections.ObjectModel;
using System.Windows.Input;
using SupermarketPOS.Helpers;
using SupermarketPOS.Models;
using SupermarketPOS.Services;

namespace SupermarketPOS.ViewModels;

public class DebtsViewModel : BaseViewModel
{
    private readonly ICustomerService _customerService;

    public ObservableCollection<Customer> Debtors { get; } = new();
    public ObservableCollection<Customer> FilteredDebtors { get; } = new();
    public ObservableCollection<DebtTransaction> Transactions { get; } = new();

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                FilterDebtors();
            }
        }
    }

    private Customer? _selectedCustomer;
    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
            {
                _ = LoadTransactionsAsync();
            }
        }
    }

    private decimal _paymentAmount;
    public decimal PaymentAmount
    {
        get => _paymentAmount;
        set => SetProperty(ref _paymentAmount, value);
    }

    private decimal _selectedCustomerAddDebtAmount;
    public decimal SelectedCustomerAddDebtAmount
    {
        get => _selectedCustomerAddDebtAmount;
        set => SetProperty(ref _selectedCustomerAddDebtAmount, value);
    }

    private string _newCustomerPhone = string.Empty;
    public string NewCustomerPhone { get => _newCustomerPhone; set => SetProperty(ref _newCustomerPhone, value); }

    private string _newCustomerName = string.Empty;
    public string NewCustomerName { get => _newCustomerName; set => SetProperty(ref _newCustomerName, value); }

    private decimal _newDebtAmount;
    public decimal NewDebtAmount { get => _newDebtAmount; set => SetProperty(ref _newDebtAmount, value); }

    public ICommand PayDebtCommand { get; }
    public ICommand UpdateTermCommand { get; }
    public ICommand AddDebtCommand { get; }
    public ICommand AddDebtToSelectedCommand { get; }

    public DebtsViewModel(ICustomerService customerService)
    {
        _customerService = customerService;
        PayDebtCommand = new AsyncRelayCommand(PayDebtAsync, () => SelectedCustomer != null && PaymentAmount > 0);
        UpdateTermCommand = new AsyncRelayCommand(UpdateTermAsync, () => SelectedCustomer != null);
        AddDebtCommand = new AsyncRelayCommand(AddDebtAsync);
        AddDebtToSelectedCommand = new AsyncRelayCommand(AddDebtToSelectedAsync, () => SelectedCustomer != null && SelectedCustomerAddDebtAmount > 0);

        _ = LoadDebtorsAsync();
    }

    public async Task LoadDebtorsAsync()
    {
        await RunAsync(async () =>
        {
            var debtors = await _customerService.GetAllWithDebtAsync();
            Debtors.Clear();
            foreach (var d in debtors) Debtors.Add(d);
            
            FilterDebtors();
            
            if (SelectedCustomer != null && !Debtors.Contains(SelectedCustomer))
            {
                SelectedCustomer = null;
                Transactions.Clear();
            }
        });
    }

    private void FilterDebtors()
    {
        FilteredDebtors.Clear();
        var lowerQuery = SearchQuery?.ToLower() ?? "";
        
        var filtered = Debtors.Where(d => 
            string.IsNullOrWhiteSpace(lowerQuery) || 
            (d.Name?.ToLower().Contains(lowerQuery) == true) || 
            (d.Phone?.Contains(lowerQuery) == true)
        ).ToList();

        foreach (var d in filtered)
        {
            FilteredDebtors.Add(d);
        }
    }

    private async Task LoadTransactionsAsync()
    {
        if (SelectedCustomer == null)
        {
            Transactions.Clear();
            return;
        }

        await RunAsync(async () =>
        {
            var txs = await _customerService.GetDebtTransactionsAsync(SelectedCustomer.Id);
            Transactions.Clear();
            foreach (var tx in txs) Transactions.Add(tx);
            
            PaymentAmount = SelectedCustomer.DebtBalance;
            SelectedCustomerAddDebtAmount = 0;
        });
    }

    private async Task PayDebtAsync()
    {
        if (SelectedCustomer == null || PaymentAmount <= 0) return;
        if (PaymentAmount > SelectedCustomer.DebtBalance)
        {
            SetStatus("To'lov summasi joriy qarzdan katta bo'lishi mumkin emas!", true);
            return;
        }

        await RunAsync(async () =>
        {
            await _customerService.PayDebtAsync(SelectedCustomer.Id, PaymentAmount);
            SetStatus($"{SelectedCustomer.Name} dan {PaymentAmount:N0} so'm qarz to'lovi qabul qilindi!");
            
            PaymentAmount = 0;
            await LoadDebtorsAsync();
            
            // Reload selected customer to update UI
            var updatedCustomer = Debtors.FirstOrDefault(c => c.Id == SelectedCustomer?.Id);
            if (updatedCustomer != null)
            {
                SelectedCustomer = updatedCustomer;
            }
        });
    }

    private async Task UpdateTermAsync()
    {
        if (SelectedCustomer == null) return;
        await RunAsync(async () =>
        {
            await _customerService.UpdateDebtTermAsync(SelectedCustomer.Id, SelectedCustomer.DebtTermDays);
            SetStatus("Qarz muddati yangilandi");
            
            var id = SelectedCustomer.Id;
            await LoadDebtorsAsync();
            SelectedCustomer = Debtors.FirstOrDefault(c => c.Id == id);
        });
    }

    private async Task AddDebtAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCustomerPhone) || string.IsNullOrWhiteSpace(NewCustomerName) || NewDebtAmount <= 0)
        {
            SetStatus("Telefon, ism va summani to'g'ri kiriting!", true);
            return;
        }

        await RunAsync(async () =>
        {
            var customer = await _customerService.GetOrCreateAsync(NewCustomerPhone, NewCustomerName);
            await _customerService.AddDebtAsync(customer.Id, NewDebtAmount, null, "Qo'lda kiritilgan qarz");
            
            NewCustomerPhone = string.Empty;
            NewCustomerName = string.Empty;
            NewDebtAmount = 0;
            
            SetStatus("Qarz muvaffaqiyatli qo'shildi!");
            await LoadDebtorsAsync();
        });
    }

    private async Task AddDebtToSelectedAsync()
    {
        if (SelectedCustomer == null || SelectedCustomerAddDebtAmount <= 0) return;

        await RunAsync(async () =>
        {
            await _customerService.AddDebtAsync(SelectedCustomer.Id, SelectedCustomerAddDebtAmount, null, "Qo'lda kiritilgan qarz");
            SetStatus($"{SelectedCustomer.Name} hisobiga {SelectedCustomerAddDebtAmount:N0} so'm qarz qo'shildi!");
            
            SelectedCustomerAddDebtAmount = 0;
            
            var id = SelectedCustomer.Id;
            await LoadDebtorsAsync();
            SelectedCustomer = Debtors.FirstOrDefault(c => c.Id == id);
        });
    }
}
