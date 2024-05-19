namespace QuadPlayer;

public class Singleton<T>
{
    private static Singleton<T> instance;
    public static Singleton<T> Instance => instance ??= new Singleton<T>();
}