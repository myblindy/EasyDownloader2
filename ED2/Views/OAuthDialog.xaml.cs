using ED2.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ED2.Views;

public sealed partial class OAuthDialog : ContentDialog
{
    public OAuthViewModel? ViewModel { get; set; }

    public OAuthDialog()
    {
        InitializeComponent();

        Closed += (s, e) => App.MainWindow.SizeChanged -= MainWindowSizeChanged;
        App.MainWindow.SizeChanged += MainWindowSizeChanged;
        MainWindowSizeChanged(null!, null!);
    }

    private void MainWindowSizeChanged(object sender, WindowSizeChangedEventArgs args) =>
        (WebView.Width, WebView.Height) = (App.MainWindow.Bounds.Width * .8, App.MainWindow.Bounds.Height * .7);

    private void WebViewNavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
    {
        if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri) && uri.ToString().StartsWith(ViewModel!.ExpectedUri.ToString()))
        {
            ViewModel!.Result = uri;
            Hide();
        }
    }
}
