using Microsoft.Extensions.DependencyInjection;
using SupermarketPOS.Data;
using SupermarketPOS.Repositories;
using SupermarketPOS.Services;
using SupermarketPOS.ViewModels;
using SupermarketPOS.Views;
using System.Windows;

namespace SupermarketPOS;

public partial class App : Application
{
    private IServiceProvider _services = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        // Seed database
        var db = _services.GetRequiredService<DatabaseContext>();
        var seeder = new DatabaseSeeder(db);
        seeder.Seed();

        // Ensure the app doesn't close when the Login window closes
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Show Login
        var loginVm = _services.GetRequiredService<LoginViewModel>();
        var loginWin = new LoginWindow { DataContext = loginVm };

        loginVm.OnLoginSuccess += () =>
        {
            loginWin.DialogResult = true;
            loginWin.Close();
        };

        if (loginWin.ShowDialog() == true)
        {
            try
            {
                var mainVm = _services.GetRequiredService<MainViewModel>();
                var mainWin = new MainWindow { DataContext = mainVm };
                MainWindow = mainWin;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWin.Show();
                _ = mainVm.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Xato", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        else
        {
            Shutdown();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core
        services.AddSingleton<DatabaseContext>();

        // Repositories
        services.AddSingleton<IProductRepository, ProductRepository>();
        services.AddSingleton<ISaleRepository, SaleRepository>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IShiftRepository, ShiftRepository>();
        services.AddSingleton<ICustomerRepository, CustomerRepository>();
        services.AddSingleton<IExpenseRepository, ExpenseRepository>();

        // Services
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<ISaleService, SaleService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IReceiptPrinterService, ReceiptPrinterService>();
        services.AddSingleton<IShiftService, ShiftService>();
        services.AddSingleton<ICustomerService, CustomerService>();
        services.AddSingleton<IBarcodePrinterService, BarcodePrinterService>();
        services.AddSingleton<ISyncService, SyncService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddSingleton<SalesViewModel>();
        services.AddSingleton<ProductManagementViewModel>();
        services.AddSingleton<ReportsViewModel>();
        services.AddSingleton<CategoryManagementViewModel>();
        services.AddSingleton<ReturnsViewModel>();
        services.AddSingleton<ExpensesViewModel>();
        services.AddSingleton<DebtsViewModel>();
        services.AddSingleton<UserManagementViewModel>();
        services.AddSingleton<MainViewModel>();
    }
}
