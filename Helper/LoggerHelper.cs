using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;
using Serilog;
using Serilog.Core;

namespace QTSAvalonia.Helper;

public enum LogLevel : uint
{
    Debug = 1,
    Info = 2,
    Warn = 3,
    Error = 4
}

public static class LoggerHelper
{
    private static readonly SettingsViewModel SettingsViewModel =
        Instances.ServiceProvider.GetRequiredService<SettingsViewModel>();
    private static Logger? _logger;
    private static readonly List<(LogLevel level, string message)> LogCache = [];
    private const string OutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}][{Level:u3}] {Message:lj}{NewLine}{Exception}";

    public static void InitializeLogger()
    {
        if (Design.IsDesignMode)
            return;
        if (_logger != null) return;
        _logger = new LoggerConfiguration()
            .WriteTo.File(
                "logs/log-.log",
                rollingInterval: RollingInterval.Day,
                shared: true,
                outputTemplate: OutputTemplate)
            .WriteTo.Console(
                outputTemplate: OutputTemplate)
            .CreateLogger();
        FlushCache();
    }

    public static void DisposeLogger()
    {
        _logger?.Dispose();
        _logger = null;
    }

    private static void FlushCache()
    {
        if (_logger == null) return;
        foreach (var (level, msg) in LogCache)
            switch (level)
            {
                case LogLevel.Info:
                    _logger.Information(msg);
                    break;
                case LogLevel.Error:
                    _logger.Error(msg);
                    break;
                case LogLevel.Warn:
                    _logger.Warning(msg);
                    break;
            }

        LogCache.Clear();
    }
    private static string FormatLogMessage(string? message, LogLevel level)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelString = level switch
        {
            LogLevel.Debug => "DEBUG",
            LogLevel.Info => "INFO",
            LogLevel.Warn => "WARN",
            LogLevel.Error => "ERROR",
            _ => "UNKNOWN"
        };
        
        return $"[{timestamp}][{levelString}] {message}";
    }
    private static void Log(object? message, LogLevel level)
    {
        if (_logger == null)
            LogCache.Add((level, message?.ToString() ?? string.Empty));
        else
        {
            switch (level)
            {
                case LogLevel.Debug:
                    _logger.Debug(message?.ToString() ?? string.Empty);
                    break;
                case LogLevel.Info:
                    _logger.Information(message?.ToString() ?? string.Empty);
                    break;
                case LogLevel.Warn:
                    _logger.Warning(message?.ToString() ?? string.Empty);
                    break;
                case LogLevel.Error:
                    _logger.Error(message?.ToString() ?? string.Empty);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        DispatcherHelper.RunOnMainThreadAsync((() =>
        {
            if (SettingsViewModel.Logs.Count > 50)
                SettingsViewModel.Logs.Dequeue();
            SettingsViewModel.Logs.Enqueue(new TextBlock
            {
                Text = FormatLogMessage(message?.ToString(),level),
            });
        }));
    }
    public static void Info(object? message)
    {
        Log(message, LogLevel.Info);
    }

    public static void Debug(object? message)
    {
        Log(message,LogLevel.Debug);
    }
    
    public static void Error(object message)
    {
        Log(message,LogLevel.Error);

    }

    public static void Error(object message, Exception e)
    {
        Log($"{message}\n{e}",LogLevel.Error);

    }

    public static void Warning(object message)
    {
        Log(message, LogLevel.Warn);
    }
}