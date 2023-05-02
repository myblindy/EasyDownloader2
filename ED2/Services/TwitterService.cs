using Tweetinvi;
using Tweetinvi.Auth;
using Tweetinvi.Parameters;

namespace ED2.Services;
internal class TwitterService : ITwitterService
{
    public TwitterClient AppTwitterClient { get; } = new("huD8jnPQTMc74AHwg1KrGg8pv", "08qBm26MzpOOg62lgOnhi1CsgzJnwbhKHpa3SI7jIxoQoemkBb");

    static readonly Uri baseRedirectUri = new("https://easydownloader2.myblindy.com/twittercallback");
    readonly IAuthenticationRequestStore myAuthRequestStore = new LocalAuthenticationRequestStore();
    readonly IDialogService dialogService;

    TwitterClient? userTwitterClient;

    public TwitterService(IDialogService dialogService)
    {
        this.dialogService = dialogService;
    }

    public async ValueTask<TwitterClient?> TryGetUserTwitterClient()
    {
        if (userTwitterClient is not null)
            return userTwitterClient;

        // try to authenticate
        //var requestId = Guid.NewGuid().ToString();
        //var redirectUri = new Uri(myAuthRequestStore.AppendAuthenticationRequestIdToCallbackUrl(baseRedirectUri.ToString(), requestId));
        //var authRequestToken = await AppTwitterClient.Auth.RequestAuthenticationUrlAsync(redirectUri);
        //await myAuthRequestStore.AddAuthenticationTokenAsync(requestId, authRequestToken);

        //if (await dialogService.ShowOAuthWindowAsync(new(authRequestToken.AuthorizationURL), redirectUri) is { } resultUri)
        //{
        //    var reqParams = await RequestCredentialsParameters.FromCallbackUrlAsync(resultUri.ToString(), myAuthRequestStore);
        //    var userCreds = await AppTwitterClient.Auth.RequestCredentialsAsync(reqParams);

        //    return userTwitterClient = new TwitterClient(userCreds);
        //}

        await AppTwitterClient.Auth.InitializeClientBearerTokenAsync();

        return userTwitterClient = AppTwitterClient;
    }
}
