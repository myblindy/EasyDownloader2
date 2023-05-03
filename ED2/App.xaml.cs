﻿namespace ED2;

public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if (((App)Current).Host.Services.GetService<T>() is not { } service)
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");

        return service;
    }

    public static HttpClient HttpClient { get; } = new(new HttpClientHandler()
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        CookieContainer = new(),
        AllowAutoRedirect = true,
        //ServerCertificateCustomValidationCallback = (a, b, c, d) => true,
    })
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/112.0" }
        }
    };

    public static WindowEx MainWindow { get; } = new MainWindow();

    public App()
    {
        InitializeComponent();

#if DEBUG
        if (Debugger.IsAttached)
        {
            DebugSettings.IsTextPerformanceVisualizationEnabled = true;
            //DebugSettings.EnableFrameRateCounter = true;
            //DebugSettings.FailFastOnErrors = true;
        }
#endif

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                // Default Activation Handler
                services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

                // Other Activation Handlers

                // Services
                services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<ITwitterService, TwitterService>();
                services.AddSingleton<IRedditService, RedditService>();
                services.AddSingleton<IJumpListService, JumpListService>();

                // Core Services
                services.AddSingleton<IFileService, FileService>();

                // Views and ViewModels
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainPage>();
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<ShellPage>();

                // Sources
                services.AddTransient<TwitterSource>();
                services.AddTransient<RedditSource>();
                services.AddTransient<DirectImageSource>();
                services.AddTransient<ImgurSource>();
                services.AddTransient<RedditGallerySource>();
                services.AddTransient<TistorySource>();
                services.AddTransient<LocalSource>();

                // Configuration
                services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
            })
            .Build();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        var kind = args.UWPLaunchActivatedEventArgs.Kind;
        var currentInstance = AppInstance.GetCurrent();

        if (currentInstance != null)
        {
            var activationArgs = currentInstance.GetActivatedEventArgs();
            if (activationArgs != null)
            {
                var extendedKind = activationArgs.Kind;
            }
        }

        await GetService<IActivationService>().ActivateAsync(args);
    }
}
