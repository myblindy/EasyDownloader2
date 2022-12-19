namespace ED2.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    public ILocalSettingsService LocalSettingsService { get; }
    readonly IDialogService dialogService;

    static readonly Dictionary<ImageQuality, int> minimumPixelsPerImageQuality = typeof(ImageQuality).GetFields()
        .Where(f => f.IsLiteral)
        .GroupBy(f => (ImageQuality)f.GetValue(null)!)
        .ToDictionary(w => w.Key, w => w.First().GetCustomAttribute<ValueAttribute<int>>()!.Value);

    public MainViewModel(IDialogService dialogService, ILocalSettingsService localSettingsService)
    {
        this.dialogService = dialogService;
        LocalSettingsService = localSettingsService;

        ImageQualities = minimumPixelsPerImageQuality.Keys.ToArray();

        Images.ObserveFilterProperty(nameof(ImageDetails.Completed));
        Images.ObserveFilterProperty(nameof(ImageDetails.OriginalWidth));
        Images.ObserveFilterProperty(nameof(ImageDetails.OriginalHeight));

        Images.Filter = _image => _image is ImageDetails imageDetails
            && imageDetails.OriginalWidth > 0 && imageDetails.OriginalHeight > 0
            && !imageDetails.Completed
            && imageDetails.OriginalWidth * imageDetails.OriginalHeight >= minimumPixelsPerImageQuality[RequestedImageQuality]
            && (!imageDetails.IsHorizontal || ShowHorizontal && imageDetails.IsHorizontal)
            && (!imageDetails.IsVertical || ShowVertical && imageDetails.IsVertical)
            && (!imageDetails.IsSquare || ShowSquare && imageDetails.IsSquare);

        Task.WhenAll(
            localSettingsService.ReadSettingAsync(nameof(ShowHorizontal), true),
            localSettingsService.ReadSettingAsync(nameof(ShowVertical), true),
            localSettingsService.ReadSettingAsync(nameof(ShowSquare), true))
            .ContinueWith(async t =>
            {
                var values = await t;
                (ShowHorizontal, ShowVertical, ShowSquare) = (values[0], values[1], values[2]);
            }, TaskContinuationOptions.ExecuteSynchronously);

        Task.WhenAll(
            localSettingsService.ReadSettingAsync<string>(nameof(HorizontalSaveFolder)),
            localSettingsService.ReadSettingAsync<string>(nameof(VerticalSaveFolder)),
            localSettingsService.ReadSettingAsync<string>(nameof(SquareSaveFolder)))
            .ContinueWith(async t =>
            {
                var values = await t;
                (HorizontalSaveFolder, VerticalSaveFolder, SquareSaveFolder) = (values[0], values[1], values[2]);
            }, TaskContinuationOptions.ExecuteSynchronously);

        localSettingsService.ReadSettingAsync(nameof(RequestedImageQuality), ImageQuality.HD)
            .ContinueWith(async t => RequestedImageQuality = await t, TaskContinuationOptions.ExecuteSynchronously);
    }

    [ObservableProperty]
    int minimumPixelsRequested;

    [ObservableProperty]
    ImageQuality requestedImageQuality;
    partial void OnRequestedImageQualityChanged(ImageQuality value)
    {
        Images.RefreshFilter();
        MinimumPixelsRequested = minimumPixelsPerImageQuality[value];
    }

    public ImageQuality[] ImageQualities { get; }

    [ObservableProperty]
    bool showHorizontal = true, showVertical = true, showSquare = true;
    partial void OnShowHorizontalChanged(bool value) { LocalSettingsService.SaveSettingAsync(nameof(ShowHorizontal), value); Images.RefreshFilter(); }
    partial void OnShowVerticalChanged(bool value) { LocalSettingsService.SaveSettingAsync(nameof(ShowVertical), value); Images.RefreshFilter(); }
    partial void OnShowSquareChanged(bool value) { LocalSettingsService.SaveSettingAsync(nameof(ShowSquare), value); Images.RefreshFilter(); }

    [ObservableProperty]
    string? horizontalSaveFolder, verticalSaveFolder, squareSaveFolder;

    partial void OnHorizontalSaveFolderChanged(string? value) => LocalSettingsService.SaveSettingAsync(nameof(HorizontalSaveFolder), value);
    partial void OnVerticalSaveFolderChanged(string? value) => LocalSettingsService.SaveSettingAsync(nameof(VerticalSaveFolder), value);
    partial void OnSquareSaveFolderChanged(string? value) => LocalSettingsService.SaveSettingAsync(nameof(SquareSaveFolder), value);

    [ObservableProperty]
    bool isOpening = true;

    [ObservableProperty]
    Uri? currentUri, currentNormalizedUri;
    string? currentPrefix;

    public MB.CommunityToolkit.WinUI.UI.AdvancedCollectionView Images { get; } = new(new ObservableCollection<ImageDetails>(), true);

    CancellationTokenSource cancellationTokenSource = new();
    IAsyncEnumerator<ImageDetails>? currentSourceEnumerator;
    BaseSource? currentSource;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand), nameof(LoadNextPageCommand), nameof(CompleteAllImagesCommand), nameof(CompleteAllImagesAndLoadNextPageCommand))]
    bool loadingIsDone = true;

    [RelayCommand(CanExecute = nameof(LoadingIsDone))]
    async Task OpenAsync(Uri uri)
    {
        Images.Clear();
        CurrentUri = uri;
        IsOpening = false;

        foreach (var source in new BaseSource[]
        {
            App.GetService<DirectImageSource>(),
            App.GetService<TwitterSource>(),
            App.GetService<RedditSource>(),
            App.GetService<ImgurSource>(),
            App.GetService<RedditGallerySource>(),
        })
        {
            if (source.CanHandle(uri, out var normalizedUri, out var currentPrefix))
            {
                CurrentNormalizedUri = normalizedUri;
                this.currentPrefix = currentPrefix;

                cancellationTokenSource.Cancel();
                cancellationTokenSource = new();

                currentSource = source;

                try
                {
                    LoadingIsDone = false;
                    await source.LoadAsync(uri, DispatcherQueue.GetForCurrentThread());
                }
                finally { LoadingIsDone = true; }

                currentSourceEnumerator = source.EnumerateImageDetails().GetAsyncEnumerator(cancellationTokenSource.Token);
                await LoadNextPageAsync();

                return;
            }
        }

        CurrentNormalizedUri = null;
        await dialogService.ShowErrorAsync("Unable to find a loader for the given URI.");
    }

    [RelayCommand(CanExecute = nameof(LoadingIsDone))]
    async Task LoadNextPageAsync()
    {
        try
        {
            LoadingIsDone = false;

            const int MaxImagesPerPage = 50;
            Images.Clear();

            var ct = cancellationTokenSource.Token;

            if (currentSourceEnumerator is not null)
                for (int i = 0; i < MaxImagesPerPage; ++i)
                    if (ct.IsCancellationRequested || !await currentSourceEnumerator.MoveNextAsync())
                        break;
                    else
                        Images.Add(currentSourceEnumerator.Current);
        }
        finally { LoadingIsDone = true; }
    }

    [RelayCommand(CanExecute = nameof(LoadingIsDone))]
    void CompleteAllImages()
    {
        foreach (ImageDetails image in Images.ToList())
            image.Completed = true;
    }

    [RelayCommand(CanExecute = nameof(LoadingIsDone))]
    async Task CompleteAllImagesAndLoadNextPageAsync()
    {
        CompleteAllImages();
        await LoadNextPageAsync();
    }

    [RelayCommand]
    async Task SaveImage(ImageDetails image)
    {
        var path = image.IsVertical ? VerticalSaveFolder
            : image.IsHorizontal ? HorizontalSaveFolder
            : image.IsSquare ? SquareSaveFolder : null;

        if (path is null || image.Link is null)
            return;

        if (image.Link.IsFile)
            throw new NotImplementedException();
        var extension = SavePathRegex().Match(image.Link.LocalPath) is { Success: true } m ? m.Groups[1].Value
            : throw new NotImplementedException();
        var localFileName = Path.Combine(path, $"{(currentPrefix is null ? null : $"{currentPrefix}-")}{Guid.NewGuid()}-{Guid.NewGuid()}.{extension}");

        image.Completed = true;

        async Task download()
        {
            using var srcStream = await App.HttpClient.GetStreamAsync(image.Link);
            using var dstStream = File.Create(localFileName!);
            await srcStream.CopyToAsync(dstStream);
        }

        await Task.WhenAll(download(), currentSource is null ? Task.CompletedTask : currentSource.OnSaveImage(image));
    }

    [GeneratedRegex("/[^/:]+?(?:\\.([^:]+?))(?::.*?)?$")]
    private static partial Regex SavePathRegex();
}
