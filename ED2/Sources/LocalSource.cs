using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ED2.Sources;

class LocalSource : BaseSource
{
    DispatcherQueue? mainDispatcherQueue;
    string? path;
    private readonly MainViewModel mainViewModel;

    public LocalSource(MainViewModel mainViewModel, ILocalSettingsService localSettingsService) : base(localSettingsService)
    {
        this.mainViewModel = mainViewModel;
    }

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

    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        foreach (var item in Directory.EnumerateFiles(path!, "*", SearchOption.AllDirectories))
            if (Path.GetExtension(item) is { } extension
                && (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)))
            {
                yield return new ImageDetails(mainViewModel)
                {
                    IsCompleted = localSettingsService.IsImageCompleted(new(item)),
                    Link = new(item),
                    Title = Path.GetFileNameWithoutExtension(item)
                };
            }
    }

    public override Task OnSaveImage(ImageDetails imageDetails) => Task.CompletedTask;
}
