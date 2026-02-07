using Avalonia.Media;

namespace QTSAvalonia.ViewModels.UserControls;


public class ChessboardBackgroundExtension : MarkupExtension
{
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


        var tileSize = cellSize * 2;


        var drawingGroup = new DrawingGroup();


        var geometryGroup1 = new GeometryGroup();
        var geometryGroup2 = new GeometryGroup();


        geometryGroup1.Children.Add(new RectangleGeometry(
            new Rect(0, 0, cellSize, cellSize)));


        geometryGroup1.Children.Add(new RectangleGeometry(
            new Rect(cellSize, cellSize, cellSize, cellSize)));

        geometryGroup2.Children.Add(new RectangleGeometry(
            new Rect(cellSize, 0, cellSize, cellSize)));

        geometryGroup2.Children.Add(new RectangleGeometry(
            new Rect(0, cellSize, cellSize, cellSize)));

        var drawing1 = new GeometryDrawing
        {
            Geometry = geometryGroup1, Brush = new SolidColorBrush(color1)
        };

        var drawing2 = new GeometryDrawing
        {
            Geometry = geometryGroup2, Brush = new SolidColorBrush(color2)
        };

        drawingGroup.Children.Add(drawing1);
        drawingGroup.Children.Add(drawing2);

        return new DrawingBrush
        {
            Drawing = drawingGroup, TileMode = TileMode.Tile, DestinationRect = new RelativeRect(0, 0, tileSize, tileSize, RelativeUnit.Absolute), Stretch = Stretch.None
        };
    }
}