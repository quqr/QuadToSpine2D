using Microsoft.Extensions.DependencyInjection;

namespace QTSAvalonia.Helper;

public static partial class Instances
{
    public static IServiceProvider ServiceProvider;
    
    public static void Initialize()
    {
        var services = new ServiceCollection();
        AddServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }
    static partial void AddServices(IServiceCollection services);
}