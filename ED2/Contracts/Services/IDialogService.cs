namespace ED2.Contracts.Services;

public interface IDialogService
{
    public Task ShowErrorAsync(string message);
    public Task<Uri?> ShowOAuthWindowAsync(Uri uri, Uri expectedUri);
}
