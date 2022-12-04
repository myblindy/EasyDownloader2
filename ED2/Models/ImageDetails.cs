using CommunityToolkit.Mvvm.ComponentModel;
using ED2.Contracts.Services;
using ED2.Helpers;
using ED2.Services;
using ED2.Sources;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Core;

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

    public int ScaledWidth => (int)(OriginalWidth * BaseSource.ScalingFactor);
    public int ScaledHeight => (int)(OriginalHeight * BaseSource.ScalingFactor);

    public bool IsVertical => (double)OriginalWidth / OriginalHeight <= .8;
    public bool IsHorizontal => (double)OriginalWidth / OriginalHeight >= 1.2;
    public bool IsSquare => !IsVertical && !IsHorizontal;

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

    static readonly ImageSource LoadingImageSource = new BitmapImage(new Uri("ms-appx:///Assets/loading.png"));

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
                    var (scaledWidth, scaledHeight) = ((uint)(decoder.PixelWidth * BaseSource.ScalingFactor), (uint)(decoder.PixelHeight * BaseSource.ScalingFactor));

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
