using ED2.Contracts.Services;
using ED2.Core.Contracts.Services;
using ED2.Core.Helpers;
using ED2.Helpers;
using ED2.Models;
using LiteDB;
using Microsoft.Extensions.Options;
using System.Collections;
using Windows.ApplicationModel;
using Windows.Storage;

namespace ED2.Services;

public class LocalSettingsService : ILocalSettingsService
{
    private const string _defaultApplicationDataFolder = "ED2/ApplicationData";
    private const string _defaultLocalSettingsFile = "LocalSettings.json";

    private readonly IFileService _fileService;
    private readonly LocalSettingsOptions _options;

    private readonly ILiteDatabase db;
    private readonly ILiteCollection<CompletedImageDbSettings> completedImageDbSettingsDbCollection;

    private readonly string _localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _applicationDataFolder;
    private readonly string _localsettingsFile;

    private IDictionary<string, object> _settings;

    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _options = options.Value;

        _applicationDataFolder = Path.Combine(_localApplicationData, _options.ApplicationDataFolder ?? _defaultApplicationDataFolder);
        _localsettingsFile = _options.LocalSettingsFile ?? _defaultLocalSettingsFile;

        _settings = new Dictionary<string, object>();

        db = _fileService.GetSettingsDatabase(_applicationDataFolder);
        completedImageDbSettingsDbCollection = db.GetCollection<CompletedImageDbSettings>();
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            _settings = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(_applicationDataFolder, _localsettingsFile)) ?? new Dictionary<string, object>();

            _isInitialized = true;
        }
    }

    public async Task<T?> ReadSettingAsync<T>(string key, T? defaultValue = default)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
                return await Json.ToObjectAsync<T>((string)obj);
        }
        else
        {
            await InitializeAsync();

            if (_settings != null && _settings.TryGetValue(key, out var obj))
                return await Json.ToObjectAsync<T>((string)obj);
        }

        return defaultValue;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        if (RuntimeHelper.IsMSIX)
            ApplicationData.Current.LocalSettings.Values[key] = await Json.StringifyAsync(value);
        else
        {
            await InitializeAsync();

            _settings[key] = await Json.StringifyAsync(value);
            await Task.Run(() => _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings));
        }
    }

    class GeneralDbSettings
    {
        public int Id { get; set; }

        public List<Uri>? RecentlyUsedUris { get; set; }
    }

    class CompletedImageDbSettings
    {
        public int Id { get; set; }

        public Uri Uri { get; set; } = null!;
    }

    public IList<Uri> GetSuggestions(string partial) =>
        db.GetCollection<GeneralDbSettings>().FindAll().FirstOrDefault()?.RecentlyUsedUris?
            .Where(w => w.ToString().Contains(partial, StringComparison.InvariantCultureIgnoreCase)).ToList() ?? (IList<Uri>)Array.Empty<Uri>();

    public void AddSuggestion(Uri suggestion)
    {
        var collection = db.GetCollection<GeneralDbSettings>();
        if (collection.Count() == 0)
            collection.Insert(new GeneralDbSettings()
            {
                RecentlyUsedUris = new() { suggestion }
            });
        else
        {
            var settings = collection.FindAll().First();
            settings.RecentlyUsedUris ??= new();
            if (!settings.RecentlyUsedUris.Contains(suggestion))
            {
                settings.RecentlyUsedUris.Add(suggestion);
                collection.Update(settings);
            }
        }
    }

    public bool IsImageCompleted(Uri uri) =>
        completedImageDbSettingsDbCollection.FindOne(w => w.Uri == uri) is not null;

    public void SetImageCompleted(Uri uri)
    {
        completedImageDbSettingsDbCollection.EnsureIndex(w => w.Uri);
        if (!IsImageCompleted(uri))
            completedImageDbSettingsDbCollection.Insert(new CompletedImageDbSettings() { Uri = uri });
    }
}
