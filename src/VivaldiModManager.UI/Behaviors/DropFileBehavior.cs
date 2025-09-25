using System.Windows;
using System.Windows.Input;

namespace VivaldiModManager.UI.Behaviors;

/// <summary>
/// Behavior that enables drag-and-drop file functionality on UI elements.
/// </summary>
public static class DropFileBehavior
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command", 
            typeof(ICommand), 
            typeof(DropFileBehavior), 
            new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty AllowedExtensionsProperty =
        DependencyProperty.RegisterAttached(
            "AllowedExtensions", 
            typeof(string), 
            typeof(DropFileBehavior), 
            new PropertyMetadata(".js"));

    public static ICommand? GetCommand(DependencyObject obj)
    {
        return (ICommand?)obj.GetValue(CommandProperty);
    }

    public static void SetCommand(DependencyObject obj, ICommand? value)
    {
        obj.SetValue(CommandProperty, value);
    }

    public static string GetAllowedExtensions(DependencyObject obj)
    {
        return (string)obj.GetValue(AllowedExtensionsProperty);
    }

    public static void SetAllowedExtensions(DependencyObject obj, string value)
    {
        obj.SetValue(AllowedExtensionsProperty, value);
    }

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            element.AllowDrop = true;
            
            if (e.OldValue == null && e.NewValue != null)
            {
                element.DragOver += OnDragOver;
                element.Drop += OnDrop;
            }
            else if (e.OldValue != null && e.NewValue == null)
            {
                element.DragOver -= OnDragOver;
                element.Drop -= OnDrop;
            }
        }
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;

        if (sender is FrameworkElement element && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            if (files != null && files.Length > 0)
            {
                var allowedExts = GetAllowedExtensions(element).Split(';', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var file in files)
                {
                    var extension = System.IO.Path.GetExtension(file).ToLowerInvariant();
                    if (allowedExts.Any(ext => ext.Trim().ToLowerInvariant() == extension))
                    {
                        e.Effects = DragDropEffects.Copy;
                        break;
                    }
                }
            }
        }

        e.Handled = true;
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement element && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var command = GetCommand(element);
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            
            if (files != null && files.Length > 0)
            {
                var allowedExts = GetAllowedExtensions(element).Split(';', StringSplitOptions.RemoveEmptyEntries);
                var validFiles = files.Where(file =>
                {
                    var extension = System.IO.Path.GetExtension(file).ToLowerInvariant();
                    return allowedExts.Any(ext => ext.Trim().ToLowerInvariant() == extension);
                }).ToArray();

                if (validFiles.Length > 0 && command?.CanExecute(validFiles) == true)
                {
                    command.Execute(validFiles);
                }
            }
        }

        e.Handled = true;
    }
}