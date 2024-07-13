using HtmlAgilityPack;
using System.Globalization;
using System.Text.Json;

namespace ED2.Sources;
sealed partial class KpoppingSource(MainViewModel mainViewModel, ILocalSettingsService localSettingsService) : BaseSource(localSettingsService)
{
    Func<ImageDetails>? imageDetailsGenerator;
    string? prefix;
    Uri? uri;

    public override bool CanHandle(Uri uri, [NotNullWhen(true)] out Uri? normalizedUri, [NotNullWhen(true)] out string? prefix)
    {
        if (UriRegex().Match(uri.ToString()) is { Success: true } m)
        {
            normalizedUri = new($"https://kpopping.com/profiles/idol/{m.Groups[1].Value}");
            this.prefix = prefix = m.Groups[1].Value;
            return true;
        }

        normalizedUri = null;
        prefix = null;
        return false;
    }

    public override Task LoadAsync(Uri uri, DispatcherQueue mainDispatcherQueue, Func<ImageDetails>? imageDetailsGenerator = null)
    {
        this.uri = uri;
        this.imageDetailsGenerator = imageDetailsGenerator;
        return Task.CompletedTask;
    }

    record AlbumPageEntry(string? Content);

    JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        for (int albumPage = 0; ; ++albumPage)
        {
            // request the json page
            var albumPageUri = new Uri($"https://kpopping.com/profiles/idol/{prefix}/latest-pictures/{albumPage + 1}");
            using var albumPageResponse = await App.HttpClient.PostAsync(albumPageUri, null);
            if (!albumPageResponse.IsSuccessStatusCode)
                break;

            var albumPageContent = await albumPageResponse.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(albumPageContent) || JsonSerializer.Deserialize<AlbumPageEntry>(albumPageContent, jsonSerializerOptions) is not { } albumPageEntry)
                break;

            // the content property is an html string
            var albumPageDoc = new HtmlDocument();
            albumPageDoc.LoadHtml(albumPageEntry!.Content);

            foreach (var (photoAlbumUrl, photoAlbumName) in albumPageDoc.DocumentNode.SelectNodes(@"//a[@href]")
                .Select(w => (url: w.Attributes["href"]?.Value, name: w.SelectSingleNode(@".//span")?.InnerText))
                .Where(w => !string.IsNullOrWhiteSpace(w.url)))
            {
                using var photoAlbumResponse = await App.HttpClient.GetAsync(
                    new Uri(new Uri("https://kpopping.com/"), photoAlbumUrl));
                if (!photoAlbumResponse.IsSuccessStatusCode)
                    continue;

                var photoAlbumContent = await photoAlbumResponse.Content.ReadAsStringAsync();
                var photoAlbumDoc = new HtmlDocument();
                photoAlbumDoc.LoadHtml(photoAlbumContent);

                foreach (var photoUrl in photoAlbumDoc.DocumentNode.SelectNodes(@"//div[contains(@class, 'justified-gallery')]/a[@href]")
                    .Select(w => w.Attributes["href"]?.Value).Where(w => !string.IsNullOrWhiteSpace(w)))
                {
                    var photoUri = new Uri(new Uri("https://kpopping.com/"), photoUrl);
                    var img = (imageDetailsGenerator ?? (() => new ImageDetails(mainViewModel)))();
                    img.IsCompleted = localSettingsService.IsImageCompleted(photoUri);
                    img.Link = photoUri;
                    img.Title = WebUtility.HtmlDecode(photoAlbumName);
                    img.DatePosted = Regex.Match(photoAlbumUrl!, @"^\/kpics\/(\d{6})\b") is { Success: true } m
                        ? DateTime.TryParseExact(m.Groups[1].Value, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var datePosted)
                        ? datePosted : null : null;
                    yield return img;
                }
            }
        }
    }

    public override Task OnSaveImage(ImageDetails imageDetails) => Task.CompletedTask;

    [GeneratedRegex(@"^(?:https?:\/\/)?(?:www\.)?kpopping\.com\/profiles\/idol\/([^\/]+)$")]
    private static partial Regex UriRegex();
}
