using Reddit;
using Reddit.AuthTokenRetriever;

namespace ED2.Services;
internal class RedditService(IDialogService dialogService) : IRedditService
{
    RedditClient? redditClient;

    public async ValueTask<RedditClient?> TryGetRedditClient()
    {
        if (redditClient is not null)
            return redditClient;

        // try to authenticate
        const string appId = "x_EWzUH9LUzUB9attm4jBA";
        var authTokenRetrieverLib = new AuthTokenRetrieverLib(appId, 18081);
        authTokenRetrieverLib.AuthSuccess += (s, e) =>
        {
            authTokenRetrieverLib.StopListening();
            redditClient = new(appId, e.RefreshToken, accessToken: e.AccessToken);
        };
        authTokenRetrieverLib.AwaitCallback();

        _ = await dialogService.ShowOAuthWindowAsync(new(authTokenRetrieverLib.AuthURL("vote%20read")), new(@"\boauthRedirect\b"));

        var waitUntil = DateTime.Now + TimeSpan.FromSeconds(5);
        while (redditClient is null && DateTime.Now < waitUntil)
            await Task.Delay(TimeSpan.FromSeconds(0.1));

        return redditClient;
    }
}
