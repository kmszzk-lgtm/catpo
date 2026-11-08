using UnityEngine;

namespace Catpo.Game
{
    /// <summary>
    /// ▶(Play)를 누르면 어느 장면에서든 저절로 시작한다. 장면에 아무것도 놓지 않아도 된다.
    /// 첫 버전 3단계 시험판: 옥탑방 화면(HomeScreen)을 띄운다.
    /// </summary>
    public sealed class CatpoBoot : MonoBehaviour
    {
        private HomeScreen screen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("좀비냥");
            DontDestroyOnLoad(go);
            go.AddComponent<CatpoBoot>();
        }

        private void Awake()
        {
            screen = new HomeScreen();
        }

        private void Update()
        {
            screen.Update(Time.deltaTime);
        }

        private void OnGUI()
        {
            screen.OnGUI();
        }
    }
}
