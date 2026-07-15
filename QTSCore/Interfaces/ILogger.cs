namespace QTSCore.Interfaces;

/// <summary>
/// 日志接口，解耦核心层对具体日志实现的依赖。
/// </summary>
public interface ILogger
{
    /// <summary>
    /// 记录信息级别日志
    /// </summary>
    void Info(string message);

    /// <summary>
    /// 记录调试级别日志
    /// </summary>
    void Debug(string message);

    /// <summary>
    /// 记录警告级别日志
    /// </summary>
    void Warning(string message);

    /// <summary>
    /// 记录错误级别日志
    /// </summary>
    void Error(string message);

    /// <summary>
    /// 记录错误级别日志（含异常信息）
    /// </summary>
    void Error(string message, Exception ex);
}
