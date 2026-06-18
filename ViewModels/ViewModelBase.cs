namespace QTSAvalonia.ViewModels;

/// <summary>
/// 所有ViewModel的基类
/// </summary>
/// <remarks>
/// <para>
/// ViewModelBase类是MVVM架构中所有视图模型的抽象基类。
/// 它继承自CommunityToolkit.Mvvm的ObservableObject，提供属性变更通知功能。
/// </para>
/// <para>
/// 派生类可以通过重写Initialize方法来执行初始化逻辑。
/// </para>
/// </remarks>
public abstract partial class ViewModelBase : ObservableObject
{
    /// <summary>
    /// 初始化ViewModel
    /// </summary>
    /// <remarks>
    /// 此方法在ViewModel实例化后调用，用于执行初始化操作。
    /// 派生类应重写此方法以实现自定义初始化逻辑。
    /// </remarks>
    public virtual void Initialize()
    {
    }
}
