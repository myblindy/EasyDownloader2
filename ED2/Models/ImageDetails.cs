namespace ED2.Models;

public enum ImageQuality
{
    [Value<int>(1)]
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
    public MainViewModel MainViewModel { get; }

    public ImageDetails(MainViewModel mainViewModel) =>
        MainViewModel = mainViewModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScaledWidth))]
    int originalWidth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScaledHeight))]
    int originalHeight;

    public int ScaledWidth => BaseSource.GetScaledSize(OriginalWidth, OriginalHeight).scaledWidth;
    public int ScaledHeight => BaseSource.GetScaledSize(OriginalHeight, OriginalHeight).scaledHeight;

    public bool IsVertical => (double)OriginalWidth / OriginalHeight <= .8;
    public bool IsHorizontal => (double)OriginalWidth / OriginalHeight >= 1.2;
    public bool IsSquare => !IsVertical && !IsHorizontal;

    [ObservableProperty]
    Uri? link;

    partial void OnLinkChanged(Uri? value)
    {
        if (value is null || IsCompleted) return;

        MainViewModel.MainDispatcherQueue.TryEnqueue(() => ++MainViewModel.LoadingImages);

        _ = Task.Run(async () =>
        {
            var rawBytes = value!.IsLoopback ? File.ReadAllBytes(value.LocalPath) : await App.HttpClient.GetByteArrayAsync(value);

            using var tempStream = new MemoryStream();
            tempStream.Write(rawBytes);
            tempStream.Position = 0;

            using var inputStream = tempStream.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(inputStream);
            var (originalWidth, originalHeight) = ((int)decoder.PixelWidth, (int)decoder.PixelHeight);
            var (scaledWidth, scaledHeight) = BaseSource.GetScaledSize(originalWidth, originalHeight);

            inputStream.Seek(0);
            var pixelData = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                new BitmapTransform
                {
                    ScaledWidth = (uint)scaledWidth,
                    ScaledHeight = (uint)scaledHeight,
                }, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
            var sourceDecodedPixels = pixelData.DetachPixelData();

            MainViewModel.MainDispatcherQueue.TryEnqueue(() =>
            {
                var output = new WriteableBitmap((int)scaledWidth, (int)scaledHeight);
                sourceDecodedPixels.AsBuffer().CopyTo(output.PixelBuffer);

                (OriginalWidth, OriginalHeight) = (originalWidth, originalHeight);

                --MainViewModel.LoadingImages;
                ++MainViewModel.LoadedImages;

                ImageSource = output;
                Loaded = true;
            });
        });
    }

    [ObservableProperty]
    string? flair, title;

    [ObservableProperty]
    DateTime? datePosted;

    [ObservableProperty]
    bool isCompleted;

    partial void OnIsCompletedChanged(bool value)
    {
        if (value && Link is not null)
            App.GetService<ILocalSettingsService>().SetImageCompleted(Link);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageSource))]
    bool loaded;

    [ObservableProperty]
    ImageSource? imageSource;
}
