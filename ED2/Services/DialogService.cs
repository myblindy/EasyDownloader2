namespace ED2.Services;

class DialogService : IDialogService
{
    public async Task ShowErrorAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.MainWindow.Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    public async Task<Uri?> ShowOAuthWindowAsync(Uri uri, Regex expectedUriRegex)
    {
        var vm = new OAuthViewModel(uri, expectedUriRegex);

        var dialog = new OAuthDialog
        {
            ViewModel = vm,
            XamlRoot = App.MainWindow.Content.XamlRoot
        };
        await dialog.ShowAsync();

        return vm.Result;
    }
}
