namespace QTSAvalonia.Utilities;

public class Singleton<T>  where T : new() 
{
    private static Singleton<T>? _instance;
    public static Singleton<T> Instance
    {
        get
        {
            _instance ??= new Singleton<T>();
            return _instance;
        }
    }
}