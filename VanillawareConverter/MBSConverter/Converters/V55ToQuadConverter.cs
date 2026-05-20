using System.Globalization;
using Newtonsoft.Json;
using VanillawareConverter.Mbs.Math;
using VanillawareConverter.Mbs.Models;

namespace VanillawareConverter.Mbs.Converters;

/// <summary>
/// V55格式到Quad格式的转换器
/// </summary>
/// <remarks>
/// 用于将Vanillaware游戏的MBS文件中的V55动画数据转换为Quad格式，
/// 以便导入到Spine2D动画软件中
/// </remarks>
public class V55ToQuadConverter
{
    private int _s4Flag;
    private int _s8Flag;
    private V55Data _v55 = new();

    /// <summary>
    /// 将V55数据转换为Quad格式数据
    /// </summary>
    /// <param name="v55">V55格式的动画数据</param>
    /// <returns>转换后的Quad格式数据</returns>
    public QuadData Convert(V55Data v55)
    {
        _v55 = v55;

        var quad = new QuadData
        {
            Blend = GetBlendModes(),
            Mix = GetMixingModes()
        };

        ProcessKeyframes(quad);
        var saList = ProcessAnimationList();
        ProcessSkeletons(quad, saList);

        return quad;
    }

    /// <summary>
    /// 从JSON字符串解析并转换V55数据
    /// </summary>
    /// <param name="jsonContent">包含V55数据的JSON字符串</param>
    /// <returns>转换后的Quad格式数据</returns>
    /// <exception cref="ArgumentException">当JSON内容无效时抛出</exception>
    public QuadData ConvertFromJson(string jsonContent)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        _v55 = JsonConvert.DeserializeObject<V55Data>(jsonContent, settings)
               ?? throw new ArgumentException("Invalid JSON content");
        return Convert(_v55);
    }

    private void ProcessKeyframes(QuadData quad)
    {
        for (var s6k = 0; s6k < _v55.S6.Count; s6k++)
        {
            var s6v = _v55.S6[s6k];
            if (s6v == null) continue;

            _s4Flag = ParseHex(s6v.Bits);

            KeyframeData? s4Data = null;
            HitboxData? s5Data = null;

            if (s6v.S4[1] > 0)
            {
                var layer = new List<KeyframeLayer?>();
                for (var i = 0; i < s6v.S4[1]; i++)
                {
                    var s4k = s6v.S4[0] + i;
                    if (s4k >= _v55.S4.Count) continue;
                    var s4v = _v55.S4[s4k];
                    if (s4v == null) continue;

                    _s4Flag = ParseHex(s4v.Bits);

                    if (CheckS4Skip()) continue;

                    var s2k = s4v.S0S1S2[2];
                    if (s2k >= _v55.S2.Count) continue;
                    var s2v = _v55.S2[s2k];

                    var layerData = new KeyframeLayer
                    {
                        Debug = [s4v.Bits, s4v.Blend, s4v.Color, s4v.Attr],
                        DstQuad = s2v?.DstQuad,
                        BlendId = s4v.Blend
                    };

                    var s0k = s4v.S0S1S2[0];
                    if (s0k >= 0 && s0k < _v55.S0.Count)
                    {
                        var s0v = _v55.S0[s0k];
                        if (s0v?.Fog != null)
                            layerData.FogQuad = s0v.Fog;
                    }

                    if (CheckS4Tex())
                    {
                        layerData.TexId = s4v.Tex;
                        var s1k = s4v.S0S1S2[1];
                        if (s1k >= 0 && s1k < _v55.S1.Count)
                        {
                            var s1v = _v55.S1[s1k];
                            layerData.SrcQuad = s1v?.SrcQuad;
                        }
                    }

                    var s4Attr = ParseHex(s4v.Attr);
                    if (s4Attr > 0)
                        layerData.Attribute = s4Attr;

                    if (s4v.Color > 0)
                        layerData.Colorize = $"COLOR_{s4v.Color:x}";

                    while (layer.Count <= i) layer.Add(null);
                    layer[i] = layerData;
                }

                s4Data = new KeyframeData
                {
                    Debug = s6v.Bits,
                    Name = $"keyframe {s6k}",
                    Layer = layer
                };
            }

            if (s6v.S5[1] > 0)
            {
                var layer = new List<HitboxLayer?>();
                for (var i = 0; i < s6v.S5[1]; i++)
                {
                    var s5k = s6v.S5[0] + i;
                    if (s5k >= _v55.S5.Count) continue;
                    var s5v = _v55.S5[s5k];
                    if (s5v == null) continue;

                    var s3k = s5v.S3;
                    if (s3k >= _v55.S3.Count) continue;
                    var s3v = _v55.S3[s3k];

                    var layerData = new HitboxLayer
                    {
                        Debug = s5v.Bits,
                        HitQuad = s3v?.Rect
                    };

                    while (layer.Count <= i) layer.Add(null);
                    layer[i] = layerData;
                }

                s5Data = new HitboxData
                {
                    Name = $"hitbox {s6k}",
                    Layer = layer
                };
            }

            if (s4Data != null)
            {
                while (quad.Keyframe.Count <= s6k) quad.Keyframe.Add(null);
                quad.Keyframe[s6k] = s4Data;
            }

            if (s5Data != null)
            {
                while (quad.Hitbox.Count <= s6k) quad.Hitbox.Add(null);
                quad.Hitbox[s6k] = s5Data;
            }

            if (s4Data != null && s5Data != null)
            {
                var slot = new SlotData
                {
                    Attach =
                    [
                        new { type = "keyframe", id = s6k },
                        new { type = "hitbox", id = s6k }
                    ]
                };
                while (quad.Slot.Count <= s6k) quad.Slot.Add(null);
                quad.Slot[s6k] = slot;
            }
        }
    }

    private List<AnimationTimeData> ProcessAnimationList()
    {
        var saList = new List<AnimationTimeData>();

        for (var sak = 0; sak < _v55.Sa.Count; sak++)
        {
            var sav = _v55.Sa[sak];
            if (sav == null) continue;

            var time = new List<S8AnimFrame?>();
            var line = new Dictionary<int, int>();
            var loop = -1;
            var i = 0;

            while (true)
            {
                var s8k = sav.S8[0] + i;
                if (s8k >= _v55.S8.Count) break;
                var s8v = _v55.S8[s8k];
                if (s8v == null) break;

                _s8Flag = ParseHex(s8v.Bits);

                if (!line.ContainsKey(s8k))
                {
                    line[s8k] = time.Count;
                    time.Add(s8v);

                    if (CheckS8Jump())
                    {
                        i = s8v.Loop;
                    }
                    else
                    {
                        if (CheckS8Last())
                            break;
                        i++;
                    }
                }
                else
                {
                    loop = line[s8k];
                    break;
                }
            }

            while (saList.Count <= sak) saList.Add(null!);
            saList[sak] = new AnimationTimeData { Time = time, Loop = loop };
        }

        return saList;
    }

    private void ProcessSkeletons(QuadData quad, List<AnimationTimeData> saList)
    {
        for (var s9k = 0; s9k < _v55.S9.Count; s9k++)
        {
            var s9v = _v55.S9[s9k];
            if (s9v == null) continue;
            if (s9v.Sa[1] < 1) continue;

            var bone = new List<BoneData?>();

            for (var i = 0; i < s9v.Sa[1]; i++)
            {
                var sak = s9v.Sa[0] + i;
                if (sak >= saList.Count || saList[sak] == null) continue;

                var time = new List<TimelineEntry>();
                foreach (var s8v in saList[sak].Time)
                {
                    if (s8v == null) continue;

                    _s8Flag = ParseHex(s8v.Bits);

                    var s6k = s8v.S6;
                    object? attach = null;
                    if (quad.Slot != null && s6k < quad.Slot.Count && quad.Slot[s6k] != null)
                        attach = new { type = "slot", id = s6k };
                    else if (quad.Keyframe != null && s6k < quad.Keyframe.Count && quad.Keyframe[s6k] != null)
                        attach = new { type = "keyframe", id = s6k };
                    else if (quad.Hitbox != null && s6k < quad.Hitbox.Count && quad.Hitbox[s6k] != null)
                        attach = new { type = "hitbox", id = s6k };

                    var flipX = CheckS8FlipX();
                    var flipY = CheckS8FlipY();

                    var s7k = s8v.S7;
                    var matrix = CalculateMatrix(s7k, flipX, flipY);

                    var entry = new TimelineEntry
                    {
                        Debug = s8v.Bits,
                        Time = s8v.Time,
                        MatrixMixId = s8v.InS7,
                        ColorMixId = s8v.InS7,
                        DstquadMixId = s8v.InS6,
                        FogquadMixId = s8v.InS6,
                        HitquadMixId = s8v.InS5S3
                    };

                    if (attach != null)
                        entry.Attach = attach;

                    if (s7k >= 0 && s7k < _v55.S7.Count)
                    {
                        var s7v = _v55.S7[s7k];
                        if (s7v?.Fog != "#ffffffff")
                            entry.Color = s7v.Fog;
                    }

                    if (matrix != null)
                        entry.Matrix = matrix;

                    time.Add(entry);
                }

                var anim = new AnimationData
                {
                    Name = $"animation {sak}",
                    Timeline = time,
                    LoopId = saList[sak].Loop
                };

                while (quad.Animation.Count <= sak) quad.Animation.Add(null);
                quad.Animation[sak] = anim;

                bone.Add(new BoneData
                {
                    Attach = new { type = "animation", id = sak }
                });
            }

            var skel = new SkeletonData
            {
                Name = s9v.Name,
                Bone = bone
            };

            while (quad.Skeleton.Count <= s9k) quad.Skeleton.Add(null);
            quad.Skeleton[s9k] = skel;
        }
    }

    private double[]? CalculateMatrix(int s7k, bool flipX, bool flipY)
    {
        if (s7k < 0 || s7k >= _v55.S7.Count) return null;
        var s7 = _v55.S7[s7k];
        if (s7 == null) return null;

        double bx = flipX ? -1 : 1;
        double by = flipY ? -1 : 1;

        var m = Matrix4x4.Scale(s7.Scale[0] * bx, s7.Scale[1] * by);

        var t = Matrix4x4.RotateZ(s7.Rotate[2]);
        m = m * t;

        if (s7.Rotate[1] != 0)
        {
            t = Matrix4x4.RotateY(s7.Rotate[1]);
            m = m * t;
        }

        if (s7.Rotate[0] != 0)
        {
            t = Matrix4x4.RotateX(s7.Rotate[0]);
            m = m * t;
        }

        m = m * Matrix4x4.Translate(s7.Move[0] * bx, s7.Move[1] * by, s7.Move[2]);

        if (m.IsIdentity()) return null;
        return m.ToArray();
    }

    private bool CheckS4Skip()
    {
        return (_s4Flag & 0x02) != 0;
    }

    private bool CheckS4Tex()
    {
        return (_s4Flag & 0x04) == 0;
    }

    private bool CheckS8Last()
    {
        return (_s8Flag & 0x800) != 0;
    }

    private bool CheckS8Jump()
    {
        return (_s8Flag & 0x04) != 0;
    }

    private bool CheckS8FlipX()
    {
        return (_s8Flag & 0x01) != 0;
    }

    private bool CheckS8FlipY()
    {
        return (_s8Flag & 0x02) != 0;
    }

    private static int ParseHex(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return 0;
        if (hex.StartsWith("0x") || hex.StartsWith("0X"))
            hex = hex.Substring(2);
        return int.TryParse(hex, NumberStyles.HexNumber, null, out var result) ? result : 0;
    }

    private List<BlendMode?> GetBlendModes()
    {
        var blend = new List<BlendMode?>();
        var tag = _v55.Tag;

        if (tag is "ps2_grim" or "ps2_odin")
        {
            while (blend.Count < 1) blend.Add(null);
            blend[0] = new BlendMode { Debug = "44 = 0101" };
            while (blend.Count < 2) blend.Add(null);
            blend[1] = new BlendMode { Debug = "48 = 0201" };
            while (blend.Count < 3) blend.Add(null);
            blend[2] = new BlendMode { Debug = "42 = 2001" };
        }
        else
        {
            while (blend.Count < 1) blend.Add(null);
            blend[0] = new BlendMode { Debug = "normal" };
        }

        return blend;
    }

    private List<string?> GetMixingModes()
    {
        var mix = new List<string?> { "", "", "" };
        var tag = _v55.Tag;

        if (tag is "ps2_grim" or "ps2_odin")
        {
            mix[1] = "LINEAR";
            mix[2] = "CATMULL ROM";
        }
        else
        {
            mix[1] = "LINEAR";
            mix[2] = "LINEAR";
        }

        return mix;
    }
}

internal class AnimationTimeData
{
    public List<S8AnimFrame?> Time { get; set; } = [];
    public int Loop { get; set; }
}