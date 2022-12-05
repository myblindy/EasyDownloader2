using ED2.Contracts.Services;
using ED2.Models;
using HtmlAgilityPack;
using Microsoft.UI.Dispatching;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Display.Core;

namespace ED2.Sources;

partial class RedditGallerySource : BaseSource
{
    Func<ImageDetails>? imageDetailsGenerator;
    Uri? uri;

    public RedditGallerySource(ILocalSettingsService localSettingsService) : base(localSettingsService)
    {
    }

    public override bool CanHandle(Uri uri, [NotNullWhen(true)] out Uri? normalizedUri, out string? prefix)
    {
        prefix = null;

        if (UriRegex().Match(uri.ToString()) is { Success: true } m)
        {
            normalizedUri = new($"https://{m.Groups[1].Value}/");
            return true;
        }

        normalizedUri = null;
        return false;
    }

    public override Task Load(Uri uri, DispatcherQueue mainDispatcherQueue, Func<ImageDetails>? imageDetailsGenerator = null)
    {
        this.imageDetailsGenerator = imageDetailsGenerator;
        this.uri = uri;
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        var doc = new HtmlDocument();
        using (var pageStream = await App.HttpClient.GetStreamAsync(uri))
            doc.Load(pageStream);

        const string jsonVarName = "window.___r";
        var json = JObject.Parse(WindowJsVarAssignmentRegex().Replace(doc.DocumentNode.SelectSingleNode($"//script[contains(.,'{jsonVarName}')]").InnerText, ""));

        if (json["posts"]?["models"]?.Values()?.First()?["media"]?["mediaMetadata"] is { } mediaMetaData)
            foreach (var previewPath in mediaMetaData.Select(w => w.First()["s"]?["u"]?.Value<string>()).Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                if (PreviewRedditUrlRegex().Match(previewPath!) is not { Success: true } m
                    || !m.Groups[1].Success)
                {
                    continue;
                }

                var link = new Uri("https://i." + m.Groups[1].Value);

                var img = (imageDetailsGenerator ?? (() => new ImageDetails()))();
                img.Completed = localSettingsService.IsImageCompleted(link);
                img.Link = link;
                yield return img;
            }
    }

    public override Task OnSaveImage(ImageDetails imageDetails) => Task.CompletedTask;

    [GeneratedRegex(@"^(?:https?:\/\/)?(?:www\.)?(reddit\.com\/gallery\/[^/]+)$")]
    private static partial Regex UriRegex();
    [GeneratedRegex("window.___r\\s*=\\s*")]
    private static partial Regex WindowJsVarAssignmentRegex();
    [GeneratedRegex("^https?://preview\\.(redd\\.it/[^.]+\\.\\w+)")]
    private static partial Regex PreviewRedditUrlRegex();
}
