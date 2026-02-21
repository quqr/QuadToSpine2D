// 添加这些简化名称的特性别名

using Microsoft.Extensions.DependencyInjection;

namespace QTSAvalonia.Helper;

[AttributeUsage(AttributeTargets.Class)]
public class SingletonService : Attribute
{
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
}

[AttributeUsage(AttributeTargets.Class)]
public class TransientService : Attribute
{
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
}

[AttributeUsage(AttributeTargets.Class)]
public class ScopedService : Attribute
{
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
}