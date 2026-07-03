using System.Text;
using Avalonia.Input;
using Avalonia.Threading;
using ObservableCollections;
using QTSAvalonia.Models;

namespace QTSAvalonia.ViewModels.Pages;

/// <summary>
/// 设置页面的视图模型
/// </summary>
/// <remarks>
/// 管理应用程序设置页面，主要功能是显示、过滤和操作日志队列。
/// 使用[SingletonService]特性标记为单例服务。
/// </remarks>
[SingletonService]
public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableQueue<LogEntry> _logs = new(150);

    /// <summary>
    /// 过滤后的日志集合（绑定到 UI）
    /// </summary>
    public ObservableCollection<LogEntry> FilteredLogs { get; } = [];

    [ObservableProperty]
    private string _logFilterText = string.Empty;

    [ObservableProperty]
    private string _selectedLogLevel = "All";

    public SettingsViewModel()
    {
    }

    /// <summary>
    /// 添加一条日志条目（供 LoggerHelper 调用）
    /// </summary>
    internal void AddLog(LogEntry entry)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Logs.Count >= 150)
                Logs.Dequeue();
            Logs.Enqueue(entry);
            FilterLogs();
        });
    }

    /// <summary>
    /// 根据关键字和级别过滤日志
    /// </summary>
    public void FilterLogs()
    {
        var keyword = LogFilterText.ToLowerInvariant();
        var level = SelectedLogLevel;

        FilteredLogs.Clear();
        foreach (var entry in Logs)
        {
            if (!string.IsNullOrEmpty(keyword) && !entry.Message.ToLowerInvariant().Contains(keyword))
                continue;
            if (level != "All" && !string.Equals(entry.Level, level, StringComparison.OrdinalIgnoreCase))
                continue;
            FilteredLogs.Add(entry);
        }
    }

    partial void OnLogFilterTextChanged(string value)
    {
        FilterLogs();
    }

    partial void OnSelectedLogLevelChanged(string value)
    {
        FilterLogs();
    }

    /// <summary>
    /// 清空所有日志
    /// </summary>
    [RelayCommand]
    private void ClearLogs()
    {
        Logs.Clear();
        FilteredLogs.Clear();
    }

    /// <summary>
    /// 复制所有过滤后日志到剪贴板
    /// </summary>
    [RelayCommand]
    private async Task CopyAllLogsAsync()
    {
        if (FilteredLogs.Count == 0) return;

        var sb = new StringBuilder();
        foreach (var entry in FilteredLogs)
            sb.AppendLine(entry.FullText);

        try
        {
            var topLevel = App.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;

            if (topLevel?.Clipboard != null)
            {
                var item = new DataTransferItem();
                item.Set(DataFormat.Text, sb.ToString());
                var data = new DataTransfer();
                data.Add(item);
                await topLevel.Clipboard.SetDataAsync(data);
                ToastHelper.Success("Copied", $"{FilteredLogs.Count} log entries copied");
            }
        }
        catch (Exception ex)
        {
            ToastHelper.Error("Copy failed", ex.Message);
        }
    }
}
