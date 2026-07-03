using System.Collections.Concurrent;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using QTSAvalonia.ViewModels.UserControls;
using VanillawareConverter.Ftex;
using VanillawareConverter.Mbs.Models;
using VanillawareConverter.Mbs.Parsers;
using VanillawareConverter.Mbs.Converters;
using Newtonsoft.Json;
using QTSCore.Process;
using SkiaSharp;

namespace QTSAvalonia.ViewModels.Pages;

[SingletonService]
public partial class FileManagerViewModel : ViewModelBase, IDisposable
{
    /// <summary>
    /// 缩略图最大尺寸（按需解码时缩放上限）
    /// </summary>
    private const int ThumbnailMaxSize = 256;

    [ObservableProperty]
    private ObservableCollection<FileCardViewModel> _fileCards = [];

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _pairedCount;

    [ObservableProperty]
    private int _unpairedCount;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private double _exportProgress;

    [ObservableProperty]
    private string _selectedFolderPath = string.Empty;

    [ObservableProperty]
    private FileCardViewModel? _selectedCard;

    [ObservableProperty]
    private bool _isPreviewOpen;

    [ObservableProperty]
    private string _previewImagePath = string.Empty;

    [ObservableProperty]
    private string _exportOutputPath = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private double _loadProgress;

    [ObservableProperty]
    private string _loadStatus = string.Empty;

    private CancellationTokenSource? _loadCts;
    private readonly ConcurrentBag<string> _tempFiles = new();

    public FileManagerViewModel()
    {
    }

    /// <summary>
    /// 是否有文件已加载
    /// </summary>
    public bool HasFiles => FileCards.Count > 0;

    /// <summary>
    /// 是否未处于加载或导出状态
    /// </summary>
    public bool IsNotBusy => !IsLoading && !IsExporting;

    /// <summary>
    /// 是否全部选中
    /// </summary>
    public bool IsAllSelected => FileCards.Count > 0 && FileCards.All(c => c.IsSelected);

