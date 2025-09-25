using System.Windows;

namespace VivaldiModManager.UI.Views;

/// <summary>
/// Interaction logic for AboutWindow.xaml
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}