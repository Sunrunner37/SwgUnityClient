using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Swg.Client.Runtime.UI.Skills
{
    public sealed class SkillBoxHover : MonoBehaviour, IPointerEnterHandler
    {
        public event Action Entered;
        public void OnPointerEnter(PointerEventData pointer) => Entered?.Invoke();
    }
}
