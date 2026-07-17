namespace QTSCore.Data.Quad;

public class Blend
{
    public string Name { get; set; } = string.Empty;
    public string[] ModeRgb { get; set; } = [];
    public string[] ModeAlpha { get; set; } = [];
    public string Color { get; set; } = string.Empty;

    [JsonIgnore] public string[] LogicOp { get; set; } = [];
}