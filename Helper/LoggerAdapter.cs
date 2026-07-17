using QTSCore.Interfaces;

namespace QTSAvalonia.Helper;

/// <summary>
///     LoggerHelper 的 ILogger 适配器，解耦核心层对静态日志类的依赖。
/// </summary>
public class LoggerAdapter : ILogger
{
    public void Info(string message)
    {
        LoggerHelper.Info(message);
    }

    public void Debug(string message)
    {
        LoggerHelper.Debug(message);
    }

    public void Warning(string message)
    {
        LoggerHelper.Warning(message);
    }

    public void Error(string message)
    {
        LoggerHelper.Error(message);
    }

    public void Error(string message, Exception ex)
    {
        LoggerHelper.Error(message, ex);
    }
}