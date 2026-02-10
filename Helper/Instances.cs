using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;

namespace QTSAvalonia.Helper;

public static partial class Instances
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;
    
    public static void Initialize()
    {
        var services = new ServiceCollection();
        AddServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }
    
    static partial void AddServices(IServiceCollection services);
    
    private static ConverterSettingViewModel? _converterSetting;
    public static ConverterSettingViewModel ConverterSetting 
    { 
        get
        {
            if (_converterSetting != null) return _converterSetting;
            if (ServiceProvider == null)
                throw new InvalidOperationException("Instances must be initialized before accessing services");
            _converterSetting = ServiceProvider.GetRequiredService<ConverterSettingViewModel>();
            return _converterSetting;
        }
    }
}