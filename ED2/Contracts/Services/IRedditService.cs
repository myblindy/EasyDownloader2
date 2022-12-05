using Reddit;

namespace ED2.Contracts.Services;
internal interface IRedditService
{
    ValueTask<RedditClient?> TryGetRedditClient();
}
