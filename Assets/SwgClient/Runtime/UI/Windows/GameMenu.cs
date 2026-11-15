using UnityEngine;
using Swg.Client.Runtime.UI.Screens;
namespace Swg.Client.Runtime.UI.Windows
{
    public sealed class GameMenu : MonoBehaviour
    {
        public GameObject credits;
        public void ToggleOpen() { gameObject.SetActive(!gameObject.activeSelf); if (gameObject.activeSelf) transform.SetAsLastSibling(); }
        public void Resume() => gameObject.SetActive(false);
        public void ShowCredits() => credits.SetActive(!credits.activeSelf);
        public void Disconnect() { GameServices.Disconnect(); ScreenChanger.Open(ScreenId.Login); }
        public void ExitGame()
        {
            GameServices.Disconnect();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
