using ED2.Contracts.Services;
using Reddit;
using Reddit.AuthTokenRetriever;
using Reddit.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Auth;
using Tweetinvi.Client;
using Tweetinvi.Client.V2;
using Tweetinvi.Parameters;

namespace ED2.Services;
internal class RedditService : IRedditService
{
    static readonly Uri baseRedirectUri = new("http://127.0.0.1:18081/Reddit.NET/oauthRedirect");
    readonly IDialogService dialogService;

    RedditClient? redditClient;

    public RedditService(IDialogService dialogService)
    {
        this.dialogService = dialogService;
    }

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

        _ = await dialogService.ShowOAuthWindowAsync(new(authTokenRetrieverLib.AuthURL("vote%20read")), baseRedirectUri);

        var waitUntil = DateTime.Now + TimeSpan.FromSeconds(5);
        while (redditClient is null && DateTime.Now < waitUntil)
            await Task.Delay(TimeSpan.FromSeconds(0.1));

        return redditClient;
    }
}
