namespace ED2.Sources;

abstract class BaseSource
{
    protected readonly ILocalSettingsService localSettingsService;
    protected BaseSource(ILocalSettingsService localSettingsService)
    {
        this.localSettingsService = localSettingsService;
    }

    public static (int scaledWidth, int scaledHeight) GetScaledSize(int width, int height)
    {
        const int maxScaledSize = 800;
        const double scaleFactor = 1 / 3d;

        if (width < maxScaledSize / scaleFactor && height < maxScaledSize / scaleFactor)
            return ((int)Math.Round(width * scaleFactor), (int)Math.Round(height * scaleFactor));

        return width > height
            ? (maxScaledSize, (int)((double)maxScaledSize / width * height))
            : ((int)((double)maxScaledSize / height * width), maxScaledSize);
    }

    public abstract bool CanHandle(Uri uri, [NotNullWhen(true)] out Uri? normalizedUri, out string? prefix);
    public abstract Task Load(Uri uri, DispatcherQueue mainDispatcherQueue, Func<ImageDetails>? imageDetailsGenerator = null);
    public abstract IAsyncEnumerable<ImageDetails> EnumerateImageDetails();
    public abstract Task OnSaveImage(ImageDetails imageDetails);
}
