﻿namespace ED2.Contracts.Services;

public interface INavigationService
{
    event NavigatedEventHandler Navigated;

    bool CanGoBack { get; }

    Frame? Frame { get; set; }

    bool NavigateTo<T>(object? parameter = null, bool clearNavigation = false);

    bool GoBack();
}
