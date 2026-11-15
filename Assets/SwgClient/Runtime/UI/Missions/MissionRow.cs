using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Swg.Client.Runtime.UI.Missions
{
    public sealed class MissionRow : MonoBehaviour, IPointerClickHandler
    {
        public TMP_Text creator;
        public TMP_Text title;
        public TMP_Text reward;
        public TMP_Text location;
        public Image background;
        public Button button;
        Action open;
        public void Bind(Action select, Action details) { button.onClick.AddListener(() => select()); open = details; }
        public void OnPointerClick(PointerEventData pointer) { if (pointer.button == PointerEventData.InputButton.Left && pointer.clickCount == 2) open?.Invoke(); }
    }
}
