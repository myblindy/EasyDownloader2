namespace ED2.Activation;

public class DefaultActivationHandler(INavigationService navigationService) : ActivationHandler<LaunchActivatedEventArgs>
{
    protected override bool CanHandleInternal(LaunchActivatedEventArgs args) =>
        // None of the ActivationHandlers has handled the activation.
        navigationService.Frame?.Content == null;

    protected override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        navigationService.NavigateTo<MainViewModel>(args.Arguments);
        return Task.CompletedTask;
    }
}
