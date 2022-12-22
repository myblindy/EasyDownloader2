using Reddit;
using Reddit.Controllers;

namespace ED2.Sources;

partial class RedditSource : BaseSource
{
    readonly MainViewModel mainViewModel;
    readonly IRedditService redditService;
    DispatcherQueue? mainDispatcherQueue;
    RedditClient? redditClient;
    string? subReddit;

    public RedditSource(MainViewModel mainViewModel, IRedditService redditService, ILocalSettingsService localSettingsService) : base(localSettingsService)
    {
        this.mainViewModel = mainViewModel;
        this.redditService = redditService;
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

    public override async Task LoadAsync(Uri uri, DispatcherQueue mainDispatcherQueue, Func<ImageDetails>? imageDetailsGenerator = null)
    {
        if (redditClient is null)
        {
            if (await redditService.TryGetRedditClient() is not { } redditClient)
                throw new InvalidOperationException("Reddit authentication failure");
            this.redditClient = redditClient;
        }

        subReddit = UriRegex().Match(uri.ToString()).Groups[2].Value;
        this.mainDispatcherQueue = mainDispatcherQueue;
    }

    public override async IAsyncEnumerable<ImageDetails> EnumerateImageDetails()
    {
        const int pageLimit = 60;
        string? afterPostId = null, beforePostId = null;

        if (redditClient is not null)
            while (true)
            {
                foreach (var post in await Task.Run(() => redditClient.Subreddit(subReddit).Posts.GetNew(afterPostId, beforePostId, pageLimit)))
                {
                    beforePostId ??= post.Fullname;
                    afterPostId = post.Fullname;

                    if (post is not LinkPost linkPost || linkPost.URL.StartsWith("/r/")) continue;

                    // figure out if we can find media links in this
                    foreach (var source in new BaseSource[]
                    {
                        App.GetService<DirectImageSource>(),
                        App.GetService<ImgurSource>(),
                        App.GetService<RedditGallerySource>(),
                    })
                    {
                        if (source.CanHandle(new Uri(linkPost.URL), out _, out _))
                        {
                            await source.LoadAsync(new Uri(linkPost.URL), mainDispatcherQueue!, () => new RedditImageDetails(mainViewModel)
                            {
                                Post = post,
                                Flair = string.IsNullOrWhiteSpace(post.Listing.LinkFlairText) ? null : WebUtility.HtmlDecode(post.Listing.LinkFlairText).Trim(),
                                Title = WebUtility.HtmlDecode(post.Title),
                                DatePosted = post.Created
                            });

                            await foreach (var imageDetails in source.EnumerateImageDetails())
                                yield return imageDetails;

                            break;
                        }
                    }
                }
                beforePostId = null;
            }
    }

    public override async Task OnSaveImage(ImageDetails imageDetails)
    {
        if (imageDetails is RedditImageDetails redditImageDetails)
            await redditImageDetails.Post.UpvoteAsync();
    }

    [GeneratedRegex(@"^(?:https?:\/\/)?(?:www.)?(reddit\.com\/r\/([^/]+))")]
    private static partial Regex UriRegex();
}

class RedditImageDetails : ImageDetails
{
    public RedditImageDetails(MainViewModel mainViewModel) : base(mainViewModel)
    {
    }

    public required Post Post { get; init; }
}
