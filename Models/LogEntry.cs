namespace QTSAvalonia.Models;

/// <summary>
///     日志条目模型，支持级别、消息和完整文本
/// </summary>
public class LogEntry
{
    /// <summary>
    ///     日志级别：Debug / Info / Warn / Error
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    ///     日志消息内容（不含时间戳前缀）
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     完整格式化文本（含时间戳和级别），用于显示
    /// </summary>
    public string FullText { get; set; } = string.Empty;

    /// <summary>
    ///     日志记录时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}