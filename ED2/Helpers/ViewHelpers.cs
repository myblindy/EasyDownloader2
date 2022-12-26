namespace ED2.Helpers;

static class ViewHelpers
{
    public static string? GetFullFlairText(string? rawFlair) =>
        string.IsNullOrWhiteSpace(rawFlair) ? null : $"[{rawFlair}] ";

    public static string GetPrettyResolution(int originalWidth, int originalHeight) =>
        $"{originalWidth}x{originalHeight}";

    public static Brush GetResolutionBrush(int originalWidth, int originalHeight, int minimumPixels) =>
        ((double)originalWidth * originalHeight / minimumPixels) switch
        {
            > 2.5 => new SolidColorBrush(Colors.Red),
            > 1.5 => new SolidColorBrush(Colors.Yellow),
            _ => new SolidColorBrush(Colors.LightGray)
        };

    public static int Sum(int a, int b) => a + b;

    public static Visibility VisibleIfNotZero(int a) => a != 0 ? Visibility.Visible: Visibility.Collapsed;
}
