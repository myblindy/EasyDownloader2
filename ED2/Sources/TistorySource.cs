using HtmlAgilityPack;

namespace ED2.Sources;

partial class TistorySource : BaseSource
{
    readonly MainViewModel mainViewModel;
    Uri? uri;

    public TistorySource(MainViewModel mainViewModel, ILocalSettingsService localSettingsService) : base(localSettingsService)
    {
        this.mainViewModel = mainViewModel;
    }

    public override bool CanHandle(Uri uri, [NotNullWhen(true)] out Uri? normalizedUri, out string? prefix)
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

    public override Task LoadAsync(Uri uri, DispatcherQueue mainDispatcherQueue, Func<ImageDetails>? imageDetailsGenerator = null)
    {
        this.uri = new("https://" + UriRegex().Match(uri.ToString()).Groups[1].Value);
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        if (uri is null) yield break;

        for (int page = 1; ; ++page)
        {
            var uri = new Uri(this.uri, $"/?page={page}");

            var doc = new HtmlDocument();
            using (var stream = await App.HttpClient.GetStreamAsync(uri).ConfigureAwait(true))
                doc.Load(stream);

            var postTasks = new List<Task<List<(string name, DateTime? date, int width, int height, Uri uri)>>>();

            void AddImageCrawlingTask(string postName, DateTime? date, Uri postUri) =>
                postTasks.Add(Task.Run(async () =>
                {
                    var postDoc = new HtmlDocument();
                    using (var postStream = await App.HttpClient.GetStreamAsync(postUri).ConfigureAwait(false))
                        postDoc.Load(postStream);

                    var images = new List<(string name, DateTime? date, int width, int height, Uri uri)>();
                    if (postDoc.DocumentNode.SelectNodes("//figure[contains(@class, 'imageblock')]") is { } imageNodes)
                        foreach (var imageNode in imageNodes)
                        {
                            if (imageNode.Attributes["data-origin-width"].Value is not { } widthString || !int.TryParse(widthString, out var width))
                                width = 0;
                            if (imageNode.Attributes["data-origin-height"].Value is not { } heightString || !int.TryParse(heightString, out var height))
                                height = 0;
                            if (imageNode.SelectSingleNode(".//img[@src]")?.Attributes["src"].Value is { } url)
                                images.Add((postName, date, width, height, new Uri(url)));
                        }

                    return images;
                }));

            if (doc.DocumentNode.SelectNodes(@"//li[contains(@class, 'item_category')]") is { } itemCategoryNode)
            {
                foreach (var pageNode in itemCategoryNode)
                    if (pageNode.SelectNodes(".//a[contains(@class, 'link_category')]") is [{ } firstChildNode]
                        && firstChildNode.SelectNodes(".//div[contains(@class, 'info')]//*[contains(@class, 'name')]") is [{ } nameNode]
                        && firstChildNode.SelectNodes(".//div[contains(@class, 'info')]//*[contains(@class, 'date')]") is [{ } dateNode])
                    {
                        AddImageCrawlingTask(nameNode.InnerText, DateTime.Parse(dateNode.InnerText),
                            new Uri(uri, firstChildNode.Attributes["href"].Value));
                    }
            }
            else if (doc.DocumentNode.SelectNodes(@"//li[contains(@class, 'item_post')]") is { } itemPostNode)
            {
                foreach (var pageNode in itemPostNode)
                    if (pageNode.SelectSingleNode(@".//a[contains(@class, 'link_post')]") is { } linkPostNode
                        && pageNode.SelectSingleNode(@".//strong[contains(@class, 'name')]") is { } nameNode)
                    {
                        AddImageCrawlingTask(nameNode.InnerText, null,
                            new Uri(uri, linkPostNode.Attributes["href"].Value));
                    }
            }
            else if (doc.DocumentNode.SelectNodes(@"//div[contains(@class, 'area-main')]/div[contains(@class, 'area-common')]/article[contains(@class, 'article-type-common')]") is { } articleNodes)
            {
                foreach (var articleNode in articleNodes)
                    if (articleNode.SelectSingleNode(@"./a[contains(@class, 'link-article')]") is { } linkArticleNode
                        && articleNode.SelectSingleNode(@"./div[contains(@class, 'article-content')]/a[contains(@class, 'link-article')]/strong[contains(@class, 'title')]") is { } titleNode
                        && articleNode.SelectSingleNode(@"./div[contains(@class, 'article-content')]/div[contains(@class, 'box-meta')]/span[contains(@class, 'date')]") is { } dateNode)
                    {
                        AddImageCrawlingTask(titleNode.InnerText, DateTime.TryParse(dateNode.InnerText, out var date) ? date : null,
                            new Uri(uri, linkArticleNode.Attributes["href"].Value));
                    }
            }
            else if (doc.DocumentNode.SelectNodes(@"//article[@id = 'content']/div[contains(@class, 'inner')]/div[contains(@class, 'post-item')]") is { } postNodes)
                foreach (var postNode in postNodes)
                    if (postNode.SelectSingleNode(@"./a[@href]") is { } postLinkNode
                        && postLinkNode.SelectSingleNode(@"./span[contains(@class, 'title')]") is { } postTitleNode)
                    {
                        AddImageCrawlingTask(postTitleNode.InnerText, null, new Uri(uri, postLinkNode.Attributes["href"].Value));
                    }

            // we're done when we find no more posts
            if (postTasks.Count == 0)
                yield break;

            foreach (var post in await Task.WhenAll(postTasks))
                foreach (var image in post)
                    if (!image.uri.AbsolutePath.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase))
                        yield return new ImageDetails(mainViewModel)
                        {
                            IsCompleted = localSettingsService.IsImageCompleted(image.uri),
                            Title = image.name,
                            DatePosted = image.date,
                            OriginalWidth = image.width,
                            OriginalHeight = image.height,
                            Link = image.uri,
                        };
        }
    }

    public override Task OnSaveImage(ImageDetails imageDetails) => Task.CompletedTask;

    [GeneratedRegex(@"^(?:https?:\/\/)?(([^.]+)\.tistory\.com)")]
    private static partial Regex UriRegex();
}
