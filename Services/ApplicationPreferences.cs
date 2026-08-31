using Windows.Storage;

namespace NotiRelay.Services;

internal enum WindowCloseAction
{
    MinimizeToSystemTray = 0,
    Exit = 1,
    AskEveryTime = 2
}

internal static class ApplicationPreferences
{
    private const string SilentStartKey = "SilentStart";
    private const string WindowCloseActionKey = "WindowCloseAction";

    private static ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;

    public static bool SilentStart
    {
        get => LocalSettings.Values[SilentStartKey] is true;
        set => LocalSettings.Values[SilentStartKey] = value;
    }

    public static WindowCloseAction WindowCloseAction
    {
        get => LocalSettings.Values[WindowCloseActionKey] is int stored &&
            System.Enum.IsDefined(typeof(WindowCloseAction), stored)
                ? (WindowCloseAction)stored
                : WindowCloseAction.MinimizeToSystemTray;
        set => LocalSettings.Values[WindowCloseActionKey] = (int)value;
    }
}
