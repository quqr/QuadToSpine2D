namespace QuadToSpine2D.Core.Data;

public class LayerData
{
    public string ImageName { get; set; }
    public string SkinName { get; set; }
    public KeyframeLayer KeyframeLayer { get; set; }
    public int ImageIndex { get; set; }
    public int SkinIndex { get; set; }
    public int TexId { get; set; }
    public int CopyIndex { get; set; }
    private int[] _imageNameData = new int[4];
    public int[] ImageNameData
    {
        get => _imageNameData;
        set
        {
            _imageNameData = value;
            ImageName = $"Slice_{value[0]}_{value[1]}_{value[2]}_{value[3]}";
        } 
    }
}