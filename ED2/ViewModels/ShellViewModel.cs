namespace ED2.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    bool isBackEnabled;

    public INavigationService NavigationService { get; }

    public MainViewModel MainViewModel { get; }

    public ShellViewModel(INavigationService navigationService, MainViewModel mainViewModel)
    {
        NavigationService = navigationService;
        MainViewModel = mainViewModel;
        NavigationService.Navigated += OnNavigated;
    }

    private void OnNavigated(object sender, NavigationEventArgs e) =>
        IsBackEnabled = NavigationService.CanGoBack;

    [RelayCommand]
    void Open()
    {
        var nextIsOpening = NavigationService.Frame?.GetPageViewModel() is MainViewModel ? !MainViewModel.IsOpening : true;
        NavigationService.NavigateTo<MainViewModel>();
        MainViewModel.IsOpening = nextIsOpening;
    }

    [RelayCommand]
    void Settings()
    {
        if (NavigationService.Frame?.GetPageViewModel() is SettingsViewModel)
            NavigationService.NavigateTo<MainViewModel>();
        else
            NavigationService.NavigateTo<SettingsViewModel>();
    }
}
