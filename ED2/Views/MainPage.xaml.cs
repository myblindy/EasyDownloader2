using CommunityToolkit.Mvvm.Input;
using ED2.Contracts.Services;
using ED2.Models;
using ED2.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace ED2.Views;

public sealed partial class MainPage : Page
{
    readonly IDialogService dialogService = App.GetService<IDialogService>();

    public MainViewModel ViewModel { get; } = App.GetService<MainViewModel>();

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OpenBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (Uri.TryCreate(sender.Text, UriKind.RelativeOrAbsolute, out var uri))
        {
            await ViewModel.OpenCommand.ExecuteAsync(uri);

            if (ViewModel.CurrentNormalizedUri is not null)
                ViewModel.LocalSettingsService.AddSuggestion(ViewModel.CurrentNormalizedUri);
        }
        else
            await dialogService.ShowErrorAsync("Unrecognized path format.");
    }

    private void OpenBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason is AutoSuggestionBoxTextChangeReason.UserInput)
            sender.ItemsSource = ViewModel.LocalSettingsService.GetSuggestions(sender.Text);
    }

    private void OpenBoxSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args) =>
        sender.Text = args.SelectedItem.ToString();

    private void ImagePointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var pointer = e.GetCurrentPoint((UIElement)sender);

        if (((Grid)sender).DataContext is ImageDetails image)
            if (pointer.Properties.IsLeftButtonPressed)
                ViewModel.SaveImageCommand.Execute(image);
            else if (pointer.Properties.IsMiddleButtonPressed)
                Process.Start(new ProcessStartInfo(image.Link!.ToString()) { UseShellExecute = true });
            else if (pointer.Properties.IsRightButtonPressed)
                image.Completed = true;
    }
}
