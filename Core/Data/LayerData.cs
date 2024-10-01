namespace QuadToSpine2D.Core.Data;

public class LayerData
{
    public KeyframeLayer KeyframeLayer          { get; set; }
    public string        SlotAndImageName       { get; set; }
    public string        BaseSkinAttackmentName { get; set; }
    public string        SkinName               { get; set; }
    public int           ImageIndex             { get; set; }
    public int           SkinIndex              { get; set; }
    public int           TexId                  { get; set; }
    public int           CopyIndex              { get; set; }
}