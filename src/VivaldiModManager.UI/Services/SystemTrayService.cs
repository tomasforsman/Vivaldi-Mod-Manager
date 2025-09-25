using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;
using System.Windows.Controls;

namespace VivaldiModManager.UI.Services;

/// <summary>
/// Implementation of ISystemTrayService using Hardcodet.NotifyIcon.Wpf.
/// </summary>
public class SystemTrayService : ISystemTrayService, IDisposable
{
    private TaskbarIcon? _notifyIcon;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler? TrayIconDoubleClicked;

    /// <inheritdoc />
    public event EventHandler? ShowRequested;

    /// <inheritdoc />
    public event EventHandler? ExitRequested;

    /// <inheritdoc />
    public void Initialize()
    {
        if (_notifyIcon != null)
            return;

        _notifyIcon = new TaskbarIcon
        {
            ToolTipText = "Vivaldi Mod Manager",
            Visibility = Visibility.Visible
        };

        // Create context menu
        var contextMenu = new ContextMenu();
        
        var showItem = new MenuItem { Header = "Show" };
        showItem.Click += (s, e) => ShowRequested?.Invoke(this, EventArgs.Empty);
        
        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);

        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenu = contextMenu;
        _notifyIcon.TrayMouseDoubleClick += (s, e) => TrayIconDoubleClicked?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Show()
    {
        if (_notifyIcon != null)
            _notifyIcon.Visibility = Visibility.Visible;
    }

    /// <inheritdoc />
    public void Hide()
    {
        if (_notifyIcon != null)
            _notifyIcon.Visibility = Visibility.Hidden;
    }

    /// <inheritdoc />
    public void UpdateTooltip(string text)
    {
        if (_notifyIcon != null)
            _notifyIcon.ToolTipText = text;
    }

    /// <inheritdoc />
    public void ShowNotification(string title, string message, NotificationIcon icon = NotificationIcon.Info)
    {
        if (_notifyIcon == null)
            return;

        var balloonIcon = icon switch
        {
            NotificationIcon.Info => BalloonIcon.Info,
            NotificationIcon.Warning => BalloonIcon.Warning,
            NotificationIcon.Error => BalloonIcon.Error,
            _ => BalloonIcon.None
        };

        _notifyIcon.ShowBalloonTip(title, message, balloonIcon);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _notifyIcon?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}