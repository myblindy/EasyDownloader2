using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ED2.Contracts.Services;
using ED2.Helpers;

using Microsoft.UI.Xaml;

using Windows.ApplicationModel;
using Windows.Storage.Pickers;

namespace ED2.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    ElementTheme elementTheme;

    [ObservableProperty]
    string versionDescription;

    public MainViewModel MainViewModel { get; }

    [RelayCommand]
    async Task SwitchTheme(ElementTheme param)
    {
        if (ElementTheme != param)
        {
            ElementTheme = param;
            await _themeSelectorService.SetThemeAsync(param);
        }
    }

    async Task BrowseImageSaveFolderHelper(Action<string> setter)
    {
        var picker = new FolderPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.MainWindow.GetWindowHandle());
        picker.FileTypeFilter.Add("*");

        if (await picker.PickSingleFolderAsync() is { } folder)
            setter(folder.Path);
    }

    [RelayCommand]
    Task BrowseHorizontalSaveFolder() =>
        BrowseImageSaveFolderHelper(path => MainViewModel.HorizontalSaveFolder = path);

    [RelayCommand]
    Task BrowseVerticalSaveFolder() =>
        BrowseImageSaveFolderHelper(path => MainViewModel.VerticalSaveFolder = path);

    [RelayCommand]
    Task BrowseSquareSaveFolder() =>
        BrowseImageSaveFolderHelper(path => MainViewModel.SquareSaveFolder = path);

    public SettingsViewModel(IThemeSelectorService themeSelectorService, MainViewModel mainViewModel)
    {
        _themeSelectorService = themeSelectorService;
        MainViewModel = mainViewModel;
        elementTheme = _themeSelectorService.Theme;
        versionDescription = GetVersionDescription();
    }

    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
