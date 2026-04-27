using System.Collections.ObjectModel;
using System.Windows.Input;
using SupermarketPOS.Models;
using SupermarketPOS.Repositories;
using SupermarketPOS.Helpers;
using System.Windows;

namespace SupermarketPOS.ViewModels;

public class ReturnsViewModel : BaseViewModel
{
    private readonly ISaleRepository _saleRepo;

    private string _searchSaleNumber = string.Empty;
    public string SearchSaleNumber
    {
        get => _searchSaleNumber;
        set => SetProperty(ref _searchSaleNumber, value);
    }

    private Sale? _foundSale;
    public Sale? FoundSale
    {
        get => _foundSale;
        set => SetProperty(ref _foundSale, value);
    }

    public ICommand SearchCommand { get; }
    public ICommand ReturnSaleCommand { get; }

    public ReturnsViewModel(ISaleRepository saleRepo)
    {
        _saleRepo = saleRepo;
        SearchCommand = new AsyncRelayCommand(SearchAsync, () => !string.IsNullOrWhiteSpace(SearchSaleNumber));
        ReturnSaleCommand = new AsyncRelayCommand(ReturnSaleAsync, () => FoundSale != null && FoundSale.Status == "Completed");
    }

    private async Task SearchAsync()
    {
        await RunAsync(async () =>
        {
            FoundSale = await _saleRepo.GetBySaleNumberAsync(SearchSaleNumber.Trim());
            if (FoundSale == null)
            {
                MessageBox.Show("Bunday raqamli chek topilmadi!", "Xato", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        });
    }

    private async Task ReturnSaleAsync()
    {
        if (FoundSale == null) return;

        var confirm = MessageBox.Show($"Sotuv {FoundSale.SaleNumber} to'liq qaytariladimi?\nSumma: {FoundSale.Total} so'm", 
                                      "Vozvrat qilish", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (confirm != MessageBoxResult.Yes) return;

        await RunAsync(async () =>
        {
            // Qaytarish uchun yangi "Manfiy" sotuv yaratamiz
            var returnSale = new Sale
            {
                SaleNumber = await _saleRepo.GenerateSaleNumberAsync() + "-RET",
                UserId = FoundSale.UserId, // yoki joriy user
                CashierName = FoundSale.CashierName,
                SubTotal = -FoundSale.SubTotal,
                Discount = -FoundSale.Discount,
                Total = -FoundSale.Total,
                AmountPaid = -FoundSale.AmountPaid,
                Change = 0,
                PaymentMethod = FoundSale.PaymentMethod,
                Status = "Returned",
                Items = FoundSale.Items.Select(i => new SaleItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Barcode = i.Barcode,
                    UnitPrice = i.UnitPrice,
                    CostPrice = i.CostPrice,
                    Quantity = -i.Quantity, // Manfiy miqdor
                    Discount = -i.Discount
                }).ToList()
            };

            await _saleRepo.CreateSaleAsync(returnSale);

            MessageBox.Show("Vozvrat muvaffaqiyatli amalga oshirildi va omborga mahsulotlar qaytarildi!", 
                            "Muvaffaqiyatli", MessageBoxButton.OK, MessageBoxImage.Information);

            // Holatni yangilash
            FoundSale = null;
            SearchSaleNumber = string.Empty;
        });
    }
}
