using ED2.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ED2.Views;

public sealed partial class OAuthDialog : ContentDialog
{
    public OAuthViewModel? ViewModel { get; set; }

    public OAuthDialog()
    {
        InitializeComponent();

        WebView.Width = App.MainWindow.Bounds.Width * .8;
        WebView.Height = App.MainWindow.Bounds.Height * .7;
    }

    private void WebViewNavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
    {
        if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri) && uri.ToString().StartsWith(ViewModel!.ExpectedUri.ToString()))
        {
            ViewModel!.Result = uri;
            Hide();
        }
    }
}
