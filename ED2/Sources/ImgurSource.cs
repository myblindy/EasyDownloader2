namespace ED2.Sources;

partial class ImgurSource : BaseSource
{
    Func<ImageDetails>? imageDetailsGenerator;
    string? albumName;

    public ImgurSource(ILocalSettingsService localSettingsService) : base(localSettingsService)
    {
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
        albumName = UriRegex().Match(uri.ToString()).Groups[2].Value;
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.imgur.com/3/album/{albumName}/images");
        request.Headers.Add("Authorization", "Client-ID 51db2131e3d1f7f");
        var response = await App.HttpClient.SendAsync(request);
        var resultjson = await response.Content.ReadAsStringAsync();

        if (JObject.Parse(resultjson) is { } jsonObj && jsonObj["data"] is { } photoData)
            foreach (var w in photoData)
                if (w["link"]?.Value<string>() is { } link && Uri.TryCreate(link, UriKind.Absolute, out var uri))
                {
                    if (Path.GetExtension(link) is ".mp4" or ".gif") continue;     // ignore video links

                    var img = (imageDetailsGenerator ?? (() => new ImageDetails()))();
                    img.Completed = localSettingsService.IsImageCompleted(uri);
                    img.Link = uri;

                    if (w["width"]?.Value<int>() is { } width)
                        img.OriginalWidth = width;
                    if (w["height"]?.Value<int>() is { } height)
                        img.OriginalHeight = height;

                    yield return img;
                }
    }

    public override Task OnSaveImage(ImageDetails imageDetails) => Task.CompletedTask;

    [GeneratedRegex(@"^(?:https?:\/\/)?(imgur\.com\/(?:a|gallery)\/([a-zA-Z0-9]+))\/?$")]
    private static partial Regex UriRegex();
}
