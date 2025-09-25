namespace VivaldiModManager.UI.Services;

/// <summary>
/// Provides services for displaying dialogs and user interactions.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows an information message dialog.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="caption">The dialog caption.</param>
    Task ShowInformationAsync(string message, string caption = "Information");

    /// <summary>
    /// Shows a warning message dialog.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="caption">The dialog caption.</param>
    Task ShowWarningAsync(string message, string caption = "Warning");

    /// <summary>
    /// Shows an error message dialog.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="caption">The dialog caption.</param>
    Task ShowErrorAsync(string message, string caption = "Error");

    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="caption">The dialog caption.</param>
    /// <returns>True if the user confirmed; otherwise, false.</returns>
    Task<bool> ShowConfirmationAsync(string message, string caption = "Confirm");

    /// <summary>
    /// Shows a file open dialog.
    /// </summary>
    /// <param name="filter">The file filter.</param>
    /// <param name="title">The dialog title.</param>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowOpenFileDialogAsync(string filter = "All Files (*.*)|*.*", string title = "Open File");

    /// <summary>
    /// Shows a folder browser dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <returns>The selected folder path, or null if cancelled.</returns>
    Task<string?> ShowFolderBrowserDialogAsync(string title = "Select Folder");
}