using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace ED2;

static class Program
{
    static DispatcherQueue? MainDispatcherQueue;

    [STAThread]
    static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        bool isRedirect = DecideRedirection();
        if (!isRedirect)
        {
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(MainDispatcherQueue = DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
    }

    #region Redirection

    // Decide if we want to redirect the incoming activation to another instance.
    private static bool DecideRedirection()
    {
        bool isRedirect = false;

        // Find out what kind of activation this is.
        var mainAppInstance = AppInstance.FindOrRegisterForKey("MainInstance");

        if (!mainAppInstance.IsCurrent)
        {
            isRedirect = true;
            var x = AppInstance.GetCurrent().GetActivatedEventArgs().Data as Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
            AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
            RedirectActivationTo(args, mainAppInstance);
        }
        else
        {
            mainAppInstance.Activated += (s, e) =>
                MainDispatcherQueue!.TryEnqueue(() => App.GetService<IActivationService>()!.ActivateAsync(e.Data));
        }

        return isRedirect;
    }

    private static IntPtr redirectEventHandle = IntPtr.Zero;

    // Do the redirection on another thread, and use a non-blocking
    // wait method to wait for the redirection to complete.
    public static void RedirectActivationTo(AppActivationArguments args, AppInstance keyInstance)
    {
        var redirectSemaphore = new Semaphore(0, 1);
        Task.Run(() =>
        {
            keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
            redirectSemaphore.Release();
        });
        redirectSemaphore.WaitOne();
    }

    #endregion
}
