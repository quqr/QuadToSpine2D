using Avalonia.Threading;

namespace QTSAvalonia.Helper;

/// <summary>
///     提供UI线程调度功能的静态类
/// </summary>
/// <remarks>
///     <para>
///         DispatcherHelper类封装了Avalonia的线程调度功能，用于确保UI操作在正确的线程上下文中执行。
///         这对于从后台线程更新UI元素至关重要。
///     </para>
///     <para>
///         提供同步和异步两种执行方式：
///         <list type="bullet">
///             <item>
///                 <description>同步方法：RunOnMainThread - 阻塞当前线程直到操作完成</description>
///             </item>
///             <item>
///                 <description>异步方法：RunOnMainThreadAsync - 返回可等待的Task</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public static class DispatcherHelper
{
    /// <summary>
    ///     在UI线程上同步执行指定操作
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <remarks>
    ///     如果当前已在UI线程上，则直接执行；
    ///     否则，使用Invoke阻塞等待UI线程执行完成。
    /// </remarks>
    public static void RunOnMainThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Invoke(action);
    }

    /// <summary>
    ///     在UI线程上同步执行指定函数并返回结果
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="func">要执行的函数</param>
    /// <returns>函数执行结果</returns>
    public static T RunOnMainThread<T>(Func<T> func)
    {
        return Dispatcher.UIThread.CheckAccess() ? func() : Dispatcher.UIThread.Invoke(func);
    }

    /// <summary>
    ///     在UI线程上异步执行指定操作
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <param name="priority">调度优先级（可选）</param>
    /// <param name="cancellationToken">取消令牌（可选）</param>
    /// <returns>表示异步操作的Task</returns>
    /// <remarks>
    ///     如果当前已在UI线程上，则直接执行并返回已完成的Task；
    ///     否则，将操作投递到UI线程异步执行。
    /// </remarks>
    public static Task RunOnMainThreadAsync(Action action, DispatcherPriority? priority = null,
        CancellationToken? cancellationToken = null)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return Dispatcher.UIThread.InvokeAsync(action, priority ?? DispatcherPriority.Normal,
            cancellationToken ?? CancellationToken.None).GetTask();
    }

    /// <summary>
    ///     在UI线程上异步执行指定函数并返回结果
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="func">要执行的函数</param>
    /// <returns>包含函数执行结果的Task</returns>
    public static Task<T> RunOnMainThreadAsync<T>(Func<T> func)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? Task.FromResult(func())
            : Dispatcher.UIThread.InvokeAsync(func).GetTask();
    }
}