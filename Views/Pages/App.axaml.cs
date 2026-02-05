using Avalonia.Controls.ApplicationLifetimes;
using QTSCore.Data;

namespace QTSAvalonia.Views.Pages;

public class App : Application
{

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new RootView();
            desktop.MainWindow = mainWindow;
            
            Instances.Initialize();
            LoggerHelper.InitializeLogger();
            AvaloniaFilePickerService.Initialize(TopLevel.GetTopLevel(mainWindow));
            GlobalData.InitializeUiResources();
        }

        base.OnFrameworkInitializationCompleted();
    }
}