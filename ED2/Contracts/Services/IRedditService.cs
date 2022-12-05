using Reddit;
using Tweetinvi;

namespace ED2.Contracts.Services;
internal interface IRedditService
{
    ValueTask<RedditClient?> TryGetRedditClient();
}
