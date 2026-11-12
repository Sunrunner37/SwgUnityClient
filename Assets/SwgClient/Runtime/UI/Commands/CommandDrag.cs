using UnityEngine;
using Swg.Client.Runtime.Commands;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Commands
{
    public sealed class CommandDrag : MonoBehaviour
    {
        public Canvas canvas;
        public Image ghost;
        public CommandCatalog.Command Command { get; private set; }

        public void Begin(CommandCatalog.Command command, Sprite icon, PointerEventData pointer)
        {
            Command = command;
            ghost.sprite = icon;
            ghost.gameObject.SetActive(true);
            ghost.transform.SetAsLastSibling();
            Move(pointer);
        }

        public void Move(PointerEventData pointer)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, pointer.position, pointer.pressEventCamera, out var position);
            ghost.rectTransform.localPosition = position;
        }

        public void End()
        {
            Command = null;
            ghost.gameObject.SetActive(false);
        }

        void OnDisable() => End();
    }
}
