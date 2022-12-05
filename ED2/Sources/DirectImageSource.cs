using ED2.Contracts.Services;
using ED2.Models;
using Microsoft.UI.Dispatching;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace ED2.Sources;

partial class DirectImageSource : BaseSource
{
    public DirectImageSource(ILocalSettingsService localSettingsService) : base(localSettingsService)
    {
    }

    public override bool CanHandle(Uri uri, [NotNullWhen(true)] out Uri? normalizedUri, [NotNullWhen(true)] out string? prefix)
    {
        normalizedUri = null;
        prefix = null;
        return UriRegex().IsMatch(uri.ToString());
    }

    Uri? uri;
    Func<ImageDetails>? imageDetailsGenerator;
    public override Task Load(Uri uri, DispatcherQueue mainDispatcherQueue, Func<ImageDetails>? imageDetailsGenerator = null)
    {
        this.uri = uri;
        this.imageDetailsGenerator = imageDetailsGenerator;
        return Task.CompletedTask;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        if (uri is not null)
        {
            var result = (imageDetailsGenerator ?? (static () => new ImageDetails()))();
            result.Link = uri;
            result.Completed = localSettingsService.IsImageCompleted(uri);
            yield return result;
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    public override Task OnSaveImage(ImageDetails imageDetails) => Task.CompletedTask;

    [GeneratedRegex(@"\.(?:jpe?g|gif|png|bmp|webp)$", RegexOptions.IgnoreCase)]
    private static partial Regex UriRegex();
}
