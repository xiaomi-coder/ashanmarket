using System;
using System.Threading.Tasks;
using System.Windows.Input;
using SupermarketPOS.Helpers;
using SupermarketPOS.Models;
using SupermarketPOS.Services;

namespace SupermarketPOS.ViewModels;

public class ShiftViewModel : BaseViewModel
{
    private readonly IShiftService _shiftService;
    private readonly IAuthService _authService;
    private readonly bool _isOpening;
    private readonly int _shiftId;
    public Action<bool>? CloseAction { get; set; }

    public string Title => _isOpening ? "Smenani Ochish" : "Smenani Yopish (Z-Report)";
    public string InputLabel => _isOpening ? "Kassadagi boshlang'ich naqd pul (so'm):" : "Kassadagi haqiqiy naqd pul (so'm):";
    public string ButtonText => _isOpening ? "Smenani Boshlash" : "Smenani Yakunlash";
    
    private string _infoMessage = "";
    public string InfoMessage { get => _infoMessage; set => SetProperty(ref _infoMessage, value); }

    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    private string _inputAmount = "";
    public string InputAmount { get => _inputAmount; set => SetProperty(ref _inputAmount, value); }

    public ICommand SubmitCommand { get; }

    public ShiftViewModel(IShiftService shiftService, IAuthService authService, bool isOpening, Shift? activeShift = null)
    {
        _shiftService = shiftService;
        _authService = authService;
        _isOpening = isOpening;
        _shiftId = activeShift?.Id ?? 0;

        if (_isOpening)
        {
            InfoMessage = $"Kassir: {_authService.CurrentUser?.FullName}\n" +
                          $"Smena ochilmoqda. Iltimos, kassadagi mavjud pul miqdorini kiriting.";
        }
        else if (activeShift != null)
        {
            InfoMessage = $"Smena boshlangan vaqt: {activeShift.OpenedAt:dd.MM.yyyy HH:mm}\n" +
                          $"Boshlang'ich qoldiq: {activeShift.StartingBalance:N0} so'm\n" +
                          $"Kassadagi kutilayotgan summa (sotuvlar bilan): {activeShift.ExpectedBalance:N0} so'm\n" +
                          "Iltimos, kassadagi haqiqiy pulni sanab kiriting.";
        }

        SubmitCommand = new AsyncRelayCommand(SubmitAsync);
    }

    private async Task SubmitAsync()
    {
        if (!decimal.TryParse(InputAmount, out decimal amount))
        {
            ErrorMessage = "Faqat son kiriting!";
            return;
        }

        ErrorMessage = "";
        try
        {
            if (_isOpening)
            {
                await _shiftService.OpenShiftAsync(_authService.CurrentUser!.Id, _authService.CurrentUser.FullName, amount);
            }
            else
            {
                await _shiftService.CloseShiftAsync(_shiftId, amount);
            }
            CloseAction?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
