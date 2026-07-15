namespace QTSCore.Data.Quad;

public class Animation : Attach
{
    private string _name = string.Empty;
    private Timeline[] _timeline = [];
    private int _loopId;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            var splitName = _name.Split(' ');
            // avoid "ALL KEYFRAMES"
            if (!splitName[0].Equals("animation")) return;
            Id = Convert.ToInt32(splitName[^1]);
            AttachType = AttachType.Animation;
        }
    }

    public Timeline[] Timeline
    {
        get => _timeline;
        set
        {
            for (var i = 0; i < value.Length; i++)
            {
                value[i].Prev = i > 0 ? value[i - 1] : null;
                value[i].Next = i < value.Length - 1 ? value[i + 1] : null;
            }

            _timeline = value;
        }
    }

    public bool IsLoop { get; set; }

    [JsonProperty]
    public int LoopId
    {
        get => _loopId;
        set
        {
                IsLoop = value >= 0;
                _loopId = value;
        }
    }
}
