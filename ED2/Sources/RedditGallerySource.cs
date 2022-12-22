using HtmlAgilityPack;

namespace ED2.Sources;

partial class RedditGallerySource : BaseSource
{
    readonly MainViewModel mainViewModel;
    Func<ImageDetails>? imageDetailsGenerator;
    Uri? uri;

    public RedditGallerySource(MainViewModel mainViewModel, ILocalSettingsService localSettingsService) : base(localSettingsService)
    {
        this.mainViewModel = mainViewModel;
    }

    public override bool CanHandle(Uri uri, [NotNullWhen(true)] out Uri? normalizedUri, out string? prefix)
    {
        prefix = null;

        if (UriRegex().Match(uri.ToString()) is { Success: true } m)
        {
            normalizedUri = new($"https://{m.Groups[1].Value}/");
            return true;
        }

        normalizedUri = null;
        return false;
    }

    public override Task LoadAsync(Uri uri, DispatcherQueue mainDispatcherQueue, Func<ImageDetails>? imageDetailsGenerator = null)
    {
        this.imageDetailsGenerator = imageDetailsGenerator;
        this.uri = uri;
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        var doc = new HtmlDocument();
        using (var pageStream = await App.HttpClient.GetStreamAsync(uri))
            doc.Load(pageStream);

        const string jsonVarName = "window.___r";
        var json = JObject.Parse(WindowJsVarAssignmentRegex().Replace(doc.DocumentNode.SelectSingleNode($"//script[contains(.,'{jsonVarName}')]").InnerText, ""));

        if (json["posts"]?["models"]?.Values()?.First()?["media"]?["mediaMetadata"] is { } mediaMetaData)
            foreach (var previewPath in mediaMetaData.Select(w => w.First()["s"]?["u"]?.Value<string>()).Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                if (PreviewRedditUrlRegex().Match(previewPath!) is not { Success: true } m
                    || !m.Groups[1].Success)
                {
                    continue;
                }

                var link = new Uri("https://i." + m.Groups[1].Value);

                var img = (imageDetailsGenerator ?? (() => new ImageDetails(mainViewModel)))();
                img.IsCompleted = localSettingsService.IsImageCompleted(link);
                img.Link = link;
                yield return img;
            }
    }

    public override Task OnSaveImage(ImageDetails imageDetails) => Task.CompletedTask;

    [GeneratedRegex(@"^(?:https?:\/\/)?(?:www\.)?(reddit\.com\/gallery\/[^/]+)$")]
    private static partial Regex UriRegex();
    [GeneratedRegex("window.___r\\s*=\\s*")]
    private static partial Regex WindowJsVarAssignmentRegex();
    [GeneratedRegex("^https?://preview\\.(redd\\.it/[^.]+\\.\\w+)")]
    private static partial Regex PreviewRedditUrlRegex();
}