    /// <summary>
    /// 公共方法：通知 IsAllSelected 属性变更（供 FileCardViewModel 调用）
    /// </summary>
    public void NotifyIsAllSelectedChanged() => OnPropertyChanged(nameof(IsAllSelected));

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
    }

    partial void OnIsExportingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
    }

    [RelayCommand]
    private async Task OpenFolder()
    {
        var topLevel = App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null;

        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var folder = folders[0];
            SelectedFolderPath = folder.Path.LocalPath;
            LoggerHelper.Info($"[FileManager] Opening folder: {SelectedFolderPath}");
            await LoadFilesFromFolderAsync(SelectedFolderPath);
        }
    }

    [RelayCommand]
    private async Task OpenFiles()
    {
        var topLevel = App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null;

        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Files",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Supported Files")
                {
                    Patterns = new[] { "*.ftx", "*.ftp", "*.mbs", "*.mbp" }
                }
            }
        });

        if (files.Count > 0)
        {
            var filePaths = files.Select(f => f.Path.LocalPath).ToList();
            LoggerHelper.Info($"[FileManager] Opening {filePaths.Count} files");
            await LoadFilesAsync(filePaths);
        }
    }

    /// <summary>
    /// 从拖放操作加载文件
    /// </summary>
    public async Task LoadFilesFromDropAsync(IEnumerable<string> paths)
    {
        var filePaths = new List<string>();
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                var dirFiles = await Task.Run(() =>
                {
                    var ftxFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                        .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".ftx" or ".ftp")
                        .ToList();
                    var mbsFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                        .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".mbs" or ".mbp")
                        .ToList();
                    return ftxFiles.Concat(mbsFiles).ToList();
                });
                filePaths.AddRange(dirFiles);
            }
            else if (File.Exists(path))
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext is ".ftx" or ".ftp" or ".mbs" or ".mbp")
                    filePaths.Add(path);
            }
        }

        if (filePaths.Count > 0)
        {
            LoggerHelper.Info($"[FileManager] Dropped {filePaths.Count} files");
            await LoadFilesAsync(filePaths);
        }
    }

    public async Task LoadFilesFromFolderAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        IsLoading = true;
        LoadProgress = 0;
        LoadStatus = "Scanning folder...";

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            var allFiles = await Task.Run(() =>
            {
                var ftxFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".ftx" or ".ftp")
                    .ToList();
                var mbsFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".mbs" or ".mbp")
                    .ToList();
                return ftxFiles.Concat(mbsFiles).ToList();
            }, ct);

            LoggerHelper.Info($"[FileManager] Found {allFiles.Count} files in {folderPath}");
            await LoadFilesInternalAsync(allFiles, ct);
        }
        catch (OperationCanceledException)
        {
            LoadStatus = "Loading cancelled";
            LoggerHelper.Info("[FileManager] Loading cancelled");
        }
        catch (Exception ex)
        {
            LoadStatus = $"Error: {ex.Message}";
            LoggerHelper.Error($"Failed to load folder: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadFilesAsync(List<string> filePaths)
    {
        IsLoading = true;
        LoadProgress = 0;
        LoadStatus = "Loading files...";

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            await LoadFilesInternalAsync(filePaths, ct);
        }
        catch (OperationCanceledException)
        {
            LoadStatus = "Loading cancelled";
            LoggerHelper.Info("[FileManager] Loading cancelled");
        }
        catch (Exception ex)
        {
            LoadStatus = $"Error: {ex.Message}";
            LoggerHelper.Error($"Failed to load files: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadFilesInternalAsync(List<string> filePaths, CancellationToken ct)
    {
        // 清理旧卡片，释放内存
        DisposeAllCards();

        // 立即通知 UI 清空状态
        OnPropertyChanged(nameof(HasFiles));
        UpdateCounts();

        var grouped = filePaths
            .GroupBy(f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        var totalGroups = grouped.Count;
        var processedGroups = 0;

        LoggerHelper.Info($"[FileManager] Loading {totalGroups} file groups...");

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = ct
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(grouped, parallelOptions, group =>
            {
                ct.ThrowIfCancellationRequested();

                var ftxFile = group.Value.FirstOrDefault(f =>
                    Path.GetExtension(f).ToLowerInvariant() is ".ftx" or ".ftp");
                var mbsFile = group.Value.FirstOrDefault(f =>
                    Path.GetExtension(f).ToLowerInvariant() is ".mbs" or ".mbp");

                var card = new FileCardViewModel();

                if (ftxFile != null && mbsFile != null)
                {
                    card.FileName = group.Key;
                    card.FilePath = ftxFile;
                    card.PairWith(ftxFile, mbsFile);
                    LoadFtxInfoSync(card, ftxFile);
                    LoadMbsInfoSync(card, mbsFile);
                }
                else if (ftxFile != null)
                {
                    card = new FileCardViewModel(ftxFile);
                    card.MarkAsUnpaired();
                    LoadFtxInfoSync(card, ftxFile);
                }
                else if (mbsFile != null)
                {
                    card = new FileCardViewModel(mbsFile);
                    card.MarkAsUnpaired();
                    LoadMbsInfoSync(card, mbsFile);
                }

                card.Parent = this;

                var current = Interlocked.Increment(ref processedGroups);
                Dispatcher.UIThread.Post(() =>
                {
                    FileCards.Add(card);
                    LoadProgress = (double)current / totalGroups * 100;
                    LoadStatus = $"Loading {current}/{totalGroups}...";

                    if (current == totalGroups)
                    {
                        UpdateCounts();
                        OnPropertyChanged(nameof(HasFiles));
                        LoadStatus = $"Loaded {FileCards.Count} files";
                        LoadProgress = 100;
                        LoggerHelper.Info($"[FileManager] Loading complete: {FileCards.Count} cards");
                        ToastHelper.Success($"Loaded {FileCards.Count} files");
                    }
                });
            });
        }, ct);
    }

    private void LoadFtxInfoSync(FileCardViewModel card, string ftxPath)
    {
        try
        {
            var reader = new UnifiedFtexReader();
            var results = reader.ParseFile(ftxPath);

            // 懒加载：只记录图片数量，不生成 PNG
            // 实际解码在 FileCardViewModel.ThumbnailBitmap getter 中按需执行
            Dispatcher.UIThread.Post(() =>
            {
                card.ImageCount = results.Count;
                card.SetFtxSource(ftxPath);
                OnPropertyChanged(nameof(HasFiles)); // 触发 UI 刷新缩略图
            });

            LoggerHelper.Debug($"[FileManager] FTX metadata loaded: {card.BaseName} ({results.Count} images, lazy decode)");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Failed to load FTX info for {card.BaseName}: {ex.Message}");
        }
    }

    private void LoadMbsInfoSync(FileCardViewModel card, string mbsPath)
    {
        try
        {
            var fileData = File.ReadAllBytes(mbsPath);
            var tag = PlatformConfigs.DetectPlatform(fileData);

            if (tag == PlatformTag.Unknown)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    card.AnimationCount = 0;
                    card.AnimationNames = ["Unknown platform"];
                });
                LoggerHelper.Warning($"[FileManager] Unknown platform: {Path.GetFileName(mbsPath)}");
                return;
            }

            var parser = new MbsToV55Parser();
            var v55Data = parser.Parse(fileData, tag);

            // S9 = 骨架数据，过滤空名称
            var skeletonNames = new List<string>();
            for (int i = 0; i < v55Data.S9.Count; i++)
            {
                var bone = v55Data.S9[i];
                if (bone != null && !string.IsNullOrWhiteSpace(bone.Name))
                    skeletonNames.Add(bone.Name);
            }

            // 动画数量 = Sa 集合总数
            var animCount = v55Data.Sa.Count;

            Dispatcher.UIThread.Post(() =>
            {
                card.LoadAnimationInfo(animCount, skeletonNames);
            });

            LoggerHelper.Debug($"[FileManager] MBS loaded: {card.BaseName} ({animCount} animations, {skeletonNames.Count} skeletons)");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Failed to load MBS info for {card.BaseName}: {ex.Message}");
        }
    }

    private void SaveAsPng(ImageResult result, string outputPath)
    {
        try
        {
            // 导出原始分辨率，不缩放（Quad JSON 坐标引用原始尺寸）
            using var srcBitmap = new SKBitmap(result.Width, result.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

            var pixels = new byte[result.Width * result.Height * 4];
            for (int y = 0; y < result.Height; y++)
            {
                for (int x = 0; x < result.Width; x++)
                {
                    int dstOffset = (y * result.Width + x) * 4;
                    int srcOffset = dstOffset;
                    if (srcOffset + 3 < result.PixelData.Length)
                    {
                        pixels[dstOffset] = result.PixelData[srcOffset];
                        pixels[dstOffset + 1] = result.PixelData[srcOffset + 1];
                        pixels[dstOffset + 2] = result.PixelData[srcOffset + 2];
                        pixels[dstOffset + 3] = result.PixelData[srcOffset + 3];
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, srcBitmap.GetPixels(), pixels.Length);

            using var image = SKImage.FromBitmap(srcBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);

            File.WriteAllBytes(outputPath, data.ToArray());
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Failed to save PNG: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemoveCard(FileCardViewModel? card)
    {
        if (card == null) return;

        LoggerHelper.Debug($"[FileManager] Removing card: {card.BaseName}");

        FileCards.Remove(card);
        UpdateCounts();
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(IsAllSelected));

        // 删除临时缩略图文件
        foreach (var path in card.ThumbnailPaths)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        // 释放 Bitmap 资源和循环引用
        card.Dispose();
    }

    [RelayCommand]
    private void RemoveSelectedCards()
    {
        var selected = FileCards.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0) return;
        LoggerHelper.Info($"[FileManager] Removing {selected.Count} selected cards");
        foreach (var card in selected)
        {
            RemoveCard(card);
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var card in FileCards)
            card.IsSelected = true;
        OnPropertyChanged(nameof(IsAllSelected));
        LoggerHelper.Debug("[FileManager] Selected all cards");
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var card in FileCards)
            card.IsSelected = false;
        OnPropertyChanged(nameof(IsAllSelected));
        LoggerHelper.Debug("[FileManager] Deselected all cards");
    }

    /// <summary>
    /// 切换全选/取消全选
    /// </summary>
    [RelayCommand]
    private void ToggleSelectAll()
    {
        if (IsAllSelected)
            DeselectAll();
        else
            SelectAll();
    }

    [RelayCommand]
    private async Task ExportAll()
    {
        await ExportAllAsync();
    }

    [RelayCommand]
    private async Task ExportCard(FileCardViewModel? card)
    {
        if (card == null) return;

        var topLevel = App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null;

        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Export Directory",
            AllowMultiple = false
        });

        if (folders.Count == 0) return;

        var exportDir = folders[0].Path.LocalPath;
        LoggerHelper.Info($"[FileManager] Exporting card: {card.BaseName} → {exportDir}");

        try
        {
            if (!string.IsNullOrEmpty(card.PairedFtxPath))
                await ExportFtxAsync(card.PairedFtxPath, exportDir);
            else if (card.IsFtxFile)
                await ExportFtxAsync(card.FilePath, exportDir);

            if (!string.IsNullOrEmpty(card.PairedMbsPath))
                await ExportMbsAsync(card.PairedMbsPath, exportDir);
            else if (card.IsMbsFile)
                await ExportMbsAsync(card.FilePath, exportDir);

            LoggerHelper.Info($"[FileManager] Export complete: {card.BaseName}");
            ToastHelper.Success($"Exported {card.BaseName}");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Failed to export {card.FileName}: {ex.Message}");
            ToastHelper.Error($"Export failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClosePreview()
    {
        IsPreviewOpen = false;
        SelectedCard = null;
        PreviewImagePath = string.Empty;
    }

    [RelayCommand]
    private void OpenPreview(FileCardViewModel? card)
    {
        if (card == null) return;
        SelectedCard = card;
        if (card.ThumbnailPaths.Count > 0)
            PreviewImagePath = card.ThumbnailPaths[card.SelectedThumbnailIndex];
        IsPreviewOpen = true;
    }

    /// <summary>
    /// 在 Previewer 页面中打开卡片（导出临时文件 + 设置 Player + 跳转页面）
    /// </summary>
    [RelayCommand]
    private async Task PreviewInPlayer(FileCardViewModel? card)
    {
        if (card == null) return;

        LoggerHelper.Info($"[FileManager] Opening in Previewer: {card.BaseName}");

        try
        {
            var playerVm = Instances.ServiceProvider.GetRequiredService<PlayerViewModel>();
            var rootVm = Instances.ServiceProvider.GetRequiredService<RootViewModel>();

            // 清空 Player 旧数据
            playerVm.UnloadAndClear();

            var tempDir = Path.Combine(Path.GetTempPath(), "qts_player");
            Directory.CreateDirectory(tempDir);

            // 处理 MBS → 导出 Quad JSON
            string quadPath = string.Empty;
            if (!string.IsNullOrEmpty(card.PairedMbsPath) && File.Exists(card.PairedMbsPath))
                quadPath = await ExportMbsToTempAsync(card.PairedMbsPath, tempDir);
            else if (card.IsMbsFile && File.Exists(card.FilePath))
                quadPath = await ExportMbsToTempAsync(card.FilePath, tempDir);

            // 导出 FTX 图片到临时目录（后台线程）
            var imagePaths = new List<string>();
            var ftxPath = !string.IsNullOrEmpty(card.PairedFtxPath) ? card.PairedFtxPath
                        : card.IsFtxFile ? card.FilePath : string.Empty;

            if (!string.IsNullOrEmpty(ftxPath) && File.Exists(ftxPath))
            {
                imagePaths = await Task.Run(() => ExportFtxImages(ftxPath, tempDir, card.BaseName));
            }

            // 如果有 Quad 文件，设置到 Player
            if (!string.IsNullOrEmpty(quadPath) && File.Exists(quadPath))
            {
                playerVm.SetQuadFilePath(quadPath);
            }

            // 设置图片路径
            foreach (var imgPath in imagePaths)
                playerVm.ImagePaths.Add(imgPath);

            // 加载数据
            await playerVm.LoadAsync();

            // 跳转到 Previewer（index=2）
            rootVm.NavigateTo(2);

            ToastHelper.Success("Opened in Previewer", card.BaseName);
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Failed to open in Previewer: {ex.Message}");
            ToastHelper.Error("Preview failed", ex.Message);
        }
    }

    private async Task<string> ExportMbsToTempAsync(string mbsPath, string tempDir)
    {
        return await Task.Run(() =>
        {
            var fileData = File.ReadAllBytes(mbsPath);
            var tag = PlatformConfigs.DetectPlatform(fileData);
            if (tag == PlatformTag.Unknown) return string.Empty;

            var parser = new MbsToV55Parser();
            var v55Data = parser.Parse(fileData, tag);

            var converter = new V55ToQuadConverter();
            var quadData = converter.Convert(v55Data);

            var outputPath = Path.Combine(tempDir,
                Path.GetFileNameWithoutExtension(mbsPath) + ".quad.json");

            File.WriteAllText(outputPath, JsonConvert.SerializeObject(quadData, Formatting.Indented));
            return outputPath;
        });
    }

    /// <summary>
    /// 导出 FTX 文件中的所有图片为 PNG（后台线程执行）
    /// </summary>
    private List<string> ExportFtxImages(string ftxPath, string tempDir, string baseName)
    {
        var paths = new List<string>();
        try
        {
            var reader = new UnifiedFtexReader();
            var results = reader.ParseFile(ftxPath);

            for (int i = 0; i < results.Count; i++)
            {
                var outputPath = Path.Combine(tempDir, $"{baseName}_{i}.png");
                SaveAsPng(results[i], outputPath);
                if (File.Exists(outputPath))
                    paths.Add(outputPath);
            }

            LoggerHelper.Info($"[FileManager] Exported {paths.Count} images from {baseName}");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"[FileManager] Failed to export FTX images: {ex.Message}");
        }
        return paths;
    }

    public async Task ExportAllAsync()
    {
        if (FileCards.Count == 0) return;

        var topLevel = App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null;

        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Export Directory",
            AllowMultiple = false
        });

        if (folders.Count == 0) return;

        var exportDir = folders[0].Path.LocalPath;
        ExportOutputPath = exportDir;
        IsExporting = true;
        ExportProgress = 0;

        LoggerHelper.Info($"[FileManager] Exporting all to: {exportDir}");

        try
        {
            var totalFiles = FileCards.Count;
            var processedFiles = 0;

            foreach (var card in FileCards)
            {
                ExportProgress = (double)processedFiles / totalFiles * 100;

                try
                {
                    if (!string.IsNullOrEmpty(card.PairedFtxPath))
                        await ExportFtxAsync(card.PairedFtxPath, exportDir);
                    else if (card.IsFtxFile)
                        await ExportFtxAsync(card.FilePath, exportDir);

                    if (!string.IsNullOrEmpty(card.PairedMbsPath))
                        await ExportMbsAsync(card.PairedMbsPath, exportDir);
                    else if (card.IsMbsFile)
                        await ExportMbsAsync(card.FilePath, exportDir);

                    LoggerHelper.Debug($"[FileManager] Exported: {card.BaseName}");
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error($"Failed to export {card.FileName}: {ex.Message}");
                }

                processedFiles++;
            }

            ExportProgress = 100;
            LoggerHelper.Info($"[FileManager] Export complete: {processedFiles} files");
            ToastHelper.Success($"Export completed. {processedFiles} files processed.");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"Export failed: {ex.Message}");
            ToastHelper.Error($"Export failed: {ex.Message}");
        }
        finally
        {
            IsExporting = false;
        }
    }

    private async Task ExportFtxAsync(string ftxPath, string exportDir)
    {
        await Task.Run(() =>
        {
            var reader = new UnifiedFtexReader();
            reader.ParseAndSave(ftxPath, true, exportDir);
        });
    }

    private async Task ExportMbsAsync(string mbsPath, string exportDir)
    {
        await Task.Run(() =>
        {
            var fileData = File.ReadAllBytes(mbsPath);
            var tag = PlatformConfigs.DetectPlatform(fileData);

            if (tag == PlatformTag.Unknown) return;

            var parser = new MbsToV55Parser();
            var v55Data = parser.Parse(fileData, tag);

            var converter = new V55ToQuadConverter();
            var quadData = converter.Convert(v55Data);

            var outputPath = Path.Combine(exportDir,
                Path.GetFileNameWithoutExtension(mbsPath) + ".quad.json");

            var quadJson = JsonConvert.SerializeObject(quadData, Formatting.Indented);
            File.WriteAllText(outputPath, quadJson);
        });
    }

    /// <summary>
    /// 清空所有卡片并释放内存（含 Player 关联清理）
    /// </summary>
    [RelayCommand]
    private void ClearAll()
    {
        LoggerHelper.Info("[FileManager] Clearing all cards and resources");

        // 通知 PlayerViewModel 清理关联资源
        try
        {
            var playerVm = Instances.ServiceProvider.GetService<PlayerViewModel>();
            playerVm?.UnloadAndClear();
        }
        catch { /* Player 可能未注册 */ }

        DisposeAllCards();
        Cleanup();

        TotalCount = 0;
        PairedCount = 0;
        UnpairedCount = 0;
        SelectedFolderPath = string.Empty;
        ExportOutputPath = string.Empty;
        LoadStatus = string.Empty;
        LoadProgress = 0;

        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(IsAllSelected));

        LoggerHelper.Info("[FileManager] All cleared");
        ToastHelper.Success("All files cleared");
    }

    private void UpdateCounts()
    {
        TotalCount = FileCards.Count;
        PairedCount = FileCards.Count(c => c.IsPaired);
        UnpairedCount = FileCards.Count(c => !c.IsPaired);
    }

    /// <summary>
    /// 释放所有卡片的资源
    /// </summary>
    private void DisposeAllCards()
    {
        foreach (var card in FileCards)
            card.Dispose();
        FileCards.Clear();
    }

    public void Cleanup()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();

        foreach (var path in _tempFiles)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }
        _tempFiles.Clear();
    }

    public void Dispose()
    {
        DisposeAllCards();
        Cleanup();
    }
}
