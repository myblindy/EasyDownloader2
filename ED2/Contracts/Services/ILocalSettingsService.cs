namespace ED2.Contracts.Services;

public interface ILocalSettingsService
{
    Task<T?> ReadSettingAsync<T>(string key, T? missingValue = default);
    Task SaveSettingAsync<T>(string key, T value);

    IList<Uri> GetSuggestions(string partial);
    void AddSuggestion(Uri suggestion);

    bool IsImageCompleted(Uri uri);
    void SetImageCompleted(Uri uri);
}
