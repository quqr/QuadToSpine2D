using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using QTSAvalonia.ViewModels.Pages;
using SkiaSharp;
using VanillawareConverter.Ftex;

namespace QTSAvalonia.ViewModels.UserControls;

public partial class FileCardViewModel : ViewModelBase, IDisposable
{
    private const int ThumbnailMaxSize = 512;

    private static readonly ImmutableSolidColorBrush BlueBrush = new(Color.Parse("#2196F3"));
    private static readonly ImmutableSolidColorBrush GreenBrush = new(Color.Parse("#4CAF50"));
    private static readonly ImmutableSolidColorBrush OrangeBrush = new(Color.Parse("#FF9800"));

    [ObservableProperty] private int _animationCount;

    [ObservableProperty] private List<string> _animationNames = [];

    private int _cachedIndex = -1;
    private Task? _currentDecodeTask;
    private CancellationTokenSource? _decodeCts;

    [ObservableProperty] private string _fileName = string.Empty;

    [ObservableProperty] private string _filePath = string.Empty;

    /// <summary>FTX 源文件路径</summary>
    private string _ftxSourcePath = string.Empty;

    [ObservableProperty] private int _imageCount;

    /// <summary>
    ///     详情面板是否展开（Popover）
    /// </summary>
    [ObservableProperty] private bool _isDetailsOpen;

    [ObservableProperty] private bool _isFtxFile;

    [ObservableProperty] private bool _isMbsFile;

    [ObservableProperty] private bool _isPaired;

    [ObservableProperty] private bool _isSelected;

    /// <summary>
    ///     缩略图是否正在异步加载中
    /// </summary>
    [ObservableProperty] private bool _isThumbnailLoading;

    [ObservableProperty] private string _pairedFtxPath = string.Empty;

    [ObservableProperty] private string _pairedMbsPath = string.Empty;

    [ObservableProperty] private int _selectedThumbnailIndex;

    [ObservableProperty] private string _statusText = string.Empty;

    // Bitmap 缓存（只由后台线程写入，UI 线程读取）

    [ObservableProperty] private List<string> _thumbnailPaths = [];

    public FileCardViewModel()
    {
    }

    public FileCardViewModel(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        IsFtxFile = ext is ".ftx" or ".ftp";
        IsMbsFile = ext is ".mbs" or ".mbp";
    }

    public FileManagerViewModel? Parent { get; set; }

    public string BaseName => Path.GetFileNameWithoutExtension(FileName);

    public string DisplayName => IsPaired ? $"{BaseName} (FTX+MBS)"
        : IsFtxFile ? $"{BaseName} (FTX)"
        : IsMbsFile ? $"{BaseName} (MBS)"
        : FileName;

    public IBrush AccentColor => IsSelected ? BlueBrush : IsPaired ? GreenBrush : OrangeBrush;

    public bool HasThumbnails => !string.IsNullOrEmpty(_ftxSourcePath) || ThumbnailPaths.Count > 0;
    public bool HasMultipleImages => ImageCount > 1;
    public bool HasAnimations => AnimationCount > 0;

    public string ThumbnailIndicatorText =>
        ImageCount > 0 ? $"{SelectedThumbnailIndex + 1}/{ImageCount}" : "";

    public string StatsText
    {
        get
        {
            var parts = new List<string>();
            if (IsFtxFile || (IsPaired && PairedFtxPath != string.Empty))
                parts.Add($"{ImageCount} images");
            if (IsMbsFile || (IsPaired && PairedMbsPath != string.Empty))
                parts.Add($"{AnimationCount} anims");
            return parts.Count > 0 ? string.Join(" · ", parts) : "No data";
        }
    }

    /// <summary>
    ///     缩略图 Bitmap（纯读取缓存，绝不触发解码，不阻塞 UI 线程）
    /// </summary>
    public Bitmap? ThumbnailBitmap { get; private set; }

    // ─── Dispose ─────────────────────────────────────────

    public void Dispose()
    {
        _decodeCts?.Cancel();
        _decodeCts?.Dispose();
        ClearBitmapCache();
        Parent = null;
    }

    public List<string> GetExportPaths()
    {
        var paths = new List<string>();
        if (!string.IsNullOrEmpty(PairedFtxPath) && File.Exists(PairedFtxPath)) paths.Add(PairedFtxPath);
        if (!string.IsNullOrEmpty(PairedMbsPath) && File.Exists(PairedMbsPath)) paths.Add(PairedMbsPath);
        if (paths.Count == 0 && File.Exists(FilePath)) paths.Add(FilePath);
        return paths;
    }

    // ─── 缩略图加载 ──────────────────────────────────────

    /// <summary>
    ///     设置 FTX 源路径并触发首次异步加载
    /// </summary>
    public void SetFtxSource(string ftxPath)
    {
        _ftxSourcePath = ftxPath;
        ClearBitmapCache();
        SelectedThumbnailIndex = 0;
        NotifyThumbnailPropsChanged();
        TriggerAsyncLoad(); // 触发后台解码
    }

    public void LoadThumbnails(List<string> paths)
    {
        ClearBitmapCache();
        ThumbnailPaths = paths;
        ImageCount = paths.Count;
        SelectedThumbnailIndex = 0;
        NotifyThumbnailPropsChanged();
        // 有预生成 PNG 路径时直接从路径加载（快速）
        if (paths.Count > 0)
            LoadFromPngPath(paths[0]);
    }

