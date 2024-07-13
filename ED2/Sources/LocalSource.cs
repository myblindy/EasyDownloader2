namespace ED2.Sources;

class LocalSource(MainViewModel mainViewModel, ILocalSettingsService localSettingsService) : BaseSource(localSettingsService)
{
    DispatcherQueue? mainDispatcherQueue;
    string? path;

    public override bool CanHandle(Uri uri, [NotNullWhen(true)] out Uri? normalizedUri, out string? prefix)
    {
        try
        {
            var path = Path.GetFullPath(uri.LocalPath);
            if (Directory.Exists(path))
            {
                normalizedUri = new(path);
                prefix = Path.GetFileName(path);
                return true;
            }

        }
        catch { }

        prefix = null;
        normalizedUri = null;
        return false;
    }

    public override Task LoadAsync(Uri uri, DispatcherQueue mainDispatcherQueue, Func<ImageDetails>? imageDetailsGenerator = null)
    {
        path = uri.LocalPath;
        this.mainDispatcherQueue = mainDispatcherQueue;

        return Task.CompletedTask;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        foreach (var item in Directory.EnumerateFiles(path!, "*", SearchOption.AllDirectories))
            if (Path.GetExtension(item) is { } extension
                && (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)))
            {
                yield return new ImageDetails(mainViewModel)
                {
                    IsCompleted = localSettingsService.IsImageCompleted(new(item)),
                    Link = new(item),
                    Title = Path.GetFileNameWithoutExtension(item)
                };
            }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    public override Task OnSaveImage(ImageDetails imageDetails) => Task.CompletedTask;
}
