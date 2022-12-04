using ED2.Contracts.Services;
using ED2.Models;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Core.Models;
using Tweetinvi.Models;

namespace ED2.Sources;

partial class TwitterSource : BaseSource
{
    readonly ITwitterService twitterService;
    TwitterClient? userTwitterClient;
    string? username;

    public TwitterSource(ILocalSettingsService localSettingsService, ITwitterService twitterService) : base(localSettingsService)
    {
        this.twitterService = twitterService;
    }

    public override bool CanHandle(Uri uri, [NotNullWhen(true)] out Uri? normalizedUri, [NotNullWhen(true)] out string? prefix)
    {
        if (UriRegex().Match(uri.ToString()) is { Success: true } m)
        {
            normalizedUri = new($"https://{m.Groups[1].Value}/");
            prefix = m.Groups[2].Value;
            return true;
        }

        prefix = null;
        normalizedUri = null;
        return false;
    }

    public override async Task Load(Uri uri, DispatcherQueue mainDispatcherQueue)
    {
        if (userTwitterClient is null)
        {
            if (await twitterService.TryGetUserTwitterClient() is not { } userTwitterClient)
                throw new InvalidOperationException("Twitter authentication failure.");
            this.userTwitterClient = userTwitterClient;
        }

        username = UriRegex().Match(uri.ToString()).Groups[2].Value;
    }

    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        var iterator = userTwitterClient!.Timelines.GetUserTimelineIterator(username);
        while (!iterator.Completed)
        {
            var page = await iterator.NextPageAsync();

            foreach (var item in page)
                foreach (var mediaEntry in item.Media)
                {
                    var size = mediaEntry.Sizes.MaxBy(s => s.Value.Width * s.Value.Height);
                    var link = new Uri($"{mediaEntry.MediaURLHttps}:orig");
                    yield return new TwitterImageDetails()
                    {
                        Tweet = item,
                        Completed = localSettingsService.IsImageCompleted(link),
                        OriginalWidth = size.Value.Width!.Value,
                        OriginalHeight = size.Value.Height!.Value,
                        DatePosted = item.CreatedAt.DateTime,
                        Link = link,
                        Title = item.Text
                    };
                }
        }
    }

    public override async Task OnSaveImage(ImageDetails imageDetails)
    {
        if (imageDetails is TwitterImageDetails twitterImageDetails && twitterImageDetails.Tweet is { } tweet)
            await tweet.FavoriteAsync();
    }

    [GeneratedRegex("^(?:https?:\\/\\/)?(?:mobile\\.)?(twitter\\.com\\/([^/?]+))")]
    private static partial Regex UriRegex();
}

class TwitterImageDetails : ImageDetails
{
    public ITweet? Tweet { get; set; }
}