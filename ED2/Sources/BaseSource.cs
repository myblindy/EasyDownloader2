using ED2.Contracts.Services;
using ED2.Models;
using Microsoft.UI.Dispatching;
using System.Diagnostics.CodeAnalysis;

namespace ED2.Sources;

abstract class BaseSource
{
    protected readonly ILocalSettingsService localSettingsService;
    protected BaseSource(ILocalSettingsService localSettingsService)
    {
        this.localSettingsService = localSettingsService;
    }

    public const double ScalingFactor = 1 / 3d;

    public abstract bool CanHandle(Uri uri, [NotNullWhen(true)] out Uri? normalizedUri, out string? prefix);
    public abstract Task Load(Uri uri, DispatcherQueue mainDispatcherQueue, Func<ImageDetails>? imageDetailsGenerator = null);
    public abstract IAsyncEnumerable<ImageDetails> EnumerateImageDetails();
    public abstract Task OnSaveImage(ImageDetails imageDetails);
}
