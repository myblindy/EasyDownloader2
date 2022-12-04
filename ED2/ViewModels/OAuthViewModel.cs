using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ED2.ViewModels;

public partial class OAuthViewModel : ObservableRecipient
{
    public OAuthViewModel(Uri uri, Uri expectedUri)
    {
        Uri = uri;
        ExpectedUri = expectedUri;
    }

    public Uri Uri { get; }
    public Uri ExpectedUri { get; }

    public Uri? Result { get; set; }
}
