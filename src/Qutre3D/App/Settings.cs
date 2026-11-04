using System.Text.Json;
using Qutre3D.Core;

namespace Qutre3D.App;

/// <summary>
/// Пользовательские настройки. Лежат рядом с данными приложения:
/// Windows — %APPDATA%\Qutre3D, Linux — ~/.config/Qutre3D (или $XDG_CONFIG_HOME).
/// </summary>
public sealed class AppSettings
{
    public string Language { get; set; } = DefaultLanguage();
    public string LastDirectory { get; set; } = "";

    private static string DefaultLanguage()
    {
        // Если установщик не записал язык — ориентируемся на локаль системы.
        string culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return string.Equals(culture, "ru", StringComparison.OrdinalIgnoreCase) ? "ru" : "en";
    }

    public static string Directory
    {
        get
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(root))
                root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

            return Path.Combine(root, "Qutre3D");
        }
    }

    public static string FilePath => Path.Combine(Directory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (loaded is not null) return loaded;
            }
        }
        catch
        {
            // Битый файл настроек не должен мешать запуску.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Настройки — не критичная вещь; молча продолжаем.
        }
    }

    public AppLanguage ResolveLanguage()
    {
        return string.Equals(Language, "en", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.English
            : AppLanguage.Russian;
    }

    public void SetLanguage(AppLanguage language)
    {
        Language = language == AppLanguage.English ? "en" : "ru";
    }
}
