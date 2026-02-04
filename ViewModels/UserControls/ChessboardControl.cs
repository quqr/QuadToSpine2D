// ChessboardBackgroundExtension.cs

using System;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace QTSAvalonia.ViewModels.UserControls;

public class ChessboardBackgroundExtension : MarkupExtension
{
    // 公开的属性，可以在 XAML 中设置
    public int CellSize { get; set; } = 20;

    public Color Color1 { get; set; } = Color.FromRgb(80, 80, 80);

    public Color Color2 { get; set; } = Color.FromRgb(110, 110, 110);

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return CreateChessboardBrush(CellSize, Color1, Color2);
    }

    public static DrawingBrush CreateChessboardBrush(int cellSize, Color color1, Color color2)
    {
        if (cellSize <= 0)
            cellSize = 20;

        // 创建一个 2x2 的棋盘格单元用于平铺
        var tileSize = cellSize * 2;

        // 创建绘制组
        var drawingGroup = new DrawingGroup();

        // 创建两个几何组，分别对应两种颜色
        var geometryGroup1 = new GeometryGroup();
        var geometryGroup2 = new GeometryGroup();

        // 左上角格子 - 使用 Color1
        geometryGroup1.Children.Add(new RectangleGeometry(
            new Rect(0, 0, cellSize, cellSize)));

        // 右下角格子 - 使用 Color1
        geometryGroup1.Children.Add(new RectangleGeometry(
            new Rect(cellSize, cellSize, cellSize, cellSize)));

        // 右上角格子 - 使用 Color2
        geometryGroup2.Children.Add(new RectangleGeometry(
            new Rect(cellSize, 0, cellSize, cellSize)));

        // 左下角格子 - 使用 Color2
        geometryGroup2.Children.Add(new RectangleGeometry(
            new Rect(0, cellSize, cellSize, cellSize)));

        // 创建绘制对象
        var drawing1 = new GeometryDrawing
        {
            Geometry = geometryGroup1, Brush = new SolidColorBrush(color1)
        };

        var drawing2 = new GeometryDrawing
        {
            Geometry = geometryGroup2, Brush = new SolidColorBrush(color2)
        };

        // 将绘制对象添加到组中
        drawingGroup.Children.Add(drawing1);
        drawingGroup.Children.Add(drawing2);

        // 创建 DrawingBrush
        return new DrawingBrush
        {
            Drawing = drawingGroup, TileMode = TileMode.Tile, DestinationRect = new RelativeRect(0, 0, tileSize, tileSize, RelativeUnit.Absolute), Stretch = Stretch.None
        };
    }
}