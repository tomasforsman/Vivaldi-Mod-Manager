using CommunityToolkit.Mvvm.ComponentModel;

namespace VivaldiModManager.UI.ViewModels;

/// <summary>
/// Base class for all ViewModels providing common functionality.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the ViewModel is currently busy.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Gets or sets the current status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Executes an async operation with busy state management.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="statusMessage">The status message to display during operation.</param>
    protected async Task ExecuteAsync(Func<Task> operation, string statusMessage = "Working...")
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = statusMessage;
            await operation();
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    /// <summary>
    /// Executes an async operation with busy state management and returns a result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="statusMessage">The status message to display during operation.</param>
    /// <returns>The operation result.</returns>
    protected async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string statusMessage = "Working...")
    {
        if (IsBusy)
            return default!;

        try
        {
            IsBusy = true;
            StatusMessage = statusMessage;
            return await operation();
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }
}