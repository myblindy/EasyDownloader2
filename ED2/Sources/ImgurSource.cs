namespace ED2.Sources;

partial class ImgurSource : BaseSource
{
    readonly MainViewModel mainViewModel;
    private readonly ImgurService imgurService;
    Func<ImageDetails>? imageDetailsGenerator;
    string? albumName;

    public ImgurSource(MainViewModel mainViewModel, ImgurService imgurService, ILocalSettingsService localSettingsService) : base(localSettingsService)
    {
        this.mainViewModel = mainViewModel;
        this.imgurService = imgurService;
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
        await foreach (var (uri, width, height) in imgurService.EnumerateAlbumImages(albumName!))
        {
            if (Path.GetExtension(uri.AbsolutePath) is ".mp4" or ".gif") continue;     // ignore video links

            var img = (imageDetailsGenerator ?? (() => new ImageDetails(mainViewModel)))();
            img.IsCompleted = localSettingsService.IsImageCompleted(uri);
            img.Link = uri;

            if (width > 0)
                img.OriginalWidth = width;
            if (height > 0)
                img.OriginalHeight = height;

            yield return img;
        }
    }

    public override Task OnSaveImage(ImageDetails imageDetails) => Task.CompletedTask;

    [GeneratedRegex(@"^(?:https?:\/\/)?(imgur\.com\/(?:a|gallery)\/([a-zA-Z0-9]+))\/?$")]
    private static partial Regex UriRegex();
}
