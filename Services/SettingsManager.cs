using System.IO;
using System.Text.Json;

namespace SupermarketPOS.Services;

public class AppSettings
{
    public string StoreName { get; set; } = "🛒 SuperMarket";
    
    // Cloud Sync Settings
    public string BackendApiUrl { get; set; } = "https://sotuvpos.uz/api";
    public string CloudSlug { get; set; } = "";
    public string CloudUsername { get; set; } = "";
    public string ApiKey { get; set; } = "";
}

public static class SettingsManager
{
    private static readonly string FilePath = "settings.json";

    public static AppSettings Load()
    {
        if (File.Exists(FilePath))
        {
            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch { }
        }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
