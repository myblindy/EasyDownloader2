using HtmlAgilityPack;

namespace ED2.Sources;

partial class RedditGallerySource(MainViewModel mainViewModel, ILocalSettingsService localSettingsService)
    : BaseSource(localSettingsService)
{
    Func<ImageDetails>? imageDetailsGenerator;
    Uri? uri;

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

        HashSet<Uri> images = [];

        if (doc.DocumentNode.SelectSingleNode(@"//shreddit-redirect") is not { } commentsPageNode
            || commentsPageNode.GetAttributeValue("href", null) is not { } commentPageUrl
            || Uri.TryCreate($"https://reddit.com{commentPageUrl}", UriKind.Absolute, out var commentsPageUri) is false)
        {
            // old format?
            if (doc.DocumentNode.SelectNodes(@"//div[contains(@class, 'gallery-tile-content')]/img") is { } previewNodes)
                foreach (var previewNode in previewNodes)
                    if (previewNode.GetAttributeValue("src", null) is { } src
                        && PreviewRedditUrlRegex().Match(src) is { Success: true } m)
                    {
                        images.Add(new Uri("https://i." + m.Groups[1].Value));
                    }
        }
        else
        {
            using (var commentsPageStream = await App.HttpClient.GetStreamAsync(commentsPageUri))
                doc.Load(commentsPageStream);

            foreach (var previewNode in doc.DocumentNode.SelectNodes(@"//figure/img"))
                if (previewNode.GetAttributeValue("data-lazy-srcset", null) is { } previewPaths)
                    if (PreviewRedditUrlRegex().Matches(previewPaths!) is [.., { Success: true } m])
                        images.Add(new Uri("https://i." + m.Groups[1].Value));
        }

        foreach (var image in images)
        {
            var img = (imageDetailsGenerator ?? (() => new ImageDetails(mainViewModel)))();
            img.IsCompleted = localSettingsService.IsImageCompleted(image);
            img.Link = image;
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
