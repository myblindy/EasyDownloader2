namespace ED2.ViewModels;

public partial class OAuthViewModel(Uri uri, Regex expectedUriRegex) : ObservableRecipient
{
    public Uri Uri { get; } = uri;
    public Regex ExpectedUriRegex { get; } = expectedUriRegex;

    public Uri? Result { get; set; }
}
