using Avalonia.Controls.Notifications;
using SukiUI.Toasts;

namespace QTSAvalonia.Helper;

public static class ToastHelper
{
    public static readonly ISukiToastManager ToastManager = new SukiToastManager();

    public static SukiToastBuilder CreateToastByType(NotificationType toastType, string title = "", string content = "", int duration = 3)
    {
        if (duration <= 0)
        {
            return ToastManager.CreateToast()
                               .WithTitle(title)
                               .WithContent(
                                   content)
                               .OfType(toastType).Dismiss().ByClicking();
        }

        return ToastManager.CreateToast()
                           .WithTitle(title)
                           .WithContent(
                               content)
                           .OfType(toastType).Dismiss().After(TimeSpan.FromSeconds(duration))
                           .Dismiss().ByClicking();
    }

    public static void Success(string title = "", string content = "", int duration = 3)
    {
        DispatcherHelper.RunOnMainThread(() => CreateToastByType(NotificationType.Success, title, content, duration).Queue());
    }

    public static void Loading(string title = "", string content = "", int duration = 3)
    {
        DispatcherHelper.RunOnMainThread(() => ToastManager.CreateToast()
                                                           .WithLoadingState(true)
                                                           .WithTitle(title)
                                                           .WithContent(content)
                                                           .Dismiss().After(TimeSpan.FromSeconds(duration))
                                                           .Dismiss().ByClicking());
    }
    
    public static void Info(string title = "", string content = "", int duration = 3)
    {
        DispatcherHelper.RunOnMainThread(() => CreateToastByType(NotificationType.Information, title, content, duration).Queue());
    }

    public static void Warn(string title = "", string content = "", int duration = 3)
    {
        DispatcherHelper.RunOnMainThread(() => CreateToastByType(NotificationType.Warning, title, content, duration).Queue());
    }

    public static void Error(string title = "", string content = "", int duration = 3)
    {
        DispatcherHelper.RunOnMainThread(() => CreateToastByType(NotificationType.Error, title, content, duration).Queue());
    }
}