    /// <summary>
    ///     从 PNG 路径加载到缓存（用于 PreviewInPlayer 等已有 PNG 的场景）
    /// </summary>
    private void LoadFromPngPath(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                ThumbnailBitmap?.Dispose();
                ThumbnailBitmap = new Bitmap(path);
                _cachedIndex = 0;
                OnPropertyChanged(nameof(ThumbnailBitmap));
            }
        }
        catch
        {
            /* 文件可能尚未就绪 */
        }
    }

    /// <summary>
    ///     异步加载当前索引的缩略图（后台线程解码，完成后通知 UI）
    /// </summary>
    public void TriggerAsyncLoad()
    {
        if (string.IsNullOrEmpty(_ftxSourcePath) || !File.Exists(_ftxSourcePath)) return;
        if (ImageCount <= 0) return;

        var targetIdx = Math.Clamp(SelectedThumbnailIndex, 0, ImageCount - 1);

        // 已有缓存则跳过
        if (ThumbnailBitmap != null && _cachedIndex == targetIdx) return;

        // 取消之前的未完成任务
        _decodeCts?.Cancel();
        _decodeCts = new CancellationTokenSource();

        var idx = targetIdx;
        var ftxPath = _ftxSourcePath;
        var cts = _decodeCts.Token;

        IsThumbnailLoading = true;

        _currentDecodeTask = Task.Run(() =>
        {
            try
            {
                return DecodeFtxFrame(ftxPath, idx);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"[FileCard] Async decode failed ({idx}): {ex.Message}");
                return null;
            }
        }, cts).ContinueWith(t =>
        {
            if (cts.IsCancellationRequested) return;

            var bitmap = t.Result;
            Dispatcher.UIThread.Post(() =>
            {
                if (cts.IsCancellationRequested) return;

                ThumbnailBitmap?.Dispose();
                ThumbnailBitmap = bitmap;
                _cachedIndex = idx;
                IsThumbnailLoading = false;
                OnPropertyChanged(nameof(ThumbnailBitmap));
            });
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    /// <summary>
    ///     FTX 帧解码（必须在后台线程调用）
    /// </summary>
    private static Bitmap? DecodeFtxFrame(string ftxPath, int frameIndex)
    {
        var reader = new UnifiedFtexReader();
        var results = reader.ParseFile(ftxPath);
        if (frameIndex < 0 || frameIndex >= results.Count) return null;

        var result = results[frameIndex];
        var maxDim = Math.Max(result.Width, result.Height);
        var scale = maxDim > ThumbnailMaxSize ? (double)ThumbnailMaxSize / maxDim : 1.0;
        var newW = Math.Max(1, (int)(result.Width * scale));
        var newH = Math.Max(1, (int)(result.Height * scale));

        using var srcBitmap = new SKBitmap(result.Width, result.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var pixels = new byte[result.Width * result.Height * 4];
        var minLen = Math.Min(pixels.Length, result.PixelData.Length);
        Array.Copy(result.PixelData, pixels, minLen);
        Marshal.Copy(pixels, 0, srcBitmap.GetPixels(), Math.Min(pixels.Length, result.Width * result.Height * 4));

        using var dstBitmap = srcBitmap.Resize(new SKImageInfo(newW, newH), SKFilterQuality.Medium);
        using var image = SKImage.FromBitmap(dstBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        using var ms = new MemoryStream(data.ToArray());
        return new Bitmap(ms);
    }

    private void ClearBitmapCache()
    {
        ThumbnailBitmap?.Dispose();
        ThumbnailBitmap = null;
        _cachedIndex = -1;
    }

    private void NotifyThumbnailPropsChanged()
    {
        OnPropertyChanged(nameof(HasThumbnails));
        OnPropertyChanged(nameof(HasMultipleImages));
        OnPropertyChanged(nameof(ThumbnailIndicatorText));
        OnPropertyChanged(nameof(StatsText));
        OnPropertyChanged(nameof(ThumbnailBitmap));
    }

    // ─── 动画信息 ──────────────────────────────────────

    public void LoadAnimationInfo(int count, List<string> names)
    {
        AnimationCount = count;
        AnimationNames = names;
        OnPropertyChanged(nameof(HasAnimations));
        OnPropertyChanged(nameof(StatsText));
    }

    // ─── 图片切换 ──────────────────────────────────────

    [RelayCommand]
    private void NextThumbnail()
    {
        if (ImageCount <= 1) return;
        SelectedThumbnailIndex = (SelectedThumbnailIndex + 1) % ImageCount;
    }

    [RelayCommand]
    private void PreviousThumbnail()
    {
        if (ImageCount <= 1) return;
        SelectedThumbnailIndex = (SelectedThumbnailIndex - 1 + ImageCount) % ImageCount;
    }

    partial void OnSelectedThumbnailIndexChanged(int value)
    {
        ClearBitmapCache();
        NotifyThumbnailPropsChanged();
        TriggerAsyncLoad(); // 切换图片时触发异步加载新帧
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(AccentColor));
        Parent?.NotifyIsAllSelectedChanged();
    }

    partial void OnIsPairedChanged(bool value)
    {
        OnPropertyChanged(nameof(AccentColor));
        OnPropertyChanged(nameof(StatsText));
    }

    // ─── 详情 Popover ──────────────────────────────────

    [RelayCommand]
    private void ToggleDetails()
    {
        IsDetailsOpen = !IsDetailsOpen;
    }

    // ─── 配对 ───────────────────────────────────────────

    public void PairWith(string ftxPath, string mbsPath)
    {
        IsPaired = true;
        PairedFtxPath = ftxPath;
        PairedMbsPath = mbsPath;
        StatusText = "Paired";
    }

    public void MarkAsUnpaired()
    {
        IsPaired = false;
        StatusText = "Unpaired";
    }
}