// Unity 없이 컴파일하고 돌려 보기 위한 흉내. 실제 Unity API와 이름·모양을 맞추고, 우리 코드가 쓰는 것만 둔다.
// 실제 Unity에 없는 것을 여기 만들어 쓰면 Unity에서 오류가 나니, 새로 쓰는 API는 Unity 문서와 맞춰서 추가한다.
using System;
using System.Collections.Generic;

namespace CatpoStubControl
{
    /// <summary>확인 프로그램이 화면을 "눌러 보는" 데 쓴다. Unity에는 없다.</summary>
    public static class Ui
    {
        public static readonly Queue<string> Clicks = new Queue<string>();
        public static readonly List<string> Drawn = new List<string>();
        public static int ScreenWidth = 1920;
        public static int ScreenHeight = 1080;
        public static int Errors;
        private static readonly Stack<string> groups = new Stack<string>();

        public static void BeginFrame()
        {
            Drawn.Clear();
            groups.Clear();
        }

        public static void EndFrame()
        {
            if (groups.Count != 0)
                throw new InvalidOperationException("GUILayout Begin/End 짝이 안 맞음: " + string.Join(", ", groups));
        }

        internal static void Push(string group) => groups.Push(group);

        internal static void Pop(string group)
        {
            if (groups.Count == 0 || groups.Peek() != group)
                throw new InvalidOperationException($"GUILayout.End{group}()가 Begin{group}()과 짝이 안 맞음");
            groups.Pop();
        }

        internal static void Label(string text) => Drawn.Add(text);

        internal static bool Button(string text)
        {
            Drawn.Add(text);
            if (Clicks.Count > 0 && text.StartsWith(Clicks.Peek(), StringComparison.Ordinal))
            {
                Clicks.Dequeue();
                return true;
            }
            return false;
        }
    }
}

namespace UnityEngine
{
    using CatpoStubControl;

    public class Object
    {
        public static void DontDestroyOnLoad(Object target) { }
    }

    public class Component : Object { }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }

    public sealed class GameObject : Object
    {
        public GameObject(string name) { }
        public T AddComponent<T>() where T : Component => null;
    }

    public enum RuntimeInitializeLoadType
    {
        AfterSceneLoad = 0,
        BeforeSceneLoad = 1,
        AfterAssembliesLoaded = 2,
        BeforeSplashScreen = 3,
        SubsystemRegistration = 4,
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }

    public static class Debug
    {
        public static void Log(object message) => Console.WriteLine(message);
        public static void LogWarning(object message) => Console.WriteLine("WARNING: " + message);
        public static void LogError(object message) { Ui.Errors++; Console.WriteLine("ERROR: " + message); }
        public static void LogException(Exception exception) { Ui.Errors++; Console.WriteLine("EXCEPTION: " + exception); }
    }

    public static class Time
    {
        public static float deltaTime => 0.02f;
    }

    public static class Screen
    {
        public static int width => Ui.ScreenWidth;
        public static int height => Ui.ScreenHeight;
    }

    public struct Vector2
    {
        public float x;
        public float y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
    }

    public struct Quaternion
    {
        public static Quaternion identity => default;
    }

    public struct Matrix4x4
    {
        public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s) => default;
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height)
        {
            this.x = x; this.y = y; this.width = width; this.height = height;
        }
        public float x { get; set; }
        public float y { get; set; }
        public float width { get; set; }
        public float height { get; set; }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) : this(r, g, b, 1f) { }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1, 1);
    }

    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }

    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }

    public sealed class Font : Object
    {
        public static Font CreateDynamicFontFromOSFont(string[] fontnames, int size) => new Font();
    }

    public sealed class RectOffset
    {
        public RectOffset() { }
        public RectOffset(int left, int right, int top, int bottom) { }
    }

    public sealed class GUIStyleState
    {
        public Color textColor { get; set; }
    }

    public sealed class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public bool wordWrap { get; set; }
        public bool richText { get; set; }
        public TextAnchor alignment { get; set; }
        public GUIStyleState normal { get; set; } = new GUIStyleState();
        public RectOffset padding { get; set; } = new RectOffset();
        public RectOffset margin { get; set; } = new RectOffset();
    }

    public sealed class GUISkin : Object
    {
        public Font font { get; set; }
        public GUIStyle label { get; set; } = new GUIStyle();
        public GUIStyle button { get; set; } = new GUIStyle();
        public GUIStyle box { get; set; } = new GUIStyle();
    }

    public sealed class GUILayoutOption
    {
        internal GUILayoutOption() { }
    }

    public class GUI
    {
        public static GUISkin skin { get; set; } = new GUISkin();
        public static Matrix4x4 matrix { get; set; }
        public static Color color { get; set; } = Color.white;
        public static Color backgroundColor { get; set; } = Color.white;
        public static Color contentColor { get; set; } = Color.white;
        public static bool enabled { get; set; } = true;
        public static void Box(Rect position, string text) { }
        public static void Label(Rect position, string text) => Ui.Label(text);
        public static bool Button(Rect position, string text) => Ui.Button(text);
    }

    public class GUILayout
    {
        public static void BeginArea(Rect screenRect) => Ui.Push("Area");
        public static void EndArea() => Ui.Pop("Area");
        public static void BeginHorizontal(params GUILayoutOption[] options) => Ui.Push("Horizontal");
        public static void BeginHorizontal(GUIStyle style, params GUILayoutOption[] options) => Ui.Push("Horizontal");
        public static void EndHorizontal() => Ui.Pop("Horizontal");
        public static void BeginVertical(params GUILayoutOption[] options) => Ui.Push("Vertical");
        public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options) => Ui.Push("Vertical");
        public static void EndVertical() => Ui.Pop("Vertical");

        public static Vector2 BeginScrollView(Vector2 scrollPosition, params GUILayoutOption[] options)
        {
            Ui.Push("ScrollView");
            return scrollPosition;
        }

        public static void EndScrollView() => Ui.Pop("ScrollView");

        public static void Label(string text, params GUILayoutOption[] options) => Ui.Label(text);
        public static void Label(string text, GUIStyle style, params GUILayoutOption[] options) => Ui.Label(text);
        public static bool Button(string text, params GUILayoutOption[] options) => Ui.Button(text);
        public static bool Button(string text, GUIStyle style, params GUILayoutOption[] options) => Ui.Button(text);
        public static void Space(float pixels) { }
        public static void FlexibleSpace() { }

        public static GUILayoutOption Width(float width) => new GUILayoutOption();
        public static GUILayoutOption Height(float height) => new GUILayoutOption();
        public static GUILayoutOption MinWidth(float minWidth) => new GUILayoutOption();
        public static GUILayoutOption MinHeight(float minHeight) => new GUILayoutOption();
        public static GUILayoutOption ExpandWidth(bool expand) => new GUILayoutOption();
    }
}

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MenuItem : Attribute
    {
        public MenuItem(string itemName) { }
    }
}
