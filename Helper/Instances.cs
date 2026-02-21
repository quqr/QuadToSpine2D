using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;

namespace QTSAvalonia.Helper;

public static partial class Instances
{
    private static ConverterSettingViewModel? _converterSetting;
    private static ConverterViewModel? _converter;
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

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

    public static ConverterViewModel Converter
    {
        get
        {
            if (_converter != null) return _converter;
            if (ServiceProvider == null)
                throw new InvalidOperationException("Instances must be initialized before accessing services");
            _converter = ServiceProvider.GetRequiredService<ConverterViewModel>();
            return _converter;
        }
    }

    public static void Initialize()
    {
        var services = new ServiceCollection();
        AddServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    static partial void AddServices(IServiceCollection services);
}