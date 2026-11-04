namespace Qutre3D.Core;

public enum AppLanguage
{
    Russian = 0,
    English = 1,
}

/// <summary>
/// Очень простой словарь без ключей: пара «русский / английский» прямо в месте использования.
/// Так перевод невозможно «потерять» — он всегда рядом с кодом.
/// </summary>
public static class Loc
{
    public static AppLanguage Language { get; private set; } = AppLanguage.Russian;

    public static bool IsEnglish => Language == AppLanguage.English;

    public static event Action LanguageChanged;

    public static void SetLanguage(AppLanguage language)
    {
        if (Language == language) return;
        Language = language;
        LanguageChanged?.Invoke();
    }

    public static string T(string ru, string en) => IsEnglish ? en : ru;

    public static string F(string ru, string en, params object[] args) =>
        string.Format(IsEnglish ? en : ru, args);

    public static string Name(AppLanguage language) =>
        language == AppLanguage.English ? "English" : "Русский";
}
