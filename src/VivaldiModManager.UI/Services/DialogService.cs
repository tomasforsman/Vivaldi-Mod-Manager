using Microsoft.Win32;
using System.Windows;

namespace VivaldiModManager.UI.Services;

/// <summary>
/// Implementation of IDialogService for WPF dialogs.
/// </summary>
public class DialogService : IDialogService
{
    /// <inheritdoc />
    public Task ShowInformationAsync(string message, string caption = "Information")
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowWarningAsync(string message, string caption = "Warning")
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowErrorAsync(string message, string caption = "Error")
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ShowConfirmationAsync(string message, string caption = "Confirm")
    {
        var result = MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    /// <inheritdoc />
    public Task<string?> ShowOpenFileDialogAsync(string filter = "All Files (*.*)|*.*", string title = "Open File")
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Title = title
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == true ? dialog.FileName : null);
    }

    /// <inheritdoc />
    public Task<string?> ShowFolderBrowserDialogAsync(string title = "Select Folder")
    {
        // For now, use OpenFileDialog in folder mode - in a real implementation we'd use a proper folder dialog
        var dialog = new OpenFileDialog
        {
            Title = title,
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Select Folder"
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == true ? System.IO.Path.GetDirectoryName(dialog.FileName) : null);
    }
}