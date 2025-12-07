using System.Web;

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

    private async void WebViewNavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
    {
        if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri) && ViewModel!.ExpectedUriRegex.IsMatch(uri.AbsolutePath.ToString()))
        {
            ViewModel!.Result = uri;

            foreach (var cookie in await WebView.CoreWebView2.CookieManager.GetCookiesAsync(WebView.CoreWebView2.Source))
                App.HttpClientCookieContainer.Add(new Cookie(cookie.Name, HttpUtility.UrlEncode(cookie.Value), cookie.Path, cookie.Domain));

            Hide();
        }
    }
}
