using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace QTSAvalonia.Views.Pages;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Instances.Initialize();
        LoggerHelper.InitializeLogger();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new RootView();
            desktop.MainWindow = mainWindow;

            AvaloniaFilePickerService.Initialize(TopLevel.GetTopLevel(mainWindow));
        }

        base.OnFrameworkInitializationCompleted();
    }
}