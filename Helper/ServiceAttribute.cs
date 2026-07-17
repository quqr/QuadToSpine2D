using Microsoft.Extensions.DependencyInjection;

namespace QTSAvalonia.Helper;

/// <summary>
///     标记类为单例服务的特性
/// </summary>
/// <remarks>
///     <para>
///         应用此特性的类将被源代码生成器自动注册为单例服务。
///         单例服务在整个应用程序生命周期中只创建一个实例。
///     </para>
///     <para>
///         使用示例：
///         <code>
/// [SingletonService]
/// public class MyService
/// {
///     // 服务实现
/// }
/// </code>
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class SingletonService : Attribute
{
    /// <summary>
    ///     获取或设置服务生命周期
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
}

/// <summary>
///     标记类为作用域服务的特性
/// </summary>
/// <remarks>
///     <para>
///         应用此特性的类将被源代码生成器自动注册为作用域服务。
///         作用域服务在每个作用域内创建一个实例。
///     </para>
///     <para>
///         使用示例：
///         <code>
/// [ScopedService]
/// public class MyScopedService
/// {
///     // 服务实现
/// }
/// </code>
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ScopedService : Attribute
{
    /// <summary>
    ///     获取或设置服务生命周期
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
}