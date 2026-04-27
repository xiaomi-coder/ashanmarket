using System.Collections.ObjectModel;
using System.Windows.Input;
using SupermarketPOS.Models;
using SupermarketPOS.Repositories;
using SupermarketPOS.Helpers;
using System.Windows;

namespace SupermarketPOS.ViewModels;

public class UserManagementViewModel : BaseViewModel
{
    private readonly IUserRepository _userRepo;

    public ObservableCollection<User> Users { get; } = new();

    private User? _selectedUser;
    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                if (value != null)
                {
                    FullName = value.FullName;
                    Username = value.Username;
                    Role = value.Role;
                    IsEditing = true;
                }
                else
                {
                    ClearForm();
                }
            }
        }
    }

    private string _fullName = string.Empty;
    public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }

    private string _username = string.Empty;
    public string Username { get => _username; set => SetProperty(ref _username, value); }

    private string _password = string.Empty;
    public string Password { get => _password; set => SetProperty(ref _password, value); }

    private string _role = "Cashier";
    public string Role { get => _role; set => SetProperty(ref _role, value); }

    public ObservableCollection<string> AvailableRoles { get; } = new() { "Cashier", "Admin" };

    private bool _isEditing;
    public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }

    public ICommand SaveCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand ToggleStatusCommand { get; }

    public UserManagementViewModel(IUserRepository userRepo)
    {
        _userRepo = userRepo;
        
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !string.IsNullOrWhiteSpace(FullName) && !string.IsNullOrWhiteSpace(Username));
        ClearCommand = new RelayCommand(ClearForm);
        ToggleStatusCommand = new AsyncRelayCommand(ToggleStatusAsync, () => SelectedUser != null);

        _ = LoadUsersAsync();
    }

    public async Task LoadUsersAsync()
    {
        await RunAsync(async () =>
        {
            var data = await _userRepo.GetAllAsync();
            Users.Clear();
            foreach (var u in data) Users.Add(u);
        });
    }

    private async Task SaveAsync()
    {
        await RunAsync(async () =>
        {
            if (IsEditing && SelectedUser != null)
            {
                SelectedUser.FullName = FullName.Trim();
                SelectedUser.Username = Username.Trim();
                SelectedUser.Role = Role;
                // Parol o'zgartirilgan bo'lsa
                if (!string.IsNullOrWhiteSpace(Password))
                {
                    SelectedUser.PasswordHash = Password; // Haqiqiy hayotda hashlash kerak (BCrypt)
                }

                await _userRepo.UpdateAsync(SelectedUser);
            }
            else
            {
                var user = new User
                {
                    FullName = FullName.Trim(),
                    Username = Username.Trim(),
                    PasswordHash = string.IsNullOrWhiteSpace(Password) ? "12345" : Password,
                    Role = Role,
                    IsActive = true
                };
                await _userRepo.AddAsync(user);
            }

            ClearForm();
            await LoadUsersAsync();
        });
    }

    private async Task ToggleStatusAsync()
    {
        if (SelectedUser == null) return;

        if (SelectedUser.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Asosiy adminni o'chirib bo'lmaydi!", "Xato", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await RunAsync(async () =>
        {
            SelectedUser.IsActive = !SelectedUser.IsActive;
            await _userRepo.UpdateAsync(SelectedUser);
            ClearForm();
            await LoadUsersAsync();
        });
    }

    private void ClearForm()
    {
        SelectedUser = null;
        FullName = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        Role = "Cashier";
        IsEditing = false;
    }
}
