using Imgur.API.Authentication;
using Imgur.API.Endpoints;
using Imgur.API.Models;
using Nito.AsyncEx;
using System.Runtime.CompilerServices;

namespace ED2.Services;
internal partial class ImgurService
{
    private readonly IDialogService dialogService;
    IApiClient? apiClient;

    readonly AsyncMonitor connectSync = new();

    public ImgurService(IDialogService dialogService)
    {
        this.dialogService = dialogService;
    }

    public async IAsyncEnumerable<(Uri uri, int width, int height)> EnumerateAlbumImages(string albumId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await EnsureConnected();

        var imageEndpoint = new AlbumEndpoint(apiClient, new());
        foreach (var image in await imageEndpoint.GetImagesAsync(albumId, ct))
            yield return (new(image.Link), image.Width, image.Height);

        yield break;
    }

    async Task EnsureConnected()
    {
        if (apiClient is not null) return;

        using (await connectSync.EnterAsync())
        {
            apiClient = new ApiClient("8cbed3b16b6456c", "9ca0de50071cfa64be9b02db3bdacca1cb794edf");
            var oAuth2Endpoint = new OAuth2Endpoint(apiClient, new());
            var authUrl = oAuth2Endpoint.GetAuthorizationUrl();

            var resultUri = await dialogService.ShowOAuthWindowAsync(new(authUrl), new("http://localhost/ED2-oauth2-imgur"));

            if (OAuth2TokensRegex().Matches(resultUri!.OriginalString) is { Count: >= 6 } matches)
            {
                apiClient.SetOAuth2Token(new OAuth2Token
                {
                    AccessToken = getValue("access_token"),
                    ExpiresIn = int.Parse(getValue("expires_in")),
                    TokenType = getValue("token_type"),
                    RefreshToken = getValue("refresh_token"),
                    AccountUsername = getValue("account_username"),
                    AccountId = int.Parse(getValue("account_id"))
                });

                string getValue(string key) =>
                    matches!.First(m => m.Groups[1].Value == key).Groups[2].Value;
            }
        }
    }

    [GeneratedRegex("(\\w+)=([^=#&]+)")]
    private static partial Regex OAuth2TokensRegex();
}
