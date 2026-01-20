using HtmlAgilityPack;
using System.Collections.Concurrent;

namespace ED2.Sources;

partial class RedditSource(MainViewModel mainViewModel, ILocalSettingsService localSettingsService) : BaseSource(localSettingsService)
{
    DispatcherQueue? mainDispatcherQueue;
    string? subReddit;

    public override bool CanHandle(Uri uri, [NotNullWhen(true)] out Uri? normalizedUri, [NotNullWhen(true)] out string? prefix)
    {
        if (UriRegex().Match(uri.ToString()) is { Success: true } m)
        {
            normalizedUri = new($"https://{m.Groups[1].Value}/");
            prefix = m.Groups[2].Value;
            return true;
        }

        prefix = null;
        normalizedUri = null;
        return false;
    }

    public override async Task LoadAsync(Uri uri, DispatcherQueue mainDispatcherQueue, Func<ImageDetails>? imageDetailsGenerator = null)
    {
        subReddit = UriRegex().Match(uri.ToString()).Groups[2].Value;
        this.mainDispatcherQueue = mainDispatcherQueue;
    }

    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        var cookieContainer = new CookieContainer();
        cookieContainer.Add(new Cookie("csv", "2", "/", ".reddit.com"));
        cookieContainer.Add(new Cookie("edgebucket", "nj8gC2FrlM5carcTJW", "/", ".reddit.com"));
        cookieContainer.Add(new Cookie("theme", "1", "/", ".reddit.com"));
        cookieContainer.Add(new Cookie("t2_93z9x_recentclicks3", "t3_1ofeq71", "/", ".reddit.com"));
        cookieContainer.Add(new Cookie("loid", "0000000000000wdbxu.2.1457890557883.Z0FBQUFBQm9fNmlaNXh0bXg5SzNQbzFoV21Mb1Y5OTBsMURIU3l0TTVXTWNtRE1tdHdnMmNNbTA0ZEJwTzY3TlRyNDR5QkRTbHdJRVNFZ2JCdWdmMVkxNllXamFTYlpFcFhHSmp5NUdraDVORkFIMzM1Q0IwSTFqR2t6S2tWZk5tTV9pcy1XNExEZTQ", "/", ".reddit.com"));
        cookieContainer.Add(new Cookie("reddit_session", "eyJhbGciOiJSUzI1NiIsImtpZCI6IlNIQTI1NjpsVFdYNlFVUEloWktaRG1rR0pVd1gvdWNFK01BSjBYRE12RU1kNzVxTXQ4IiwidHlwIjoiSldUIn0.eyJzdWIiOiJ0Ml93ZGJ4dSIsImV4cCI6MTc4MzI3NzE5MS42NDQyMDQsImlhdCI6MTc2NzYzODc5MS42NDQyMDQsImp0aSI6Inc3THdqeGtnYXZhVl9VYzZiVGc0WWswc2RxdXlZQSIsImF0IjoxLCJjaWQiOiJjb29raWUiLCJsY2EiOjE0NTc4OTA1NTc4ODMsInNjcCI6ImVKeUtqZ1VFQUFEX193RVZBTGsiLCJmbG8iOjIsImFtciI6WyJwd2QiXX0.Lofvj9rdaYFXhsdINwpmCMWTXKndZlHfWpgRA_xszpgN3jq1vehGpr1IHbe2y7t-qBtSwByz9JFaUsKVi2IEfRMwOKw-mFxyErhH0RCtqXEC5CUHotTnbsCe6aN5CloSOIO3_DLajv5kFUtgpa9fDq-8sDDLSQAHTzbAGhB0KN8GuhTJ0XuzsP_gshC05tkPgLr5M7PL44qzsOtBrvILAowq7MXPo9FaXYX5NQY7jmaUd03pTufn5vlLjqPGP8_xJNWrnQXK-JlV6cD1vsVeDeAnwUwtbnUSawCnoDFrirIkqFnU4K_R0tDkxuegZSiCap7BNdglrVuwsHzTqNkLLw", "/", ".reddit.com"));
        cookieContainer.Add(new Cookie("csrf_token", "0723020ce65af9cc3d31d296efc37bf3", "/", ".reddit.com"));
        cookieContainer.Add(new Cookie("token_v2", "eyJhbGciOiJSUzI1NiIsImtpZCI6IlNIQTI1NjpzS3dsMnlsV0VtMjVmcXhwTU40cWY4MXE2OWFFdWFyMnpLMUdhVGxjdWNZIiwidHlwIjoiSldUIn0.eyJzdWIiOiJ1c2VyIiwiZXhwIjoxNzY4NDE4NzA5Ljk3ODUyMiwiaWF0IjoxNzY4MzMyMzA5Ljk3ODUyMiwianRpIjoiSmxYQkxhWlJPcHhPSnpmN1NmRmZ4U0lWZVd1MG5RIiwiY2lkIjoiMFItV0FNaHVvby1NeVEiLCJsaWQiOiJ0Ml93ZGJ4dSIsImFpZCI6InQyX3dkYnh1IiwiYXQiOjEsImxjYSI6MTQ1Nzg5MDU1Nzg4Mywic2NwIjoiZUp4a2tkR090REFJaGQtRmE1X2dmNVVfbTAxdGNZYXNMUWFvazNuN0RWb2NrNzA3Y0Q0cEhQOURLb3FGRENaWGdxbkFCRmdUclREQlJ1VDluTG0zZzJpTmU4dFlzWm5DQkZtd0ZEcmttTEdzaVFRbWVKSWF5eHNtb0lMTnlGeXV0R05OTFQwUUpxaGNNcmVGSHBjMm9ia2JpNTZkR0ZXNXJEeW9zVmZsMHRqR0ZMWW54amNicXcycHVDNm5Na25MUXZrc1h2VGpOOVczOXZtel9TYTBKOE9LcXVtQjNobEpDRzRzZnBpbTNkOVRrNTZ0Q3hhMTkzcVEydWQ2M0s1OTFpdzBPN2VmNl9sckl4bVhZMmgtSnZ0MzF5LWhBNDg4THpQcUFFYXM0VWNaZG1RZF9sVUhVTG1nSkdNSjR0TUk1TXJsMjM4SnRtdlR2OGJ0RXo5OE0tS21OX3pXRE5SekNlTFFwX0gxR3dBQV9fOFExZVRSIiwicmNpZCI6ImJGSXI3NDlKbGNsWkREN2VsWG0wOHg3ZGxFRjNsLUJpQ1F0cU1EY1NhelEiLCJmbG8iOjJ9.pIUNTX-SAwd0U_dXw_bhKjEN9R5CxTURr9kWzRvzCDn3gZ1X7ybDoGBXWK74pTwJwlcfbo_Zd_Bn2FMCvqW0Rp24EiJgj5TdpAoNJ5KTPGS4ZrqUhOTMds4scAlyn5uekzSZ00XaxlxRze6YuXYA30XtArjHSkNbpjHinC_vEJMhTo-J0cLjXdT4GNiSIGGM4M78iZ9Ft2ckHe28NR_B1Uk6e4CRLK_Z_JC-Av7c8M-Uuad9mKvMIPaGMkHZNJX5JYvcs6l9d7kNKZmvVD43KNJ_sdb7_v0hLCk69kXai8jlpqSal1-IZrbPsdhim23RsfKOFi7IsF1DCD4i1y1CVQ", "/", ".reddit.com"));
        cookieContainer.Add(new Cookie("t2_wdbxu_recentclicks3", "t3_1idlern%2Ct3_1kc35zj%2Ct3_1lkuth9%2Ct3_phs1w6%2Ct3_gf9uzn%2Ct3_tex1ob%2Ct3_9m410w%2Ct3_1oo6167%2Ct3_6wrptd%2Ct3_dxnxaw", "/", ".reddit.com"));
        cookieContainer.Add(new Cookie("redesign_optout", "true", "/", ".reddit.com"));
        cookieContainer.Add(new Cookie("pc", "0n", "/", ".reddit.com"));
        cookieContainer.Add(new Cookie("session_tracker", "irkqkcohgcpnqpqilp.0.1768408894555.Z0FBQUFBQnBaOGNfTGVOQ2VxbEkwaDZvWTllMGF4Rm81ckRZZm83MVBoWkh6S0dtamhoejVnVTFPOU1kZmVSU3owcXR3eUVzdFQ1QjdfU1hhSC01MWUzNUVLUGdyTGZSR0NJbXMyMHlrZFdXMUpfaHBSZEw2Mk50dGJGZmxLQjJPcEF1dFVFZERRZEs", "/", ".reddit.com"));

