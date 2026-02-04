using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.Utilities;
using QTSCore.Data;

namespace QTSAvalonia.Views.Pages;

public class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 1. 先创建 MainWindow（此时 DataContext 为 null）
            var mainWindow = new RootView();
            desktop.MainWindow = mainWindow;

            // 2. 配置 DI 容器
            var services = new ServiceCollection();

            // 注册 TopLevel 获取工厂（关键：延迟获取，避免空引用）
            services.AddSingleton<Func<TopLevel?>>(() =>
                TopLevel.GetTopLevel(mainWindow));

            // 注册服务（使用工厂避免构造时 TopLevel 为空）
            services.AddSingleton<AvaloniaFilePickerService>(sp =>
                new AvaloniaFilePickerService(sp.GetRequiredService<Func<TopLevel?>>()));
            _serviceProvider = services.BuildServiceProvider();

            InstanceSingleton.Instance.FilePickerService =
                _serviceProvider.GetRequiredService<AvaloniaFilePickerService>();
        }

        GlobalData.InitializeUIResources();

        base.OnFrameworkInitializationCompleted();
    }
}