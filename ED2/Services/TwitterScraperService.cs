using CefSharp;
using CefSharp.Handler;
using CefSharp.OffScreen;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ED2.Services;

partial class TwitterScraperService(ILogger<TwitterScraperService> logger) : IDisposable
{
    ChromiumWebBrowser? wb;
    private bool disposedValue;

    public async IAsyncEnumerable<Uri> EnumerateMediaAsync(Uri timeline)
    {
        await EnsureCefCreated();
        await Stop();
        wb.Load(timeline.ToString());

        var token = CancellationTokenSource.Token;
        while (!Loaded && !token.IsCancellationRequested)
            await Task.Delay(100);

        while (!token.IsCancellationRequested)
        {
            while (Uris.TryTake(out var uri))
                yield return uri;

            // next page
            wb.ExecuteScriptAsync("window.scrollBy(0, 1500);");
            await Task.Delay(50);
        }
    }

    async Task Stop()
    {
        CancellationTokenSource.Cancel();
        Uris.Clear();
        Loaded = false;
        CancellationTokenSource = new();
    }

    bool Loaded { get; set; }
    CancellationTokenSource CancellationTokenSource { get; set; } = new();
    ConcurrentBag<Uri> Uris { get; } = [];
    class BasicRequestHandler(TwitterScraperService service, ILogger<TwitterScraperService> logger) : RequestHandler
    {
        protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload,
            string requestInitiator, ref bool disableDefaultHandling)
        {
            var url = request.Url;
            logger.LogDebug("URL found: {0}", url);

            if (Regex.Match(url, @"(https:\/\/pbs\.twimg\.com\/media\/[^?]+\?format=[^&]+&name=).*") is { Success: true } m)
            {
                var fullUrl = m.Groups[1].Value.Replace("format=webp", "format=jpg") + "orig";

                service.Uris.Add(new(fullUrl));
                logger.LogDebug("URL matched as media: {0}", fullUrl);
            }

            return base.GetResourceRequestHandler(chromiumWebBrowser, browser, frame, request, isNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
        }
    }

    [MemberNotNull(nameof(wb))]
    private async Task EnsureCefCreated()
    {
        const string cookies = """
            x.com	TRUE	/	TRUE	1755155409	d_prefs	MjoxLGNvbnNlbnRfdmVyc2lvbjoyLHRleHRfdmVyc2lvbjoxMDAw
            x.com	TRUE	/	TRUE	1755155409	att	1-lpEriJwbHLHan9P7Wx2wCc0jFGIw3ksrFxIgczyO
            x.com	TRUE	/	TRUE	1787085668	guest_id_marketing	v1%3A175261228305238894
            x.com	TRUE	/	TRUE	1787085668	guest_id_ads	v1%3A175261228305238894
            x.com	TRUE	/	TRUE	1782916058	personalization_id	"v1_tYha2XeKz7QTnhP8SxoY3A=="
            x.com	TRUE	/	TRUE	1782172985	guest_id	v1%3A175261228305238894
            x.com	FALSE	/	FALSE	1763165228	g_state	{"i_l":0}
            x.com	TRUE	/	TRUE	1782176160	kdt	IY9gN39yiO1CVRreI6yZBEglL5zrlWA3X0TOrqw5
            x.com	TRUE	/	TRUE	1784061668	twid	u%3D485917107
            x.com	TRUE	/	TRUE	1782176160	ct0	d6c847d9e1fb7df8c051dd010b21db36034b2a6e5210213314e6cd467f9877ca4c360422c45167e09f0d01cf06fa94f71f12c0d2271a76f08b946e28f49078b04c65d634cd526382a6f5b3f9456e9401
            x.com	TRUE	/	TRUE	1782176160	auth_token	1d2c1ae39729b61bb5236c3b506fb9d8d44419dd
            x.com	FALSE	/	FALSE	0	lang	en
            x.com	TRUE	/	TRUE	1752527466	__cf_bm	Bj0ld6CumAEp3drgZLHn8_6T1RR07iaWLF1E6f2o_NQ-1752612145-1.0.1.1-rR5NUmc1GYIdyDcwfXw6s1RRDWV1cAiEYngUO3KldeOH8.b2M8oPLbTm9USTrQnN4lVHw4lApE6HlUZHKOHrqxSwvzDwkDuFRrDag1nx6VM
            """;

        if (wb is not null) return;
        wb = new("about:blank");
        await wb.WaitForInitialLoadAsync();

        var cookieManager = Cef.GetGlobalCookieManager();
        foreach (var (url, path, name, value) in cookies.Split(Environment.NewLine).Select(c => c.Split('\t')).Select(w => (w[0], w[2], w[5], w[6])))
        {
            var ok = cookieManager.SetCookie("https://" + url, new()
            {
                Name = name,
                Value = value,
            });
            if (!ok) { }
        }

        wb.Size = new(1000, 2000);

        wb.RequestHandler = new BasicRequestHandler(this, logger);
        wb.FrameLoadEnd += (s, e) =>
        {
            if (e.Frame.IsMain)
                Loaded = true;
        };
    }

    public async Task<byte[]?> GetScreenshotAsync()
    {
        if (wb is not null)
            return await wb.CaptureScreenshotAsync(CefSharp.DevTools.Page.CaptureScreenshotFormat.Png, 100);
        return null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // managed
            }

            // unmanaged
            wb?.Dispose();
            wb = null;

            CancellationTokenSource.Dispose();

            disposedValue = true;
        }
    }

    // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~TwitterScraperService()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
