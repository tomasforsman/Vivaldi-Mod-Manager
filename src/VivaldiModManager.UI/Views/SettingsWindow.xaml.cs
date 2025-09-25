using System.Windows;
using VivaldiModManager.UI.ViewModels;

namespace VivaldiModManager.UI.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}