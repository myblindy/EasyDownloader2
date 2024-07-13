namespace ED2.Sources;

partial class TwitterScraperSource(MainViewModel mainViewModel, ILocalSettingsService localSettingsService, TwitterScraperService twitterService) : BaseSource(localSettingsService)
{
    string? username;

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

    public override Task LoadAsync(Uri uri, DispatcherQueue mainDispatcherQueue, Func<ImageDetails>? imageDetailsGenerator = null)
    {
        username = UriRegex().Match(uri.ToString()).Groups[2].Value;
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        await foreach (var uri in twitterService.EnumerateMediaAsync(new($"https://x.com/{username}")))
        {
            yield return new ImageDetails(mainViewModel)
            {
                IsCompleted = localSettingsService.IsImageCompleted(uri),
                Link = uri,
            };
        }
    }

    public override Task OnSaveImage(ImageDetails imageDetails)
    {
        return Task.CompletedTask;
    }

    [GeneratedRegex(@"^(?:https?:\/\/)?(?:mobile\.)?((?:twitter|x)\.com\/([^/?]+))")]
    private static partial Regex UriRegex();
}