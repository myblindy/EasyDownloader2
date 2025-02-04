using SharpCompress;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Net.Http.Headers;
using Windows.Devices.Usb;

namespace ED2.Sources;

partial class ArchiveSource(MainViewModel mainViewModel, ILocalSettingsService localSettingsService) : BaseSource(localSettingsService)
{
    public override bool CanHandle(Uri uri, [NotNullWhen(true)] out Uri? normalizedUri, [NotNullWhen(true)] out string? prefix)
    {
        normalizedUri = null;
        prefix = null;
        return UriRegex.IsMatch(uri.ToString());
    }

    Uri? uri;
    Func<ImageDetails>? imageDetailsGenerator;

    public override Task LoadAsync(Uri uri, DispatcherQueue mainDispatcherQueue, Func<ImageDetails>? imageDetailsGenerator = null)
    {
        this.uri = uri;
        this.imageDetailsGenerator = imageDetailsGenerator;
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        var fileParts = ArchiveFactory.GetFileParts(uri!.LocalPath).Select(p => new FileInfo(p)).ToArray();
        if (ArchiveFactory.Open(fileParts) is not { } archive)
            yield break;

        try
        {
            // try to get the first entry, if we get CryptographicException then we need a password
            var firstEntry = archive.Entries.FirstOrDefault();
        }
        catch (CryptographicException)
        {
            // ask for password
            archive?.Dispose();
            archive = ArchiveFactory.Open(fileParts, new() { Password = "mrcong.com" });
        }

        if (archive is null) yield break;

        using (archive)
            foreach (var entry in archive.Entries)
            {
                if (entry.IsDirectory || entry.Key is null) continue;
                if (!SupportedFileTypeRegex.IsMatch(entry.Key)) continue;

                var fullUri = new Uri(uri, entry.Key);
                var result = (imageDetailsGenerator ?? (() => new ImageDetails(mainViewModel)))();
                result.IsCompleted = localSettingsService.IsImageCompleted(fullUri);

                if (!result.IsCompleted)
                {
                    using var stream = entry.OpenEntryStream();
                    
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    ms.Position = 0;

                    var decoder = await BitmapDecoder.CreateAsync(ms.AsRandomAccessStream());

                    var (originalWidth, originalHeight) = ((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    var (scaledWidth, scaledHeight) = GetScaledSize(originalWidth, originalHeight);

                    var pixelData = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                        new BitmapTransform { ScaledWidth = (uint)scaledWidth, ScaledHeight = (uint)scaledHeight },
                        ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
                    var sourcePixelData = pixelData.DetachPixelData();

                    result.OriginalWidth = originalWidth;
                    result.OriginalHeight = originalHeight;
                    result.RawBytes = sourcePixelData;
                    result.Title = entry.Key;
                    result.DatePosted = entry.CreatedTime;

                    mainViewModel.MainDispatcherQueue.TryEnqueue(() =>
                    {
                        var output = new WriteableBitmap(scaledWidth, scaledHeight);
                        output.PixelBuffer.AsStream().Write(sourcePixelData, 0, sourcePixelData.Length);

                        result.ImageSource = output;
                    });
                }

                yield return result;
            }
    }

    public override Task OnSaveImage(ImageDetails imageDetails) => Task.CompletedTask;

    [GeneratedRegex(@"\.(?:rar|7z|zip)$", RegexOptions.IgnoreCase)]
    private static partial Regex UriRegex { get; }

    [GeneratedRegex(@"\.(?:jpe?g|gif|png|bmp|webp)$", RegexOptions.IgnoreCase)]
    private static partial Regex SupportedFileTypeRegex { get; }

}
