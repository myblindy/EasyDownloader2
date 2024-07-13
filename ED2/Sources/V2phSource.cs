using HtmlAgilityPack;
using System.Globalization;
using System.Web;

namespace ED2.Sources;

partial class V2phSource(MainViewModel mainViewModel, ILocalSettingsService localSettingsService)
    : BaseSource(localSettingsService)
{
    Func<ImageDetails>? imageDetailsGenerator;
    Uri? uri;

    public override bool CanHandle(Uri uri, [NotNullWhen(true)] out Uri? normalizedUri, out string? prefix)
    {
        prefix = null;

        if (UriRegex().Match(uri.ToString()) is { Success: true } m)
        {
            normalizedUri = new($"https://album/{m.Groups[1].Value}/");
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
        var pageUri = uri;

        for (int page = 1; ; ++page)
        {
            try
            {
                using var pageStream = await App.HttpClient.GetStreamAsync(pageUri);
                doc.Load(pageStream);
            }
            catch { break; }

            foreach (var imgNode in doc.DocumentNode.SelectNodes(
                "//div[contains(@class, 'photos-list')]/div[contains(@class, 'album-photo')]/img"))
            {
                if (imgNode.Attributes["data-src"]?.Value is not { } src) continue;

                var result = (imageDetailsGenerator ?? (() => new ImageDetails(mainViewModel)))();
                result.Link = new(src);
                result.IsCompleted = localSettingsService.IsImageCompleted(result.Link);
                yield return result;
            }

            var uriBuilder = new UriBuilder(uri!);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["page"] = (page + 1).ToString(CultureInfo.InvariantCulture);
            uriBuilder.Query = query.ToString();
            pageUri = uriBuilder.Uri;
        }
    }

    public override Task OnSaveImage(ImageDetails imageDetails) => Task.CompletedTask;

    [GeneratedRegex(@"^(?:https?:\/\/)?(?:www\.)?(v2ph\.com\/album\/[^?/]+)")]
    private static partial Regex UriRegex();
}
