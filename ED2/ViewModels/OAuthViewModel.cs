namespace ED2.ViewModels;

public partial class OAuthViewModel(Uri uri, Uri expectedUri) : ObservableRecipient
{
    public Uri Uri { get; } = uri;
    public Uri ExpectedUri { get; } = expectedUri;

    public Uri? Result { get; set; }
}
