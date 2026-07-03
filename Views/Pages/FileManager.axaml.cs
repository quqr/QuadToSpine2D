using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.Pages;
using QTSAvalonia.ViewModels.UserControls;

namespace QTSAvalonia.Views.Pages;

public partial class FileManager : UserControl
{
    private ContextMenu? _currentContextMenu;

    public FileManager()
    {
        InitializeComponent();

        // 从 DI 容器获取单例 ViewModel
        DataContext = Instances.ServiceProvider.GetRequiredService<FileManagerViewModel>();

        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not FileManagerViewModel viewModel) return;

        if (e.Key == Key.Escape && viewModel.IsPreviewOpen)
        {
            viewModel.ClosePreviewCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            viewModel.RemoveSelectedCardsCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            viewModel.SelectAllCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.O && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _ = viewModel.OpenFolderCommand.ExecuteAsync(null);
            e.Handled = true;
        }
        else if (e.Key == Key.E && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _ = viewModel.ExportAllCommand.ExecuteAsync(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// 卡片右键事件处理 - 显示上下文菜单
    /// </summary>
    private void OnCardRightClick(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Right) return;
        if (sender is not Border border || border.DataContext is not FileCardViewModel card) return;
        if (DataContext is not FileManagerViewModel vm) return;

        // 关闭上一次打开的菜单，防止堆叠
        _currentContextMenu?.Close();
        _currentContextMenu = null;

        var menu = new ContextMenu();

        // Open in Previewer（导出并跳转到 Previewer 页面）
        var previewPlayerItem = new MenuItem { Header = "Open in Previewer" };
        previewPlayerItem.Command = vm.PreviewInPlayerCommand;
        previewPlayerItem.CommandParameter = card;
        menu.Items.Add(previewPlayerItem);

        // Separator
        menu.Items.Add(new Separator());

        // Preview（仅在有缩略图时，打开本地遮罩预览）
        if (card.HasThumbnails)
        {
            var previewItem = new MenuItem { Header = "Preview Image" };
            previewItem.Command = vm.OpenPreviewCommand;
            previewItem.CommandParameter = card;
            menu.Items.Add(previewItem);
        }

        // Export
        var exportItem = new MenuItem { Header = "Export" };
        exportItem.Command = vm.ExportCardCommand;
        exportItem.CommandParameter = card;
        menu.Items.Add(exportItem);

        // Separator
        menu.Items.Add(new Separator());

        // Open Containing Folder
        var folderItem = new MenuItem { Header = "Open Containing Folder" };
        folderItem.Click += (_, _) => OpenContainingFolder(card);
        menu.Items.Add(folderItem);

        // Copy Path
        var copyItem = new MenuItem { Header = "Copy Path" };
        copyItem.Click += async (_, _) => await CopyPathAsync(card);
        menu.Items.Add(copyItem);

        // Separator
        menu.Items.Add(new Separator());

        // Delete
        var deleteItem = new MenuItem { Header = "Delete" };
        deleteItem.Command = vm.RemoveCardCommand;
        deleteItem.CommandParameter = card;
        menu.Items.Add(deleteItem);

        _currentContextMenu = menu;
        menu.Open(border);
        e.Handled = true;
    }

    /// <summary>
    /// 拖放进入事件
    /// </summary>
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;

        if (!e.DataTransfer.Formats.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    /// <summary>
    /// 拖放放下事件
    /// </summary>
    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not FileManagerViewModel viewModel) return;

        var paths = new List<string>();
        foreach (var item in e.DataTransfer.Items)
        {
            if (item.TryGetValue(DataFormat.File) is IStorageItem storageItem)
            {
                var localPath = storageItem.TryGetLocalPath();
                if (!string.IsNullOrEmpty(localPath))
                    paths.Add(localPath);
            }
        }

        if (paths.Count > 0)
        {
            _ = viewModel.LoadFilesFromDropAsync(paths);
        }
    }

    /// <summary>
    /// 打开文件所在文件夹
    /// </summary>
    private void OpenContainingFolder(FileCardViewModel card)
    {
        var dir = Path.GetDirectoryName(card.FilePath);
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo("open", $"\"{dir}\"") { UseShellExecute = true });
                }
                else
                {
                    Process.Start(new ProcessStartInfo("xdg-open", $"\"{dir}\"") { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"Failed to open folder: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 复制文件路径到剪贴板
    /// </summary>
    private async Task CopyPathAsync(FileCardViewModel card)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard != null)
        {
            var item = new DataTransferItem();
            item.Set(DataFormat.Text, card.FilePath);
            var data = new DataTransfer();
            data.Add(item);
            await topLevel.Clipboard.SetDataAsync(data);
            ToastHelper.Info("Path copied to clipboard");
        }
    }
}
