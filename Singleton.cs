namespace QuadPlayer;

public class Singleton<T>
{
    private static Singleton<T> _instance;
    public static Singleton<T> Instance => _instance ??= new Singleton<T>();
}