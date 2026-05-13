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
    private static string GetFilePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "settings.json");
    }

    public static AppSettings Load()
    {
        var filePath = GetFilePath();
        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch { }
        }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        var filePath = GetFilePath();
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }
}
