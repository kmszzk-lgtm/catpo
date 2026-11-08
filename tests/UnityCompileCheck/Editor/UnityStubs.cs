// Unity 없이 에디터 메뉴를 컴파일하고 실행해 보기 위한 흉내. 메뉴가 쓰는 것만 둔다.
namespace UnityEngine
{
    public static class Debug
    {
        public static void Log(object message) => System.Console.WriteLine(message);
        public static void LogError(object message) => System.Console.WriteLine("ERROR: " + message);
        public static void LogException(System.Exception e) => System.Console.WriteLine("EXCEPTION: " + e);
    }
}

namespace UnityEditor
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class MenuItem : System.Attribute
    {
        public MenuItem(string path) { }
    }
}

public static class Program
{
    public static void Main() => Catpo.EditorTools.CoreCheckMenu.Run();
}
