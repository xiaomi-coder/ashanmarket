using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;
using SupermarketPOS.Helpers;
using SupermarketPOS.Services;

namespace SupermarketPOS.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly AppSettings _settings;

    private string _storeName = string.Empty;
    public string StoreName { get => _storeName; set => SetProperty(ref _storeName, value); }

    private string _backendApiUrl = string.Empty;
    public string BackendApiUrl { get => _backendApiUrl; set => SetProperty(ref _backendApiUrl, value); }

    private string _cloudSlug = string.Empty;
    public string CloudSlug { get => _cloudSlug; set => SetProperty(ref _cloudSlug, value); }

    private string _cloudUsername = string.Empty;
    public string CloudUsername { get => _cloudUsername; set => SetProperty(ref _cloudUsername, value); }

    private string _cloudPassword = string.Empty;
    public string CloudPassword { get => _cloudPassword; set => SetProperty(ref _cloudPassword, value); }

    private bool _isCloudConnected;
    public bool IsCloudConnected { get => _isCloudConnected; set => SetProperty(ref _isCloudConnected, value); }

    public ICommand SaveCommand { get; }
    public ICommand ConnectCloudCommand { get; }

    public event Action? OnSettingsSaved;

    public SettingsViewModel()
    {
        _settings = SettingsManager.Load();
        StoreName = _settings.StoreName;
        BackendApiUrl = _settings.BackendApiUrl;
        CloudSlug = _settings.CloudSlug;
        CloudUsername = _settings.CloudUsername;
        IsCloudConnected = !string.IsNullOrWhiteSpace(_settings.ApiKey);

        SaveCommand = new RelayCommand(SaveSettings);
        ConnectCloudCommand = new AsyncRelayCommand(ConnectCloudAsync);
    }

    private void SaveSettings()
    {
        _settings.StoreName = StoreName;
        _settings.BackendApiUrl = BackendApiUrl;
        _settings.CloudSlug = CloudSlug;
        _settings.CloudUsername = CloudUsername;
        // ApiKey is only updated by ConnectCloudAsync
        
        SettingsManager.Save(_settings);
        SetStatus("Sozlamalar saqlandi", false);
        OnSettingsSaved?.Invoke();
    }

    private async Task ConnectCloudAsync()
    {
        if (string.IsNullOrWhiteSpace(CloudSlug) || string.IsNullOrWhiteSpace(CloudUsername) || string.IsNullOrWhiteSpace(CloudPassword))
        {
            SetStatus("Barcha maydonlarni to'ldiring!", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(BackendApiUrl))
        {
            SetStatus("API URL kiritilmagan!", true);
            return;
        }

        await RunAsync(async () =>
        {
            try
            {
                using var client = new HttpClient();
                var loginUrl = $"{BackendApiUrl.TrimEnd('/')}/auth/login";
                
                var payload = new
                {
                    slug = CloudSlug,
                    username = CloudUsername,
                    password = CloudPassword
                };

                var response = await client.PostAsJsonAsync(loginUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var tenant = content.GetProperty("tenant");
                    
                    if (tenant.TryGetProperty("apiKey", out var apiKeyElement) && apiKeyElement.ValueKind == JsonValueKind.String)
                    {
                        var apiKey = apiKeyElement.GetString();
                        
                        _settings.ApiKey = apiKey;
                        _settings.CloudSlug = CloudSlug;
                        _settings.CloudUsername = CloudUsername;
                        _settings.BackendApiUrl = BackendApiUrl;
                        SettingsManager.Save(_settings);

                        IsCloudConnected = true;
                        SetStatus("Bulutga muvaffaqiyatli ulandi!", false);
                    }
                    else
                    {
                        SetStatus("Serverdan API Kalit kelmadi!", true);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    SetStatus($"Ulanishda xatolik: Noto'g'ri login/parol. ({response.StatusCode})", true);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ulanishda xato: {ex.Message}", true);
            }
        });
    }
}
