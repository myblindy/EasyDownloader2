using CommunityToolkit.WinUI.UI.Controls;

namespace ED2.Helpers;

static class ViewHelpers
{
    public static string? GetFullFlairText(string? rawFlair) =>
        string.IsNullOrWhiteSpace(rawFlair) ? null : $"[{rawFlair}] ";

    public static Brush GetResolutionBrush(int originalWidth, int originalHeight, int minimumPixels) =>
        ((double)originalWidth * originalHeight / minimumPixels) switch
        {
            > 2.5 => new SolidColorBrush(Colors.DarkRed),
            > 1.5 => new SolidColorBrush(Colors.Yellow),
            _ => new SolidColorBrush(Colors.White)
        };
}
