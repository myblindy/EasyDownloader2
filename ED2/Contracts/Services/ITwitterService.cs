using Tweetinvi;

namespace ED2.Contracts.Services;
internal interface ITwitterService
{
    ValueTask<TwitterClient?> TryGetUserTwitterClient();
}
