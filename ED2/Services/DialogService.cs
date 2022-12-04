using ED2.Contracts.Services;
using ED2.ViewModels;
using ED2.Views;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public async Task<Uri?> ShowOAuthWindowAsync(Uri uri, Uri expectedUri)
    {
        var vm = new OAuthViewModel(uri, expectedUri);

        var dialog = new OAuthDialog
        {
            ViewModel = vm,
            XamlRoot = App.MainWindow.Content.XamlRoot
        };
        await dialog.ShowAsync();

        return vm.Result;
    }
}
