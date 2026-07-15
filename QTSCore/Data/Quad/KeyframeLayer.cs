using QTSAvalonia.Helper;
using QTSCore.JsonConverters;
using QTSCore.Utility;
using Matrix = QTSCore.Utility.Matrix;

namespace QTSCore.Data.Quad;

[JsonConverter(typeof(KeyframeLayerJsonConverter))]
public class KeyframeLayer : Attach
{
    private float[]? _srcquad = [];
    private int _texId = -1;
    private string _name = string.Empty;
    private Timeline[] _timeline = [];

    public float[] Dstquad
    {
        get;
        set
        {
            DstMatrix = new Matrix(4, 2, value);
            //Y is down, so we need to flip it to up
            _dstquad = value;
        }
    } = [];

    private float[]? _dstquad;
    public Matrix DstMatrix { get; set; }

    public float[]? Srcquad
    {
        get => _srcquad;
        set
        {
            _srcquad = value;
            if (_srcquad is null || _srcquad.Length < 8)
            {
                Guid = $"Fog_{Fog[0]}_{Fog[1]}_{Fog[2]}_{Fog[3]}";
                return;
            }

            CalculateGuid();
            CalculateUVs(_srcquad);

            SrcX = MinAndMaxSrcPoints[0];
            SrcY = MinAndMaxSrcPoints[1];
        }
    }

    public int ImageNameOrder { get; set; }
    public int BlendId { get; set; }

    public int TexId
    {
        get => _texId;
        set
        {
            if (value >= -1)
            {
                _texId = value;
                return;
            }
            // fog tex id
            _texId = Instances.ConverterSetting.FogTexId;
        }
    }

    public string Guid { get; set; } = string.Empty;
    public float Height { get; set; }
    public float Width { get; set; }
    public float SrcX { get; set; }
    public float SrcY { get; set; }
    public float[] MinAndMaxSrcPoints { get; set; } = new float[8];
    public float[] UVs { get; set; } = new float[8];
    public float[] ZeroCenterPoints { get; set; } = new float[8];
    public string LayerName { get; set; } = string.Empty;
    public string[] Fog { get; set; } = [];
    public string[] Attribute { get; set; } = [];
    public string Colorize { get; set; } = string.Empty;

    private void CalculateGuid()
    {
        MinAndMaxSrcPoints = MathHelper.FindMinAndMaxPoints(_srcquad);
        Width = MinAndMaxSrcPoints[2] - MinAndMaxSrcPoints[0];
        Height = MinAndMaxSrcPoints[3] - MinAndMaxSrcPoints[1];
        Guid = $"{TexId}_{_srcquad!
            .Select((t, i) => t * 3.7 / 7.3 + t * i * 97311397.135f / 773377.2746f)
            .Sum()}";
    }

    /// <summary>
    ///     recalculate UVs
    /// </summary>
    private void CalculateUVs(float[] src)
    {
        List<Vector3> points =
        [
            new(src[0], src[1], 0),
            new(src[2], src[3], 1),
            new(src[4], src[5], 2),
            new(src[6], src[7], 3)
        ];
        Vector2[] uvs = [new(0, 0), new(0, 1), new(1, 0), new(1, 1)];
        var orderPoints = points.OrderBy(a => a.X).ThenBy(b => b.Y).ToList();
        for (var i = 0; i < 4; i++)
        {
            UVs[(int)orderPoints[i].Z * 2] = uvs[i].X;
            UVs[(int)orderPoints[i].Z * 2 + 1] = uvs[i].Y;
        }

        //calculate ZeroCenterPoints, make sure it's in spine2D center in layer picture
        for (var i = 0; i < UVs.Length; i++)
            if (i % 2 == 0)
                ZeroCenterPoints[i] = (UVs[i] * 2f - 1f) * Width / 8f;
            else
                ZeroCenterPoints[i] = (UVs[i] * 2f - 1f) * Height / 8f;
    }
}
