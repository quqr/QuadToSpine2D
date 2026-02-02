namespace QTSAvalonia.Utilities;

public class InstanceSingleton
{
    private static InstanceSingleton? _instance;
    public static InstanceSingleton Instance
    {
        get
        {
            _instance ??= new InstanceSingleton();
            return _instance;
        }
    }

    public AvaloniaFilePickerService FilePickerService { get; set; }
}