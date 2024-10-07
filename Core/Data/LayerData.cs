namespace QuadToSpine2D.Core.Data;

public class LayerData
{
    private readonly string        _texId;
    public           KeyframeLayer KeyframeLayer          { get; init; }
    public           string        SlotAndImageName       { get; init; }
    public           string        BaseSkinAttachmentName { get; init; }
    public           string        SkinName               { get; set; }
    public           int           ImageIndex             { get; init; }
    public           int           SkinIndex              { get; set; }
    public           int           CopyIndex              { get; init; }
    public           int           BlendId                => KeyframeLayer.BlendId;

    public string TexId
    {
        get => _texId;
        init
        {
            _texId                 = value.Equals(GlobalData.FogTexId.ToString()) ? "Fog" : value;
            BaseSkinAttachmentName = $"Slice_{ImageIndex}_{_texId}_0_{CopyIndex}";
        }
    }
}