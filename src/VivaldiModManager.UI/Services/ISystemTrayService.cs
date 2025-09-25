namespace VivaldiModManager.UI.Services;

/// <summary>
/// Provides services for system tray integration.
/// </summary>
public interface ISystemTrayService
{
    /// <summary>
    /// Initializes the system tray icon.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Shows the system tray icon.
    /// </summary>
    void Show();

    /// <summary>
    /// Hides the system tray icon.
    /// </summary>
    void Hide();

    /// <summary>
    /// Updates the tooltip text of the system tray icon.
    /// </summary>
    /// <param name="text">The tooltip text.</param>
    void UpdateTooltip(string text);

    /// <summary>
    /// Shows a balloon notification.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    /// <param name="icon">The notification icon.</param>
    void ShowNotification(string title, string message, NotificationIcon icon = NotificationIcon.Info);

    /// <summary>
    /// Event raised when the tray icon is double-clicked.
    /// </summary>
    event EventHandler? TrayIconDoubleClicked;

    /// <summary>
    /// Event raised when the user clicks "Show" from the context menu.
    /// </summary>
    event EventHandler? ShowRequested;

    /// <summary>
    /// Event raised when the user clicks "Exit" from the context menu.
    /// </summary>
    event EventHandler? ExitRequested;
}

/// <summary>
/// Notification icon types.
/// </summary>
public enum NotificationIcon
{
    Info,
    Warning,
    Error,
    None
}