using Imgur;
using Nito.AsyncEx;
using System.Runtime.CompilerServices;

namespace ED2.Services;
internal partial class ImgurService(IDialogService dialogService)
{
    ImgurClient? apiClient;

    readonly AsyncMonitor connectSync = new();

    public async IAsyncEnumerable<(Uri uri, int width, int height)> EnumerateAlbumImages(string albumId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await EnsureConnected();

        var album = await apiClient!.Album.GetAsync(new() { AlbumHash = albumId }, ct);
        foreach (var image in album.Images)
            if (image?.Link is not null)
                yield return (new(image.Link), image.Width, image.Height);

        yield break;
    }

    async Task EnsureConnected()
    {
        if (apiClient is not null) return;

        using (await connectSync.EnterAsync())
        {
            apiClient = new ImgurClient { ClientId = "8cbed3b16b6456c", ClientSecret = "9ca0de50071cfa64be9b02db3bdacca1cb794edf" };
            var authUrl = apiClient.Authorization.GetAuthorizationUrl("");

            var resultUri = await dialogService.ShowOAuthWindowAsync(new(authUrl), new(@"\bED2-oauth2-imgur\b"));

            if (OAuth2TokensRegex().Matches(resultUri!.OriginalString) is { Count: >= 6 } matches)
            {
                apiClient.AccessToken = getValue("access_token");

                string getValue(string key) =>
                    matches!.First(m => m.Groups[1].Value == key).Groups[2].Value;
            }
        }
    }

    [GeneratedRegex("(\\w+)=([^=#&]+)")]
    private static partial Regex OAuth2TokensRegex();
}
