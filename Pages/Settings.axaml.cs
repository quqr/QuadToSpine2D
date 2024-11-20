using Avalonia.Controls;
using Avalonia.Interactivity;

namespace QuadToSpine2D.Pages;

public partial class Settings : Window
{
    public Settings()
    {
        InitializeComponent();
    }

    private void JsonReadableChanged(object? sender, RoutedEventArgs e)
    {
        if (ReadableCheckBox?.IsChecked != null)
            GlobalData.IsReadableJson = (bool)ReadableCheckBox.IsChecked;
    }

    private void ScaleFactorChanged(object? sender, TextChangedEventArgs e)
    {
        if (ScaleFactorTextBox?.Text is null || ScaleFactorTextBox.Text.Equals(string.Empty)) return;
        try
        {
            GlobalData.ScaleFactor = Convert.ToInt32(ScaleFactorTextBox.Text);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            GlobalData.BarValue = 100;
            GlobalData.ProcessBar.Foreground = GlobalData.ProcessBarErrorBrush;
            GlobalData.BarTextContent = exception.Message;
            ScaleFactorTextBox.Text = "1";
        }
    }

    private void IsSetLoopAnimationsChanged(object? sender, RoutedEventArgs e)
    {
        if (IsSetLoopCheckBox?.IsChecked != null)
            GlobalData.IsSetLoopAnimation = (bool)IsSetLoopCheckBox.IsChecked;
    }
}