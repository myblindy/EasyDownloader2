using CefSharp;
using CefSharp.Handler;
using CefSharp.OffScreen;
using System.Collections.Concurrent;

namespace ED2.Services;

class TwitterScraperService
{
    ChromiumWebBrowser? wb;

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
    ConcurrentBag<Uri> Uris { get; } = new();
    class BasicRequestHandler : RequestHandler
    {
        private readonly TwitterScraperService service;

        public BasicRequestHandler(TwitterScraperService service)
        {
            this.service = service;
        }

        protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload,
            string requestInitiator, ref bool disableDefaultHandling)
        {
            var url = request.Url;

            if (Regex.Match(url, @"(https:\/\/pbs\.twimg\.com\/media\/[^?]+\?format=[^&]+&name=).*") is { Success: true } m)
            {
                var fullUrl = m.Groups[1].Value + "orig";

                service.Uris.Add(new(fullUrl));
            }

            return base.GetResourceRequestHandler(chromiumWebBrowser, browser, frame, request, isNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
        }
    }

    [MemberNotNull(nameof(wb))]
    private async Task EnsureCefCreated()
    {
        const string cookies = """
                twitter.com	TRUE	/	TRUE	1732158340	guest_id	v1%3A166908634089571450
                twitter.com	TRUE	/	TRUE	1716347164	kdt	deExEUJrIKwjVxcuGzwDDWJdFAxYTttyqxFcthJt
                twitter.com	TRUE	/	TRUE	1826766364	auth_token	81f28262157667a6542af941e3a731a13dc1bd57
                twitter.com	TRUE	/	TRUE	1826766364	ct0	3bc99452dbbfb2087c542bd809d8fc7c38817599d3fe9f38a5588ed15d900779bbe81a8bf0a26e6e6bc025d753514e4961e82f30cad6043e4b131755cede970109ecb124ac7238d2b9ec1223d91597d4
                twitter.com	TRUE	/	TRUE	1985299243	des_opt_in	Y
                twitter.com	TRUE	/	TRUE	1835817020	dnt	1
                help.twitter.com	TRUE	/	TRUE	1746050506	_ga	GA1.3.2101702960.1682978273
                twitter.com	TRUE	/	TRUE	1699027332	d_prefs	MToxLGNvbnNlbnRfdmVyc2lvbjoyLHRleHRfdmVyc2lvbjoxMDAw
                developer.twitter.com	TRUE	/	TRUE	1752282872	_ga	GA1.3.2101702960.1682978273
                twitter.com	TRUE	/	TRUE	1752282872	_ga	GA1.2.2101702960.1682978273
                twitter.com	TRUE	/	TRUE	1752455673	mbox	session#6de48cf9ac304bfaaf4f48864b0bee5d#1689212733|PC#6de48cf9ac304bfaaf4f48864b0bee5d.34_0#1752455673
                twitter.com	TRUE	/	TRUE	0	_twitter_sess	BAh7CSIKZmxhc2hJQzonQWN0aW9uQ29udHJvbGxlcjo6Rmxhc2g6OkZsYXNo%250ASGFzaHsABjoKQHVzZWR7ADoPY3JlYXRlZF9hdGwrCEWpTJ2EAToMY3NyZl9p%250AZCIlNjRjYWI3ZDJkZTk3MTJkZjEwNzJlMjViMDFmOGM4YmI6B2lkIiViZTY5%250AZDk4NTMwNzNjYWM2ZjgwZWQzZDg3YjZjYjI3Nw%253D%253D--15f0264a86db26385400a7e0dbb38974a7a92fdb
                api.twitter.com	FALSE	/	TRUE	0	lang	en
                twitter.com	TRUE	/	TRUE	0	at_check	true
                twitter.com	FALSE	/	FALSE	0	lang	en
                twitter.com	TRUE	/	TRUE	1752609171	guest_id_ads	v1%3A166908634089571450
                twitter.com	TRUE	/	TRUE	1752609171	guest_id_marketing	v1%3A166908634089571450
                twitter.com	TRUE	/	TRUE	1752609171	personalization_id	"v1_Dn7viZoa7MZSZEy08XreXw=="
                twitter.com	TRUE	/	TRUE	1721073171	twid	u%3D485917107
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

        wb.RequestHandler = new BasicRequestHandler(this);
        wb.FrameLoadEnd += (s, e) =>
        {
            if (e.Frame.IsMain)
                Loaded = true;
        };
    }
}
