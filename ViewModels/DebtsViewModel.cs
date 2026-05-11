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
    public ObservableCollection<DebtTransaction> Transactions { get; } = new();

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

    public ICommand PayDebtCommand { get; }
    public ICommand UpdateTermCommand { get; }

    public DebtsViewModel(ICustomerService customerService)
    {
        _customerService = customerService;
        PayDebtCommand = new AsyncRelayCommand(PayDebtAsync, () => SelectedCustomer != null && PaymentAmount > 0);
        UpdateTermCommand = new AsyncRelayCommand(UpdateTermAsync, () => SelectedCustomer != null);

        _ = LoadDebtorsAsync();
    }

    public async Task LoadDebtorsAsync()
    {
        await RunAsync(async () =>
        {
            var debtors = await _customerService.GetAllWithDebtAsync();
            Debtors.Clear();
            foreach (var d in debtors) Debtors.Add(d);
            
            if (SelectedCustomer != null && !Debtors.Contains(SelectedCustomer))
            {
                SelectedCustomer = null;
                Transactions.Clear();
            }
        });
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
}
