namespace QuadPlayer;

public class JsonAttribute: Attribute
{
    public bool IsReplacedByValue { get; set; }
    public string Value { get; set; }
    public JsonAttribute()
    {
        this.IsReplacedByValue = false;
    }
}