namespace ED2.Models;

public enum ImageQuality
{
    [Value<int>(0)]
    Any,
    [Value<int>(1279 * 719)]
    SD,
    [Value<int>(1489 * 989)]
    HD,
    [Value<int>(3840 * 2160)]
    UHD
}

public partial class ImageDetails : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScaledWidth))]
    int originalWidth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScaledHeight))]
    int originalHeight;

    public int ScaledWidth => Math.Max(1, (int)(OriginalWidth * BaseSource.ScalingFactor));
    public int ScaledHeight => Math.Max(1, (int)(OriginalHeight * BaseSource.ScalingFactor));

    public bool IsVertical => OriginalHeight > 0 && (double)OriginalWidth / OriginalHeight <= .8;
    public bool IsHorizontal => OriginalHeight > 0 && (double)OriginalWidth / OriginalHeight >= 1.2;
    public bool IsSquare => OriginalHeight > 0 && !IsVertical && !IsHorizontal;

    [ObservableProperty]
    Uri? link;

    [ObservableProperty]
    string? title;

    [ObservableProperty]
    DateTime? datePosted;

    [ObservableProperty]
    bool completed;

    partial void OnCompletedChanged(bool value)
    {
        if (value && Link is not null)
            App.GetService<ILocalSettingsService>().SetImageCompleted(Link);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageSource))]
    bool loading, loaded;

    ImageSource? imageSource;
    public ImageSource? ImageSource
    {
        get
        {
            if (imageSource is not null && Loaded)
                return imageSource;

            if (!Loading && !Loaded)
            {
                Loading = true;

                // load code
                var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                _ = Task.Run(async () =>
                {
                    var rawBytes = await App.HttpClient.GetByteArrayAsync(Link);

                    using var tempStream = new MemoryStream();
                    tempStream.Write(rawBytes);
                    tempStream.Position = 0;

                    using var inputStream = tempStream.AsRandomAccessStream();
                    var decoder = await BitmapDecoder.CreateAsync(inputStream);
                    var (originalWidth, originalHeight) = ((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    var (scaledWidth, scaledHeight) = ((uint)(originalWidth * BaseSource.ScalingFactor), (uint)(originalHeight * BaseSource.ScalingFactor));

                    inputStream.Seek(0);
                    var pixelData = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                        new BitmapTransform
                        {
                            ScaledWidth = scaledWidth,
                            ScaledHeight = scaledHeight,
                        }, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
                    var sourceDecodedPixels = pixelData.DetachPixelData();

                    dispatcherQueue.TryEnqueue(() =>
                    {
                        var output = new WriteableBitmap((int)scaledWidth, (int)scaledHeight);
                        sourceDecodedPixels.AsBuffer().CopyTo(output.PixelBuffer);

                        (OriginalWidth, OriginalHeight) = (originalWidth, originalHeight);
                        ImageSource = output;
                        Loading = false;
                        Loaded = true;
                    });
                });
            }

            return null;
        }
        set => SetProperty(ref imageSource, value);
    }
}