        using var httpHandler = new HttpClientHandler { CookieContainer = cookieContainer, AutomaticDecompression = DecompressionMethods.All, AllowAutoRedirect = true };
        using var http = new HttpClient(httpHandler);

        var nextPageUri = new Uri($"https://old.reddit.com/r/{subReddit}/new/");
        while (true)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, nextPageUri);
            requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36 Edg/143.0.0.0");

            using var response = await http.SendAsync(requestMessage).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var doc = new HtmlDocument();
            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                doc.Load(stream);

            foreach (var node in doc.DocumentNode.SelectNodes("""
                //div[contains(@class, "thing") and contains(@class, "link") and not(contains(@class, "promoted"))]
                """))
            {
                if (node.Attributes["data-timestamp"]?.Value is { } timestampString
                    && long.TryParse(timestampString, out var timestamp)
                    && DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime is { } date
                    && node.Attributes["data-url"]?.Value is { } url
                    && node.SelectSingleNode(""".//a[contains(@class, "title")]""")?.InnerHtml is { } title)
                {
                    var flair = node.SelectSingleNode(""".//span[contains(@class, "linkflairlabel")]""")?.InnerHtml;

                    // figure out if we can find media links in this
                    var found = false;

                    async IAsyncEnumerable<ImageDetails> TryToLoadUri(Uri uri, BaseSource source)
                    {
                        if (source.CanHandle(uri, out _, out _))
                        {
                            await source.LoadAsync(uri, mainDispatcherQueue!, () => new RedditImageDetails(mainViewModel)
                            {
                                Flair = flair,
                                Title = title,
                                DatePosted = date
                            });
                            await foreach (var imageDetails in source.EnumerateImageDetails())
                                yield return imageDetails;
                        }
                    }

                    foreach (var source in new BaseSource[]
                    {
                        App.GetService<DirectImageSource>(),
                        //App.GetService<ImgurSource>(),
                        App.GetService<RedditGallerySource>(),
                    })
                    {
                        await foreach (var imageDetails in TryToLoadUri(new Uri(url), source))
                        {
                            yield return imageDetails;
                            found = true;
                        }
                        if (found) break;
                    }

                    if (!found)
                    {
                        // try to load the post as a gallery
                        var gallerySource = App.GetService<RedditGallerySource>();
                        await foreach (var imageDetails in TryToLoadUri(new Uri(url), gallerySource))
                            yield return imageDetails;
                    }
                }
            }

            if (doc.DocumentNode.SelectSingleNode("""//span[contains(@class, "next-button")]/a""")?.Attributes["href"]?.Value is not { } nextPageUrl)
                break;
            nextPageUri = new(nextPageUrl);
        }
    }

    public override Task OnSaveImage(ImageDetails imageDetails) => Task.CompletedTask;

    [GeneratedRegex(@"^(?:https?:\/\/)?(?:www.)?(reddit\.com\/r\/([^/]+))")]
    private static partial Regex UriRegex();
}

partial class RedditImageDetails(MainViewModel mainViewModel) : ImageDetails(mainViewModel)
{
}
