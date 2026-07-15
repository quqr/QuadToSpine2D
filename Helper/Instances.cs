using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;
using QTSCore.Interfaces;

namespace QTSAvalonia.Helper;

/// <summary>
/// 提供全局服务实例访问的静态类
/// </summary>
/// <remarks>
/// <para>
/// Instances类实现了服务定位器模式，提供对依赖注入容器中服务的全局访问。
/// 它使用延迟初始化模式，在首次访问时创建服务实例。
/// </para>
/// <para>
/// <strong>重要：</strong>在使用任何服务属性之前，必须先调用<see cref="Initialize"/>方法初始化服务提供者。
/// </para>
/// <para>
/// 典型使用方式：
/// <code>
/// // 在应用程序启动时
/// Instances.Initialize();
/// 
/// // 在需要时访问服务
/// var converterSetting = Instances.ConverterSetting;
/// </code>
/// </para>
/// </remarks>
public static partial class Instances
{
    private static ConverterSettingViewModel? _converterSetting;
    private static ConverterViewModel? _converter;
    
    /// <summary>
    /// 获取服务提供者实例
    /// </summary>
    /// <value>
    /// 初始化后的IServiceProvider实例
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// 当在调用Initialize之前访问此属性时抛出
    /// </exception>
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// 获取转换器设置视图模型实例
    /// </summary>
    /// <value>
    /// ConverterSettingViewModel的单例实例
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// 当在调用Initialize之前访问此属性时抛出
    /// </exception>
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

    /// <summary>
    /// 获取转换器视图模型实例
    /// </summary>
    /// <value>
    /// ConverterViewModel的单例实例
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// 当在调用Initialize之前访问此属性时抛出
    /// </exception>
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

    /// <summary>
    /// 初始化服务提供者
    /// </summary>
    /// <remarks>
    /// <para>
    /// 此方法应在应用程序启动时调用一次。
    /// 它会创建服务集合并构建服务提供者。
    /// </para>
    /// <para>
    /// 具体的服务注册由部分类中的AddServices方法完成，
    /// 该方法由源代码生成器自动生成。
    /// </para>
    /// </remarks>
    public static void Initialize()
    {
        var services = new ServiceCollection();
        AddServices(services);

        // 注册接口映射，使核心层可通过接口访问配置和日志
        services.AddSingleton<ILogger, LoggerAdapter>();
        services.AddSingleton<IConverterSettings>(sp => sp.GetRequiredService<ConverterSettingViewModel>());
        services.AddSingleton<IConversionResult>(sp => sp.GetRequiredService<ConverterViewModel>());

        ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// 添加服务到服务集合
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <remarks>
    /// 此方法由源代码生成器自动实现，用于注册所有标记了ServiceAttribute的服务。
    /// </remarks>
    static partial void AddServices(IServiceCollection services);
}
